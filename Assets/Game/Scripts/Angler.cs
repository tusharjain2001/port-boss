using UnityEngine;

namespace PortBoss.M1
{
    // The player in M1 (solo): walks the pier, the shore and Biscuit's deck, swims, climbs ladders, takes the helm and
    // fishes. On the deck the body moves in the boat's own space (systems §1.6.1): each frame it is first carried by
    // the boat's move (position and turn), then walks, so a rocking, moving boat never slides it.
    // Feel numbers from replan/feel: walk 1.4, run 4.2, sprint 5.8 m/s on land (ramping up); on Biscuit's small deck no
    // sprint and a calmer 2.6 m/s; eased starts, stops and turns that get heavier with the fish carried; a skid on a hard
    // reverse; a lean from acceleration; a crouch before every jump.
    [DefaultExecutionOrder(0)]
    public class Angler : MonoBehaviour
    {
        public static Angler I;
        public CharacterController Cc;
        public CharAnim Anim;
        public Transform Model;
        public Hotbar Bar = new();
        public Fishing Fish;

        public bool OnBoat { get; private set; }
        public bool Swimming { get; private set; }
        public bool AtHelm => helm;
        public bool Busy;                 // fishing owns the body (fight, reveal): no walking
        public float FaceYaw = float.NaN; // fishing faces the aim
        public Vector3 Vel;               // own walking velocity (not the boat's)
        public float Size = 1;            // body height / the 1.78 m worker the rod and hold poses were made on
        public const float SwimDepth = 1.3f;

        // feel (replan/feel spec): the sprint's run-up, where the camera follows height from, the carried load
        public float SprintW { get; private set; }  // 0..1: run 4.2 -> sprint 5.8 m/s over 0.6 s, back in 0.2 s
        public float CamY { get; private set; }     // the ground we last stood on (a jump doesn't lift the camera)
        public bool Skidding => skidT > 0;
        // Fish carried, as the feel spec's crates: every 10 kg is one (M1's tier-1 fish weigh grams; a big conger counts).
        public float Load { get { float kg = 0; foreach (var s in Bar.S) foreach (var f in s.Fish) kg += f.Kg; return Mathf.Clamp(kg / 10f, 0, 5); } }

        Vector3 localOnBoat; float localYaw;
        float vy, lastGround;
        bool helm;
        Vector3 lastPos; float animSpeed;
        int stepI; float lastPhase;
        bool wasSwim; float dunkT = -1;
        float sit, lastYaw;
        Vector3 velV, lastVel;                    // the walking spring, last frame's walk (lean from acceleration)
        float skidT;                              // > 0: skidding to a stop on a hard reverse
        float jumpBuf = -9, crouchT = -1, jumpCut, airT, peakY; bool rising, wasAir;
        float leanP, leanPV, leanR, leanRV;       // body lean forward / sideways (degrees) on a spring
        float sq, sqV;                            // squash (-) and stretch (+) of the body on take-off and landing

        public static Angler Create(M1Assets a, Vector3 at, float yaw)
        {
            var g = new GameObject("Angler");
            g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var p = g.AddComponent<Angler>(); I = p;
            p.Cc = g.AddComponent<CharacterController>();
            p.Cc.height = 1.75f; p.Cc.radius = 0.3f; p.Cc.center = new Vector3(0, 0.875f, 0); p.Cc.stepOffset = 0.42f; p.Cc.slopeLimit = 50; p.Cc.skinWidth = 0.03f;
            p.Cc.minMoveDistance = 0; // the default 1 mm drops every step at high frame rates (uncapped test runs: the swimmer barely moved)
            p.Model = new GameObject("model").transform; p.Model.SetParent(g.transform, false);
            PortBoss.Art.Lit = new Material(a.Toon);
            PortBoss.Art.Lit.SetFloat("_PaintNoise", 0.12f);
            GameObject body;
            if (a.Pip != null)
            {
                // Pip, the character kit's first body (art-src/kit): vertex-coloured like the boat, in the toon shader
                body = Instantiate(a.Pip, p.Model, false); body.name = "pip";
                body.transform.localRotation = Quaternion.Euler(0, 180, 0); // exported facing -Z, like the worker
                foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = PortBoss.Art.Lit; r.updateWhenOffscreen = true; }
                p.Size = 1.47f / 1.78f;                                     // Pip is 1.47 m (base 1.75 x her 0.84)
            }
            else
            {
                // the placeholder dock worker (Assets/Art/Worker) with his palette texture
                var w = PortBoss.Worker.Make(a.Worker, p.Model);
                w.Apply(new PortBoss.Look
                {
                    Set = 1, Face = 0, Hair = 3, Beard = 2, Hat = 2, Top = 2, Gloves = 0,
                    SkinTone = 2, HairColor = 2, TopColor = 1, AccentColor = 4, LegsColor = 4, BootsColor = 7, GloveColor = 4, HatColor = 2,
                });
                var rs = w.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                if (bb.size.y > 0.5f) w.transform.localScale = Vector3.one * Mathf.Clamp(1.78f / bb.size.y, 0.8f, 1.25f);
                body = w.gameObject;
            }
            foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            p.Bar.Selected = 1; // empty hands at the start: "1" draws the rod (F1)
            var anim = body.GetComponentInChildren<Animator>();
            var skin = ScriptableObject.CreateInstance<PortBoss.Skin>();
            skin.Idle = a.Idle; skin.Walk = a.Walk; skin.Run = a.Run; skin.Sprint = a.Sprint; skin.Jump = a.Jump; skin.Land = a.Land;
            skin.SwimIdle = a.SwimIdle; skin.Swim = a.Swim; skin.Climb = a.Climb; skin.Drive = a.Drive ?? a.Idle; skin.Push = a.Idle;
            p.Anim = anim.gameObject.AddComponent<CharAnim>();
            p.Anim.Init(anim, skin); p.Anim.Facing = g.transform; p.Anim.GripLean = 0;
            p.Anim.LandFrom = 0.25f; // Falling To Landing's feet touch down here: before it, it's still falling
            if (a.Pip != null) p.rag = Ragdoll.Build(anim, p.Cc);
            // a floppy head, and the hat (its own bone in the kit) wobbling on top of it
            var jig = Jiggle.On(anim);
            jig.Bone(anim.GetBoneTransform(HumanBodyBones.Head), Vector3.up, 0.25f, 220f, 0.55f, 8f);
            jig.Bone(Util.Find(body.transform, "Hat"), Vector3.up, 0.15f, 140f, 0.35f, 12f, false);
            p.Fish = g.AddComponent<Fishing>();
            p.lastPos = at;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var s = Skiff.I;
            if (downT >= 0) { Lying(dt); return; } // the ragdoll has the body (and the root stays put under it)
            // carried by the boat
            if (OnBoat && s != null)
            {
                var carried = s.transform.TransformPoint(localOnBoat);
                bool cc = Cc.enabled; Cc.enabled = false; transform.position = carried; Cc.enabled = cc;
                transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y + localYaw, 0);
            }
            if (getUpT >= 0) { GettingUp(dt); return; }
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F8) && !Busy) { Knock(transform.forward * 2.5f + Vector3.up * 1.5f); return; } // debug: fall over

            if (helm) { Helm(dt); Track(); CamY = transform.position.y; Animate(dt); return; }

            // hotbar
            if (!Busy && !Fish.BarLocked)
            {
                int k = Ctl.SlotKey; if (k >= 0) Select(k);
                int sc = Ctl.SlotScroll; if (sc != 0) Select(((Bar.Selected + sc) % Hotbar.Slots + Hotbar.Slots) % Hotbar.Slots);
            }

            float surface = Sea.Height(transform.position);
            Swimming = transform.position.y < surface - SwimDepth + 0.35f && !OnBoat;
            if (Swimming && !wasSwim) { dunkT = 0; Splash(surface, 1f); }
            wasSwim = Swimming;

            // walking
            var cam = CameraRig.I;
            var input = Busy ? Vector2.zero : Ctl.Move;
            var fwd = cam != null ? Quaternion.Euler(0, cam.Yaw, 0) : transform.rotation;
            var wish = fwd * new Vector3(input.x, 0, input.y);
            float load = Load;
            // Sprint ramps up over 0.6 s (a run-up, not a gear change) and drops back to a run in 0.2 s; never with 5
            // crates' worth, and every crate's worth is 8% slower (5: 2.5 m/s).
            bool sprint = Ctl.Sprint && !OnBoat && !Swimming && !Ctl.Walk && load < 5 && wish.sqrMagnitude > 0.01f;
            SprintW = Mathf.MoveTowards(SprintW, sprint ? 1 : 0, dt / (sprint ? 0.6f : 0.2f));
            float run = Mathf.Lerp(4.2f, 5.8f, Mathf.SmoothStep(0, 1, SprintW)) * (1 - 0.08f * load);
            float max = Swimming ? (Ctl.Sprint ? 2.2f : 1.4f) : Ctl.Walk ? 1.4f : OnBoat ? 2.6f : run;
            if (Fish.Holding && !Swimming) max = Mathf.Min(max, OnBoat ? 2.2f : 3.6f); // careful with a rod out
            var target = wish * max;
            bool grounded = Cc.isGrounded;
            // A hard reverse at speed (> 120°, over 60% of a run): a 0.15 s skid, feet planted, leaning back, then the turn.
            if (skidT <= 0 && grounded && !Swimming && Vel.magnitude > 0.6f * 4.2f && wish.sqrMagnitude > 0.25f && Vector3.Angle(wish, Vel) > 120f)
            { skidT = 0.15f; Dust(6, 1.2f); Snd.Play(Synth.Scuff, transform.position, 0.5f, 0.9f); }
            if (skidT > 0) { skidT -= dt; target = Vector3.zero; }
            // Eased starts and stops (a critically damped spring: 90% of the way in 1.94 x its time), slower per crate:
            // 0 -> 90% in 0.22 s + 0.05 s a crate, 90% -> stopped in 0.18 s + 0.07 s a crate; a skid stops in 0.15 s.
            bool speeding = target.sqrMagnitude > Vel.sqrMagnitude + 0.01f;
            float t90 = skidT > 0 ? 0.1f : Swimming ? 0.45f : speeding ? 0.22f + 0.05f * load : 0.18f + 0.07f * load;
            Vel = Vector3.SmoothDamp(Vel, target, ref velV, t90 / 1.94f, Mathf.Infinity, dt);
            if (sprint && Fish.Holding) Fish.Stow();

            // facing: where we walk, or the fishing aim; 600°/s empty, 70°/s less per crate; none while skidding
            float wantYaw = !float.IsNaN(FaceYaw) ? FaceYaw : Vel.sqrMagnitude > 0.05f ? Mathf.Atan2(Vel.x, Vel.z) * Mathf.Rad2Deg : transform.eulerAngles.y;
            float turnRate = !float.IsNaN(FaceYaw) ? 540f : skidT > 0 ? 0 : 600f - 70f * load;
            transform.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(transform.eulerAngles.y, wantYaw, turnRate * dt), 0);

            if (grounded) lastGround = Time.time;
            if (Swimming)
            {
                // float with head + shoulders out; a short dunk on the way in
                float wantY = surface - SwimDepth;
                if (dunkT >= 0) { dunkT += dt / 0.6f; wantY -= 0.3f * Mathf.Sin(Mathf.Clamp01(dunkT) * Mathf.PI); if (dunkT >= 1) dunkT = -1; }
                vy = (wantY - transform.position.y) * 6f;
                crouchT = -1; rising = false;
            }
            float g = Swimming ? 0 : Jump(dt, grounded, load);
            Cc.Move((Vel + Vector3.up * (vy - 0.5f * g * dt)) * dt); // the exact arc at any frame rate
            vy -= g * dt;
            Land();

            Interact();
            Track();
            Animate(dt);
        }

        // Put the player somewhere (tests, ladders): also re-anchors them in the boat's space so the deck carry doesn't undo it.
        public void Teleport(Vector3 p, float yaw)
        {
            Cc.enabled = false; transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); Cc.enabled = true;
            Vel = lastVel = velV = Vector3.zero; vy = 0; skidT = 0; crouchT = -1; rising = wasAir = jumped = fallIn = false; overW = 0; CamY = p.y; Track();
        }

        // Jump (feel spec): a 0.08 s crouch first (the press is buffered 0.12 s, with 0.12 s of coyote time, so it still
        // answers at once), then up: 0.8 m held, 0.45 m tapped (0.6 m held on Biscuit's deck: it clears the 0.5 m
        // gunwale when you mean it), a 0.3 m hop with 3+ crates' worth. Rises at g 18, falls at 35 (a heavy body).
        // Returns this frame's gravity.
        float Jump(float dt, bool grounded, float load)
        {
            if (!Busy && Ctl.Jump) jumpBuf = Time.time;
            if (crouchT < 0 && !Busy && Time.time - jumpBuf < 0.12f && Time.time - lastGround < 0.12f) { crouchT = 0; jumpBuf = -9; }
            if (crouchT >= 0 && (crouchT += dt) >= 0.08f)
            {
                float h = load >= 3 ? 0.3f : OnBoat ? 0.6f : 0.8f;
                vy = Mathf.Sqrt(2 * 18f * h); jumpCut = load >= 3 ? 18f : vy * vy / (2 * 0.45f);
                crouchT = -1; rising = jumped = true; lastGround = -1; sqV += 1.1f;
            }
            if (vy <= 0) rising = false;
            if (grounded && vy <= 0) { vy = -2f; return 0; }
            return vy > 0 ? (rising && !Ctl.JumpHeld ? jumpCut : 18f) : 35f;
        }

        // Landing: soft under a 1 m drop (a step, a small puff), hard above (a thud, a bigger puff, the camera dips).
        // The camera follows height from the ground stood on, not the jump, unless it's a real fall (off the pier).
        void Land()
        {
            bool air = !Cc.isGrounded && !Swimming;
            if (air && !wasAir) { airT = 0; peakY = transform.position.y; fallIn = !jumped && SeaBelow(); }
            if (air) { airT += Time.deltaTime; peakY = Mathf.Max(peakY, transform.position.y); }
            else if (wasAir && !Swimming && airT > 0.08f)
            {
                float drop = peakY - transform.position.y;
                if (drop > 1f) { Snd.Play(M1Assets.I.Thud, transform.position, 0.7f, 0.8f); Dust(10, 1.6f); CameraRig.Dip(0.1f); sqV -= 1.5f; }
                else if (drop > 0.15f) { Step(0.35f); Dust(4, 1f); sqV -= 0.9f; }
            }
            if (!air) jumped = fallIn = false;
            wasAir = air;
            if (!air) CamY = transform.position.y;
            else if (transform.position.y < CamY - 1f) CamY = transform.position.y + 1f;
        }

        // Stepping off something high over open water (the pier's edge): more than 0.9 m down to the sea, deep below.
        bool SeaBelow()
        {
            var p = transform.position; float sea = Sea.Height(p);
            bool ground = Physics.Raycast(p + Vector3.up * 0.2f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y > sea - 0.3f;
            return !ground && p.y - sea > 0.9f;
        }

        void Dust(int n, float size) { if (!Swimming) Fx.Dust(transform.position + Vector3.up * 0.04f, n, size); }
        void Step(float vol) { var st = M1Assets.I.Steps; if (st != null && st.Length > 0) Snd.Play(st[stepI++ % st.Length], transform.position, vol, Random.Range(0.92f, 1.08f)); }
        static void Spring(ref float x, ref float v, float target, float k, float zeta, float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            v += (k * (target - x) - 2 * zeta * Mathf.Sqrt(k) * v) * dt; x += v * dt;
        }

        public void TakeHelm() { var s = Skiff.I; if (s == null) return; BeginHelm(s); Fish.Stow(); }
        void BeginHelm(Skiff s)
        {
            helm = true; s.AtHelm = true; helmT = 0;
            helmFrom = s.transform.InverseTransformPoint(transform.position); helmFromYaw = transform.eulerAngles.y - s.transform.eulerAngles.y;
        }
        float helmT = 1, helmFromYaw; Vector3 helmFrom;

        // Where he sits at the tiller: on the stern bench, starboard of the tiller (Manan: the LEFT hand always on the
        // tiller, the right hand free), placed from the tiller's centred position so a full swing stays clear of him.
        Vector3 SeatLocal(Skiff s)
        {
            var m = s.Motor; var g = s.TillerGrip;
            Vector3 rest = m != null && g != null
                ? s.transform.InverseTransformPoint(m.parent.TransformPoint(m.localPosition + Vector3.Scale(m.localScale, m.InverseTransformPoint(g.position))))
                : s.transform.InverseTransformPoint(s.Helm.position);
            return new Vector3(rest.x + 0.6f, s.transform.InverseTransformPoint(s.Helm.position).y, rest.z - 0.02f);
        }
        public void LeaveHelm() { var s = Skiff.I; helm = false; if (s != null) { s.AtHelm = false; s.Steer = 0; } if (CameraRig.I != null) CameraRig.I.M = CameraRig.Mode.Follow; }

        public void Select(int i)
        {
            if (i == Bar.Selected) return;
            Bar.Selected = i;
            Hud.I?.Selected(i);
        }

        // Which deck are we on? Re-anchor in the boat's space after every move.
        void Track()
        {
            var s = Skiff.I;
            bool on = false;
            if (s != null && !Swimming && Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out var hit, 0.9f, ~0, QueryTriggerInteraction.Ignore))
                on = hit.transform.IsChildOf(s.transform);
            if (helm) on = true;
            OnBoat = on;
            if (on) { localOnBoat = s.transform.InverseTransformPoint(transform.position); localYaw = transform.eulerAngles.y - s.transform.eulerAngles.y; }
            if (CameraRig.I != null) CameraRig.I.OnDeck = on;
        }

        // ---- E: helm, anchor, ladders, pickups ----
        public string Prompt { get; private set; }

        void Interact()
        {
            Prompt = null;
            var s = Skiff.I;
            if (Busy || Fish.Locked) return;
            // ladders from the water
            if (Swimming)
            {
                if (s != null && s.LadderBottom != null && Near(s.LadderBottom.position, 1.8f)) { Prompt = "Climb aboard"; if (Ctl.Interact) StartClimb(s.LadderBottom, s.LadderTop, s.transform); return; }
                var l = Landing.I;
                if (l != null && l.LadderBottom != null && Near(l.LadderBottom.position, 1.8f)) { Prompt = "Climb the ladder"; if (Ctl.Interact) StartClimb(l.LadderBottom, l.LadderTop, l.transform); return; }
                return;
            }
            if (s == null) return;
            if (OnBoat && s.Helm != null && Near(s.Helm.position, 1.3f)) { Prompt = "Take the tiller"; if (Ctl.Interact) { BeginHelm(s); Fish.Stow(); } return; }
            if (OnBoat && s.AnchorPt != null && Near(s.AnchorPt.position, 1.4f) && !s.Moored) { Prompt = s.AnchorT >= 0 ? "…" : s.Anchored ? "Raise the anchor" : "Drop the anchor"; if (Ctl.Interact) s.ToggleAnchor(); return; }
            var pick = FishPickup.Nearest(transform.position, 1.6f);
            if (pick != null) { Prompt = Bar.Full ? "Hotbar full" : "Pick up " + pick.F.Species.Name; if (Ctl.Interact && !Bar.Full) pick.Take(this); return; }
        }

        bool Near(Vector3 p, float r) { var d = p - transform.position; d.y *= 0.5f; return d.magnitude < r; }

        void Helm(float dt)
        {
            var s = Skiff.I;
            Prompt = s.Moored ? "W  Cast off   ·   E  Leave the tiller" : s.CanMoor ? "F  Moor   ·   E  Leave the tiller" : "W/S  Throttle   ·   A/D  Steer   ·   E  Leave";
            Cc.enabled = false;
            // slide onto the seat (0.35 s) instead of jumping there
            helmT = Mathf.Min(1, helmT + dt / 0.35f); float k = Mathf.SmoothStep(0, 1, helmT);
            transform.position = s.transform.TransformPoint(Vector3.Lerp(helmFrom, SeatLocal(s), k));
            transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y + Mathf.LerpAngle(helmFromYaw, 0, k), 0);
            Cc.enabled = true;
            int th = Ctl.Throttle;
            if (s.Moored) { if (th > 0) { s.CastOff(); s.Notch = 1; Snd.Play2D(Synth.Ratchet, 0.4f); } }
            else if (th != 0) { s.Notch = Mathf.Clamp(s.Notch + th, -1, 3); Snd.Play2D(Synth.Tik, 0.6f, th > 0 ? 1.2f : 0.9f); }
            s.Steer = Mathf.Clamp(Ctl.Steer, -1, 1);
            if (s.CanMoor && Ctl.Key_(UnityEngine.InputSystem.Key.F)) s.Moor();
            if (Ctl.Interact || Ctl.Escape) LeaveHelm();
            if (CameraRig.I != null) CameraRig.I.M = helm ? CameraRig.Mode.Helm : CameraRig.Mode.Follow;
            Vel = Vector3.zero;
        }

        // A ladder from the water: E puts you straight on top (Tushar 1 Nov: the climb animation went up and down the
        // rungs a few times before he got off; he wants E = up). Onto the pier, or in over the gunwale toward the middle
        // of the boat, standing on whatever is under that spot.
        void StartClimb(Transform bottom, Transform top, Transform reference)
        {
            var inward = top.position - bottom.position; inward.y = 0;
            var toCentre = reference.position - top.position; toCentre.y = 0;
            bool boat = reference == Skiff.I?.transform;
            var dir = (boat && toCentre.sqrMagnitude > 0.01f ? toCentre : inward).normalized;
            var p = top.position + dir * 0.7f;
            if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Angler>() == null)
                p.y = hit.point.y;
            Fish.Stow();
            Teleport(p + Vector3.up * 0.02f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
            vy = -2f; Swimming = false; wasSwim = false; lastGround = Time.time; Prompt = null;
            Snd.Play(M1Assets.I.Splash, transform.position, 0.3f, 1.4f);
        }

        void Splash(float surface, float size)
        {
            Fx.Splash(new Vector3(transform.position.x, surface, transform.position.z), size);
            Snd.Play(M1Assets.I.Splash, transform.position, 0.8f);
        }

        // ---- knocked over (ragdoll-lite, replan/feel §4): F8, or thrown off the tiller by a crash ----
        public bool Down => downT >= 0 || getUpT >= 0;
        public bool Limp => downT >= 0;
        M1Assets.GetUpClip up; // the get-up playing (one of M1Assets.GetUps, at random)
        Ragdoll rag; float downT = -1, getUpT = -1; bool jumped, fallIn; AnimationClip overClip; float overT, overW;

        // Over she goes, limp from the pose she's in, flung by `push` (m/s) and tipping the way she was pushed.
        public void Knock(Vector3 push)
        {
            if (rag == null || Down || Swimming || Fish.Locked) return; // (never with a line out)
            if (helm) { LeaveHelm(); if (Skiff.I != null) Skiff.I.Notch = 0; }
            Fish.Stow();
            Anim.Over = null; Anim.OverW = overW = 0; Anim.Flail = false; Anim.Grip = null; Anim.Act = null;
            Model.localPosition = Vector3.zero; Model.localRotation = Quaternion.identity; Model.localScale = Vector3.one;
            leanP = leanR = leanPV = leanRV = sq = sqV = 0; crouchT = -1; jumped = fallIn = false;
            var flat = new Vector3(push.x, 0, push.z);
            rag.Go(Vel + Vector3.up * Mathf.Max(0, vy) + push, flat.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, flat.normalized) * 5f : Vector3.zero);
            Cc.enabled = false; Vel = velV = Vector3.zero; vy = 0; downT = 0; Prompt = null;
            if (CameraRig.I != null) CameraRig.I.Target = rag.Hips;
            CameraRig.Shake(0.3f); Snd.Play(M1Assets.I.Thud, transform.position, 0.6f, 1.2f);
            StartCoroutine(HitStop(0.05f));
        }

        static System.Collections.IEnumerator HitStop(float s)
        {
            float ts = Time.timeScale; Time.timeScale = ts * 0.05f;
            yield return new WaitForSecondsRealtime(s);
            Time.timeScale = ts;
        }

        void Lying(float dt)
        {
            downT += dt; Prompt = null;
            var h = rag.Hips.position;
            if (h.y < Sea.Height(h) - 0.25f) { Overboard(); return; } // flung into the sea
            if (downT > 1.0f && rag.Speed < 0.6f || downT > 1.8f) GetUp(); // (a rocking deck never lets her lie quite still)
        }

        // In the sea: straight up into treading water where the hips are.
        void Overboard()
        {
            var h = rag.Hips.position;
            rag.Stop(0.3f); downT = -1;
            if (CameraRig.I != null) CameraRig.I.Target = transform;
            Teleport(new Vector3(h.x, Sea.Height(h) - SwimDepth, h.z), transform.eulerAngles.y);
        }

        // Up again, with one of the get-ups at random: the body is put where its first frame lines up with where she lies
        // (hips on the hips, head the same way), the clip blends in from the ragdoll pose (0.3 s; 0.45 s on her back: a
        // quick roll onto her front, both clips start face down).
        void GetUp()
        {
            var a = M1Assets.I;
            if (a.GetUps == null || a.GetUps.Length == 0) { Overboard(); return; } // (no clips: never stuck lying there)
            up = a.GetUps[Random.Range(0, a.GetUps.Length)];
            var h = rag.Hips.position; var spine = rag.Head.position - h; spine.y = 0;
            bool onBack = rag.Chest.forward.y > 0;
            float yaw = Mathf.Atan2(spine.x, spine.z) * Mathf.Rad2Deg - Mathf.Atan2(up.Spine.x, up.Spine.z) * Mathf.Rad2Deg;
            var bodyRot = Quaternion.Euler(0, yaw, 0);
            float ground = float.NegativeInfinity; // the highest floor under the hips (not the rail she may lie against)
            foreach (var hit in Physics.RaycastAll(h + Vector3.up * 0.8f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Angler>() == null && hit.point.y <= h.y + 0.15f && hit.point.y > ground) ground = hit.point.y;
            if (float.IsNegativeInfinity(ground)) { Overboard(); return; } // draped over the gunwale, hips over the sea: she slides in
            var at = new Vector3(h.x, ground, h.z) - bodyRot * new Vector3(up.Hips.x, 0, up.Hips.z);
            rag.Stop(onBack ? 0.45f : 0.3f);
            transform.SetPositionAndRotation(at, bodyRot * Quaternion.Euler(0, 180, 0)); // the body sits turned round under the model
            downT = -1; getUpT = 0; CamY = ground;
            Track(); // on the deck: the boat carries her while she gets up
            if (CameraRig.I != null) CameraRig.I.Target = transform;
            Anim.Over = up.Clip; Anim.OverW = 1; Anim.OverTime = up.From;
        }

        // Getting up: the clip's creep along the deck moves the body (feet planted), the controller follows it at the end.
        void GettingUp(float dt)
        {
            Prompt = null;
            getUpT += dt;
            float len = (up.To - up.From) / up.Speed, t = Mathf.Min(up.To, up.From + getUpT * up.Speed);
            float done = Mathf.Clamp01((getUpT - len) / 0.25f); // then 0.25 s into the idle
            Anim.Over = up.Clip; Anim.OverTime = t; Anim.OverW = 1 - done;
            var creep = Quaternion.Euler(0, 180, 0) * new Vector3(up.MoveX.Evaluate(t), 0, up.MoveZ.Evaluate(t)); // body space -> model space
            Model.localPosition = creep; Model.localRotation = Quaternion.identity; Model.localScale = Vector3.one;
            Anim.Tick(CharAnim.Slot.Idle, 0);
            if (done >= 1)
            {
                getUpT = -1; Anim.Over = null; Anim.OverW = 0;
                var p = transform.TransformPoint(creep); Model.localPosition = Vector3.zero;
                Teleport(p, transform.eulerAngles.y);
            }
        }

        void Animate(float dt)
        {
            var d = transform.position - lastPos; lastPos = transform.position;
            // own speed only (the boat's motion carries us, it isn't walking)
            float sp = new Vector3(Vel.x, 0, Vel.z).magnitude;
            animSpeed = sp;
            CharAnim.Slot slot;
            // at the tiller: sat on the stern bench (Fish_Helm sits the hips down and back; this slides the body the
            // rest of the way back onto the bench), easing in and out so standing up is a move, not a pop
            sit = Util.Damp(sit, helm ? 1 : 0, 9f, dt);
            Model.localPosition = new Vector3(0, 0, -0.17f * sit);
            // Lean (feel spec): forward / back from the walk's acceleration, banked into turns, on a spring with a little
            // overshoot (k 70); max 12° (15° back in a skid), 10° banked; less with the rod out. Squash 0.92 in the
            // jump's crouch, stretch on take-off, squash on landing (a stiff spring, a few frames). The body only: the
            // root and the camera stay put.
            var la = transform.InverseTransformDirection((Vel - lastVel) / dt); lastVel = Vel;
            float lk = helm || Swimming ? 0 : Fish.Holding ? 0.4f : 1;
            float tp = skidT > 0 ? -15f : Mathf.Clamp(la.z * 0.6f, -12f, 12f) * lk, tr = Mathf.Clamp(la.x * 0.4f, -10f, 10f) * lk;
            Spring(ref leanP, ref leanPV, tp, 70f, 0.6f, dt); Spring(ref leanR, ref leanRV, tr, 70f, 0.6f, dt);
            Spring(ref sq, ref sqV, crouchT >= 0 ? -0.08f : 0, 300f, 0.45f, dt); sq = Mathf.Clamp(sq, -0.12f, 0.1f);
            Model.localRotation = Quaternion.Euler(Mathf.Clamp(leanP, -15f, 12f), 0, -Mathf.Clamp(leanR, -10f, 10f));
            Model.localScale = new Vector3(1 - sq * 0.5f, 1 + sq, 1 - sq * 0.5f);
            Anim.Ladder = null; Anim.WaterY = float.NaN;
            if (helm) { Anim.Act = M1Assets.I.Helm; Anim.ActTime = -1; }
            else if (sit > 0.02f && Anim.Act == M1Assets.I.Helm) Anim.Act = null;
            // turning on the spot: the feet step round with it (a little walk-cycle), instead of sliding
            float yawNow = OnBoat && Skiff.I != null ? transform.eulerAngles.y - Skiff.I.transform.eulerAngles.y : transform.eulerAngles.y;
            float turnRate = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yawNow)) / dt; lastYaw = yawNow;
            float animSp = sp;
            if (!helm && !Swimming) animSp = Mathf.Max(sp, Mathf.Clamp01((turnRate - 60f) / 300f) * 0.8f);
            if (skidT > 0) animSp = 0; // feet planted through the skid
            Anim.Flail = wasAir && airT > 0.6f && vy < 0 && !helm && !fallIn; // a real fall: arms going
            if (helm) slot = CharAnim.Slot.Idle;
            else if (Swimming)
            {
                bool stroke = sp > 0.6f;
                slot = stroke ? CharAnim.Slot.Swim : CharAnim.Slot.SwimIdle;
                Model.localPosition = new Vector3(0, Anim.WaterLift(stroke, SwimDepth), 0);
                if (!stroke) Anim.WaterY = Sea.Height(transform.position);
            }
            else slot = Cc.isGrounded || Time.time - lastGround < 0.15f ? CharAnim.Slot.Idle : CharAnim.Slot.Jump;
            // hands: the rod (Fishing sets Grip) or the tiller
            if (helm && Skiff.I.TillerGrip != null)
            {
                // left hand on the tiller's grip, right hand resting on the right knee
                var tg = Skiff.I.TillerGrip;
                Anim.Grip = tg; Anim.GripL = new Vector3(0.0f, 0.01f, 0.0f);
                Anim.GripR = tg.InverseTransformPoint(transform.TransformPoint(new Vector3(0.15f, 0.52f, -0.02f)));
                // elbows down and back; +x cancels the left arm's built-in outward hint, so its elbow stays off the tiller bar
                Anim.GripElbow = new Vector3(0.62f, -0.8f, -0.35f); Anim.GripFingers = new Vector3(0, -0.3f, 1); // left elbow tucked in to his side
            }
            else if (!Fish.Holding || Swimming) Anim.Grip = null;
            // Mixamo clips the game drives over everything: the jump (its crouch fitted into our 0.08 s, its push and tuck
            // over the first 0.25 s of the rise, then held) and stepping off the pier into the sea (Falling Into Pool: the
            // topple and the drop). Their own rise is taken back out of the body: the controller does the rising.
            var a = M1Assets.I;
            if (!Fish.Tossing)
            {
                AnimationClip oc = null;
                if (crouchT >= 0 && a.JumpUp != null) { oc = a.JumpUp; overT = Mathf.Lerp(0, 0.21f, crouchT / 0.08f); }
                else if (jumped && wasAir && a.JumpUp != null) { oc = a.JumpUp; overT = Mathf.Lerp(0.42f, 0.83f, Mathf.Clamp01(airT / 0.25f)); }
                else if (fallIn && wasAir && a.FallIn != null) { oc = a.FallIn; overT = Mathf.Lerp(0.85f, 1.5f, Mathf.Clamp01(airT / 0.4f)); }
                if (oc != null) overClip = oc;
                overW = Mathf.MoveTowards(overW, oc != null ? 1 : 0, dt / (oc != null ? 0.05f : 0.08f));
                Anim.Over = overW > 0 ? overClip : null; Anim.OverTime = overT; Anim.OverW = overW;
                var lift = overClip == a.JumpUp ? a.JumpLift : a.FallLift;
                if (overW > 0 && lift != null && lift.length > 0) Model.localPosition -= Vector3.up * (Mathf.Max(0, lift.Evaluate(overT)) * overW);
            }
            Anim.Tick(slot, animSp);

            // footsteps on the heel strikes (the shared stride: left at 0, right at ~0.5), wood on the deck and pier;
            // a little dust off the feet at a sprint
            float ph = Anim.Phase;
            if (!Swimming && !helm && Cc.isGrounded && animSp > 0.3f && skidT <= 0 && Mathf.FloorToInt(ph * 2) != Mathf.FloorToInt(lastPhase * 2))
            { Step(Mathf.Lerp(0.16f, 0.32f, sp / 5.8f)); if (SprintW > 0.6f) Dust(2, 0.7f); }
            lastPhase = ph;
        }
    }
}
