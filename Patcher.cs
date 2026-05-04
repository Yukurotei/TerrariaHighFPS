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

                // Load our helper DLL
                AssemblyDefinition logicAssembly = AssemblyDefinition.ReadAssembly(logicDllPath);
                TypeDefinition fpsManagerType = logicAssembly.MainModule.Types.First(t => t.Name == "FPSManager");

                // Import methods
                MethodReference preDrawMethod    = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "PreDraw"));
                MethodReference postDrawMethod   = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "PostDraw"));
                MethodReference updateMouseMethod = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "UpdateMouse"));
                MethodReference postUpdateMethod  = module.ImportReference(fpsManagerType.Methods.First(m => m.Name == "PostUpdate"));

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

                // Patch DrawInterface_36_Cursor: forward-extrapolate mouse position right
                // before the cursor sprite is drawn so it tracks at render framerate.
                Console.WriteLine("Patching Main.DrawInterface_36_Cursor...");
                MethodDefinition cursorMethod = mainType.Methods.First(m => m.Name == "DrawInterface_36_Cursor");
                {
                    var il = cursorMethod.Body.GetILProcessor();
                    il.InsertBefore(cursorMethod.Body.Instructions[0], il.Create(OpCodes.Call, updateMouseMethod));
                }

                // Patch DoUpdate: capture mouse position after each 60Hz tick so
                // UpdateMouse can extrapolate it smoothly between frames.
                Console.WriteLine("Patching Main.DoUpdate...");
                MethodDefinition updateMethod = mainType.Methods.First(m => m.Name == "DoUpdate" && m.Parameters.Count == 1);
                {
                    var il = updateMethod.Body.GetILProcessor();
                    foreach (var instr in updateMethod.Body.Instructions.ToList()) {
                        if (instr.OpCode == OpCodes.Ret)
                            il.InsertBefore(instr, il.Create(OpCodes.Call, postUpdateMethod));
                    }
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
