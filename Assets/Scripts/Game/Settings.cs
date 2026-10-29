using UnityEngine;
using UnityEngine.InputSystem;

namespace PortBoss
{
    // Player settings, kept in PlayerPrefs. GameUI.ApplySettings() pushes them into the engine.
    public static class Settings
    {
        static float F(string k, float d) => PlayerPrefs.GetFloat(k, d);
        static int I(string k, int d) => PlayerPrefs.GetInt(k, d);
        static bool B(string k, bool d) => PlayerPrefs.GetInt(k, d ? 1 : 0) == 1;
        static void S(string k, float v) => PlayerPrefs.SetFloat(k, v);
        static void S(string k, int v) => PlayerPrefs.SetInt(k, v);
        static void S(string k, bool v) => PlayerPrefs.SetInt(k, v ? 1 : 0);

        // ---- graphics ----
        public static readonly string[] PresetNames = { "Low", "Medium", "High", "Ultra", "Custom" };
        public static int Preset { get => I("gfx.preset", AutoPreset()); set => S("gfx.preset", value); }
        public static float RenderScale { get => F("gfx.scale", 1f); set => S("gfx.scale", value); }
        public static int Shadows { get => I("gfx.shadows", 2); set => S("gfx.shadows", value); }        // 0 off, 1 low, 2 high
        public static float ShadowDistance { get => F("gfx.shadowDist", 110f); set => S("gfx.shadowDist", value); }
        public static int AntiAliasing { get => I("gfx.aa", 2); set => S("gfx.aa", value); }           // 0 off, 1 SMAA, 2 MSAA 4x
        public static bool Ssao { get => B("gfx.ssao", true); set => S("gfx.ssao", value); }
        public static bool PostFx { get => B("gfx.post", true); set => S("gfx.post", value); }
        public static bool WaterHigh { get => B("gfx.water2", false); set => S("gfx.water2", value); } // false = classic cp2 sea (Manan's pick), true = stylised shader (beta)
        public static float ViewDistance { get => F("gfx.view", 1200f); set => S("gfx.view", value); }
        public static bool VSync { get => B("gfx.vsync", true); set => S("gfx.vsync", value); }
        public static readonly int[] FpsCaps = { 30, 60, 120, 144, 0 };
        public static int FpsCap { get => I("gfx.fps", 3); set => S("gfx.fps", value); }              // index into FpsCaps (the last one, 0, = unlimited)
        public static float Fov { get => F("gfx.fov", 62f); set => S("gfx.fov", value); }

        // ---- display ----
        public static float Brightness { get => F("disp.bright", 0f); set => S("disp.bright", value); }  // -1..1

        // ---- audio ----
        public static float Master { get => F("aud.master", 1f); set => S("aud.master", value); }
        public static float Music { get => F("music", 0.6f); set => S("music", value); }
        public static float Sfx { get => F("sfx", 0.9f); set => S("sfx", value); }
        public static float Ambience { get => F("aud.amb", 0.8f); set => S("aud.amb", value); }
        public static bool MuteUnfocused { get => B("aud.muteBg", false); set => S("aud.muteBg", value); }

        // ---- controls ----
        public static float Sensitivity { get => F("sens", 1f); set => S("sens", value); }
        public static bool InvertY { get => B("invertY", false); set => S("invertY", value); }
        public static float PadSensitivity { get => F("padSens", 1f); set => S("padSens", value); }
        public static readonly (string id, string label, Key def)[] Actions =
        {
            ("grab", "Grab / stack / sell", Key.E), ("drop", "Drop", Key.Q), ("throw", "Throw (also left click)", Key.R),
            ("sprint", "Sprint", Key.LeftShift), ("jump", "Jump", Key.Space), ("push", "Push trolley / shove", Key.F),
        };
        public static Key KeyFor(string id)
        {
            foreach (var a in Actions) if (a.id == id) return (Key)I("key." + id, (int)a.def);
            return Key.None;
        }
        public static void Bind(string id, Key k) => S("key." + id, (int)k);

        // ---- gameplay ----
        public static bool CameraShake { get => B("play.shake", true); set => S("play.shake", value); }
        public static float ShoutSize { get => F("play.shout", 1f); set => S("play.shout", value); }
        public static bool Hud { get => B("play.hud", true); set => S("play.hud", value); }
        public static bool ControlsHint { get => B("play.hint", true); set => S("play.hint", value); }

        // Defaults from the PC: a decent GPU gets High.
        public static int AutoPreset()
        {
            int vram = SystemInfo.graphicsMemorySize;
            return vram >= 3000 ? 2 : vram >= 1500 ? 1 : 0; // High by default on any real GPU (Kufu)
        }

        public static void ApplyPreset(int p)
        {
            Preset = p;
            switch (p)
            {
                case 0: RenderScale = 0.75f; Shadows = 1; ShadowDistance = 45; AntiAliasing = 0; Ssao = false; ViewDistance = 600; break;
                case 1: RenderScale = 0.9f; Shadows = 2; ShadowDistance = 70; AntiAliasing = 1; Ssao = false; ViewDistance = 900; break;
                case 2: RenderScale = 1f; Shadows = 2; ShadowDistance = 110; AntiAliasing = 2; Ssao = true; ViewDistance = 1200; break;
                case 3: RenderScale = 1.25f; Shadows = 2; ShadowDistance = 150; AntiAliasing = 2; Ssao = true; ViewDistance = 1500; break;
            }
            PostFx = true;
        }

        public static void ResetAll()
        {
            // DeleteAll would also wipe the all-time career stats (Stats, "pb.stat.*"): keep those.
            var stats = System.Array.ConvertAll(Stats.Rows, r => Stats.AllTime(r.key));
            PlayerPrefs.DeleteAll();
            for (int i = 0; i < stats.Length; i++) if (stats[i] != 0) PlayerPrefs.SetInt("pb.stat." + Stats.Rows[i].key, stats[i]);
            ApplyPreset(AutoPreset());
            PlayerPrefs.Save();
        }
    }
}
