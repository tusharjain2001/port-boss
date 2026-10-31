using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M1 = boat + fishing (solo). The scene holds only this; everything else is built here from M1Assets.
    public class Game : MonoBehaviour
    {
        public static Material ToonMat;
        static readonly Dictionary<string, Material> fishMats = new();
        public static bool Paused;
        public static RenderTexture BatchTarget, UiTarget;   // batchmode: the camera renders here (1920x1080) so shots include the UI
        AudioSource sea; float gullT = 6;
        UnityEngine.UI.Text pauseT; RectTransform pause;

        public static Material FishMat(FishSpecies s)
        {
            if (s == null) return ToonMat;
            if (fishMats.TryGetValue(s.Id, out var m)) return m;
            m = new Material(M1Assets.I.Toon) { name = "fish " + s.Id };
            m.SetFloat("_PaintNoise", 0.12f); m.SetFloat("_Wet", 0.8f); m.SetFloat("_SwimFreq", 3f);
            m.SetColor("_RimColor", new Color(1, 1, 0.95f, 0.35f));
            if (s.Id == "anglerfish") { m.SetFloat("_GlowStrength", 2.5f); m.SetColor("_GlowColor", Ponyo.Hex("FFE89A")); }
            fishMats[s.Id] = m; return m;
        }

        void Awake()
        {
            // a test run always ends, even if setup below throws
            var args = System.Environment.GetCommandLineArgs(); int qi = System.Array.IndexOf(args, "-pb-quit");
            if (qi >= 0 && qi + 1 < args.Length && float.TryParse(args[qi + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qs)) StartCoroutine(Failsafe(qs + 5));
            Application.targetFrameRate = Application.isBatchMode ? -1 : 144;
            QualitySettings.vSyncCount = Application.isBatchMode ? 0 : 1;
            var a = M1Assets.I;
            if (a == null) { Debug.LogError("[PB] M1Assets missing (run M1Build)"); return; }
            FishDb.Load(a.FishCsv.text);
            Debug.Log($"[PB] M1 start: {FishDb.All.Count} fish species, rods {FishDb.Rods.Length}, baits {FishDb.Baits.Length}");
            UiKit.Init(a);
            Ponyo.Apply(a);
            ToonMat = Ponyo.Toon(a, Color.white);
            Sea.Create(Ponyo.SeaMaterial(a));
            var land = Landing.Create(a.Landing, ToonMat);
            Islands();
            Skiff.Create(a.Boat, ToonMat, land.Moor);
            var spawn = land.Spawn != null ? land.Spawn : land.transform;
            var p = Angler.Create(a, spawn.position + Vector3.up * 0.05f, Skiff.I != null ? Quaternion.LookRotation(Skiff.I.transform.position - spawn.position).eulerAngles.y : 0);
            var rig = CameraRig.Create(p.transform);
            rig.Yaw = p.transform.eulerAngles.y + 25;
            if (Application.isBatchMode)
            {
                BatchTarget = new RenderTexture(1920, 1080, 24) { antiAliasing = 4, name = "batch" };
                rig.Cam.targetTexture = BatchTarget;
                // the UI camera renders on its own into a transparent texture; shots composite it over the scene
                var ui = rig.UiCam; var md = rig.Cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); md.cameraStack.Remove(ui);
                var ud = ui.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
                ui.clearFlags = CameraClearFlags.SolidColor; ui.backgroundColor = new Color(0, 0, 0, 0);
                UiTarget = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "batchUi" }; ui.targetTexture = UiTarget;
            }
            Hud.Create(rig.UiCam, p);
            BakeIcons(a);

            sea = gameObject.AddComponent<AudioSource>(); sea.clip = a.Waves; sea.loop = true; sea.volume = 0.32f; sea.spatialBlend = 0; sea.Play();
            if (!Application.isBatchMode) Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            BuildPause();

            Alerts.Push("Grandpa Walt", "Biscuit's yours now, kiddo. She leaks a bit, but she floats. The fish are out past the pier.", Alerts.Kind.Story);
            Alerts.Push("How to fish", "1 rod · hold LMB to cast, release · strike on the bite · Space stops the bar", Alerts.Kind.Info);
            Alerts.Push("Biscuit", "E at the tiller to take her out. E at the bow for the anchor.", Alerts.Kind.Info);
            Debug.Log($"[PB] hotbar self-test: {Hotbar.SelfTest()}");
            if (gameObject.GetComponent<Bot>() == null) gameObject.AddComponent<Bot>();
        }

        System.Collections.IEnumerator Failsafe(float s) { yield return new WaitForSecondsRealtime(s); Debug.Log("[PB] quit (failsafe)"); Application.Quit(); }

        void BakeIcons(M1Assets a)
        {
            foreach (var s in FishDb.All)
            {
                var pf = a.FishPrefab(s.Id);
                if (pf != null) Hud.Icons[s.Id] = IconBaker.Bake(pf, 192, 96, FishMat(s));
            }
            // a chunky stand-in rod (3.5x thicker) so the icon reads like the painted fish icons
            var rod = RodView.Make(ToonMat, 1, 6f);
            Hud.RodIcon = IconBaker.Bake(rod.gameObject, 160, 160, null, new Vector3(0.7f, -0.25f, 0.68f), 0.52f);
            Destroy(rod.gameObject);
        }

        // Distant islands on the horizon (code-made, fogged) and a sandy seabed that slopes away from the shore, so the
        // sea's depth colour fades from turquoise shallows to deep blue instead of stepping at the shore's underwater edge.
        void Islands()
        {
            var rnd = new System.Random(11);
            var spots = new[] { (new Vector3(-460, 0, -700), 150f, 30f), (new Vector3(420, 0, -820), 210f, 44f), (new Vector3(900, 0, -260), 110f, 22f), (new Vector3(-950, 0, 120), 170f, 36f) };
            foreach (var (at, r, h) in spots)
            {
                var g = Util.MeshObj("island", Mound(rnd, r, h), ToonMat, null); g.transform.position = at + Vector3.down * 1.5f;
                g.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360, 0);
                g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var bed = Util.MeshObj("seabed", Seabed(), ToonMat, null);
            bed.transform.position = Landing.I != null ? new Vector3(Landing.I.transform.position.x, 0, Landing.I.transform.position.z + 30) : Vector3.zero;
            bed.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static float Hash(float x, float y) { float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f; return h - Mathf.Floor(h); }

        static Mesh Mound(System.Random rnd, float r, float h)
        {
            int rings = 14, seg = 40; float ph = (float)rnd.NextDouble() * 10;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= rings; i++)
                for (int j = 0; j < seg; j++)
                {
                    float u = i / (float)rings, a = j * Mathf.PI * 2 / seg;
                    float wob = 1 + 0.22f * Mathf.Sin(a * 2 + ph) + 0.12f * Mathf.Sin(a * 5 + ph * 2) + 0.05f * Hash(i, j);
                    float rr = r * (1 - u * 0.93f) * wob;
                    float y = u > 0 ? h * Mathf.Pow(Mathf.Clamp01(u * 1.15f), 0.55f) * (0.85f + 0.3f * Mathf.Sin(a * 3 + ph * 1.7f)) : -3f;
                    v.Add(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                }
            for (int i = 0; i < rings; i++) for (int j = 0; j < seg; j++)
                {
                    int a0 = i * seg + j, a1 = i * seg + (j + 1) % seg, b0 = a0 + seg, b1 = a1 + seg;
                    t.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
            Color sand = Ponyo.Hex("EAD7A8"), rock = Ponyo.Hex("9A93A6"), rock2 = Ponyo.Hex("8A84A0"), grass = Ponyo.Hex("8CC46A"), grass2 = Ponyo.Hex("7DBA60");
            var fv = new List<Vector3>(); var fc = new List<Color>(); var ft = new List<int>();
            for (int k = 0; k < t.Count; k += 3)
            {
                var p0 = v[t[k]]; var p1 = v[t[k + 1]]; var p2 = v[t[k + 2]];
                float y = (p0.y + p1.y + p2.y) / 3 / h, n = Hash(p0.x, p0.z);
                var n3 = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                Color c = y < 0.06f ? sand : (y > 0.45f && Mathf.Abs(n3.y) > 0.55f) ? (n > 0.5f ? grass : grass2) : (n > 0.5f ? rock : rock2);
                for (int q = 0; q < 3; q++) { fv.Add(v[t[k + q]]); fc.Add(c); ft.Add(k + q); }
            }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(fv); m.SetColors(fc); m.SetTriangles(ft, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // A wide shallow bowl: -3 m by the shore down to -28 m 260 m out (sand colour, seen through the shallows).
        static Mesh Seabed()
        {
            int rings = 24, seg = 48; var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (int i = 0; i <= rings; i++)
                for (int j = 0; j < seg; j++)
                {
                    float u = i / (float)rings, r = 4 + 600 * u * u, a = j * Mathf.PI * 2 / seg, k = Mathf.Clamp01(r / 260f);
                    float y = -3.2f - 25f * k * k * (3 - 2 * k) - (r > 260 ? (r - 260) * 0.02f : 0);
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    c.Add(Color.Lerp(Ponyo.Hex("D8C69A"), Ponyo.Hex("7FA6A0"), u));
                }
            for (int i = 0; i < rings; i++) for (int j = 0; j < seg; j++)
                {
                    int a0 = i * seg + j, a1 = i * seg + (j + 1) % seg, b0 = a0 + seg, b1 = a1 + seg;
                    t.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                }
            var m = new Mesh(); m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void BuildPause()
        {
            var hud = Hud.I.transform;
            pause = UiKit.Card(hud, "pause", 26, Ponyo.Paper, true);
            pause.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));
            var t = UiKit.Txt(pause, "Paused", UiKit.Display, 49, Ponyo.Ink, TextAnchor.UpperCenter); t.rectTransform.Stretch(30);
            pauseT = UiKit.Txt(pause, "Esc  resume\nQ  quit to desktop\n\nF3 debug overlay · F5 rod tier · F6 bait", UiKit.Body, 22, Ponyo.Ink, TextAnchor.MiddleCenter); pauseT.rectTransform.Stretch(30); pauseT.rectTransform.offsetMax = new Vector2(-30, -60);
            pause.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Ctl.Escape && !Ctl.Bot) { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; pause.gameObject.SetActive(Paused); Cursor.lockState = Paused ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = Paused; }
            if (Paused && Ctl.Key_(UnityEngine.InputSystem.Key.Q)) Application.Quit();
            if (!Paused && !Application.isBatchMode && Cursor.lockState != CursorLockMode.Locked && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            // gulls now and then
            gullT -= Time.deltaTime;
            if (gullT <= 0 && M1Assets.I.Gull != null) { gullT = Random.Range(9f, 22f); var c = Camera.main; if (c != null) Snd.Play(M1Assets.I.Gull, c.transform.position + Random.onUnitSphere * 40 + Vector3.up * 20, Random.Range(0.25f, 0.45f), Random.Range(0.9f, 1.15f), 0.9f); }
        }
    }
}
