using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;

namespace HighFPSLogic
{
    public static class FPSManager
    {
        private static Vector2[] _origPlayerPos = new Vector2[256];
        private static Vector2[] _origNpcPos = new Vector2[201];
        private static Vector2[] _origProjPos = new Vector2[1001];

        // Mouse interpolation — DoUpdate-space coordinates only, no raw SDL pixels.
        private static int _prevMouseX, _prevMouseY;
        private static int _currMouseX, _currMouseY;
        private static bool _mouseInitialized;

        public static void Initialize() {}
        public static bool ShouldUpdate(ref GameTime gameTime) { return true; }
        public static Vector2 GetInterpolatedPosition(Vector2 pos) { return pos; }

        // Called at the end of every DoUpdate to capture mouse position in game-space.
        public static void PostUpdate()
        {
            if (!_mouseInitialized)
            {
                _prevMouseX = _currMouseX = Main.mouseX;
                _prevMouseY = _currMouseY = Main.mouseY;
                _mouseInitialized = true;
            }
            else
            {
                _prevMouseX = _currMouseX;
                _prevMouseY = _currMouseY;
                _currMouseX = Main.mouseX;
                _currMouseY = Main.mouseY;
            }
        }

        // Called right before DrawInterface_36_Cursor. Forward-extrapolates mouse position
        // from the last two DoUpdate samples so the cursor tracks smoothly at render rate.
        public static void UpdateMouse()
        {
            if (!_mouseInitialized || (int)Main.FrameSkipMode != 0) return;
            float f = Math.Min((float)(Main.UpdateTimeAccumulator * 60.0), 1.0f);
            int vx = _currMouseX - _prevMouseX;
            int vy = _currMouseY - _prevMouseY;
            Main.mouseX = _currMouseX + (int)(vx * f);
            Main.mouseY = _currMouseY + (int)(vy * f);
        }

        public static void PreDraw()
        {
            if ((int)Main.FrameSkipMode != 0) return;

            if (Main.gameMenu) return;

            float interpolationFactor = (float)(Main.UpdateTimeAccumulator * 60.0) - 1.0f;

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
        }

        public static void PostDraw()
        {
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
