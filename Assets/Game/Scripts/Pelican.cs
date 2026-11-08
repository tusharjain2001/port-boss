using UnityEngine;

namespace PortBoss.M1
{
    // M7 step 3: The Pelican, Amara Bell's restaurant on Lantern Key (art-src/m7/build_pelican.py -> Art/M5/PB_Bld_pelican.fbx).
    // On the islet's east beach, its jetty pointing at Walt's Landing. The shore point came from one run of -pb-probe on
    // 8 Nov: the islet's middle (-1704, -52), top 21.7 m; toward the landing the shore is at (-1523.0, -46.1), a gentle
    // beach, the sea floor 1.3 m down 20 m out. Registers:
    // - the visiting berth along the jetty, bow out to sea (Skiff.Visits: F moors there);
    // - Amara's counter (a Firm stall, 10 AM to 10 PM; her figure is one of Works' keepers, in chef whites);
    // - "The Pelican" on the arch over the jetty's head (Firm.Sign, read from the sea).
    // Created after the company and before Works (so Works gives the counter its keeper: Game.cs).
    public static class Pelican
    {
        public static Transform Root, Berth, Counter;
        public static readonly Vector3 Shore = new(-1523.0f, 0, -46.1f), Out = new Vector3(1f, 0, 0.03f).normalized;

        public static void Create()
        {
            var pf = M1Assets.I != null ? M1Assets.I.M5Prefab("Bld_pelican") : null;
            if (pf == null) { Debug.Log("[PB] pelican: no model (Art/M5/PB_Bld_pelican.fbx)"); return; }
            var g = Object.Instantiate(pf); g.name = "The Pelican"; Root = g.transform;
            Root.SetPositionAndRotation(new Vector3(Shore.x, Sea.Level, Shore.z), Quaternion.LookRotation(Out));
            Util.Colliders(g); Util.Toon(g, Game.ToonMat);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Transform S(string n) => Util.Find(Root, n);
            Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Out; }
            // the berth: alongside the jetty, bow out to sea
            var b = S("socket_berth"); var bf = S("socket_berth_fwd");
            if (b != null && bf != null)
            {
                Berth = new GameObject("berth_pelican").transform; Berth.SetParent(Root, true);
                Berth.SetPositionAndRotation(new Vector3(b.position.x, Sea.Level, b.position.z), Quaternion.LookRotation(Flat(bf.position - b.position)));
                Skiff.Visits.Add((Berth, "The Pelican"));
            }
            // Amara's counter: the stall faces the customer's spot
            var c = S("socket_counter_mid"); var cu = S("socket_counter");
            if (c != null && cu != null && Firm.I != null)
            {
                Counter = new GameObject("anchor_pelican").transform; Counter.SetParent(Root, true);
                Counter.SetPositionAndRotation(c.position, Quaternion.LookRotation(Flat(cu.position - c.position)));
                Firm.I.AddStall(new Firm.Stall { K = Firm.Kind.Pelican, Id = "pelican", Name = "The Pelican", Owner = Company.AmaraName, At = Counter, Real = true, Open = 600, Close = 1320,
                    Line = "Good fish only. I can smell the bad ones from here." });
            }
            // the name on the arch, facing the sea
            var sg = S("socket_sign");
            if (sg != null)
            {
                var t = new GameObject("name").transform; t.SetParent(Root, true); t.SetPositionAndRotation(sg.position, Quaternion.LookRotation(Out));
                Firm.Sign(t, "The Pelican", Vector3.zero, Ponyo.Hex("2E3F6E"), 0.034f);
            }
            Debug.Log($"[PB] pelican: on Lantern Key at {Root.position.x:F1} {Root.position.z:F1}, berth {(Berth != null)}, counter {(Counter != null)}");
        }
    }
}
