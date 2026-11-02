using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PortBoss.M1
{
    // M3: the world clock and everything the light does through the day (gdd systems §1.3, §4.4). One clock for the
    // world (Minutes since Day 1 00:00; the host's in co-op, CoopDay.cs), the 6 lighting states of the art direction
    // blended 30 in-game minutes across each boundary (Day.cs), and the weather's look on top (§1.5). Every frame the
    // director sets the sun or the moon, the sky's colours and clouds, the fog, the sea's colours, the toon shade tint,
    // the post grade and the lamps. Nothing else keeps its own time.
    [DefaultExecutionOrder(-150)]
    public class Sky : MonoBehaviour
    {
        public static Sky I;
        public static double Minutes = DayTime.Start;       // the world clock (in-game minutes)
        public static int Seed = 1;
        public static Weather W = new(1);
        public static int ForceWx = -1;                     // debug/tests: hold one weather (-1 = the forecast's)
        public static System.Action<double, double> Jumped; // (from, to): a sleep skip

        public static double Hour => DayTime.OfDay(Minutes) / 60.0;
        public static int Day => DayTime.Day(Minutes);
        public static bool Authority => !Coop.On || Coop.IsHost;

        // ---- the looks (systems §4.4's table; every number a knob)
        public struct Look
        {
            public Color Sun, Shade, Top, Mid, Hor, AmbS, AmbE, AmbG, Shallow, Deep, Fog, CloudLit, CloudShade, Refl;
            public float SunI, FogStart, FogEnd, Bloom, Exposure, Sat, Temp, Vignette;

            public static Look Lerp(in Look a, in Look b, float k)
            {
                Color C(Color x, Color y) => Color.Lerp(x, y, k); float F(float x, float y) => Mathf.Lerp(x, y, k);
                return new Look
                {
                    Sun = C(a.Sun, b.Sun), Shade = C(a.Shade, b.Shade), Top = C(a.Top, b.Top), Mid = C(a.Mid, b.Mid), Hor = C(a.Hor, b.Hor),
                    AmbS = C(a.AmbS, b.AmbS), AmbE = C(a.AmbE, b.AmbE), AmbG = C(a.AmbG, b.AmbG), Shallow = C(a.Shallow, b.Shallow), Deep = C(a.Deep, b.Deep),
                    Fog = C(a.Fog, b.Fog), CloudLit = C(a.CloudLit, b.CloudLit), CloudShade = C(a.CloudShade, b.CloudShade), Refl = C(a.Refl, b.Refl),
                    SunI = F(a.SunI, b.SunI), FogStart = F(a.FogStart, b.FogStart), FogEnd = F(a.FogEnd, b.FogEnd), Bloom = F(a.Bloom, b.Bloom),
                    Exposure = F(a.Exposure, b.Exposure), Sat = F(a.Sat, b.Sat), Temp = F(a.Temp, b.Temp), Vignette = F(a.Vignette, b.Vignette),
                };
            }
        }

        static Color H(string h) => Ponyo.Hex(h);
        // Dawn, Day, Golden, Dusk, Night, Storm. Sun = the sun's colour/intensity (the moon's is apart: night has no sun).
        public static readonly Look[] Looks =
        {
            new Look { Sun = H("FFC9A8"), SunI = 0.9f, Shade = H("A79BE0"), Top = H("8D7FC4"), Mid = H("C7A9CF"), Hor = H("FFE1B0"),
                AmbS = H("A79BE0"), AmbE = H("E2C9C6"), AmbG = H("6E6488"), Shallow = H("6CD3C9"), Deep = H("3D69B8"), Fog = H("FAD9C2"),
                FogStart = 200, FogEnd = 1500, CloudLit = H("FFD6B4"), CloudShade = H("A79BE0"), Refl = H("FFEFE6"), Bloom = 0.5f, Exposure = 0.12f, Sat = 12, Temp = 10, Vignette = 0.16f },
            new Look { Sun = H("FFF3DC"), SunI = 1.6f, Shade = H("98A6EA"), Top = H("3A86E0"), Mid = H("7DB4EC"), Hor = H("D6E8F2"),
                AmbS = H("98A6EA"), AmbE = H("C4C2D2"), AmbG = H("6C6A80"), Shallow = H("46D6C8"), Deep = H("2160B8"), Fog = H("DCE6EE"),
                FogStart = 260, FogEnd = 1900, CloudLit = H("FFF8EC"), CloudShade = H("98A6EA"), Refl = H("F4F8FF"), Bloom = 0.45f, Exposure = 0.1f, Sat = 14, Temp = 6, Vignette = 0.16f },
            new Look { Sun = H("FFC77A"), SunI = 1.8f, Shade = H("8F8FD6"), Top = H("5DA0EA"), Mid = H("A9C4E4"), Hor = H("F7CF7A"),
                AmbS = H("8F8FD6"), AmbE = H("DCC6B4"), AmbG = H("6C6478"), Shallow = H("4FCDB4"), Deep = H("23609E"), Fog = H("F2DDB8"),
                FogStart = 230, FogEnd = 1700, CloudLit = H("FFE2B0"), CloudShade = H("8F8FD6"), Refl = H("FFF2DA"), Bloom = 0.55f, Exposure = 0.1f, Sat = 16, Temp = 14, Vignette = 0.17f },
            new Look { Sun = H("FFB08A"), SunI = 0f, Shade = H("6E6AA8"), Top = H("5B5E9E"), Mid = H("9886B6"), Hor = H("F6A27A"),
                AmbS = H("7C78B8"), AmbE = H("B897A6"), AmbG = H("4A4468"), Shallow = H("4C9FB2"), Deep = H("27407F"), Fog = H("E3AE9C"),
                FogStart = 160, FogEnd = 1300, CloudLit = H("F7B596"), CloudShade = H("6E6AA8"), Refl = H("F2D2D0"), Bloom = 0.6f, Exposure = 0.12f, Sat = 10, Temp = 8, Vignette = 0.2f },
            // night: never black, Ponyo nights are blue (moon light comes apart, below)
            new Look { Sun = H("9FB4FF"), SunI = 0f, Shade = H("3A4580"), Top = H("121B36"), Mid = H("1C2A52"), Hor = H("2E3B6C"),
                AmbS = H("5263A8"), AmbE = H("3E4C88"), AmbG = H("252C55"), Shallow = H("2F6A93"), Deep = H("102656"), Fog = H("2B3766"),
                FogStart = 90, FogEnd = 650, CloudLit = H("56679C"), CloudShade = H("2B3766"), Refl = H("9AA8D8"), Bloom = 0.7f, Exposure = 0.2f, Sat = 4, Temp = -4, Vignette = 0.24f },
            // storm (the weather's state): sun hidden, slate
            new Look { Sun = H("C9D2E0"), SunI = 0.25f, Shade = H("6B7390"), Top = H("4A5470"), Mid = H("717B94"), Hor = H("A9B3C4"),
                AmbS = H("7C8AA3"), AmbE = H("8E98AA"), AmbG = H("4E5468"), Shallow = H("3E7A80"), Deep = H("1E3E58"), Fog = H("9AA4B4"),
                FogStart = 30, FogEnd = 400, CloudLit = H("BCC3D0"), CloudShade = H("5F6782"), Refl = H("C4CCD8"), Bloom = 0.3f, Exposure = 0.05f, Sat = -25, Temp = -6, Vignette = 0.22f },
        };
        static readonly Color MoonCol = H("9FB4FF"); const float MoonI = 0.35f;

        // ---- the weather's look (systems §1.5.1): waves, clouds, fog, rain, how much it greys and dims the light
        public struct WxLook
        {
            public float Amp, Len, Steep, Cover, Bank, FogEnd, FogW, Rain, Dim, Grey, Storm;
            public static WxLook Lerp(in WxLook a, in WxLook b, float k) => new()
            {
                Amp = Mathf.Lerp(a.Amp, b.Amp, k), Len = Mathf.Lerp(a.Len, b.Len, k), Steep = Mathf.Lerp(a.Steep, b.Steep, k), Cover = Mathf.Lerp(a.Cover, b.Cover, k),
                Bank = Mathf.Lerp(a.Bank, b.Bank, k), FogEnd = Mathf.Lerp(a.FogEnd, b.FogEnd, k), FogW = Mathf.Lerp(a.FogW, b.FogW, k), Rain = Mathf.Lerp(a.Rain, b.Rain, k),
                Dim = Mathf.Lerp(a.Dim, b.Dim, k), Grey = Mathf.Lerp(a.Grey, b.Grey, k), Storm = Mathf.Lerp(a.Storm, b.Storm, k),
            };
        }
        public static readonly WxLook[] WxLooks =
        {
            new WxLook { Amp = 0.14f, Len = 11, Steep = 0.33f, Cover = 0.12f, Bank = 0.85f },                                   // Clear
            new WxLook { Amp = 0.17f, Len = 11, Steep = 0.35f, Cover = 0.28f, Bank = 1f },                                      // Fair (M1's sea)
            new WxLook { Amp = 0.28f, Len = 14, Steep = 0.5f, Cover = 0.42f, Bank = 0.8f },                                     // Breezy
            new WxLook { Amp = 0.12f, Len = 11, Steep = 0.3f, Cover = 0.5f, Bank = 0.15f, FogEnd = 240, FogW = 1, Dim = 0.55f, Grey = 0.35f }, // Fog
            new WxLook { Amp = 0.26f, Len = 13, Steep = 0.45f, Cover = 0.85f, Bank = 0.3f, FogEnd = 600, FogW = 0.8f, Rain = 1, Dim = 0.6f, Grey = 0.55f }, // Rain
            new WxLook { Amp = 0.5f, Len = 19, Steep = 0.55f, Cover = 0.95f, Bank = 0f, FogEnd = 400, FogW = 1, Rain = 1.6f, Dim = 0.85f, Grey = 0.2f, Storm = 1 }, // Storm
        };

        // ---- state for tests and the UI (systems §2.7: the UI follows the light)
        public LightState State;            // the dominant lighting state now (Storm when the storm outweighs the time)
        public float NightW, StormW, LampW;
        public WxLook Wx_;
        bool snap = true;
        Light moonDummy;
        readonly List<(Light l, Transform glow, float range)> lamps = new();
        readonly List<Renderer> windows = new();
        ParticleSystem rain; float flashT = -1, nextFlash = 6;
        int lastBlockKey = -1; double lastCallCheck;

        public static Sky Create()
        {
            var g = new GameObject("Sky"); I = g.AddComponent<Sky>();
            var args = System.Environment.GetCommandLineArgs();
            string Arg(string k) { int i = System.Array.IndexOf(args, k); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            float F(string k, float d) => float.TryParse(Arg(k), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : d;
            // a new game: Day 1, 07:00; a world seed of its own (tests: -pb-seed n). -pb-hour h / -pb-day n start elsewhere,
            // -pb-weather <state> holds one weather.
            Seed = Arg("-pb-seed") != null ? (int)F("-pb-seed", 1) : System.Environment.TickCount & 0x7fffffff;
            W = new Weather(Seed);
            Minutes = DayTime.At((int)F("-pb-day", 1), F("-pb-hour", 7) * 60);
            var fw = Arg("-pb-weather"); if (fw != null && System.Enum.TryParse<Wx>(fw, true, out var wx)) ForceWx = (int)wx;
            I.Lamps(); I.Rain(); I.lastCallCheck = Minutes;
            Debug.Log($"[PB] day: start Day {Day} {DayTime.Clock(Minutes)}, seed {Seed}{(ForceWx >= 0 ? ", weather held " + (Wx)ForceWx : "")}");
            return I;
        }

        // A sleep skip (or a test): every system reads the clock, so they all move with it. The look snaps (no blend).
        public static void Jump(double to)
        {
            double from = Minutes; Minutes = to;
            if (I != null) { I.snap = true; I.lastBlockKey = -1; I.lastCallCheck = to; }
            Jumped?.Invoke(from, to);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Minutes += dt;                                  // 1 real second = 1 in-game minute (scaled time: pauses solo, -pb-timescale)
            // debug: F10 +1 hour (Shift: +6), F11 the next weather (held; F11 past Storm lets the forecast back)
            if (Authority && Ctl.Key_(UnityEngine.InputSystem.Key.F10)) { Minutes += Ctl.Sprint ? 360 : 60; snap = true; }
            if (Authority && Ctl.Key_(UnityEngine.InputSystem.Key.F11)) { ForceWx = ForceWx >= 5 ? -1 : ForceWx + 1; Alerts.Push("Debug", ForceWx < 0 ? "Weather: the forecast's again" : "Weather held: " + (Wx)ForceWx, Alerts.Kind.Weather); }
            Apply(dt);
            Calls();
        }

        // ---- the look
        public (Wx from, Wx to, float k) WeatherNow => ForceWx >= 0 ? ((Wx)ForceWx, (Wx)ForceWx, 1f) : W.At(Minutes);

        void Apply(float dt)
        {
            var (a, b, k) = DayTime.TimeState(Minutes);
            var L = Look.Lerp(Looks[(int)a], Looks[(int)b], k);
            float W_(LightState s) => (a == s ? 1 - k : 0) + (b == s ? k : 0);
            NightW = W_(LightState.Night); float dusk = W_(LightState.Dusk), dawn = W_(LightState.Dawn);

            // the weather: blend its look in (a forced change, or a jump, eases over ~2 s; the clock's own blends are in Day.cs)
            var (wa, wb, wk) = WeatherNow;
            var target = WxLook.Lerp(WxLooks[(int)wa], WxLooks[(int)wb], wk);
            Wx_ = snap ? target : WxLook.Lerp(Wx_, target, 1 - Mathf.Exp(-dt * 1.2f));
            var x = Wx_;
            StormW = x.Storm;

            // storm: slate over whatever the hour is, darker at night
            if (x.Storm > 0.001f)
            {
                var st = Looks[(int)LightState.Storm];
                var night = Look.Lerp(st, L, Mathf.Clamp01(NightW + dusk * 0.5f) * 0.6f);
                L = Look.Lerp(L, night, x.Storm);
            }
            // rain and fog: greyer and dimmer, the fog closes in (its colour from the sky's own horizon, so a foggy night is blue)
            if (x.Grey > 0.001f)
            {
                Color grey(Color c) { float l = c.grayscale; return Color.Lerp(c, new Color(l * 0.95f, l * 0.97f, l * 1.08f), x.Grey); }
                L.Top = grey(L.Top); L.Mid = grey(L.Mid); L.Hor = grey(L.Hor); L.Shallow = grey(L.Shallow); L.CloudLit = grey(L.CloudLit); L.Sat -= 12 * x.Grey;
            }
            if (x.FogW > 0.001f)
            {
                var fc = Color.Lerp(Color.Lerp(L.Fog, L.Hor, 0.5f), Color.white, 0.3f * (1 - NightW));
                L.Fog = Color.Lerp(L.Fog, fc, x.FogW);
                L.FogEnd = Mathf.Lerp(L.FogEnd, Mathf.Min(L.FogEnd, x.FogEnd), x.FogW);
                L.FogStart = Mathf.Lerp(L.FogStart, Mathf.Min(L.FogStart, x.FogEnd * 0.05f), x.FogW);
                L.Hor = Color.Lerp(L.Hor, L.Fog, 0.6f * x.FogW); L.Mid = Color.Lerp(L.Mid, L.Fog, 0.35f * x.FogW);
            }
            State = x.Storm > 0.5f ? LightState.Storm : k < 0.5f ? a : b;

            // the sun (and after dusk the moon): the sun rises 06:00 in the east-ish, highest at 13:00, sets 20:00; M1's
            // approved late-afternoon sun (30° up, yaw -125°) is where the path is at ~17:00
            double h = Hour, el = DayTime.SunElevation(Minutes);
            float sunYaw = -125f - (float)(h - 17) * 15f;
            float sunUp = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-2f, 5f, (float)el));
            float sunI = L.SunI * sunUp * (1 - x.Dim);
            float moonI = MoonI * NightW * (1 - x.Dim * 0.8f);
            float hn = (float)((h + 24 - 21) % 24);            // hours since 21:00
            var sun = Ponyo.Sun; var sunRot = Quaternion.Euler(Mathf.Max((float)el, -12f), sunYaw, 0);
            bool moon = sunI <= 0.0001f && moonI > sunI;
            sun.transform.rotation = moon ? Quaternion.Euler(28f + 8f * Mathf.Sin(hn * 0.35f), 35f + hn * 11f, 0) : sunRot;
            sun.color = moon ? MoonCol : L.Sun;
            sun.intensity = moon ? moonI : sunI;
            sun.shadowStrength = moon ? 0.55f : Mathf.Lerp(0.85f, 0.35f, x.Dim);
            sun.enabled = sun.intensity > 0.002f;

            // lightning (storm, the limiter: a flash at most every 6 s)
            float flash = 0;
            if (x.Storm > 0.7f && !Application.isBatchMode)
            {
                nextFlash -= dt;
                if (nextFlash <= 0) { flashT = 0; nextFlash = Random.Range(6f, 16f); }
            }
            if (flashT >= 0) { flashT += dt; flash = flashT < 0.08f ? 1 : flashT < 0.16f ? 0.2f : flashT < 0.24f ? 0.7f : 0; if (flashT > 0.3f) flashT = -1; }

            // ambient, fog
            Color amb(Color c) => c * (1 + flash * 1.5f);
            RenderSettings.ambientSkyColor = amb(L.AmbS); RenderSettings.ambientEquatorColor = amb(L.AmbE); RenderSettings.ambientGroundColor = amb(L.AmbG);
            Shader.SetGlobalColor("_LDAmbSky", amb(L.AmbS)); Shader.SetGlobalColor("_LDAmbEq", amb(L.AmbE)); Shader.SetGlobalColor("_LDAmbGround", amb(L.AmbG));
            RenderSettings.fogColor = L.Fog; RenderSettings.fogStartDistance = L.FogStart; RenderSettings.fogEndDistance = L.FogEnd;

            // the sky: gradient, the sun's (or the moon's) disc and glow, the clouds, the stars
            Shader.SetGlobalColor("_LDSkyTop", L.Top); Shader.SetGlobalColor("_LDSkyHorizon", L.Hor); Shader.SetGlobalColor("_LDSkyBottom", Color.Lerp(L.Hor, L.Deep, 0.25f));
            var mid = L.Mid; mid.a = 1; Shader.SetGlobalColor("_LDSkyMid", mid);
            var dir = moon ? -sun.transform.forward : -(sunRot * Vector3.forward);
            Shader.SetGlobalVector("_LDSunDir", new Vector4(dir.x, dir.y, dir.z, moon ? 60 : 10));
            Color disc = moon ? MoonCol * 0.9f : L.Sun * Mathf.Lerp(0.5f, 1f, sunUp); disc *= 1 - x.Dim * 0.9f;
            disc.a = moon ? 0.0012f : 0.0025f; Shader.SetGlobalColor("_LDSunSky", disc);
            Shader.SetGlobalColor("_LDShadeTint", new Color(L.Shade.r, L.Shade.g, L.Shade.b, 1));
            Shader.SetGlobalFloat("_LDStars", NightW * (1 - x.Cover * 0.9f) * (1 - x.FogW));
            var sm = Ponyo.SkyMat;
            if (sm != null)
            {
                sm.SetColor("_CloudColor", L.CloudLit * (1 + flash)); sm.SetColor("_CloudShade", L.CloudShade);
                sm.SetFloat("_CloudCover", x.Cover); sm.SetFloat("_CloudBank", x.Bank);
            }

            // the sea
            var w = Sea.Mat;
            if (w != null)
            {
                w.SetColor("_Shallow", L.Shallow); w.SetColor("_Deep", L.Deep); w.SetColor("_ReflTint", L.Refl);
                w.SetFloat("_SunSpec", Mathf.Lerp(1.1f, 0.25f, x.Dim));
            }
            Sea.Amp = x.Amp; Sea.Len = x.Len; Sea.Steep = x.Steep;

            // the grade
            if (Ponyo.Bloom != null)
            {
                Ponyo.Bloom.intensity.Override(L.Bloom); Ponyo.Grade.postExposure.Override(L.Exposure); Ponyo.Grade.saturation.Override(L.Sat);
                Ponyo.Balance.temperature.Override(L.Temp); Ponyo.Vig.intensity.Override(L.Vignette);
            }

            // the lamps: lit through dusk and night, and on a storm's dark day (systems §4.4: lamps on #FFB060)
            LampW = Mathf.Clamp01(Mathf.Max(Mathf.Clamp01((dusk - 0.3f) / 0.4f) + NightW, x.Storm * 0.8f + x.FogW * 0.4f) + (dawn > 0 && h < 6.3 ? dawn * (float)(6.3 - h) / 0.8f : 0));
            LampW = Mathf.Clamp01(LampW);
            foreach (var (l, glow, range) in lamps)
            {
                if (l == null) continue;
                l.intensity = 2.4f * LampW; l.enabled = LampW > 0.01f;
                if (glow != null) { glow.gameObject.SetActive(LampW > 0.01f); glow.localScale = Vector3.one * (0.55f + 0.1f * Mathf.Sin(Time.time * 7f)) * range; glow.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor", new Color(1f, 0.72f, 0.4f, 0.75f * LampW)); }
            }
            foreach (var r in windows) if (r != null) { r.enabled = LampW > 0.01f; r.sharedMaterial.SetColor("_BaseColor", new Color(1f, 0.74f, 0.42f, LampW)); }
            var cam = Camera.main;
            if (cam != null) foreach (var (_, glow, _) in lamps) if (glow != null) glow.rotation = Quaternion.LookRotation(glow.position - cam.transform.position);

            // rain round the camera
            if (rain != null)
            {
                var em = rain.emission; em.rateOverTime = 2600f * x.Rain;
                if (cam != null) rain.transform.position = cam.transform.position + cam.transform.forward * 6f + Vector3.up * 12f;
            }
            snap = false;
        }

        // ---- the weather's toasts: Dot calls a storm ≥ 3 h ahead (world §11.3), and a word when a block turns
        void Calls()
        {
            if (Title.Shown) return;
            var (day, b) = Weather.BlockAt(Minutes);
            int key = day * 4 + b;
            if (lastBlockKey >= 0 && key != lastBlockKey && ForceWx < 0)
            {
                var now = W.Block(day, b); var (pd, pb) = b == 0 ? (day - 1, 3) : (day, b - 1); var was = W.Block(pd, pb);
                if (now != was) Alerts.Push("Weather", Word(now), Alerts.Kind.Weather);
            }
            lastBlockKey = key;
            // storm calls whose time has come since the last check (any storm in the next day and a half)
            for (int d = day; d <= day + 1; d++)
                for (int k = 0; k < 4; k++)
                    if (W.Block(d, k) == Wx.Storm)
                    {
                        double at = Weather.StormCall(d, k);
                        if (at > lastCallCheck && at <= Minutes)
                            Alerts.Push("Dot · Harbour", $"Storm coming {(k == 0 ? "tomorrow morning" : k == 1 ? "this afternoon" : k == 2 ? "this evening" : "tonight")}. Tie Biscuit up and stay off the water.", Alerts.Kind.Weather);
                    }
            lastCallCheck = Minutes;
        }

        public static string Word(Wx w) => w switch
        {
            Wx.Clear => "Clear skies. Not a cloud worth the name.",
            Wx.Fair => "Fair weather, big clouds over the hills.",
            Wx.Breezy => "Wind's picking up. Mind the chop.",
            Wx.Fog => "Fog's rolling in. Go slow out there.",
            Wx.Rain => "Rain. The deck gets slippery.",
            _ => "Storm! Get to shelter.",
        };

        // one line for the F3 overlay (and the logs)
        public static string DebugLine()
        {
            if (I == null) return "";
            var (a, b, k) = DayTime.TimeState(Minutes); var (wa, wb, wk) = I.WeatherNow; var (bd, bb) = Weather.BlockAt(Minutes);
            return $"Day {Day} {DayTime.DayName(Day)} {DayTime.Clock(Minutes)} · light {(a == b ? a.ToString() : $"{a}→{b} {k:F2}")} · weather {(wa == wb ? wa.ToString() : $"{wa}→{wb} {wk:F2}")}{(ForceWx >= 0 ? " (held, F11)" : "")} · {Weather.BlockName[bb]} · lamps {I.LampW:F2} · seed {Seed} (F10 +1 h)";
        }

        // ---- the lamps of Walt's Landing: the old lamp post at the pier end, Biscuit's stern lantern, the shack window.
        // Their spots are Blender coordinates (art-src/m1/build_boat_landing.py) mapped through two of the model's own
        // sockets, so they follow the model if it moves.
        void Lamps()
        {
            var l = Landing.I; var s = Skiff.I;
            if (l != null && l.Spawn != null && l.LadderTop != null)
            {
                var top = FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(-0.4f, -21.65f, 1.3f + 2.35f));
                Lamp(l.transform, top, 9f, 0.5f);
                // the shack's window: glass at Blender (-6.92, 12.28, F + 1.3), F from the ground in front of its step
                var shack = Util.Find(l.transform, "Shack");
                if (shack != null)
                {
                    var glass = FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(-6.92f, 12.245f, 0)); // (behind the cross bars, just in front of the glass)
                    var stepAt = FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(-5.38f, 11.97f, 0));
                    if (Physics.Raycast(new Vector3(stepAt.x, 60, stepAt.z), Vector3.down, out var hit, 120, ~0, QueryTriggerInteraction.Ignore))
                    {
                        float F = hit.point.y + 0.12f;
                        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "window glow";
                        q.transform.SetParent(l.transform, true);
                        var face = FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(-6.92f, 11.0f, 0)) - glass; face.y = 0;
                        q.transform.position = new Vector3(glass.x, F + 1.3f, glass.z);
                        q.transform.rotation = Quaternion.LookRotation(-face.normalized);
                        q.transform.localScale = new Vector3(0.9f, 0.68f, 1);
                        var r = q.GetComponent<Renderer>(); r.sharedMaterial = Util.Soft(new Color(1f, 0.74f, 0.42f, 1f), Texture2D.whiteTexture); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        windows.Add(r);
                        Lamp(l.transform, new Vector3(glass.x, F + 1.3f, glass.z) + face.normalized * 0.6f, 6f, 0f);
                    }
                }
            }
            if (s != null && s.Helm != null && s.AnchorPt != null)
            {
                var at = FromBlender(s.transform, s.Helm, new Vector3(-0.32f, 1.80f, 0.12f), s.AnchorPt, new Vector3(0f, -2.0f, 0.69f), new Vector3(-0.51f, 2.28f, 0.90f));
                Lamp(s.transform, at, 6f, 0.3f);
            }
        }

        void Lamp(Transform parent, Vector3 at, float range, float glowSize)
        {
            var g = new GameObject("lamp"); g.transform.SetParent(parent, true); g.transform.position = at;
            var li = g.AddComponent<Light>(); li.type = LightType.Point; li.color = H("FFB060"); li.range = range; li.intensity = 0; li.shadows = LightShadows.None;
            Transform glow = null;
            if (glowSize > 0)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "lamp glow";
                q.transform.SetParent(g.transform, false);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = Util.Soft(new Color(1f, 0.72f, 0.4f, 0.8f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                glow = q.transform;
            }
            lamps.Add((li, glow, glowSize));
        }

        // a point given in the Blender model's coordinates (x right, y back, z up) -> world, from two sockets whose Blender
        // coordinates are known (the FBX export flips axes; the two sockets tell which way, so nothing is assumed)
        public static Vector3 FromBlender(Transform root, Transform s1, Vector3 b1, Transform s2, Vector3 b2, Vector3 p)
        {
            var l1 = root.InverseTransformPoint(s1.position); var l2 = root.InverseTransformPoint(s2.position);
            float sx = Mathf.Sign((l2.x - l1.x) * (b2.x - b1.x)), sz = Mathf.Sign((l2.z - l1.z) * (b2.y - b1.y));
            var l = l1 + new Vector3(sx * (p.x - b1.x), p.z - b1.z, sz * (p.y - b1.y));
            return root.TransformPoint(l);
        }

        void Rain()
        {
            var g = new GameObject("rain"); rain = g.AddComponent<ParticleSystem>();
            var m = rain.main; m.loop = true; m.playOnAwake = true; m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 1.1f; m.startSpeed = 16f; m.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.04f); m.maxParticles = 6000; m.startColor = new Color(0.85f, 0.9f, 1f, 0.45f);
            var e = rain.emission; e.rateOverTime = 0;
            var sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(34, 34, 0.1f); sh.rotation = new Vector3(90, 0, 0); // (a flat 34 m square, emitting down)
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.Soft(Color.white); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rain.Play();
        }
    }
}
