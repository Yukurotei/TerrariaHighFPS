import tkinter as tk
from tkinter import messagebox, ttk, filedialog
import subprocess
import os
import shutil
import threading
import sys
import json
import platform

WORK_DIR = os.path.dirname(os.path.abspath(__file__))
PATCHER_EXE = os.path.join(WORK_DIR, "Patcher.exe")
CONFIG_FILE = os.path.join(WORK_DIR, "config.json")

IS_WINDOWS = platform.system() == "Windows"
IS_MAC = platform.system() == "Darwin"

class TerrariaLauncher:
    def __init__(self, root, steam_command=None):
        self.root = root
        self.steam_command = steam_command
        self.root.title("Terraria High FPS Launcher")
        self.root.geometry("400x400")
        self.root.resizable(False, False)
        
        self.game_dir = self.load_game_dir()

        self.setup_ui()
        self.check_status()

    def load_game_dir(self):
        if IS_WINDOWS:
            default_dirs = [r"C:\Program Files (x86)\Steam\steamapps\common\Terraria"]
        elif IS_MAC:
            resources = "Terraria.app/Contents/Resources"
            default_dirs = [
                os.path.join(os.path.expanduser("~/Library/Application Support/Steam/steamapps/common/Terraria"), resources),
            ]
        else:
            default_dirs = ["/data/SteamLibrary/steamapps/common/Terraria"]

        # Try loading from config
        if os.path.exists(CONFIG_FILE):
            try:
                with open(CONFIG_FILE, 'r') as f:
                    config = json.load(f)
                    saved_dir = config.get("game_dir", "")
                    if os.path.exists(os.path.join(saved_dir, "Terraria.exe")):
                        return saved_dir
            except:
                pass

        # Try defaults
        for default_dir in default_dirs:
            if os.path.exists(os.path.join(default_dir, "Terraria.exe")):
                self.save_game_dir(default_dir)
                return default_dir

        # Ask user
        return self.prompt_for_directory()

    def resolve_mac_dir(self, selected_dir):
        """If user selects the Terraria Steam folder or .app bundle, resolve to Resources."""
        candidates = [
            selected_dir,
            os.path.join(selected_dir, "Terraria.app/Contents/Resources"),
            os.path.join(selected_dir, "Contents/Resources"),
        ]
        for path in candidates:
            if os.path.exists(os.path.join(path, "Terraria.exe")):
                return path
        return selected_dir

    def prompt_for_directory(self):
        if IS_MAC:
            msg = "Could not automatically find Terraria. Please select your 'Terraria' folder inside Steam (usually ~/Library/Application Support/Steam/steamapps/common/Terraria)."
        else:
            msg = "Could not automatically find Terraria. Please select your 'Terraria' installation folder (where Terraria.exe is located)."
        messagebox.showinfo("Select Terraria Folder", msg)
        while True:
            selected_dir = filedialog.askdirectory(title="Select Terraria Directory")
            if not selected_dir:
                sys.exit(0)

            resolved = self.resolve_mac_dir(selected_dir) if IS_MAC else selected_dir
            if os.path.exists(os.path.join(resolved, "Terraria.exe")):
                self.save_game_dir(resolved)
                return resolved
            else:
                messagebox.showerror("Error", "Terraria.exe not found in that folder. Please try again.")

    def save_game_dir(self, path):
        try:
            with open(CONFIG_FILE, 'w') as f:
                json.dump({"game_dir": path}, f)
        except:
            pass

    def change_directory(self):
        new_dir = filedialog.askdirectory(title="Select Terraria Directory")
        if not new_dir:
            return
        resolved = self.resolve_mac_dir(new_dir) if IS_MAC else new_dir
        if os.path.exists(os.path.join(resolved, "Terraria.exe")):
            self.game_dir = resolved
            self.save_game_dir(resolved)
            self.check_status()
            messagebox.showinfo("Success", "Game directory updated successfully!")
        else:
            messagebox.showerror("Error", "Terraria.exe not found in that folder.")

    def setup_ui(self):
        style = ttk.Style()
        style.configure("TButton", padding=6, font=("Helvetica", 10))
        
        main_frame = ttk.Frame(self.root, padding="20")
        main_frame.pack(fill=tk.BOTH, expand=True)

        ttk.Label(main_frame, text="Terraria 1.4.5 High FPS", font=("Helvetica", 16, "bold")).pack(pady=5)
        
        self.status_label = ttk.Label(main_frame, text="Status: Unknown", font=("Helvetica", 10))
        self.status_label.pack(pady=5)

        self.btn_launch_highfps = ttk.Button(main_frame, text="Launch High FPS (True Smooth)", command=self.launch_highfps)
        self.btn_launch_highfps.pack(fill=tk.X, pady=5)

        self.btn_launch_vanilla = ttk.Button(main_frame, text="Launch Vanilla (60 FPS)", command=self.launch_vanilla)
        self.btn_launch_vanilla.pack(fill=tk.X, pady=5)

        self.btn_patch = ttk.Button(main_frame, text="Update/Create Patch", command=self.create_patch)
        self.btn_patch.pack(fill=tk.X, pady=5)

        ttk.Separator(main_frame, orient='horizontal').pack(fill=tk.X, pady=5)
        
        self.btn_change_dir = ttk.Button(main_frame, text="Change Game Directory", command=self.change_directory)
        self.btn_change_dir.pack(fill=tk.X, pady=5)

        if self.steam_command:
            ttk.Label(main_frame, text="Mode: Steam Integration Active", foreground="blue").pack(pady=5)

        ttk.Label(main_frame, text="Note: Turn 'Frame Skip' OFF in game settings.", font=("Helvetica", 8, "italic")).pack(side=tk.BOTTOM, pady=5)

    def check_status(self):
        vanilla_bak = os.path.join(self.game_dir, "Terraria.exe.vanilla")
        if os.path.exists(vanilla_bak):
            self.status_label.config(text="Status: Ready (Backups exist)", foreground="green")
        else:
            self.status_label.config(text="Status: Initial Run (Will backup on first launch)", foreground="orange")

    def create_patch(self):
        def run():
            try:
                self.status_label.config(text="Status: Patching...", foreground="blue")
                target_exe = os.path.join(self.game_dir, "Terraria.exe")
                vanilla_bak = os.path.join(self.game_dir, "Terraria.exe.vanilla")
                
                # Always patch from vanilla to avoid double-patching
                if os.path.exists(vanilla_bak):
                    source_exe = vanilla_bak
                else:
                    source_exe = target_exe
                    # Make a backup if it doesn't exist
                    shutil.copy2(target_exe, vanilla_bak)

                output_exe = os.path.join(self.game_dir, "Terraria.exe.highfps")
                logic_dll_src = os.path.join(WORK_DIR, "HighFPSLogic.dll")
                logic_dll_dst = os.path.join(self.game_dir, "HighFPSLogic.dll")
                
                shutil.copy2(logic_dll_src, logic_dll_dst)
                
                cmd = [PATCHER_EXE, source_exe, output_exe, logic_dll_dst]
                if not IS_WINDOWS:
                    cmd.insert(0, "mono")
                    
                result = subprocess.run(cmd, capture_output=True, text=True)
                
                if "Success!" in result.stdout:
                    messagebox.showinfo("Success", "High FPS patch created successfully!\n(Uses Native Accumulator Logic)")
                    self.status_label.config(text="Status: Patch Updated", foreground="green")
                else:
                    messagebox.showerror("Error", f"Patching failed:\n{result.stdout}\n{result.stderr}")
                    self.status_label.config(text="Status: Patch Failed", foreground="red")
            except Exception as e:
                messagebox.showerror("Error", str(e))

        threading.Thread(target=run).start()

    def launch_vanilla(self):
        self.launch(use_highfps=False)

    def launch_highfps(self):
        self.launch(use_highfps=True)

    def launch(self, use_highfps):
        exe_path = os.path.join(self.game_dir, "Terraria.exe")
        vanilla_bak = os.path.join(self.game_dir, "Terraria.exe.vanilla")
        highfps_exe = os.path.join(self.game_dir, "Terraria.exe.highfps")

        # 1. Backup if needed (extra safety)
        if not os.path.exists(vanilla_bak):
            shutil.copy2(exe_path, vanilla_bak)

        # 2. Prepare EXE
        if use_highfps:
            if not os.path.exists(highfps_exe):
                # Auto-patch if missing
                self.create_patch()
                return
            shutil.copy2(highfps_exe, exe_path)
            # Always sync the logic DLL so it matches the patched exe
            logic_dll_src = os.path.join(WORK_DIR, "HighFPSLogic.dll")
            logic_dll_dst = os.path.join(self.game_dir, "HighFPSLogic.dll")
            if os.path.exists(logic_dll_src):
                shutil.copy2(logic_dll_src, logic_dll_dst)
        else:
            shutil.copy2(vanilla_bak, exe_path)

        # 3. Launch
        self.status_label.config(text="Status: Game Running...", foreground="blue")
        try:
            if self.steam_command:
                # Steam Mode: Execute the command Steam gave us
                print("DEBUG steam_command:", self.steam_command)
                subprocess.Popen(self.steam_command, cwd=self.game_dir)
            else:
                # CLI Mode: Run the standard launcher
                if IS_WINDOWS:
                    subprocess.Popen([exe_path], cwd=self.game_dir)
                elif IS_MAC:
                    mac_launcher = os.path.normpath(os.path.join(self.game_dir, "..", "MacOS", "Terraria"))
                    subprocess.Popen([mac_launcher], cwd=self.game_dir)
                else:
                    subprocess.Popen([os.path.join(self.game_dir, "Terraria")], cwd=self.game_dir)
            
            # Close launcher after launching game to avoid confusion
            self.root.destroy()
        except Exception as e:
            messagebox.showerror("Launch Error", str(e))

if __name__ == "__main__":
    # If arguments are passed, we assume Steam is launching us
    steam_cmd = sys.argv[1:] if len(sys.argv) > 1 else None
    root = tk.Tk()
    app = TerrariaLauncher(root, steam_command=steam_cmd)
    root.mainloop()
