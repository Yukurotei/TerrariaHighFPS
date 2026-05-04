using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;

namespace HighFPSLogic
{
    public static class FPSManager
    {
        private static Vector2[] _origPlayerPos = new Vector2[256];
        private static Vector2[] _origNpcPos = new Vector2[201];
        private static Vector2[] _origProjPos = new Vector2[1001];

        public static void Initialize() {}
        public static bool ShouldUpdate(ref GameTime gameTime) { return true; }
        public static Vector2 GetInterpolatedPosition(Vector2 pos) { return pos; }

        // Called at the start of Main.Update(), before DoUpdate fires.
        // On partial ticks DoUpdate won't run, so we push fresh hardware mouse
        // coords through the same pipeline DoUpdate would use, letting SetZoom_UI
        // (called later in DoDraw) transform them correctly into cursor position.
        public static void UpdateMouse()
        {
            if ((int)Main.FrameSkipMode != 0) return;

            // IsPartialTick: accumulator hasn't crossed a full 60Hz tick boundary yet.
            // On full ticks DoUpdate runs and overwrites with the same hardware value anyway.
            double accumulator = Main.UpdateTimeAccumulator;
            bool isPartialTick = accumulator < (1.0 / 60.0);
            if (!isPartialTick) return;

            MouseState state = Mouse.GetState();
            MouseState old = PlayerInput.MouseInfo;
            // Mirror exactly what DoUpdate's MouseInput() does: apply RawMouseScale
            PlayerInput.MouseX = (int)(state.X * PlayerInput.RawMouseScale.X);
            PlayerInput.MouseY = (int)(state.Y * PlayerInput.RawMouseScale.Y);
            // Preserve button state from the last real update
            PlayerInput.MouseInfo = new MouseState(
                state.X, state.Y,
                old.ScrollWheelValue,
                old.LeftButton, old.MiddleButton, old.RightButton,
                old.XButton1, old.XButton2);
            PlayerInput.UpdateMainMouse();
            PlayerInput.CacheMousePositionForZoom();
        }

        private static int _prevSkipMode = -1;
        private static bool _skipNextFrame = false;
        private static bool _didSave = false;

        public static void PreDraw()
        {
            _didSave = false;

            int curMode = (int)Main.FrameSkipMode;
            if (curMode != _prevSkipMode)
            {
                _prevSkipMode = curMode;
                if (curMode == 0) _skipNextFrame = true;
            }

            if (curMode != 0) return;
            if (Main.gameMenu) return;

            if (_skipNextFrame) { _skipNextFrame = false; return; }

            double acc = Main.UpdateTimeAccumulator;
            float interpolationFactor = Math.Max(-1.0f, Math.Min(0.0f, (float)(acc * 60.0) - 1.0f));

            if (Main.player != null)
            {
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active)
                    {
                        _origPlayerPos[i] = Main.player[i].position;
                        Main.player[i].position += Main.player[i].velocity * interpolationFactor;
                    }
                }
            }

            if (Main.npc != null)
            {
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i] != null && Main.npc[i].active)
                    {
                        _origNpcPos[i] = Main.npc[i].position;
                        Main.npc[i].position += Main.npc[i].velocity * interpolationFactor;
                    }
                }
            }

            if (Main.projectile != null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i] != null && Main.projectile[i].active)
                    {
                        _origProjPos[i] = Main.projectile[i].position;
                        Main.projectile[i].position += Main.projectile[i].velocity * interpolationFactor;
                    }
                }
            }

            _didSave = true;
        }

        public static void PostDraw()
        {
            if (!_didSave) return;
            if ((int)Main.FrameSkipMode != 0 || Main.gameMenu) return;

            if (Main.player != null)
            {
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active)
                    {
                        Main.player[i].position = _origPlayerPos[i];
                    }
                }
            }

            if (Main.npc != null)
            {
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i] != null && Main.npc[i].active)
                    {
                        Main.npc[i].position = _origNpcPos[i];
                    }
                }
            }

            if (Main.projectile != null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i] != null && Main.projectile[i].active)
                    {
                        Main.projectile[i].position = _origProjPos[i];
                    }
                }
            }
        }
    }
}
