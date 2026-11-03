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
    public partial class Skiff : MonoBehaviour
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
        // co-op: a Remote copy follows the poses of the real one (on the driver's machine, else the host's)
        public bool Remote; public readonly NetTrack<Pose> Net = new();

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
            wake = Particles(Motor != null ? Motor : transform, new Vector3(0, -0.1f, -0.4f), 0.8f, 2.4f);
            bowSpray = Particles(transform, new Vector3(0, 0.05f, 2.4f), 0.35f, 1.0f);
        }

        // Wake and bow wave as flat toon foam lying on the sea (not rising puffs): crisp-edged patches that spread
        // sideways, grow and thin out, then fade.
        ParticleSystem Particles(Transform at, Vector3 off, float size, float life)
        {
            var g = new GameObject("fx"); g.transform.SetParent(at, false); g.transform.localPosition = off;
            var ps = g.AddComponent<ParticleSystem>();
            var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life); m.startSpeed = 0; m.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, 2 * Mathf.PI); m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = new Color(1, 1, 1, 0.9f); m.maxParticles = 400; m.gravityModifier = 0;
            var e = ps.emission; e.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.35f, 0.01f, 0.1f);
            // spread out to both sides behind the boat (a V), in the emitter's frame
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f); vel.y = new ParticleSystem.MinMaxCurve(0, 0); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1, 1.5f)));
            var co = ps.colorOverLifetime; co.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.85f, 0.08f), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0, 1) });
            co.color = gr;
            var r = g.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Util.Soft(Color.white, Util.Foam); r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.sortingFudge = 10;
            return ps;
        }

        static Vector3 OnSea(Vector3 p) => new(p.x, Sea.Height(p) + 0.04f, p.z);

        public float TargetSpeed => Notch < 0 ? -Reverse : Notch * MaxSpeed / 3f;

        public struct Pose
        {
            public float T, X, Y, Z, Yaw, Pitch, Roll, Speed, Rudder, AnchorT, Water; public sbyte Notch; public byte Flags;
            public void Write(ref Unity.Netcode.FastBufferWriter w)
            {
                w.WriteValueSafe(T); w.WriteValueSafe(X); w.WriteValueSafe(Y); w.WriteValueSafe(Z); w.WriteValueSafe(Yaw); w.WriteValueSafe(Pitch); w.WriteValueSafe(Roll);
                w.WriteValueSafe(Speed); w.WriteValueSafe(Rudder); w.WriteValueSafe(AnchorT); w.WriteValueSafe(Water); w.WriteValueSafe(Notch); w.WriteValueSafe(Flags);
            }
            public static Pose Read(ref Unity.Netcode.FastBufferReader r)
            {
                var p = new Pose(); r.ReadValueSafe(out p.T); r.ReadValueSafe(out p.X); r.ReadValueSafe(out p.Y); r.ReadValueSafe(out p.Z); r.ReadValueSafe(out p.Yaw);
                r.ReadValueSafe(out p.Pitch); r.ReadValueSafe(out p.Roll); r.ReadValueSafe(out p.Speed); r.ReadValueSafe(out p.Rudder); r.ReadValueSafe(out p.AnchorT); r.ReadValueSafe(out p.Water);
                r.ReadValueSafe(out p.Notch); r.ReadValueSafe(out p.Flags); return p;
            }
        }
        public Pose NetPose(float t) => new()
        {
            T = t, X = pos.x, Y = y, Z = pos.z, Yaw = yaw, Pitch = pitch, Roll = roll, Speed = Speed, Rudder = Rudder, AnchorT = AnchorT, Water = Water, Notch = (sbyte)Notch,
            Flags = (byte)((Moored ? 1 : 0) | (Anchored ? 2 : 0) | (AtHelm ? 4 : 0) | (Leaking ? 8 : 0)),
        };

        // This copy becomes the real boat (its driver, or the host when the driver leaves): carry on from where it is shown.
        public void TakeOver()
        {
            Remote = false; Net.Clear(); AtHelm = Angler.I != null && Angler.I.AtHelm;
            var e = transform.eulerAngles; pos = new Vector3(transform.position.x, 0, transform.position.z); y = transform.position.y;
            yaw = e.y; pitch = Mathf.DeltaAngle(0, e.x); roll = Mathf.DeltaAngle(0, e.z); vy = vp = vr = 0;
            if (Anchored) anchorAt = pos;
            moorBlend = Moored ? 1 : 0;
        }

        // a remote copy: where the real one was Coop.Delay ago
        void FollowNet(float dt)
        {
            if (!Net.Sample(out var a, out var b, out float k)) return;
            var np = new Vector3(Mathf.Lerp(a.X, b.X, k), 0, Mathf.Lerp(a.Z, b.Z, k));
            Velocity = (np - pos) / Mathf.Max(dt, 1e-4f);
            pos = np; y = Mathf.Lerp(a.Y, b.Y, k); yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, k);
            pitch = Mathf.LerpAngle(a.Pitch, b.Pitch, k); roll = Mathf.LerpAngle(a.Roll, b.Roll, k);
            Speed = Mathf.Lerp(a.Speed, b.Speed, k); Rudder = Mathf.Lerp(a.Rudder, b.Rudder, k); Notch = b.Notch;
            bool wasAnch = Anchored; Moored = (b.Flags & 1) != 0; Anchored = (b.Flags & 2) != 0; AtHelm = (b.Flags & 4) != 0; AnchorT = b.AnchorT; Water = b.Water;
            bool leak = (b.Flags & 8) != 0; if (leak != Leaking) { if (leak) SpringLeak(); else Patch(); } // (the toasts, here too)
            if (Anchored != wasAnch) Snd.Play(M1Assets.I.Splash, AnchorPt != null ? AnchorPt.position : transform.position, Anchored ? 0.7f : 0.3f);
            transform.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, roll));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            RogueTick();
            if (Remote) { FollowNet(dt); Physics.SyncTransforms(); Visuals(dt); return; }
            // rudder: follows the stick, recentres in ~0.6 s
            Rudder = Mathf.Abs(Steer) > 0.05f ? Mathf.MoveTowards(Rudder, Steer * 35f, 120f * dt) : Mathf.MoveTowards(Rudder, 0, 60f * dt);
            if (!AtHelm && Notch != 0 && !Moored) { Notch = 0; Alerts.Push("Biscuit", "Throttle eased to stop.", Alerts.Kind.Info); } // no runaway boats

            LeakTick(dt);
            float want = Moored || Anchored ? 0 : TargetSpeed * SpeedMul;
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
            float tr = Mathf.Clamp(Mathf.Atan2(hR - hP, 1.7f) * Mathf.Rad2Deg, -6f, 6f) - Rudder / 35f * 3f * Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed) + List; // (+ the list of a boat shipping water)
            Spring(ref y, ref vy, ty, 3.2f, dt); Spring(ref pitch, ref vp, tp, 2.6f, dt); Spring(ref roll, ref vr, tr, 2.4f, dt);
            transform.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), q * Quaternion.Euler(pitch, 0, roll));
            // the deck's colliders where the hull is now (auto-sync is off): the walkers' controllers and their "on the deck?"
            // rays read them this frame. Stale, a long frame (a hitch on the driver's machine) stepped a rider standing beside
            // a bench up onto it, 30-40 cm (M2 step 2's test).
            Physics.SyncTransforms();
            Visuals(dt);

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

        // the motor, the prop, the engine's sound, the sea cut out of the hull, the wake: on every copy
        void Visuals(float dt)
        {
            if (Motor != null) Motor.localRotation = Quaternion.Euler(0, Rudder * 0.34f, 0); // ±12°: the tiller swings clear of the helmsman (visual only)
            if (Prop != null) Prop.Rotate(0, Speed * 900f * dt, 0, Space.Self);
            WaterVisual();
            engineVol = Mathf.MoveTowards(engineVol, Moored ? 0.12f : 0.25f + 0.5f * Mathf.Abs(Notch) / 3f, dt);
            engine.volume = engineVol; engine.pitch = 0.75f + 0.55f * Mathf.Abs(Speed) / MaxSpeed;
            // the sea is cut out inside the hull (PB_Water): its footprint, inset a little from the planking
            Shader.SetGlobalMatrix("_PBHullW2L", transform.worldToLocalMatrix);
            Shader.SetGlobalVector("_PBHull", new Vector4(Hull.extents.x - 0.1f, Hull.extents.z - 0.12f, Hull.max.y, 0));
            // foam is born on the water: behind the prop, and either side of the bow
            if (Prop != null) wake.transform.position = OnSea(Prop.position);
            if (AnchorPt != null) bowSpray.transform.position = OnSea(AnchorPt.position);
            var we = wake.emission; we.rateOverTime = Mathf.Abs(Speed) > 0.3f ? 26f * Mathf.Abs(Speed) : 0; // overlapping: one continuous band
            var be = bowSpray.emission; be.rateOverTime = Speed > 2.5f ? 10f * Speed : 0;
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
                        // a hard one (over 3 m/s) throws the driver off the tiller, forward onto the deck
                        if (Mathf.Abs(Speed) > 3f && Angler.I != null && Angler.I.AtHelm) Angler.I.Knock(fwd * Speed * 0.25f + Vector3.up * 0.8f);
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

        // M3: a boat left loose for the night anchors itself where it is (systems §1.4.2)
        public void AnchorNow() { if (Moored || Anchored) return; AnchorT = -1; Anchored = true; anchorAt = pos; Notch = 0; Speed = 0; Steer = 0; }

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
