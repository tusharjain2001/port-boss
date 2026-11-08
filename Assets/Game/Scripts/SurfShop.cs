using UnityEngine;

namespace PortBoss.M1
{
    // M8 step 4: Swell Surf Co. (economy §4.2), Kai Rennick's shop: the sun-yellow house with the boards out front at
    // the Cut (map B's landmark, anchor_surf). A counter at its door (Firm's market counter, his sun-yellow awning), Kai
    // behind it (Works' keeper look 8: sun-bleached curls, an open shirt, board shorts). Open 9 AM to 6 PM; shut in a
    // storm ("Gnarly. Shop's shut. Go home."). The shelf: Surf.cs (Act 2; his first foamie free).
    // The surf at Breaker Point: the open sea's swell (Weather.Sea) stood up on the reef: face height ~ 1.6 x Hs, in feet;
    // the wind says how clean (glassy under 8 kn, clean under 15, choppy under 22, blown out above); a storm closes it out.
    // Kai texts a report at dawn (06:00) on mornings with surf, from Act 2 once he's said hello (world §10.3: "no surf,
    // no text"). The Forecast app shows the same line. (The waves themselves: step 5.)
    public class SurfShop : MonoBehaviour
    {
        public static SurfShop I;
        public static Firm.Stall Stall;
        public const double Open = 9 * 60, Close = 18 * 60, Dawn = 6 * 60;
        static void Log(string s) => Debug.Log("[PB] surf: " + s);

        public static void Create()
        {
            var g = new GameObject("SurfShop"); I = g.AddComponent<SurfShop>();
            var a = Firm.Anchor("surf");
            if (a == null || Firm.I == null) { Log("no anchor_surf: no Swell Surf Co. counter"); return; }
            Stall = new Firm.Stall { K = Firm.Kind.Surf, Id = "surf", Name = "Swell Surf Co.", Owner = Company.KaiName, At = a, Real = true, Open = Open, Close = Close,
                Line = "Waves don't care how much money you've got. Neither do I, mostly.", Shut = Storm };
            Firm.I.AddStall(Stall, true);
            Jetty(a.position);
        }

        // Kai's jetty (art-src/m8/build_surf.py -> PB_Bld_surfjetty.fbx): from the beach out over the Cut's silted shallows.
        // Its origin is the shore point; it runs 38 m out (+Z), the berth alongside 24.5-29 m out. Placed by a look round from
        // the shop's door: every 5 degrees, where the ground first drops under 0.3 m (the shore), then the water at the berth;
        // the deepest berth wins, a nearer shore breaks a tie. Registers the visiting berth (F moors) and the name on the arch.
        public static Transform JettyRoot, Berth;
        static void Jetty(Vector3 door)
        {
            var pf = M1Assets.I != null ? M1Assets.I.M5Prefab("Bld_surfjetty") : null;
            if (pf == null) { Log("no jetty model (Art/M5/PB_Bld_surfjetty.fbx)"); return; }
            float best = -1; Vector3 bo = default, bd = default;
            for (int a = 0; a < 360; a += 5)
            {
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward; float r0 = -1;
                for (float r = 4; r < 140; r += 1) { var q = door + d * r; if (-Seabed.Depth(q.x, q.z) < 0.3f) { r0 = r; break; } }
                if (r0 < 0) continue;
                var o = door + d * r0; var side = Vector3.Cross(Vector3.up, d) * 2.65f;   // (the berth is on the jetty's right)
                float min = 99; bool clear = true;
                for (float y = 8; y <= 38 && clear; y += 2) { var q = o + d * y; if (-Seabed.Depth(q.x, q.z) > 0.1f) clear = false; }   // (no land again under it; a gentle shelf is fine)
                foreach (var y in new[] { 24.5f, 27f, 29f }) { var q = o + d * y + side; min = Mathf.Min(min, Seabed.Depth(q.x, q.z)); }
                if (!clear) continue;
                float score = min - 0.002f * r0;
                if (score > best) { best = score; bo = o; bd = d; }
            }
            if (best < 0) { Log("no water off the shop for a jetty"); return; }
            var g = Object.Instantiate(pf); g.name = "Swell Surf Co. jetty"; JettyRoot = g.transform;
            JettyRoot.SetPositionAndRotation(new Vector3(bo.x, Sea.Level, bo.z), Quaternion.LookRotation(bd));
            Util.Colliders(g); Util.Toon(g, Game.ToonMat);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Transform S(string n) => Util.Find(JettyRoot, n);
            var b = S("socket_berth"); var bf = S("socket_berth_fwd");
            if (b != null && bf != null)
            {
                Berth = new GameObject("berth_surf").transform; Berth.SetParent(JettyRoot, true);
                var f = bf.position - b.position; f.y = 0;
                Berth.SetPositionAndRotation(new Vector3(b.position.x, Sea.Level, b.position.z), Quaternion.LookRotation(f.normalized));
                Skiff.Visits.Add((Berth, "Swell Surf Co."));
            }
            var sg = S("socket_sign");
            if (sg != null) { var t = new GameObject("name").transform; t.SetParent(JettyRoot, true); t.SetPositionAndRotation(sg.position, Quaternion.LookRotation(bd)); Firm.Sign(t, "Swell Surf Co.", Vector3.zero, Ponyo.Hex("2E3F6E"), 0.026f); }
            Log($"the jetty from the shore at {bo.x:F1} {bo.z:F1}, heading {Mathf.Atan2(bd.x, bd.z) * Mathf.Rad2Deg:F0} deg, {Vector3.Distance(door, bo):F0} m from the door; the berth {best:F2} m deep");
        }

        public static bool Storm(double m) { if (Sky.W == null) return false; var (d, b) = Weather.BlockAt(m); return Sky.W.Block(d, b) == Wx.Storm; }

        // the surf at Breaker Point in a block: (feet, how it is, worth going)
        public static (int ft, string word, bool good) Report(int day, int block)
        {
            if (Sky.W == null) return (0, "", false);
            var wx = Sky.W.Block(day, block); var (kn, hs) = Sky.W.Sea(day, block, wx);
            int ft = Mathf.RoundToInt(hs * 1.6f * 3.28f);
            if (wx == Wx.Storm) return (ft, "closed out", false);
            string w = kn < 8 ? "glassy" : kn < 15 ? "clean" : kn < 22 ? "choppy" : "blown out";
            return (ft, w, ft >= 2 && kn < 22);
        }
        public static string Line(int day, int block)
        {
            var (ft, w, good) = Report(day, block);
            return ft < 2 ? "Flat. Kai's fishing." : $"{ft} ft at Breaker Point, {w}{(good ? "" : ". Kai's not going")}";
        }

        // Kai's dawn report: the host's, once a day, from the morning after he said hello
        void Update()
        {
            var c = Firm.C; if (c == null || !Sky.Authority || Sky.W == null || c.Act < 2 || !c.Fired("kai:hello")) return;
            double m = Sky.Minutes; int day = DayTime.Day(m); double t = DayTime.OfDay(m);
            if (DayTime.Day(c.BeatAt["kai:hello"]) >= day) return;                 // (from the morning after his hello)
            if (t < Dawn || t > Dawn + 120) return;
            string key = "kai:surf:" + day; if (c.Seen(key)) return;
            c.Event(key, m); Firm.I.Dirty = true;
            if (Storm(m)) { c.Text(Company.KaiName, "Gnarly. Shop's shut. Go home.", m); Log($"day {day}: storm"); return; }
            var (ft, w, good) = Report(day, 0);
            if (!good) { Log($"day {day}: no surf ({ft} ft {w}): no text"); return; }
            c.Text(Company.KaiName, ft >= 6 ? $"{ft} ft at Breaker. {w}. it's pumping. come" : $"{ft} ft at Breaker, {w}. come", m);
            Log($"day {day}: {ft} ft {w}");
        }
    }
}
