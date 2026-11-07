using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Old Saltmouth opens step by step (Tushar 7 Nov: a new player wandered the whole town not knowing what to look for).
    // Day 1 the town is roped off: from the port two lanes are open, up the lane to June's (Hook, Line & Sinker) and
    // along the Market Lane to Pratt's; every other way out has a "Closed for now" barrier. When Doyle's opens (you have
    // $90 for the engine rebuild, or Day 2) the ropes come down. The later shops stay shut at their doors until their
    // day: Plank & Post Day 2; the pub, Hartley's and the post office Act 2 or Day 5. All of it is worked out from the
    // company's shared state (bank, earnings, day, act), so every machine in a crew agrees without a message.
    public class Town : MonoBehaviour
    {
        public static Town I;
        public static bool Open { get; private set; }                   // the ropes are down
        public static bool Off;                                          // tests that walk the whole town at once

        // the ropes (world x, z), each a line of stakes; and the barriers across the lanes they cut, with their signs
        // (where, which way the sign reads from: the open side)
        static readonly Vector2[][] Ropes =
        {
            // the lane up to June's: its west side (the gap to the terrace, north of it to the plaza, round the plaza),
            // its east side (to June's front corner)
            new[] { new Vector2(46.3f, 34.2f), new Vector2(46.0f, 44.0f), new Vector2(46.8f, 52.0f), new Vector2(47.8f, 60.4f), new Vector2(46.5f, 67.9f), new Vector2(46.5f, 74.6f), new Vector2(58.6f, 74.6f) },   // (all along the terrace's fronts: a strip behind them led round to the waterfront)
            new[] { new Vector2(52.0f, 34.2f), new Vector2(54.6f, 50.0f), new Vector2(55.4f, 64.0f), new Vector2(58.6f, 67.6f) },
            // the Market Lane to Pratt's: both sides, then round Pratt's forecourt
            new[] { new Vector2(56.05f, -14.8f), new Vector2(140.0f, -14.8f), new Vector2(145.6f, -14.8f), new Vector2(145.6f, -12.4f), new Vector2(152.3f, -12.4f), new Vector2(152.3f, -28.0f) },   // (round the forecourt and across the foot of Pratt's steps: the hall up them was a way through; the counter stays in front)
            new[] { new Vector2(56.05f, -19.2f), new Vector2(140.0f, -19.2f), new Vector2(145.6f, -23.0f), new Vector2(145.6f, -28.0f), new Vector2(152.3f, -28.0f) },
        };
        static readonly (Vector2 at, Vector2 face, string what, float width)[] Barriers =
        {
            (new Vector2(56.4f, 19.0f), new Vector2(-1, 0), "The town", 4.4f),              // the port's town gate (read from the yard)
            (new Vector2(23.85f, -34.7f), new Vector2(0, 1), "Doyle Boatworks", 6.4f),     // the port's south opening, on the town quay's end (it runs in there now)
            (new Vector2(23.85f, 34.7f), new Vector2(0, -1), "The town", 6.4f),              // the port's north quay opening, on the town quay's end (Tushar 7 Nov take 2)
            (new Vector2(54.3f, 39.5f), new Vector2(-1, 0), "The town", 4.4f),              // the town gate's lane, where it meets June's lane
            (new Vector2(105.0f, -14.8f), new Vector2(0, -1), "The town", 4.4f),           // the lane north from the Market Lane
            (new Vector2(101.0f, -19.4f), new Vector2(0, 1), "The town", 4.4f),            // the lane south from it (down the house's west side)
            (new Vector2(148.0f, -12.4f), new Vector2(0, -1), "The high street", 4.4f),    // the street north from Pratt's
        };
        public readonly List<GameObject> Pieces = new();                 // ropes + barriers (all come down together)
        readonly Dictionary<string, GameObject> doorSigns = new();       // shop id -> its "closed" A-frame
        public static System.Action Opened;                              // (the board, the phone map)

        // (tests that walk the whole town on Day 1)
        public static void ForceOpen() { Off = true; }

        static Company C => Firm.C;
        static int Day => DayTime.Day(Sky.Minutes);
        // Doyle's opens and the ropes come down: $90 in the bank (the engine rebuild), or $50 made (the same, after June's
        // bait: it never goes back), or Day 2, or a later act
        public static bool DoyleOpen => Off || C == null || C.Cash >= 90 || C.Earned >= 50 || Day >= 2 || C.Act >= 2;
        // a shop that isn't open yet: why (null = open). Pratt's and June's are open from the start.
        public static string Lock(string id)
        {
            if (Off || C == null) return null;
            switch (id)
            {
                case "doyle": return DoyleOpen ? null : "opens when you have $90, or on Day 2";
                case "plank": return Day >= 2 || C.Act >= 2 ? null : "opens on Day 2";
                case "pub": case "hartleys": case "post": return Day >= 5 || C.Act >= 2 ? null : $"opens on Day 5";
                default: return null;
            }
        }

        public static void Create()
        {
            var g = new GameObject("Town"); I = g.AddComponent<Town>();
            Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pb-townopen") >= 0;
            Open = DoyleOpen;
            if (!Open) I.Build();
            I.DoorSigns(); Opened?.Invoke();
            Debug.Log($"[PB] town: {(Open ? "open" : $"roped off: {I.Pieces.Count} pieces")}, closed doors {I.doorSigns.Count}");
        }

        void Update()
        {
            if (!Open && DoyleOpen)
            {
                Open = true;
                foreach (var p in Pieces) if (p != null) { Fx.Dust(p.transform.position + Vector3.up * 0.4f, 3, 0.8f); Destroy(p); }
                Pieces.Clear();
                Alerts.Push("Old Saltmouth", "The ropes are down: the town's open. Doyle's will look at the engine.", Alerts.Kind.Story);
                Works.I?.RebuildNav(); Opened?.Invoke();
                Debug.Log($"[PB] town: the ropes come down (Day {Day}, bank ${C?.Cash}, earned ${C?.Earned}, act {C?.Act})");
            }
            // a closed door opens when its day comes
            foreach (var kv in new List<KeyValuePair<string, GameObject>>(doorSigns))
                if (kv.Value != null && Lock(kv.Key) == null) { Fx.Dust(kv.Value.transform.position + Vector3.up * 0.5f, 3, 0.8f); Destroy(kv.Value); doorSigns.Remove(kv.Key); Opened?.Invoke(); Debug.Log($"[PB] town: {kv.Key} opens"); }
        }

        // the ground under a point (never our own ropes and barriers)
        static Vector3 Ground(Vector2 p)
        {
            var hits = Physics.RaycastAll(new Vector3(p.x, 60, p.y), Vector3.down, 120, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits) if ((I == null || !h.transform.IsChildOf(I.transform)) && (Landing.I == null || !h.transform.IsChildOf(Landing.I.transform))) return h.point;   // (not the port's fence top either: a stake stood on it, the rope 1.3 m up)
            return new Vector3(p.x, 1.3f, p.y);
        }
        // nothing to stake in: the sea, or a building there already (the rope stops at its wall)
        static bool Blocked(Vector3 g)
        {
            if (g.y < Sea.Level + 0.2f) return true;
            foreach (var c in Physics.OverlapCapsule(g + Vector3.up * 0.5f, g + Vector3.up * 1.3f, 0.12f, ~0, QueryTriggerInteraction.Ignore))
                if ((I == null || !c.transform.IsChildOf(I.transform)) && (Landing.I == null || !c.transform.IsChildOf(Landing.I.transform))) return true;   // (the port's own fence: a rope ties on to it)
            return false;
        }

        void Build()
        {
            var root = new GameObject("Ropes").transform; root.SetParent(transform, false);
            Color stake = Ponyo.Hex("C9A882"), rope = Ponyo.Hex("D8C08A");
            Color[] flags = { Ponyo.Poppy, Ponyo.Sunflower, Ponyo.Hex("2FA79C"), Ponyo.Paper };
            foreach (var line in Ropes)
                for (int k = 0; k + 1 < line.Length; k++)
                {
                    var a = line[k]; var b = line[k + 1]; float L = Vector2.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(L / 2.4f));
                    for (int i = 0; i < n; i++)
                    {
                        var q0 = Vector2.Lerp(a, b, (float)i / n); var q1 = Vector2.Lerp(a, b, (float)(i + 1) / n);
                        var p0 = Ground(q0); var p1 = Ground(q1); bool b0 = Blocked(p0), b1 = Blocked(p1);
                        if (b0 && b1) continue;
                        // one end in a wall: the rope runs up to the wall (a skipped 2.4 m piece left a gap beside a house)
                        if (b0 || b1)
                        {
                            Vector2 free = b0 ? q1 : q0, wall = b0 ? q0 : q1;
                            for (int it = 0; it < 7; it++) { var m = (free + wall) / 2; if (Blocked(Ground(m))) wall = m; else free = m; }
                            if (Vector2.Distance(free, b0 ? q1 : q0) < 0.3f) continue;
                            free += (wall - free).normalized * 0.3f;                              // (a little into the wall: no sliver at a corner)
                            if (b0) p0 = Ground(free); else p1 = Ground(free);
                        }
                        var seg = new GameObject("rope").transform; seg.SetParent(root, false);
                        var d = p1 - p0; var flat = new Vector3(d.x, 0, d.z);
                        seg.SetPositionAndRotation(p0, Quaternion.LookRotation(flat.normalized));
                        float len = flat.magnitude, rise = d.y;
                        Firm.Box(seg, "stake", new Vector3(0, 0.45f, 0), new Vector3(0.07f, 0.95f, 0.07f), stake);
                        if (i == n - 1) Firm.Box(seg, "stake", new Vector3(0, 0.45f + rise, len), new Vector3(0.07f, 0.95f, 0.07f), stake);
                        var r = Firm.Box(seg, "line", new Vector3(0, 0.82f + rise / 2, len / 2), new Vector3(0.03f, 0.03f, Mathf.Sqrt(len * len + rise * rise)), rope);
                        r.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, len) * Mathf.Rad2Deg, 0, 0);
                        for (int f = 1; f * 0.6f < len - 0.2f; f++)                             // bunting under the rope
                            Firm.Box(seg, "flag", new Vector3(0, 0.72f + rise * (f * 0.6f / len), f * 0.6f), new Vector3(0.02f, 0.16f, 0.18f), flags[(f + k) % flags.Length], new Vector3(0, 0, 0));
                        var col = seg.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.7f + rise / 2, len / 2); col.size = new Vector3(0.24f, 1.4f + Mathf.Abs(rise), len);   // (down to the ground at both ends on a slope or a step)
                        Pieces.Add(seg.gameObject);
                    }
                }
            foreach (var (at, face, what, width) in Barriers) Pieces.Add(Barrier(root, at, face, what, "Closed for now", "opens when you have $90, or on Day 2", width));
        }

        // a red-and-white sawhorse across the way and an A-frame board in front of it, read from the open side
        GameObject Barrier(Transform root, Vector2 at, Vector2 face, string what, string head, string sub, float width = 4.4f)
        {
            var g = new GameObject("barrier " + what).transform; g.SetParent(root, false);
            g.SetPositionAndRotation(Ground(at), Quaternion.LookRotation(new Vector3(face.x, 0, face.y)));
            for (int s = -1; s <= 1; s += 2)
                for (int l = -1; l <= 1; l += 2) Firm.Box(g, "leg", new Vector3(s * (width / 2 - 0.3f), 0.48f, l * 0.18f), new Vector3(0.08f, 1.0f, 0.08f), Ponyo.Hex("805C52"), new Vector3(l * 12, 0, 0));
            for (int i = 0; i < 6; i++) Firm.Box(g, "stripe", new Vector3(-width / 2 + (i + 0.5f) * width / 6, 0.92f, 0), new Vector3(width / 6, 0.22f, 0.06f), i % 2 == 0 ? Ponyo.Poppy : Ponyo.Paper);
            var col = g.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.75f, 0); col.size = new Vector3(width, 1.5f, 0.5f);
            Sign(g, new Vector3(0, 0, 0.75f), what, head, sub);
            return g.gameObject;
        }

        // the A-frame: a cream board on two legs, the place in bold, then the words (TextMesh, as the shop signs)
        static void Sign(Transform parent, Vector3 p, string what, string head, string sub)
        {
            var s = new GameObject("sign").transform; s.SetParent(parent, false); s.localPosition = p;
            Firm.Box(s, "board", new Vector3(0, 1.05f, 0), new Vector3(1.5f, 0.9f, 0.05f), Ponyo.Paper, new Vector3(-8, 0, 0));
            Firm.Box(s, "rim", new Vector3(0, 1.05f, -0.01f), new Vector3(1.6f, 1.0f, 0.04f), Ponyo.Hex("805C52"), new Vector3(-8, 0, 0));
            for (int k = -1; k <= 1; k += 2) Firm.Box(s, "leg", new Vector3(k * 0.62f, 0.5f, -0.12f), new Vector3(0.06f, 1.1f, 0.06f), Ponyo.Hex("805C52"), new Vector3(-14, 0, 0));
            var t = new GameObject("text").transform; t.SetParent(s, false); t.localPosition = new Vector3(0, 1.05f, 0.04f); t.localRotation = Quaternion.Euler(-8, 0, 0);
            // (long words shrink to stay on the 1.5 m board: "Hartley's Outfitters", "opens when you have $90, or on Day 2")
            static float Fit(string s, float size, int chars) => Mathf.Min(size, size * chars / Mathf.Max(1, s.Length));
            Firm.Sign(t, what, new Vector3(0, 0.24f, 0), Ponyo.Hex("2A1C18"), Fit(what, 0.024f, 12));
            Firm.Sign(t, head, new Vector3(0, 0.0f, 0), Ponyo.Poppy, Fit(head, 0.021f, 14));
            Firm.Sign(t, sub, new Vector3(0, -0.24f, 0), Ponyo.Hex("5E4A44"), Fit(sub, 0.0125f, 28));
        }

        // the later shops: an A-frame on the step in front of the door until their day
        void DoorSigns()
        {
            foreach (var id in new[] { "doyle", "plank", "pub", "hartleys", "post" })
            {
                var why = Lock(id); var a = Firm.Anchor(id); if (why == null || a == null) continue;
                var name = System.Array.Find(Signs.Shops, s => s.Id == id).Name;
                var g = new GameObject("closed " + id).transform; g.SetParent(transform, false);
                var f = a.forward; f.y = 0;
                g.SetPositionAndRotation(a.position + f.normalized * 1.4f, Quaternion.LookRotation(f.normalized));
                Sign(g, Vector3.zero, name ?? id, "Closed for now", why);
                doorSigns[id] = g.gameObject;
            }
        }
    }
}
