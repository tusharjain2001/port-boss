using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PortBoss
{
    // Settings page (home screen + pause menu): tabs, live apply, saved in PlayerPrefs.
    public partial class GameUI
    {
        static readonly string[] Tabs = { "Graphics", "Display", "Audio", "Controls", "Gameplay" };
        int tab;
        string rebinding; // action id waiting for a key
        Vector2 scroll;
        ColorAdjustments colorAdj;
        Material waterLow, waterHigh;

        // Push every setting into the engine. Cheap; called on start and whenever something changes.
        public void ApplySettings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = Settings.RenderScale;
                urp.shadowDistance = Settings.ShadowDistance;
                urp.mainLightShadowmapResolution = Settings.Shadows >= 2 ? 4096 : 1024;
                urp.msaaSampleCount = Settings.AntiAliasing == 2 ? 4 : 1;
                foreach (var rd in urp.rendererDataList)
                    if (rd != null)
                        foreach (var f in rd.rendererFeatures)
                            if (f != null && f.GetType().Name.Contains("AmbientOcclusion")) f.SetActive(Settings.Ssao);
            }
            if (sun != null) sun.shadows = Settings.Shadows == 0 ? LightShadows.None : LightShadows.Soft;
            if (cam != null)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null)
                {
                    data.renderPostProcessing = Settings.PostFx;
                    data.antialiasing = Settings.AntiAliasing == 1 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
                }
                cam.farClipPlane = Settings.ViewDistance;
                cam.fieldOfView = Settings.Fov;
            }
            fogScale = 1200f / Mathf.Max(300f, Settings.ViewDistance);
            if (!oldFog) RenderSettings.fogDensity = 0.0021f * fogScale;
            if (colorAdj != null) colorAdj.postExposure.Override(0.25f + Settings.Brightness);
            QualitySettings.vSyncCount = Settings.VSync ? 1 : 0;
            int cap = Settings.FpsCaps[Mathf.Clamp(Settings.FpsCap, 0, Settings.FpsCaps.Length - 1)];
            Application.targetFrameRate = cap > 0 ? cap : -1;
            AudioListener.volume = Settings.Master;
            ApplyWater();
        }
        float fogScale = 1f;

        void ApplyWater()
        {
            if (waterHigh == null) { waterHigh = Art.WaterMat; waterLow = new Material(LitMat); waterLow.SetColor("_BaseColor", new Color(0.13f, 0.3f, 0.36f)); waterLow.SetFloat("_Smoothness", 0.88f); }
            var m = Settings.WaterHigh ? waterHigh : waterLow;
            foreach (var r in World.WaterRenderers) if (r != null) r.sharedMaterial = m;
        }

        public void DebugOpenSettings(int t) { tab = t; settingsFromPause = false; screen = Screen.Settings; }

        void OnApplicationFocus(bool focus) => AudioListener.pause = !focus && Settings.MuteUnfocused;

        void SettingsKeyCapture()
        {
            if (rebinding == null) return;
            var k = Keyboard.current;
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) { rebinding = null; return; }
            foreach (var key in k.allKeys)
                if (key != null && key.wasPressedThisFrame) { Settings.Bind(rebinding, key.keyCode); rebinding = null; PlayerPrefs.Save(); return; }
        }

        void DrawSettings(float W, float H)
        {
            float pw = Mathf.Min(W - 80 * S, 980 * S), ph = Mathf.Min(H - 60 * S, 820 * S);
            var r = new Rect((W - pw) / 2, (H - ph) / 2, pw, ph);
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 14 * S, pw, 56 * S), "Settings", mid, new Color(1f, 0.85f, 0.55f));
            float tw = (pw - 60 * S) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                var tr = new Rect(r.x + 30 * S + i * tw, r.y + 74 * S, tw - 8 * S, 46 * S);
                bool on = tab == i;
                GUI.color = on ? Color.white : new Color(1, 1, 1, 0.55f);
                if (Button(tr, Tabs[i])) { tab = i; scroll = Vector2.zero; rebinding = null; }
                GUI.color = Color.white;
            }
            var body = new Rect(r.x + 30 * S, r.y + 136 * S, pw - 60 * S, ph - 230 * S);
            row = 0;
            scroll = GUI.BeginScrollView(body, scroll, new Rect(0, 0, body.width - 24 * S, RowsHeight()));
            rowW = body.width - 24 * S;
            bool changed = false;
            switch (tab)
            {
                case 0: changed = GraphicsTab(); break;
                case 1: changed = DisplayTab(); break;
                case 2: changed = AudioTab(); break;
                case 3: changed = ControlsTab(); break;
                case 4: changed = GameplayTab(); break;
            }
            GUI.EndScrollView();
            if (changed) { ApplySettings(); PlayerPrefs.Save(); }

            float by = r.yMax - 80 * S;
            if (Button(new Rect(r.x + 30 * S, by, 300 * S, 56 * S), "Reset to defaults")) { Settings.ResetAll(); ApplySettings(); }
            if (Button(new Rect(r.xMax - 230 * S, by, 200 * S, 56 * S), "Back"))
            {
                PlayerPrefs.Save(); rebinding = null;
                if (settingsFromPause) settingsFromPause = false; else screen = Screen.Menu;
            }
        }

        // ---- row widgets (laid out top-down inside the scroll view) ----
        int row; float rowW;
        float RowH => 54 * S;
        float RowsHeight() => RowH * 14;
        Rect NextRow() => new Rect(0, row++ * RowH, rowW, RowH - 6 * S);
        GUIStyle RowLabel => new GUIStyle(small) { fontSize = Mathf.RoundToInt(23 * S), alignment = TextAnchor.MiddleLeft };

        bool SliderRow(string name, ref float v, float min, float max, string fmt)
        {
            var r = NextRow();
            Label(new Rect(r.x, r.y, r.width * 0.45f, r.height), $"{name}   {string.Format(fmt, v)}", RowLabel, Color.white);
            float nv = GUI.HorizontalSlider(new Rect(r.x + r.width * 0.5f, r.y + r.height * 0.4f, r.width * 0.48f, 20 * S), v, min, max);
            if (Mathf.Abs(nv - v) < 1e-4f) return false;
            v = nv; return true;
        }

        bool CycleRow(string name, ref int v, string[] options)
        {
            var r = NextRow();
            Label(new Rect(r.x, r.y, r.width * 0.45f, r.height), name, RowLabel, Color.white);
            var br = new Rect(r.x + r.width * 0.5f, r.y + 2 * S, r.width * 0.48f, r.height - 4 * S);
            if (!Button(br, $"‹  {options[Mathf.Clamp(v, 0, options.Length - 1)]}  ›")) return false;
            v = (v + 1) % options.Length; return true;
        }

        bool ToggleRow(string name, ref bool v)
        {
            int i = v ? 1 : 0;
            if (!CycleRow(name, ref i, new[] { "Off", "On" })) return false;
            v = i == 1; return true;
        }

        bool GraphicsTab()
        {
            bool ch = false;
            int p = Settings.Preset;
            if (CycleRow("Quality preset", ref p, new[] { "Low", "Medium", "High", "Ultra" })) { Settings.ApplyPreset(p % 4); ch = true; }
            float f; int i; bool b;
            // Any individual change makes the preset "Custom".
            bool Custom(bool c) { if (c) Settings.Preset = 4; return c; }
            f = Settings.RenderScale; if (Custom(SliderRow("Render scale", ref f, 0.5f, 1.5f, "{0:P0}"))) { Settings.RenderScale = Mathf.Round(f * 20) / 20; ch = true; }
            i = Settings.Shadows; if (Custom(CycleRow("Shadows", ref i, new[] { "Off", "Low", "High" }))) { Settings.Shadows = i; ch = true; }
            f = Settings.ShadowDistance; if (Custom(SliderRow("Shadow distance", ref f, 20, 200, "{0:0} m"))) { Settings.ShadowDistance = Mathf.Round(f); ch = true; }
            i = Settings.AntiAliasing; if (Custom(CycleRow("Anti-aliasing", ref i, new[] { "Off", "SMAA", "MSAA 4x" }))) { Settings.AntiAliasing = i; ch = true; }
            b = Settings.Ssao; if (Custom(ToggleRow("Ambient occlusion (SSAO)", ref b))) { Settings.Ssao = b; ch = true; }
            b = Settings.PostFx; if (Custom(ToggleRow("Post-processing (bloom, grading)", ref b))) { Settings.PostFx = b; ch = true; }
            i = Settings.WaterHigh ? 1 : 0; if (Custom(CycleRow("Water style", ref i, new[] { "Classic", "Stylised (beta)" }))) { Settings.WaterHigh = i == 1; ch = true; }
            f = Settings.ViewDistance; if (Custom(SliderRow("View distance / fog", ref f, 300, 1500, "{0:0} m"))) { Settings.ViewDistance = Mathf.Round(f / 50) * 50; ch = true; }
            b = Settings.VSync; if (ToggleRow("VSync", ref b)) { Settings.VSync = b; ch = true; }
            i = Settings.FpsCap; if (CycleRow("FPS cap", ref i, new[] { "30", "60", "120", "144", "Unlimited" })) { Settings.FpsCap = i; ch = true; }
            f = Settings.Fov; if (SliderRow("Field of view", ref f, 50, 90, "{0:0}°")) { Settings.Fov = Mathf.Round(f); ch = true; }
            var pr = NextRow();
            Label(pr, $"Current preset: {Settings.PresetNames[Mathf.Clamp(Settings.Preset, 0, 4)]}", RowLabel, new Color(1, 1, 1, 0.55f));
            return ch;
        }

        bool DisplayTab()
        {
            bool ch = false;
            var res = UnityEngine.Screen.resolutions;
            var r = NextRow();
            Label(new Rect(r.x, r.y, r.width * 0.45f, r.height), "Resolution", RowLabel, Color.white);
            if (Button(new Rect(r.x + r.width * 0.5f, r.y + 2 * S, r.width * 0.48f, r.height - 4 * S), $"‹  {UnityEngine.Screen.width} x {UnityEngine.Screen.height}  ›")) NextResolution(res);
            int mode = UnityEngine.Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : UnityEngine.Screen.fullScreenMode == FullScreenMode.FullScreenWindow ? 1 : 2;
            if (CycleRow("Window mode", ref mode, new[] { "Windowed", "Borderless", "Fullscreen" }))
                UnityEngine.Screen.fullScreenMode = mode == 0 ? FullScreenMode.Windowed : mode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen;
            if (Display.displays.Length > 1)
            {
                var displays = new System.Collections.Generic.List<DisplayInfo>();
                UnityEngine.Screen.GetDisplayLayout(displays);
                int cur = displays.IndexOf(UnityEngine.Screen.mainWindowDisplayInfo);
                var names = displays.ConvertAll(d => d.name).ToArray();
                if (names.Length > 1 && CycleRow("Monitor", ref cur, names))
                    UnityEngine.Screen.MoveMainWindowTo(displays[cur], Vector2Int.zero);
            }
            float f = Settings.Brightness; if (SliderRow("Brightness", ref f, -1, 1, "{0:+0.0;-0.0;0}")) { Settings.Brightness = Mathf.Round(f * 10) / 10; ch = true; }
            return ch;
        }

        static void NextResolution(Resolution[] list)
        {
            if (list.Length == 0) return;
            int cur = System.Array.FindIndex(list, r => r.width == UnityEngine.Screen.width && r.height == UnityEngine.Screen.height);
            // Resolutions repeat per refresh rate: step to the next different size.
            for (int i = 1; i <= list.Length; i++)
            {
                var n = list[(cur + i + list.Length) % list.Length];
                if (n.width != UnityEngine.Screen.width || n.height != UnityEngine.Screen.height)
                { UnityEngine.Screen.SetResolution(n.width, n.height, UnityEngine.Screen.fullScreenMode); return; }
            }
        }

        bool AudioTab()
        {
            bool ch = false; float f; bool b;
            f = Settings.Master; if (SliderRow("Master", ref f, 0, 1, "{0:P0}")) { Settings.Master = f; ch = true; }
            f = Settings.Music; if (SliderRow("Music", ref f, 0, 1, "{0:P0}")) { Settings.Music = f; ch = true; }
            f = Settings.Sfx; if (SliderRow("Sound effects", ref f, 0, 1, "{0:P0}")) { Settings.Sfx = f; ch = true; }
            f = Settings.Ambience; if (SliderRow("Ambience (sea, gulls)", ref f, 0, 1, "{0:P0}")) { Settings.Ambience = f; ch = true; }
            b = Settings.MuteUnfocused; if (ToggleRow("Mute when the window is in the background", ref b)) { Settings.MuteUnfocused = b; ch = true; }
            if (Music.Current == "")
                Label(NextRow(), "(Only the night track exists so far; other music is being made.)", RowLabel, new Color(1, 1, 1, 0.5f));
            return ch;
        }

        bool ControlsTab()
        {
            bool ch = false; float f; bool b;
            f = Settings.Sensitivity; if (SliderRow("Mouse sensitivity", ref f, 0.2f, 3f, "{0:0.00}")) { Settings.Sensitivity = f; ch = true; }
            b = Settings.InvertY; if (ToggleRow("Invert Y", ref b)) { Settings.InvertY = b; ch = true; }
            if (Gamepad.current != null) { f = Settings.PadSensitivity; if (SliderRow("Controller sensitivity", ref f, 0.2f, 3f, "{0:0.00}")) { Settings.PadSensitivity = f; ch = true; } }
            foreach (var (id, label, _) in Settings.Actions)
            {
                var r = NextRow();
                Label(new Rect(r.x, r.y, r.width * 0.45f, r.height), label, RowLabel, Color.white);
                string txt = rebinding == id ? "Press a key… (Esc cancels)" : Settings.KeyFor(id).ToString();
                if (Button(new Rect(r.x + r.width * 0.5f, r.y + 2 * S, r.width * 0.48f, r.height - 4 * S), txt)) rebinding = id;
            }
            Label(NextRow(), "Mouse: right click grabs, left click throws. Pad: A jump, X grab, B drop, RT throw, LB sprint, RB push.", RowLabel, new Color(1, 1, 1, 0.5f));
            return ch;
        }

        bool GameplayTab()
        {
            bool ch = false; float f; bool b;
            b = Settings.CameraShake; if (ToggleRow("Camera shake", ref b)) { Settings.CameraShake = b; ch = true; }
            f = Settings.ShoutSize; if (SliderRow("Captain shout text size", ref f, 0.7f, 1.5f, "{0:P0}")) { Settings.ShoutSize = f; ch = true; big = null; }
            b = Settings.Hud; if (ToggleRow("HUD", ref b)) { Settings.Hud = b; ch = true; }
            b = Settings.ControlsHint; if (ToggleRow("Show controls hint", ref b)) { Settings.ControlsHint = b; showHelp = b; ch = true; }
            return ch;
        }
    }
}
