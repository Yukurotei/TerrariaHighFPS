using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;

public class HighFPSPatcher
{
    public static void Main(string[] args)
    {
        if (args.Length < 3) {
            Console.WriteLine("Usage: HighFPSPatcher <input_exe> <output_exe> <logic_dll_path>");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        string logicDllPath = args[2];

        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(inputPath));

        ReaderParameters readerParams = new ReaderParameters {
            AssemblyResolver = resolver,
            ReadWrite = true
        };

        try {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(inputPath, readerParams))
            {
                ModuleDefinition module = assembly.MainModule;

                AssemblyDefinition logicAssembly = AssemblyDefinition.ReadAssembly(logicDllPath);
                TypeDefinition fpsManagerType = logicAssembly.MainModule.Types.First(t => t.Name == "FPSManager");

                MethodReference preDrawMethod    = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "PreDraw"));
                MethodReference postDrawMethod   = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "PostDraw"));
                MethodReference updateMouseMethod = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "UpdateMouse"));

                TypeDefinition mainType = module.Types.First(t => t.FullName == "Terraria.Main");

                // Patch DoDraw: PreDraw at start, PostDraw before every ret
                Console.WriteLine("Patching Main.DoDraw...");
                MethodDefinition drawMethod = mainType.Methods.First(m => m.Name == "DoDraw" && m.Parameters.Count == 1);
                {
                    var il = drawMethod.Body.GetILProcessor();
                    var first = drawMethod.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Call, preDrawMethod));

                    foreach (var instr in drawMethod.Body.Instructions.ToList()) {
                        if (instr.OpCode == OpCodes.Ret)
                            il.InsertBefore(instr, il.Create(OpCodes.Call, postDrawMethod));
                    }
                }

                // Patch Update: inject UpdateMouse() before DoUpdate call
                Console.WriteLine("Patching Main.Update...");
                MethodDefinition updateMethod = mainType.Methods.First(m => m.Name == "Update" && m.Parameters.Count == 1);
                {
                    var il = updateMethod.Body.GetILProcessor();
                    // Find the DoUpdate call instruction
                    var doUpdateCall = updateMethod.Body.Instructions
                        .First(i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
                                    && i.Operand.ToString().Contains("DoUpdate"));
                    // Walk back to the ldarg.0 that loads 'this' before DoUpdate
                    var insertPoint = doUpdateCall;
                    while (insertPoint.Previous != null && insertPoint.Previous.OpCode != OpCodes.Call && insertPoint.Previous.OpCode != OpCodes.Callvirt)
                        insertPoint = insertPoint.Previous;
                    il.InsertBefore(insertPoint, il.Create(OpCodes.Call, updateMouseMethod));
                }

                Console.WriteLine("Saving to " + outputPath);
                assembly.Write(outputPath);
            }
            Console.WriteLine("Success! (Native Accumulator Mode)");
        } catch (Exception e) {
            Console.WriteLine("Error during patching: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
}
