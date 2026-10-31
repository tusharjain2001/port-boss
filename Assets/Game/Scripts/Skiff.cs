using UnityEngine;

namespace PortBoss.M1
{
    // Biscuit, Walt's T1 skiff (gdd systems §1.6). A kinematic float: it sits on the CPU copy of the sea at 4 hull points
    // and eases its height, pitch and roll toward them with a heavy spring (it lags the waves a little, like a real
    // hull), clamped to the Clear-weather limits (roll 6°, pitch 3°). Driving: throttle notches (reverse, stop, ⅓, ⅔,
    // full) that stay where you leave them, a rudder that recentres in 0.6 s, a turn rate that falls with speed.
    // ponytail: no rigid-body buoyancy; the arcade float can't capsize or jitter the walkable deck. Real buoy-point
    // physics only if boats need to collide with each other (M2+).
    [DefaultExecutionOrder(-100)]
    public class Skiff : MonoBehaviour
    {
        public static Skiff I;
        public Transform Helm, TillerGrip, LadderTop, LadderBottom, AnchorPt, Motor, Prop, CamPoint;
        public Transform[] Rails;
        public Bounds Hull;                       // local, from HullBottom_colBox (+ the sides)

        public const float MaxSpeed = 4.5f, Reverse = 1.5f;
        public int Notch;                         // -1 reverse, 0 stop, 1..3 = ⅓ ⅔ full
        public float Steer;                       // -1..1 input this frame
        public float Speed, Rudder;               // m/s along the bow, degrees
        public bool Moored = true, Anchored, AtHelm;
        public Vector3 Velocity { get; private set; }
        public Transform MoorAt;
        public float AnchorT = -1;                // dropping / raising timer

        float yaw, y, pitch, roll, vy, vp, vr, moorBlend = 1;
        Vector3 pos, anchorAt, drift;
        float engineVol;
        AudioSource engine;
        ParticleSystem wake, bowSpray;

        public static Skiff Create(GameObject prefab, Material toon, Transform moor)
        {
            var g = Instantiate(prefab);
            g.name = "Biscuit";
            Util.Colliders(g); Util.Toon(g, toon);
            var s = g.AddComponent<Skiff>();
            I = s;
            Transform F(string n) => Util.Find(g.transform, n);
            s.Helm = F("socket_helm"); s.TillerGrip = F("socket_tiller_grip"); s.LadderTop = F("socket_ladder_top"); s.LadderBottom = F("socket_ladder_bottom");
            s.AnchorPt = F("socket_anchor"); s.Motor = F("Motor"); s.Prop = F("Prop"); s.CamPoint = F("cam_boat");
            s.Rails = new[] { F("rail_L"), F("rail_R"), F("rail_bow"), F("rail_stern") };
            // The whole hull (keel to gunwale) for the bump check; the underwater box itself must not trip the walker.
            s.Hull = new Bounds(new Vector3(0, 0.25f, 0), new Vector3(1.9f, 1.1f, 4.9f));
            var hb = F("HullBottom_colBox"); if (hb != null && hb.TryGetComponent<Collider>(out var hc)) hc.enabled = false;
            var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.None;
            // a hull-shaped trigger box, only for working out how far the hull sits inside the pier or the shore
            var px = new GameObject("HullProxy"); px.transform.SetParent(g.transform, false);
            s.proxy = px.AddComponent<BoxCollider>(); s.proxy.isTrigger = true; s.proxy.center = s.Hull.center; s.proxy.size = s.Hull.size;
            s.MoorAt = moor;
            s.pos = moor.position; s.yaw = moor.eulerAngles.y; s.y = Sea.Level;
            g.transform.SetPositionAndRotation(new Vector3(s.pos.x, s.y, s.pos.z), Quaternion.Euler(0, s.yaw, 0));
            s.Fx();
            return s;
        }

        void Fx()
        {
            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = Synth.Engine; engine.loop = true; engine.spatialBlend = 0.85f; engine.volume = 0; engine.minDistance = 4; engine.Play();
            wake = Particles(Motor != null ? Motor : transform, new Vector3(0, -0.1f, -0.4f), 0.5f, 1.6f);
            bowSpray = Particles(transform, new Vector3(0, 0.05f, 2.4f), 0.25f, 0.8f);
        }

        ParticleSystem Particles(Transform at, Vector3 off, float size, float life)
        {
            var g = new GameObject("fx"); g.transform.SetParent(at, false); g.transform.localPosition = off;
            var ps = g.AddComponent<ParticleSystem>();
            var m = ps.main; m.startLifetime = life; m.startSpeed = 0.4f; m.startSize = size; m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = new Color(1, 1, 1, 0.75f); m.maxParticles = 300; m.gravityModifier = 0.05f;
            var e = ps.emission; e.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var co = ps.colorOverLifetime; co.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.8f, 0), new GradientAlphaKey(0, 1) });
            co.color = gr;
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = Util.Soft(Color.white);
            return ps;
        }

        public float TargetSpeed => Notch < 0 ? -Reverse : Notch * MaxSpeed / 3f;

        void Update()
        {
            float dt = Time.deltaTime;
            // rudder: follows the stick, recentres in ~0.6 s
            Rudder = Mathf.Abs(Steer) > 0.05f ? Mathf.MoveTowards(Rudder, Steer * 35f, 120f * dt) : Mathf.MoveTowards(Rudder, 0, 60f * dt);
            if (!AtHelm && Notch != 0 && !Moored) { Notch = 0; Alerts.Push("Biscuit", "Throttle eased to stop.", Alerts.Kind.Info); } // no runaway boats

            float want = Moored || Anchored ? 0 : TargetSpeed;
            float acc = Mathf.Abs(want) > Mathf.Abs(Speed) ? 1.1f : 0.8f;
            Speed = Mathf.MoveTowards(Speed, want, acc * dt);
            float sp01 = Mathf.Clamp01(Mathf.Abs(Speed) / 2f);
            float turn = Rudder / 35f * 38f * sp01 * (1 - 0.35f * Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed)) * Mathf.Sign(Speed == 0 ? 1 : Speed);
            float newYaw = yaw + turn * dt;
            var fwd = Quaternion.Euler(0, newYaw, 0) * Vector3.forward;
            var newPos = pos + fwd * Speed * dt;

            // drift: wind + current when free (fishing §6.6: 0.1–0.3 m/s); the anchor holds a 6 m swing circle
            if (!Moored)
            {
                var windDrift = new Vector3(0.12f, 0, -0.08f);
                if (Anchored)
                {
                    var off = newPos + windDrift * dt - anchorAt; off.y = 0;
                    newPos = off.magnitude > 6f ? anchorAt + off.normalized * 6f : newPos + windDrift * dt * 0.3f;
                    newYaw = Mathf.MoveTowardsAngle(newYaw, Quaternion.LookRotation(-windDrift).eulerAngles.y, 2f * dt); // nose to the wind
                }
                else newPos += windDrift * dt;
            }

            if (Moored && MoorAt != null)
            {
                // the assisted 1.5 s settle into the berth (systems §1.6.3), then held by the ropes
                moorBlend = Mathf.MoveTowards(moorBlend, 1, dt / 1.5f);
                var mp = MoorAt.position; mp.y = 0;
                newPos = Util.Damp(newPos, mp, 2f + 20f * moorBlend * moorBlend, dt);
                newYaw = Mathf.LerpAngle(newYaw, MoorAt.eulerAngles.y, 1 - Mathf.Exp(-(2f + 20f * moorBlend * moorBlend) * dt));
            }

            // the pier, the shore: stop with a bump instead of sailing through
            if (!Moored) newPos = PushOut(newPos, newYaw);
            Velocity = (newPos - pos) / Mathf.Max(dt, 1e-4f);
            pos = newPos; yaw = newYaw;

            // float on the waves: heave, pitch, roll from 4 hull points, heavy spring
            var q = Quaternion.Euler(0, yaw, 0);
            float hB = Sea.Height(pos + q * new Vector3(0, 0, 2.1f)), hS = Sea.Height(pos + q * new Vector3(0, 0, -2.1f));
            float hP = Sea.Height(pos + q * new Vector3(-0.85f, 0, 0)), hR = Sea.Height(pos + q * new Vector3(0.85f, 0, 0));
            float ty = (hB + hS + hP + hR) * 0.25f - 0.02f;
            float tp = Mathf.Clamp(-Mathf.Atan2(hB - hS, 4.2f) * Mathf.Rad2Deg, -3f, 3f) - 2.5f * Mathf.Clamp01(Speed / MaxSpeed);
            float tr = Mathf.Clamp(Mathf.Atan2(hR - hP, 1.7f) * Mathf.Rad2Deg, -6f, 6f) - Rudder / 35f * 3f * Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
            Spring(ref y, ref vy, ty, 3.2f, dt); Spring(ref pitch, ref vp, tp, 2.6f, dt); Spring(ref roll, ref vr, tr, 2.4f, dt);
            transform.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), q * Quaternion.Euler(pitch, 0, roll));

            if (Motor != null) Motor.localRotation = Quaternion.Euler(0, Rudder * 0.9f, 0);
            if (Prop != null) Prop.Rotate(0, Speed * 900f * dt, 0, Space.Self);
            engineVol = Mathf.MoveTowards(engineVol, Moored ? 0.12f : 0.25f + 0.5f * Mathf.Abs(Notch) / 3f, dt);
            engine.volume = engineVol; engine.pitch = 0.75f + 0.55f * Mathf.Abs(Speed) / MaxSpeed;
            var we = wake.emission; we.rateOverTime = Mathf.Abs(Speed) > 0.3f ? 18f * Mathf.Abs(Speed) : 0;
            var be = bowSpray.emission; be.rateOverTime = Speed > 2.5f ? 10f * Speed : 0;

            if (AnchorT >= 0)
            {
                AnchorT += dt;
                if (AnchorT >= 1.5f)
                {
                    AnchorT = -1; Anchored = !Anchored; anchorAt = pos;
                    Alerts.Push("Biscuit", Anchored ? "Anchor down. She'll hold here." : "Anchor up.", Alerts.Kind.Info);
                    Snd.Play(M1Assets.I.Splash, AnchorPt != null ? AnchorPt.position : transform.position, Anchored ? 0.7f : 0.3f);
                }
            }
        }

        static void Spring(ref float x, ref float v, float target, float w, float dt)
        {
            // critically damped
            float f = 1 + 2 * dt * w, oo = w * w, hoo = dt * oo, hhoo = dt * hoo, det = 1 / (f + hhoo);
            float dx = f * x + dt * v + hhoo * target, dv = v + hoo * (target - x);
            x = dx * det; v = dv * det;
        }

        static readonly Collider[] hits = new Collider[16], now = new Collider[16];
        // Blocked = the move would touch something the hull isn't already touching (so it can always back or slide
        // out of a contact, e.g. leaving the berth beside the piles, but never push into the pier or the shore).
        bool Blocked(Vector3 p, float yw)
        {
            int m = Overlap(pos, yaw, now), n = Overlap(p, yw, hits);
            for (int i = 0; i < n; i++)
            {
                bool old = false; for (int j = 0; j < m; j++) if (now[j] == hits[i]) old = true;
                if (!old) { if (Time.time - lastBlockLog > 1f) { lastBlockLog = Time.time; Debug.Log($"[PB] boat blocked by {hits[i].name} at {transform.position} speed {Speed:F2}"); } return true; }
            }
            return false;
        }
        float lastBlockLog; BoxCollider proxy;

        // The pier, the shore: the hull is pushed back out of whatever it would overlap (it slides along piles and
        // rocks instead of sticking); a head-on hit stops it with a thud and a little bounce.
        Vector3 PushOut(Vector3 p, float yw)
        {
            var q = Quaternion.Euler(0, yw, 0);
            var fwd = q * Vector3.forward;
            for (int it = 0; it < 3; it++)
            {
                int n = Overlap(p, yw, hits);
                if (n == 0) break;
                for (int i = 0; i < n; i++)
                {
                    var o = hits[i].transform;
                    if (!Physics.ComputePenetration(proxy, new Vector3(p.x, y, p.z), q, hits[i], o.position, o.rotation, out var dir, out float d)) continue;
                    dir.y = 0; if (dir.sqrMagnitude < 1e-6f) continue; dir.Normalize();
                    p += dir * (d + 0.005f);
                    if (Vector3.Dot(dir, fwd * Mathf.Sign(Speed == 0 ? 1 : Speed)) < -0.6f && Mathf.Abs(Speed) > 0.6f)
                    {
                        if (Mathf.Abs(Speed) > 1.2f) { Snd.Play(M1Assets.I.Thud, transform.position, 0.8f); CameraRig.Shake(0.25f * Mathf.Abs(Speed) / MaxSpeed); }
                        if (Time.time - lastBlockLog > 1f) { lastBlockLog = Time.time; Debug.Log($"[PB] boat bumped {hits[i].name} at {Speed:F1} m/s"); }
                        Speed *= -0.25f;
                    }
                }
            }
            return p;
        }
        int Overlap(Vector3 p, float yw, Collider[] into)
        {
            var q = Quaternion.Euler(0, yw, 0);
            var c = new Vector3(p.x, y, p.z) + q * Hull.center;
            int n = Physics.OverlapBoxNonAlloc(c, Hull.extents + new Vector3(0.04f, 0, 0.04f), into, q, ~0, QueryTriggerInteraction.Ignore), k = 0;
            for (int i = 0; i < n; i++) if (!into[i].transform.IsChildOf(transform) && into[i].GetComponentInParent<Angler>() == null) into[k++] = into[i];
            return k;
        }

        public void CastOff()
        {
            if (!Moored) return;
            Moored = false; moorBlend = 0;
            Alerts.Push("Biscuit", "Cast off. W / S for the throttle, A / D to steer.", Alerts.Kind.Info);
        }

        public bool CanMoor => !Moored && MoorAt != null && Vector3.Distance(Flat(pos), Flat(MoorAt.position)) < 6f && Mathf.Abs(Speed) < 2f;
        public void Moor() { Moored = true; Anchored = false; moorBlend = 0; Notch = 0; Speed = 0; Alerts.Push("Biscuit", "Moored at Walt's Landing.", Alerts.Kind.Info); }
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        public void ToggleAnchor() { if (AnchorT < 0 && !Moored) { AnchorT = 0; Snd.Play(Synth.Ratchet, AnchorPt != null ? AnchorPt.position : transform.position, 0.6f); } }

        // Is this world point inside the hull's footprint (for the fishing line and the cast marker)?
        public bool Inside(Vector3 w, float pad = 0)
        {
            var l = transform.InverseTransformPoint(w);
            return Mathf.Abs(l.x) < 1.0f + pad && Mathf.Abs(l.z) < 2.6f + pad;
        }
        public float GunwaleY(Vector3 w) => transform.TransformPoint(new Vector3(0, 0.7f, transform.InverseTransformPoint(w).z)).y;
    }
}
