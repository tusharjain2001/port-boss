using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M7 step 3: berths away from home (The Pelican's jetty on Lantern Key). Near one, F moors there; casting off clears it.
    // MoorAt stays the home berth (the workers, the Kestrel's spot, the tests). A boat tied up away saves as anchored where
    // she lies (Save.cs), so a loaded world never drags her back across the bay. A copy that becomes real while moored
    // far from home (a crewmate took the tiller) finds the visiting berth it's at.
    public partial class Skiff
    {
        public static readonly List<(Transform at, string name)> Visits = new();
        public Transform AwayAt; public string AwayName;
        public Transform Berth => AwayAt != null ? AwayAt : MoorAt;
        public bool MooredAway => Moored && AwayAt != null;

        // the nearest berth within 6 m (home or a visit)
        Transform BerthNear(out string name, float within = 6f)
        {
            name = "Walt's Landing"; Transform best = null; float bd = within;
            if (MoorAt != null) { float d = Vector3.Distance(Flat(pos), Flat(MoorAt.position)); if (d < bd) { bd = d; best = MoorAt; } }
            foreach (var (at, n) in Visits) { if (at == null) continue; float d = Vector3.Distance(Flat(pos), Flat(at.position)); if (d < bd) { bd = d; best = at; name = n; } }
            return best;
        }
        // (the real copy, before the settle) moored far from home with no visit set: it's the visit it lies at
        void FindAway()
        {
            if (!Moored || AwayAt != null || MoorAt == null || Vector3.Distance(Flat(pos), Flat(MoorAt.position)) < 12f) return;
            var b = BerthNear(out var n, 14f); if (b != null && b != MoorAt) { AwayAt = b; AwayName = n; }
        }
    }
}
