using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PortBoss
{
    // Owner-authoritative movement (NetworkTransform AuthorityMode.Owner), host-authoritative cargo.
    public class Player : NetworkBehaviour
    {
        public struct In
        {
            public Vector2 move, look;
            public bool sprint, jump, jumpHeld, grab, grabHeld, drop, dropHeld, throwP, push;
            public bool hasWorldMove; public Vector3 worldMove; // bot steering
        }

        public static Player Local;
        public static readonly List<Player> All = new();
        public static System.Func<In> BotInput; // set by the test bot

        public const int MaxStack = 5;
        static readonly Color[] Colors = { Art.Hex("#d8892b"), Art.Hex("#3f86c4"), Art.Hex("#5aa34a"), Art.Hex("#c4473f") };

        public NetworkVariable<Vector2> SwayNet = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Vector2 Sway => IsOwner ? sway : SwayNet.Value;
        public int Carrying { get; private set; }
        public bool Pushing => Trolley.I != null && Trolley.I.PusherId.Value == OwnerClientId;
        public float Yaw => yaw;

        CharacterController cc;
        NetworkTransform nt;
        readonly List<Crate> stack = new(); // server
        Vector3 hv, serverVel, lastServerPos;
        float vy, yaw, pitch = 18f;
        Vector2 sway, swayVel;
        Transform platform; Vector3 platLocal; float platYaw;
        Vector3 camPos;
        float stepDist;
        Vector3 lastStepPos;
        // Jump: ~1.2 m high when held, ~0.6 m on a tap.
        const float JumpSpeed = 7f, RiseGravity = 20f, FallGravity = 38f, JumpGrace = 0.12f;
        float jumpBufferedUntil, groundedUntil;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            nt = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            var c = Colors[(int)(OwnerClientId % (ulong)Colors.Length)];
            model = new GameObject("model").transform;
            model.SetParent(transform, false);
            if (Skin.On && Skin.Pick(Skin.I.Players, 0) != null) BuildWorker(model, (int)OwnerClientId);
            else BuildDockhand(model, c, (int)OwnerClientId);
            if (IsOwner)
            {
                Local = this;
                Teleport(World.Spawn + new Vector3((int)OwnerClientId * 1.2f, 0, 0), 0, false);
                camPos = transform.position + new Vector3(0, 3, -6);
            }
            lastServerPos = transform.position;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (Local == this) Local = null;
            if (IsServer) ReleaseAll(Vector3.up);
        }

        Transform model, legL, legR, armL, armR;
        float walkPhase;

        // Chunky stylised dockhand built from primitives: big head, knit beanie in the player's colour,
        // jumper + navy overalls, thick limbs, rubber boots, mitts. Faces +Z.
        Transform torso, head;
        float throwT = -1, jumpBlend;

        void BuildDockhand(Transform m, Color c, int id)
        {
            var skin = Art.Hex(id % 2 == 0 ? "#d6a27f" : "#b07a55");
            var navy = Art.Hex("#34465a");
            var boots = Art.Hex(id % 3 == 1 ? "#2b2b2b" : "#e0b23a");
            torso = new GameObject("torso").transform; torso.SetParent(m, false); torso.localPosition = new Vector3(0, 0.78f, 0);
            Art.Prim(PrimitiveType.Capsule, torso, new Vector3(0, 0.36f, 0), Quaternion.identity, new Vector3(0.72f, 0.42f, 0.56f), c, false, "jumper");
            Art.Box(torso, new Vector3(0, 0.18f, 0.02f), new Vector3(0.7f, 0.36f, 0.56f), navy, false, "overalls");
            Art.Box(torso, new Vector3(0, 0.42f, 0.26f), new Vector3(0.36f, 0.3f, 0.06f), navy, false, "bib");
            foreach (var x in new[] { -0.13f, 0.13f }) Art.Box(torso, new Vector3(x, 0.62f, 0.24f), new Vector3(0.06f, 0.28f, 0.05f), navy, false, "strap");
            head = new GameObject("head").transform; head.SetParent(torso, false); head.localPosition = new Vector3(0, 0.86f, 0.02f);
            Art.Prim(PrimitiveType.Sphere, head, Vector3.zero, Quaternion.identity, new Vector3(0.5f, 0.48f, 0.48f), skin, false, "face");
            Art.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.02f, 0.24f), Quaternion.identity, new Vector3(0.12f, 0.11f, 0.12f), skin * 0.93f, false, "nose");
            foreach (var x in new[] { -0.1f, 0.1f }) Art.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.06f, 0.21f), Quaternion.identity, new Vector3(0.06f, 0.07f, 0.04f), Art.Hex("#1c1c1c"), false, "eye");
            if (id % 2 == 0) Art.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.15f, 0.12f), Quaternion.identity, new Vector3(0.38f, 0.24f, 0.3f), Art.Hex("#6b4a2e"), false, "beard");
            Art.Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.13f, -0.01f), Quaternion.identity, new Vector3(0.54f, 0.36f, 0.52f), c * 0.85f, false, "beanie");
            Art.Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.1f, -0.01f), Quaternion.identity, new Vector3(0.55f, 0.05f, 0.53f), c * 0.7f, false, "cuff");
            Art.Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.33f, -0.02f), Quaternion.identity, Vector3.one * 0.13f, c * 0.7f, false, "pompom");
            legL = Limb(m, new Vector3(-0.15f, 0.8f, 0), 0.24f, 0.6f, navy, boots, new Vector3(0.28f, 0.22f, 0.38f));
            legR = Limb(m, new Vector3(0.15f, 0.8f, 0), 0.24f, 0.6f, navy, boots, new Vector3(0.28f, 0.22f, 0.38f));
            armL = Limb(torso, new Vector3(-0.42f, 0.58f, 0), 0.17f, 0.56f, c, skin, new Vector3(0.18f, 0.18f, 0.18f));
            armR = Limb(torso, new Vector3(0.42f, 0.58f, 0), 0.17f, 0.56f, c, skin, new Vector3(0.18f, 0.18f, 0.18f));
        }

        // A limb pivots at its top (hip/shoulder) and ends in a boot or a mitt.
        static Transform Limb(Transform parent, Vector3 pivot, float thick, float len, Color c, Color endColor, Vector3 endSize)
        {
            var p = new GameObject("limb").transform;
            p.SetParent(parent, false);
            p.localPosition = pivot;
            Art.Prim(PrimitiveType.Capsule, p, new Vector3(0, -len / 2, 0), Quaternion.identity, new Vector3(thick, len / 2, thick), c, false, "limb_mesh");
            bool boot = endSize.z > endSize.x;
            Art.Prim(boot ? PrimitiveType.Cube : PrimitiveType.Sphere, p, new Vector3(0, -len + (boot ? -0.05f : 0.02f), boot ? 0.06f : 0), Quaternion.identity, endSize, endColor, false, boot ? "boot" : "mitt");
            return p;
        }

        // Toon Harbor dock worker (one outfit per player slot), animated by CharAnim.
        CharAnim anim;
        void BuildWorker(Transform m, int id)
        {
            var g = Instantiate(Skin.Pick(Skin.I.Players, id), m, false);
            g.transform.localRotation = Quaternion.Euler(0, 180, 0); // the pack's people face -Z
            foreach (var col in g.GetComponentsInChildren<Collider>()) Destroy(col);
            float h = Art.Bounds(g).size.y;
            if (h > 0.5f) g.transform.localScale = Vector3.one * Mathf.Clamp(1.8f / h, 0.8f, 1.25f);
            var a = g.GetComponentInChildren<Animator>();
            if (a == null) a = g.AddComponent<Animator>();
            anim = a.gameObject.AddComponent<CharAnim>();
            anim.Init(a, Skin.I);
            anim.Facing = transform;
            anim.Scale = g.transform.localScale.x;
        }

        void AnimateWorker(float speed)
        {
            float dt = Time.deltaTime;
            model.localRotation = Quaternion.identity; model.localPosition = Vector3.zero;
            bool swimming = transform.position.y < Water.Height(transform.position) - SwimDepth + 0.35f;
            bool grounded = Mathf.Abs(transform.position.y - lastAnimY) < 0.05f * Mathf.Max(1f, dt * 60f);
            lastAnimY = transform.position.y;
            Ripples(swimming);
            // Falling in: the body plunges under, then bobs back up (with a second, smaller splash) and starts treading.
            // (Not when climbing down a ladder into the water.)
            bool climbing = Climbing.Value;
            if (swimming && !wasSwimmingAnim && !wasClimbingAnim) { dunkT = 0; resurfaced = false; }
            wasSwimmingAnim = swimming; wasClimbingAnim = climbing;
            if (dunkT >= 0 && (!swimming || (dunkT += dt / 1.1f) >= 1)) dunkT = -1;
            anim.Flail = swimming && OutOfStamina.Value; // going under
            CharAnim.Slot slot;
            anim.Ladder = null; anim.WaterY = float.NaN;
            if (Driving != null) { slot = CharAnim.Slot.Drive; model.localPosition = new Vector3(0, -0.15f, 0); }
            else if (climbing)
            {
                // On a ladder: hands and feet put on the rungs by CharAnim; eased off while stepping on and off.
                slot = CharAnim.Slot.Climb;
                var l = LadderAt(transform.position);
                var line = l.Edge - l.In * ClimbOut; var off = transform.position - line; off.y = 0;
                anim.Ladder = l; anim.LadderW = 1f - Mathf.Clamp01((off.magnitude - 0.1f) / 0.6f);
            }
            else if (knockT >= 0)
            {
                // Knocked over: tip backwards, lie there a moment, scramble up.
                knockT += dt / 1.2f;
                float fall = knockT < 0.25f ? knockT / 0.25f : knockT < 0.7f ? 1f : 1f - (knockT - 0.7f) / 0.3f;
                model.localRotation = Quaternion.Euler(-80f * fall, 0, 8f * fall);
                model.localPosition = new Vector3(0, 0.15f * fall, -0.4f * fall);
                slot = CharAnim.Slot.Idle;
                if (knockT >= 1) knockT = -1;
            }
            else if (anim.Flail) { slot = CharAnim.Slot.SwimIdle; model.localPosition = new Vector3(0, SwimModelY + 0.25f + Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.15f, 0); }
            else if (swimming)
            {
                // Breaststroke when moving (body tipped forward along the surface), treading water when still.
                bool stroke = speed > 0.6f;
                slot = stroke ? CharAnim.Slot.Swim : CharAnim.Slot.SwimIdle;
                swimLean = Mathf.MoveTowards(swimLean, stroke ? 1f : 0f, dt * 3f);
                model.localPosition = new Vector3(0, Mathf.Lerp(SwimModelY, SwimStrokeY, swimLean), 0);
                if (dunkT >= 0)
                {
                    // Down fast (to just over the head), a beat under, then up with a little overshoot; tipped forward on the way in.
                    float u = dunkT, dip = u < 0.25f ? 1 - (1 - u / 0.25f) * (1 - u / 0.25f) : u < 0.4f ? 1f : 1f - Mathf.SmoothStep(0, 1, (u - 0.4f) / 0.45f);
                    float pop = u > 0.8f ? Mathf.Sin((u - 0.8f) / 0.2f * Mathf.PI) * 0.12f : 0f;
                    model.localPosition += new Vector3(0, -1.25f * dip + pop, 0);
                    model.localRotation = Quaternion.Euler(25f * dip, 0, 0);
                    slot = CharAnim.Slot.SwimIdle;
                    stroke = false;
                    if (!resurfaced && u > 0.62f)
                    {
                        resurfaced = true;
                        Fx.Splash(new Vector3(transform.position.x, Water.Height(transform.position), transform.position.z), 0.5f);
                        Sfx.Play("splash", transform.position + Vector3.up, 0.35f, 1.5f);
                    }
                }
                if (!stroke) anim.WaterY = Water.Height(transform.position); // treading: arms down in the water
            }
            else
            {
                slot = !grounded ? CharAnim.Slot.Jump : Pushing ? CharAnim.Slot.Push : CharAnim.Slot.Idle; // Idle = on foot: CharAnim blends the gaits by speed
                // Lean into the run and bank into turns (less with a stack in your arms).
                float turn = Mathf.DeltaAngle(lastAnimYaw, transform.eulerAngles.y) / Mathf.Max(dt, 1e-4f);
                float run = grounded ? Mathf.Clamp01((speed - 2f) / 5f) : 0f, carryK = Carrying > 0 ? 0.4f : 1f;
                lean = Vector2.Lerp(lean, new Vector2(7f * run * carryK, Mathf.Clamp(-turn * speed * 0.0035f, -12f, 12f) * carryK), 1 - Mathf.Exp(-8f * dt));
                model.localRotation = Quaternion.Euler(lean.x, 0, lean.y);
            }
            lastAnimYaw = transform.eulerAngles.y;
            if (!swimming) swimLean = 0;
            if (throwT >= 0 && (throwT += dt / 0.45f) >= 1) throwT = -1;
            // Hands under the bottom crate of the stack. Picking one up, the hands go to it where it lies (it waits a
            // moment there, so we squat to it); after setting the last one down they stay on it until it has landed.
            Crate bottom = null;
            if (Carrying > 0) foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out var cr) && (bottom == null || cr.Slot.Value < bottom.Slot.Value)) bottom = cr;
            if (bottom != null) { held = bottom; letGoT = 0; }
            else if (held != null && ((letGoT += dt) > 0.45f || !held.IsSpawned || (held.transform.position - transform.position).sqrMagnitude > 2.5f * 2.5f)) held = null;
            bool hands = !swimming && !climbing && Driving == null && knockT < 0;
            anim.Box = hands && held != null ? held.Vis : null;
            anim.BoxHalf = Crate.Size / 2;
            anim.BoxW = letGoT < 0.25f ? 1f : 1f - (letGoT - 0.25f) / 0.2f;
            // Pushing: both hands on the trolley's grip.
            var tr = Trolley.I;
            anim.Grip = hands && Pushing && tr != null ? tr.transform : null;
            anim.GripL = Trolley.GripL; anim.GripR = Trolley.GripR;
            anim.Throw01 = throwT;
            anim.Tick(slot, speed);
        }
        Crate held; float letGoT;
        // The UAL swim clips sit the body ~1 m lower than standing; lifts measured with -pb-swimtest's "swim pose" line.
        const float SwimModelY = 1.24f;  // treading: head out, shoulders at the surface
        const float SwimStrokeY = 1.4f;  // stroke (lies flat): back at the surface, head just out
        float swimLean, dunkT = -1, lastAnimYaw;
        Vector2 lean;
        bool resurfaced;

        static Material ringMat;
        static Material RingMat()
        {
            if (ringMat != null) return ringMat;
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.12f)));
                }
            t.Apply();
            ringMat = new Material(Art.Particle); ringMat.SetTexture("_BaseMap", t);
            return ringMat;
        }

        // Rings spreading out round a swimmer.
        ParticleSystem ripples;
        void Ripples(bool on)
        {
            if (ripples == null)
            {
                if (!on) return;
                var g = new GameObject("ripples"); g.transform.SetParent(transform, false);
                ripples = g.AddComponent<ParticleSystem>(); ripples.Stop();
                var m = ripples.main; m.startLifetime = 1.8f; m.startSpeed = 0; m.startSize = 0.9f; m.maxParticles = 12;
                m.startColor = new Color(0.92f, 0.96f, 1f, 0.45f); m.simulationSpace = ParticleSystemSimulationSpace.World;
                var e = ripples.emission; e.rateOverTime = 2.2f;
                var sh = ripples.shape; sh.enabled = false;
                var sz = ripples.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 3.2f));
                var col = ripples.colorOverLifetime; col.enabled = true;
                var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.55f, 0), new GradientAlphaKey(0, 1) });
                col.color = gr;
                var r = g.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; r.sharedMaterial = RingMat();
            }
            ripples.transform.position = new Vector3(transform.position.x, Water.Height(transform.position) + 0.04f, transform.position.z);
            if (on && !ripples.isPlaying) ripples.Play(); else if (!on && ripples.isPlaying) ripples.Stop();
        }
        bool wasSwimmingAnim, wasClimbingAnim; // last frame (falling in starts the dunk)

        void Animate(float speed)
        {
            if (anim != null) { AnimateWorker(speed); return; }
            float dt = Time.deltaTime;
            if (Driving != null)
            {
                // Seated at the controls.
                legL.localRotation = legR.localRotation = Quaternion.Euler(-80f, 0, 0);
                armL.localRotation = armR.localRotation = Quaternion.Euler(-55f, 0, 0);
                torso.localRotation = Quaternion.identity;
                model.localPosition = new Vector3(0, -0.15f, 0);
                return;
            }
            if (knockT >= 0)
            {
                // Knocked over: tip backwards, lie there a moment, scramble up.
                knockT += dt / 1.2f;
                float fall = knockT < 0.25f ? knockT / 0.25f : knockT < 0.7f ? 1f : 1f - (knockT - 0.7f) / 0.3f;
                torso.localRotation = Quaternion.Euler(-75f * fall, 0, 10f * fall);
                legL.localRotation = Quaternion.Euler(-40f * fall, 0, 0); legR.localRotation = Quaternion.Euler(-25f * fall, 0, 0);
                armL.localRotation = Quaternion.Euler(-150f * fall, 0, -30f * fall); armR.localRotation = Quaternion.Euler(-150f * fall, 0, 30f * fall);
                model.localPosition = new Vector3(0, -0.45f * fall, -0.3f * fall);
                if (knockT >= 1) knockT = -1;
                return;
            }
            if (transform.position.y < Water.Height(transform.position) - SwimDepth + 0.35f)
            {
                // Swimming: body tipped forward, arms paddling, legs kicking.
                swimPhase += dt * (3f + speed * 1.5f);
                torso.localRotation = Quaternion.Euler(50f, 0, Mathf.Sin(swimPhase) * 6f);
                armL.localRotation = Quaternion.Euler(-120f + Mathf.Sin(swimPhase) * 60f, 0, -25f);
                armR.localRotation = Quaternion.Euler(-120f - Mathf.Sin(swimPhase) * 60f, 0, 25f);
                legL.localRotation = Quaternion.Euler(30f + Mathf.Sin(swimPhase * 2.2f) * 20f, 0, 0);
                legR.localRotation = Quaternion.Euler(30f - Mathf.Sin(swimPhase * 2.2f) * 20f, 0, 0);
                model.localPosition = new Vector3(0, 0.25f, 0);
                head.localRotation = Quaternion.Euler(-40f, 0, 0);
                return;
            }
            float k = Mathf.Clamp01(speed / 4.5f), run = Mathf.Clamp01((speed - 4.8f) / 2.5f);
            walkPhase += speed * dt * 2.1f;
            float sw = Mathf.Sin(walkPhase) * k * (36f + 14f * run);
            bool grounded = Mathf.Abs(transform.position.y - lastAnimY) < 0.05f * Mathf.Max(1f, dt * 60f);
            lastAnimY = transform.position.y;
            jumpBlend = Mathf.MoveTowards(jumpBlend, grounded ? 0 : 1, dt * 6f);
            legL.localRotation = Quaternion.Euler(Mathf.Lerp(sw, -35f, jumpBlend), 0, 0);
            legR.localRotation = Quaternion.Euler(Mathf.Lerp(-sw, 15f, jumpBlend), 0, 0);
            bool laden = Carrying > 0 || Pushing;
            if (throwT >= 0)
            {
                // Overhand throw: right arm winds back, whips forward.
                throwT += dt / 0.45f;
                float a = throwT < 0.35f ? Mathf.Lerp(-80f, -170f, throwT / 0.35f) : Mathf.Lerp(-170f, -20f, (throwT - 0.35f) / 0.65f);
                armR.localRotation = Quaternion.Euler(a, 0, 0);
                armL.localRotation = Quaternion.Euler(laden ? -80f : 10f, 0, 0);
                if (throwT >= 1) throwT = -1;
            }
            else if (laden) armL.localRotation = armR.localRotation = Quaternion.Euler(-85f + Mathf.Sin(walkPhase) * 4f * k, 0, 0);
            else
            {
                float flail = jumpBlend * 70f;
                armL.localRotation = Quaternion.Euler(-sw * 0.9f - flail, 0, -flail * 0.3f);
                armR.localRotation = Quaternion.Euler(sw * 0.9f - flail, 0, flail * 0.3f);
            }
            // Body: bounce on each step, lean into speed, breathe when idle; the head lags a touch.
            float breathe = (1 - k) * Mathf.Sin(Time.time * 2.2f) * 0.012f;
            model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.06f * k, 0);
            torso.localRotation = Quaternion.Euler(4f * k + 8f * run + (laden ? -4f : 0f), 0, Mathf.Sin(walkPhase) * 3f * k);
            torso.localScale = new Vector3(1, 1 + breathe, 1);
            head.localRotation = Quaternion.Euler(-3f * k + Mathf.Sin(Time.time * 0.7f) * 3f * (1 - k), Mathf.Sin(Time.time * 0.45f) * 12f * (1 - k), 0);
        }
        float lastAnimY, swimPhase;

        void Teleport(Vector3 pos, float newYaw, bool viaNet = true)
        {
            EndClimb();
            yaw = newYaw; hv = Vector3.zero; vy = 0; platform = null;
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, newYaw, 0));
            cc.enabled = true;
            if (viaNet && nt != null && nt.CanCommitToTransform) nt.Teleport(pos, transform.rotation, transform.localScale);
        }

        // Test hook (host only): put n fresh crates straight into this player's hands.
        public void DebugGiveCrates(int n)
        {
            for (int i = 0; i < n && stack.Count < MaxStack; i++)
            {
                var c = PortGame.I.SpawnCrate(transform.position + Vector3.up * 3, 1f);
                c.Attach(NetworkObject, HoldPos(stack.Count), stack.Count);
                stack.Add(c);
            }
        }

        public void DebugTeleport(Vector3 p, float yawDeg) => Teleport(p, yawDeg);

        static In ReadInput()
        {
            if (BotInput != null) return BotInput();
            var k = Keyboard.current; var m = Mouse.current; var pad = Gamepad.current;
            var i = new In();
            if (k != null)
            {
                bool Down(string id) { var key = Settings.KeyFor(id); return key != Key.None && k[key].wasPressedThisFrame; }
                i.move = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                var sprintKey = Settings.KeyFor("sprint");
                i.sprint = sprintKey != Key.None && k[sprintKey].isPressed;
                bool Held(string id) { var key = Settings.KeyFor(id); return key != Key.None && k[key].isPressed; }
                i.jumpHeld = Held("jump"); i.grabHeld = Held("grab"); i.dropHeld = Held("drop");
                i.jump = Down("jump"); i.grab = Down("grab"); i.drop = Down("drop"); i.push = Down("push"); i.throwP = Down("throw");
            }
            if (m != null)
            {
                i.look = m.delta.ReadValue() * (0.1f * Settings.Sensitivity);
                i.throwP |= m.leftButton.wasPressedThisFrame;
                i.grab |= m.rightButton.wasPressedThisFrame; i.grabHeld |= m.rightButton.isPressed;
            }
            if (pad != null)
            {
                var ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f) i.move = ls;
                i.look += pad.rightStick.ReadValue() * (220f * Settings.PadSensitivity * Time.deltaTime);
                i.jump |= pad.buttonSouth.wasPressedThisFrame; i.jumpHeld |= pad.buttonSouth.isPressed; i.grab |= pad.buttonWest.wasPressedThisFrame;
                i.grabHeld |= pad.buttonWest.isPressed; i.dropHeld |= pad.buttonEast.isPressed;
                i.drop |= pad.buttonEast.wasPressedThisFrame; i.throwP |= pad.rightTrigger.wasPressedThisFrame;
                i.sprint |= pad.leftShoulder.isPressed || pad.leftStickButton.isPressed; i.push |= pad.rightShoulder.wasPressedThisFrame;
            }
            if (Settings.InvertY) i.look.y = -i.look.y;
            return i;
        }

        void Update()
        {
            // Every peer counts what this player is carrying (children crates), for visuals and speed.
            int n = 0;
            foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out _)) n++;
            if (IsOwner && n > Carrying) Stats.Add("carried", n - Carrying);
            Carrying = n;

            // Footsteps on every peer, from how far the body moved on the ground.
            var flat = transform.position - lastStepPos; flat.y = 0;
            stepDist += Mathf.Min(flat.magnitude, 1f);
            if (model != null) Animate(Mathf.Min(flat.magnitude, 1f) / Mathf.Max(Time.deltaTime, 1e-4f));
            lastStepPos = transform.position;
            if (stepDist > 1.7f && Mathf.Abs(transform.position.y - World.PierTop) < 1.2f) { stepDist = 0; Sfx.Play("step" + Random.Range(0, 5), transform.position, IsOwner ? 0.35f : 0.25f); }

            if (!IsOwner || !IsSpawned) return;
            if (!swimming && Driving == null && flat.magnitude < 1f) Stats.Walk(flat.magnitude);
            float dt = Time.deltaTime;
            if (Sit()) return;
            var input = GameUI.InputBlocked || Time.time < stunnedUntil ? new In() : ReadInput();

            yaw += input.look.x;
            pitch = Mathf.Clamp(pitch - input.look.y, -25f, 65f);

            // Ride the boat deck / anything we stand on that moves.
            if (platform != null)
            {
                var target = platform.TransformPoint(platLocal);
                var d = target - transform.position;
                if (d.sqrMagnitude < 1f) cc.Move(d);
                float dy = Mathf.DeltaAngle(platYaw, platform.eulerAngles.y);
                yaw += dy;
                transform.Rotate(0, dy, 0);
            }

            Vector3 wish = input.hasWorldMove ? input.worldMove : Quaternion.Euler(0, yaw, 0) * new Vector3(input.move.x, 0, input.move.y);
            wish.y = 0;
            wish = Vector3.ClampMagnitude(wish, 1f);
            bool sprint = input.sprint && wish.sqrMagnitude > 0.1f;

            if (climbAt >= 0) { Climb(dt); return; }
            float surface = Water.Height(transform.position);
            if (!swimming && transform.position.y < surface - SwimDepth + 0.3f && vy <= 0 && !Shallow()) EnterWater();
            if (swimming)
            {
                // Swimming costs stamina (sprinting more); tired swimmers slow down; out of stamina, they go under
                // and the harbour master fishes them out onto the nearest ladder.
                bool spent = exhaustedAt >= 0;
                if (!spent)
                {
                    Stamina -= dt / (sprint ? SprintSwimStamina : SwimStamina);
                    if (Stamina <= 0) { Stamina = 0; exhaustedAt = Time.time; OutOfStamina.Value = true; if (Towing) TowRpc(default, false); GameUI.Toast("Out of stamina...!"); Sfx.Play("splash", transform.position, 0.6f, 0.7f); }
                }
                float tired = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Stamina / 0.25f));
                hv = Vector3.MoveTowards(hv, spent ? Vector3.zero : wish * ((sprint ? 3.1f : 2.2f) * tired), 6f * dt);
                float sink = spent ? Mathf.Min(1.3f, (Time.time - exhaustedAt) * 0.9f) : 0f;
                float dy = (surface - SwimDepth - sink - transform.position.y) * Mathf.Min(1f, 5f * dt);
                cc.Move(hv * dt + Vector3.up * dy);
                vy = 0;
                if (Shallow()) LeaveWater(); // slipway / shore: walk out
                else if (spent && Time.time - exhaustedAt > 1.8f) FishedOut();
            }
            else
            {
                Stamina = Mathf.Min(1f, Stamina + dt / StaminaBack);
                if (flung) Fly(dt);
                else
                {
                    float speed = (sprint ? 7.4f : 4.6f) * (1f - 0.07f * Carrying) * (Pushing ? 0.85f : 1f);
                    hv = Vector3.MoveTowards(hv, wish * speed, (cc.isGrounded ? 30f : 18f) * dt);
                    // Jump: a little grace after walking off an edge, and a press just before landing still counts.
                    if (input.jump) jumpBufferedUntil = Time.time + JumpGrace;
                    if (cc.isGrounded)
                    {
                        if (vy < -4f) Sfx.Play("land", transform.position, Mathf.Clamp01(-vy / 12f), Random.Range(0.9f, 1.1f));
                        vy = -2f; groundedUntil = Time.time + JumpGrace;
                    }
                    if (Time.time < jumpBufferedUntil && Time.time < groundedUntil && !Pushing)
                    {
                        vy = JumpSpeed * (1f - 0.06f * Carrying); // a full stack is heavy
                        jumpBufferedUntil = groundedUntil = 0;
                        Sfx.Play("jump", transform.position, 0.55f);
                    }
                    // Snappy arc: floaty on the way up while the button is held, quick on the way down (and on a short tap).
                    vy -= (vy > 0 && input.jumpHeld ? RiseGravity : FallGravity) * dt;
                    var wind = Storm.WindDir * (1.1f * Storm.Gust);
                    if (Pushing) { hv = TrolleyClear(hv, dt); wind = TrolleyClear(wind, dt); }
                    cc.Move((hv + Vector3.up * vy + wind) * dt);
                }
            }

            if (wish.sqrMagnitude > 0.01f && !flung)
            {
                var face = Quaternion.LookRotation(wish);
                var turned = Quaternion.RotateTowards(transform.rotation, face, (Pushing ? 160f : 720f) * dt);
                // Pushing: the trolley swings round in front of us, so we only turn where it has room.
                var t = Trolley.I;
                if (!Pushing || t == null || t.Fits(this, transform.position, turned.eulerAngles.y) || !t.Fits(this, transform.position, transform.eulerAngles.y))
                    transform.rotation = turned;
            }
            if (Pushing != wasPushing) { wasPushing = Pushing; PushChanged(Pushing); }

            // Ground probe for moving platforms.
            platform = null;
            if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.3f, Vector3.down, out var hit, 0.45f, ~0, QueryTriggerInteraction.Ignore))
            {
                var b = hit.collider.GetComponentInParent<Boat>();
                var gp = hit.collider.GetComponentInParent<Gangplank>();
                if (b != null || gp != null)
                {
                    platform = b != null ? b.transform : gp.transform;
                    platLocal = platform.InverseTransformPoint(transform.position);
                    platYaw = platform.eulerAngles.y;
                }
            }

            BoatPush();
            UpdateSway(dt, sprint);
            if (swimming) SwimInteract(input); else Interact(input);

            // Safety net: fell through the world somehow.
            if (transform.position.y < -12f) { swimming = false; Teleport(World.Spawn, 0); }
        }

        // Boats are solid even when they move into you (a kinematic hull doesn't push a CharacterController by itself):
        // a swimmer inside a hull's footprint, or anyone in the way of a boat under way, is moved out to the nearer
        // side, and if the boat is moving it shoves them: bowled over on the quay, pushed aside with a splash in the water.
        float boatBumpAt;
        void BoatPush()
        {
            foreach (var b in Boat.All)
            {
                if (b == null || platform == b.transform || (b.transform.position - transform.position).sqrMagnitude > 18f * 18f) continue;
                // A boat at its berth only bobs: leave people alone (they'd be nudged off the gangplank).
                if (!swimming && b.Velocity.sqrMagnitude < 1f) continue;
                var lp = b.transform.InverseTransformPoint(transform.position);
                if (lp.y > Boat.DeckY - 0.1f || lp.y < -4f) continue;
                // The footprint: the hull's width, narrowing to the bow.
                const float r = 0.45f;
                float half = Boat.HalfW * (lp.z > Boat.HalfL - 1.5f ? Mathf.Clamp01(1 - (lp.z - (Boat.HalfL - 1.5f)) / 3.2f) : 1f) + r;
                if (Mathf.Abs(lp.x) >= half || lp.z < -Boat.HalfL - r || lp.z > Boat.HalfL + 1.7f + r) continue;
                float side = lp.x == 0 ? 1 : Mathf.Sign(lp.x);
                var to = b.transform.TransformPoint(new Vector3(side * (half + 0.05f), lp.y, lp.z));
                var push = to - transform.position; push.y = 0;
                cc.enabled = false; transform.position += push; cc.enabled = true;
                var bv = b.Velocity; bv.y = 0;
                if (bv.magnitude < 1f || Time.time < boatBumpAt) continue;
                boatBumpAt = Time.time + 2f;
                var shove = (b.transform.right * side * 1.2f + bv.normalized * 0.5f).normalized;
                Debug.Log($"[PB] boat bump: boat at {bv.magnitude:F1} m/s, swimming={swimming}");
                if (swimming)
                {
                    hv = shove * 5f;
                    Sfx.Play("splash", transform.position + Vector3.up, 0.8f);
                    Fx.Splash(new Vector3(transform.position.x, Water.Height(transform.position), transform.position.z), 1f);
                    Shake(0.4f);
                    GameUI.Toast(PortGame.Pick("Bonk! Boats don't brake, boss.", "Shoved aside by a boat. Rude.", "Mind the hull!"));
                }
                else KnockedRpc(shove * 7f + Vector3.up, Down.Boat);
            }
        }

        // ---------- forklift ----------
        public Forklift Driving => Forklift.I != null && Forklift.I.DriverId.Value == OwnerClientId ? Forklift.I : null;
        bool wasDriving;

        // Owner: while driving, ride in the seat (the forklift reads the controls). Returns true while seated.
        bool Sit()
        {
            var f = Driving;
            if (f != null)
            {
                if (!wasDriving) { wasDriving = true; cc.enabled = false; }
                transform.SetPositionAndRotation(f.Seat + Vector3.down * 0.95f, Quaternion.Euler(0, f.transform.eulerAngles.y, 0));
                yaw = Mathf.LerpAngle(yaw, f.transform.eulerAngles.y, 1 - Mathf.Exp(-2f * Time.deltaTime));
                var look = GameUI.InputBlocked || Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * (0.1f * Settings.Sensitivity);
                pitch = Mathf.Clamp(pitch - (Settings.InvertY ? -look.y : look.y), -25f, 65f);
                return true;
            }
            if (wasDriving)
            {
                // Hop out beside the forklift.
                wasDriving = false;
                var fl = Forklift.I;
                var p = fl != null ? fl.transform.TransformPoint(new Vector3(-1.4f, 0.2f, -0.3f)) : transform.position;
                Teleport(p, transform.eulerAngles.y);
            }
            return false;
        }

        // ---------- knocked over ----------
        float stunnedUntil = -1, knockT = -1;
        public bool Stunned => Time.time < stunnedUntil;

        enum Down : byte { Crate, Shove, Boat, Rescued }

        // Server: a flying crate hit this player.
        public void Knock(Vector3 crateVel)
        {
            var dir = crateVel; dir.y = 0;
            KnockedRpc(dir.normalized * Mathf.Min(6f, dir.magnitude * 0.5f), Down.Crate);
        }

        // Shove: a crewmate within arm's reach in front of us (co-op mischief; they drop whatever they carry).
        float shoveReady;
        public Player ShoveTarget()
        {
            if (swimming || Driving != null || Pushing) return null;
            foreach (var p in All)
            {
                if (p == this || p.swimming || p.Driving != null) continue;
                var d = p.transform.position - transform.position; d.y = 0;
                if (d.magnitude < 1.8f && Vector3.Dot(d.normalized, transform.forward) > 0.4f) return p;
            }
            return null;
        }

        public bool DebugShove() { var v = ShoveTarget(); if (v != null) ShoveRpc(v.NetworkObject); return v != null; }

        [Rpc(SendTo.Server)]
        void ShoveRpc(NetworkObjectReference who)
        {
            if (!who.TryGet(out var no) || !no.TryGetComponent(out Player p) || Vector3.Distance(p.transform.position, transform.position) > 2.6f) return;
            var d = p.transform.position - transform.position; d.y = 0;
            Debug.Log($"[PB] shove: player {OwnerClientId} shoved player {p.OwnerClientId}");
            p.KnockedRpc(d.normalized * 5f, Down.Shove);
        }

        [Rpc(SendTo.Everyone)]
        void KnockedRpc(Vector3 push, Down why)
        {
            knockT = 0;
            Sfx.Play("thud", transform.position + Vector3.up, 1f, 0.7f);
            if (!IsOwner) return;
            Debug.Log($"[PB] knocked down: {why}");
            Stats.Add(why == Down.Rescued ? "fishedOut" : "knocked");
            stunnedUntil = Time.time + (why == Down.Rescued ? 1.6f : 1.2f);
            hv = push; vy = why == Down.Rescued ? 0f : 3.5f;
            if (Carrying > 0) DropAllRpc();
            if (Pushing) PushRpc(false);
            Shake(why == Down.Boat ? 0.9f : 0.6f);
            GameUI.Toast(why switch
            {
                Down.Shove => PortGame.Pick("Hey! Hands off!", "Shoved! Very professional.", "Oi! Watch it!"),
                Down.Boat => PortGame.Pick("Bonk! Boats don't brake, boss.", "Shoved by a boat. Rude.", "Overboard! That boat wasn't stopping."),
                Down.Rescued => PortGame.Pick("Out of stamina! The harbour master hauls you out.", "Fished out like a mackerel. Catch your breath, boss."),
                _ => PortGame.Pick("Oof! Right in the crate.", "Ow! Who threw that?!", "Down you go!"),
            });
        }

        // ---------- swimming ----------
        public const float SwimDepth = 1.3f; // feet this far below the surface: head + shoulders out
        public bool Swimming => swimming;
        public bool Towing => TowedId.Value != 0;
        public NetworkVariable<ulong> TowedId = new(0); // server writes: the floating crate being towed
        bool swimming;
        public NetworkVariable<bool> Climbing = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Ground close enough to stand on? Measured against the mean sea level so waves don't flip it back and forth.
        bool Shallow() => Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var gh, 2.8f, ~0, QueryTriggerInteraction.Ignore)
            && gh.point.y > Water.Level - 1.1f && gh.collider.GetComponentInParent<Crate>() == null;

        void EnterWater()
        {
            swimming = true; flung = false;
            Stats.Add("swims");
            Sfx.Play("splash", transform.position + Vector3.up, 1f);
            Fx.Splash(new Vector3(transform.position.x, Water.Height(transform.position), transform.position.z), 1.2f);
            Shake(0.25f);
            if (Carrying > 0) DropAllRpc();
            if (Pushing) PushRpc(false);
            GameUI.Toast("SPLOSH! Swim to a ladder (E) or the slipway.");
            GameUI.Punch(transform.position, 0.9f);
        }

        void LeaveWater()
        {
            swimming = false;
            exhaustedAt = -1; OutOfStamina.Value = false;
            if (Towing) TowRpc(default, false);
        }

        // ---------- swimming stamina ----------
        public const float SwimStamina = 24f, SprintSwimStamina = 13f, StaminaBack = 4f; // seconds to empty / to refill on land
        public float Stamina { get; private set; } = 1f; // owner
        public NetworkVariable<bool> OutOfStamina = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        float exhaustedAt = -1;

        // Out of stamina: the harbour master hauls you to the nearest ladder; you drag yourself up it, then lie on the
        // deck a moment catching your breath.
        void FishedOut()
        {
            var l = LadderAt(transform.position);
            Debug.Log($"[PB] fished out at {transform.position:F1} -> ladder at {l.Top:F1}");
            Stamina = 0.35f;
            Teleport(l.Bottom, yaw);
            Sfx.Play("splash", l.Bottom, 0.7f, 1.3f);
            StartClimb(l, false, true);
        }

        public static World.Ladder LadderAt(Vector3 p)
        {
            var best = World.Ladders[0]; float bd = float.MaxValue;
            foreach (var l in World.Ladders) { var d = l.Edge - p; d.y = 0; if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = l; } }
            return best;
        }

        // On the deck at the top of a ladder (E climbs down to the water, e.g. to fetch a floating crate).
        public World.Ladder? LadderTopNear()
        {
            foreach (var l in World.Ladders)
            {
                var d = l.Top - transform.position;
                if (Mathf.Abs(d.y) < 0.6f && new Vector2(d.x, d.z).sqrMagnitude < 1.1f * 1.1f) return l;
            }
            return null;
        }

        public World.Ladder? NearLadder()
        {
            foreach (var l in World.Ladders)
            {
                var d = l.Bottom - transform.position; d.y = 0;
                if (d.sqrMagnitude < 1.7f * 1.7f) return l;
            }
            return null;
        }

        public Crate FloatingCrateNear()
        {
            Crate best = null; float bd = 2.2f;
            foreach (var c in Crate.All)
            {
                if (c.Attached.Value) continue;
                var p = c.transform.position;
                if (p.y > Water.Height(p) + 0.5f) continue;
                float d = Vector3.Distance(p, transform.position + Vector3.up * SwimDepth);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        void SwimInteract(In input)
        {
            if (input.grab)
            {
                var l = NearLadder();
                if (l != null)
                {
                    if (Towing) TowRpc(default, false);
                    StartClimb(l.Value, false);
                    Sfx.Play("splash", transform.position, 0.5f, 1.4f);
                }
                else if (Towing) TowRpc(default, false);
                else { var c = FloatingCrateNear(); if (c != null) TowRpc(c.NetworkObject, true); }
            }
            if (input.drop && Towing) TowRpc(default, false);
        }

        // Climbing a ladder (owner): along a path at the speed the climb animation steps at (CharAnim puts hands and
        // feet on the rungs from our height). Up: to the ladder, up the rungs, over onto the deck. Down: the reverse,
        // ending in the water.
        const float ClimbSpeed = 1.5f, ClimbOut = 0.42f; // m/s; how far out from the quay face the body hangs
        readonly List<Vector3> climbPath = new();
        float climbAt = -1; // metres along climbPath, < 0 = not climbing
        bool climbDown, climbRescued;

        void StartClimb(World.Ladder l, bool down, bool rescued = false)
        {
            var line = l.Edge - l.In * ClimbOut;
            Vector3 foot = new(line.x, l.Bottom.y, line.z), lip = new(line.x, l.TopY + 0.05f, line.z);
            climbPath.Clear();
            climbPath.Add(transform.position);
            if (down) { climbPath.Add(lip); climbPath.Add(foot); }
            else { climbPath.Add(foot); climbPath.Add(lip); climbPath.Add(l.Top); }
            climbAt = 0; climbDown = down; climbRescued = rescued;
            Climbing.Value = true;
            if (Carrying > 0) DropAllRpc();
            cc.enabled = false; hv = Vector3.zero; vy = 0;
            transform.rotation = Quaternion.LookRotation(l.In);
        }

        void Climb(float dt)
        {
            climbAt += dt * ClimbSpeed * (climbRescued ? 0.7f : 1f);
            float d = climbAt; int i = 0;
            while (i < climbPath.Count - 2 && d > Vector3.Distance(climbPath[i], climbPath[i + 1])) { d -= Vector3.Distance(climbPath[i], climbPath[i + 1]); i++; }
            var seg = climbPath[i + 1] - climbPath[i];
            transform.position = climbPath[i] + seg.normalized * Mathf.Min(d, seg.magnitude);
            if (i < climbPath.Count - 2 || d < seg.magnitude) return;
            EndClimb();
            if (climbDown) { swimming = true; Stats.Add("swims"); return; }
            swimming = false; exhaustedAt = -1; OutOfStamina.Value = false;
            if (climbRescued) KnockedRpc(Vector3.zero, Down.Rescued);
        }

        void EndClimb()
        {
            if (climbAt < 0) return;
            climbAt = -1; Climbing.Value = false;
            vy = 0; hv = Vector3.zero;
            cc.enabled = true;
        }

        [Rpc(SendTo.Server)]
        void TieRpc(NetworkObjectReference r)
        {
            if (!r.TryGet(out var no) || !no.TryGetComponent<Crate>(out var c) || c.Attached.Value || c.InSea) return;
            if (Vector3.Distance(c.transform.position, transform.position) > 3f) return;
            c.Tied.Value = !c.Tied.Value;
            FxRpc("grab", c.transform.position);
        }

        [Rpc(SendTo.Server)]
        void TowRpc(NetworkObjectReference r, bool on)
        {
            if (!on) { TowedId.Value = 0; return; }
            if (!r.TryGet(out var no) || !no.TryGetComponent<Crate>(out var c) || c.Attached.Value) return;
            if (Vector3.Distance(c.transform.position, transform.position) > 3.5f) return;
            TowedId.Value = no.NetworkObjectId;
        }

        public const float TeeterAt = 0.55f;
        public float Teeter => Sway.magnitude;
        bool creaked;

        // Balance model (tuned offline, see docs/wobble.md): starts/stops/turns shove the stack (more the taller it is),
        // height makes it unstable, your balance springs it back. Lean 1.0 = topple. Counter-steer or stop to save it.
        void UpdateSway(float dt, bool sprint)
        {
            var accLocal = transform.InverseTransformDirection((hv - lastHv) / Mathf.Max(dt, 1e-4f));
            lastHv = hv;
            int n = Carrying;
            if (n < 2) { sway = Vector2.MoveTowards(sway, Vector2.zero, dt * 2); swayVel = Vector2.zero; creaked = false; SyncSway(); return; }
            float inertia = 0.1f * Mathf.Pow(n - 1, 1.2f);
            float k = 16f - (n - 1) * 2.9f - (sprint ? 3.5f : 0f);
            float bob = 0.05f * n * Mathf.Sin(walkPhase) * Mathf.Clamp01(hv.magnitude / 7.4f) * 6f
                + Storm.Level * n * 0.5f * Mathf.Sin(Time.time * 2.3f); // storm gusts rock the stack
            var acc = new Vector2(-accLocal.x * inertia + bob, -accLocal.z * inertia) - sway * k - swayVel * 3.2f;
            swayVel += acc * dt;
            sway += swayVel * dt;

            if (sway.magnitude > TeeterAt && !creaked) { creaked = true; Sfx.Play("creak", transform.position + Vector3.up * 2.5f, 0.8f); }
            if (sway.magnitude < TeeterAt * 0.7f) creaked = false;
            if (sway.magnitude > 1f)
            {
                var dir = transform.TransformDirection(new Vector3(sway.x, 0, sway.y)).normalized;
                ToppleRpc(dir);
                GameUI.Toast("Whoa! The stack toppled.");
                Shake(0.5f);
                sway *= 0.3f; swayVel *= 0.2f;
            }
            SyncSway();
        }
        Vector3 lastHv;

        void SyncSway()
        {
            if ((SwayNet.Value - sway).sqrMagnitude > 0.0009f) SwayNet.Value = sway;
        }

        public bool NearBuyer => Vector3.Distance(transform.position, World.BuyerPos) < World.BuyerReach;

        // Crates the buyer can see: what I carry (top first) and the trolley's load if it's parked by the stall.
        public List<Crate> SellableCrates()
        {
            var list = new List<Crate>();
            foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out var cr)) list.Add(cr);
            list.Sort((a, b) => b.Slot.Value.CompareTo(a.Slot.Value));
            var t = Trolley.I;
            if (t != null && t.NearBuyer)
                foreach (Transform c in t.transform) if (c.TryGetComponent<Crate>(out var cr)) list.Add(cr);
            foreach (var pa in Pallet.All)
                if (pa.NearBuyer)
                    foreach (Transform c in pa.transform) if (c.TryGetComponent<Crate>(out var cr)) list.Add(cr);
            return list;
        }

        public void RequestSell(Crate c) => SellRpc(c.NetworkObject);
        public void RequestSellAll() => SellAllRpc();

        [Rpc(SendTo.Server)]
        void SellRpc(NetworkObjectReference r)
        {
            if (!r.TryGet(out var no) || !no.TryGetComponent<Crate>(out var c) || !NearBuyer || PortGame.I == null) return;
            if (TakeForSale(c)) PortGame.I.Sell(new List<Crate> { c });
        }

        // Everything the buyer can see goes in one sale: one till ring, one popup with the total.
        [Rpc(SendTo.Server)]
        void SellAllRpc()
        {
            if (!NearBuyer || PortGame.I == null) return;
            var sold = SellableCrates().FindAll(TakeForSale);
            if (sold.Count > 0) PortGame.I.Sell(sold);
        }

        // Server: lift a crate off whatever holds it (hands, trolley or pallet by the stall), ready to sell.
        bool TakeForSale(Crate c)
        {
            var parent = c.transform.parent;
            if (parent == transform) { stack.Remove(c); Restack(); return true; }
            if (parent != null && parent.TryGetComponent<Trolley>(out var t) && t.NearBuyer) { t.Remove(c); return true; }
            if (parent != null && parent.TryGetComponent<Pallet>(out var pa) && pa.NearBuyer) { pa.Remove(c); return true; }
            return false;
        }

        // Close the gap after a crate from the middle of the stack is sold.
        void Restack()
        {
            stack.RemoveAll(x => x == null || !x.IsSpawned || x.transform.parent != transform);
            for (int i = 0; i < stack.Count; i++) { stack[i].transform.localPosition = HoldPos(i); stack[i].Slot.Value = (sbyte)i; }
        }

        void Interact(In input)
        {
            if (input.grab && NearBuyer && !Pushing)
            {
                if (SellableCrates().Count > 0) GameUI.OpenBuyer();
                else GameUI.Toast("\"Bring me some fish, then we'll talk.\"");
                return;
            }
            var fk = Forklift.I;
            if (input.grab && Carrying == 0 && !Pushing && fk != null && fk.Free && Vector3.Distance(fk.Seat, transform.position + Vector3.up) < 2.4f)
            {
                fk.EnterRpc();
                return;
            }
            // E picks a crate up, Q puts one down; hold either to keep going (stack a pile, empty the stack).
            if (Repeat(input.grab, input.grabHeld, ref grabNextAt) && Carrying < MaxStack && !Pushing)
            {
                var c = FindCrate();
                if (c != null) GrabRpc(c.NetworkObject);
                else if (input.grab && Carrying == 0 && LadderTopNear() is World.Ladder l) StartClimb(l, true);
            }
            if (Repeat(input.drop, input.dropHeld, ref dropNextAt) && Carrying > 0) PutDownRpc();
            if (input.throwP && Carrying > 0)
            {
                var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                if (Vector3.Dot(dir, transform.forward) < 0.3f) dir = transform.forward;
                ThrowRpc(dir); Stats.Add("throws");
                throwT = 0;
                Sfx.Play("whoosh", transform.position, 0.6f);
            }
            if (input.push)
            {
                var t = Trolley.I;
                if (Pushing) PushRpc(false);
                else if (t != null && Carrying == 0 && Vector3.Distance(t.Handle, transform.position + Vector3.up) < 2.2f) PushRpc(true);
                else if (Carrying == 0 && Time.time > shoveReady && ShoveTarget() is Player victim) { shoveReady = Time.time + 1f; throwT = 0; Sfx.Play("whoosh", transform.position, 0.6f); ShoveRpc(victim.NetworkObject); }
                else if (Carrying == 0) { var c = FindCrate(); if (c != null && !c.Attached.Value) TieRpc(c.NetworkObject); }
            }
        }

        float grabNextAt, dropNextAt;
        static bool Repeat(bool pressed, bool held, ref float next)
        {
            if (pressed) { next = Time.time + 0.38f; return true; }
            if (held && Time.time >= next) { next = Time.time + 0.26f; return true; }
            return false;
        }

        // What Q would do with the top crate right now (every peer can ask, for the hint).
        public enum PutSpot { Ground, Rack, Trolley, Pallet }
        public PutSpot PutTarget(out int load, out int cap)
        {
            load = cap = 0;
            if (Storage.Near(transform.position) && Storage.Count < Storage.Slots) { load = Storage.Count; cap = Storage.Slots; return PutSpot.Rack; }
            var t = Trolley.I;
            if (t != null && !Pushing && Vector3.Distance(t.transform.position, transform.position) < 2.4f && t.Load < Trolley.Capacity) { load = t.Load; cap = Trolley.Capacity; return PutSpot.Trolley; }
            foreach (var pa in Pallet.All)
                if (!pa.Carried.Value && Vector3.Distance(pa.transform.position, transform.position) < 2.4f && pa.Load < Pallet.Capacity) { load = pa.Load; cap = Pallet.Capacity; return PutSpot.Pallet; }
            return PutSpot.Ground;
        }

        public Crate FindCrate()
        {
            Crate best = null; float bestD = 2.4f;
            var eye = transform.position + Vector3.up * 0.9f + transform.forward * 0.8f;
            foreach (var c in Crate.All)
            {
                if (c.Attached.Value && !(c.transform.parent != null && (c.transform.parent.GetComponent<Trolley>() || c.transform.parent.GetComponent<Pallet>()))) continue;
                var d = Vector3.Distance(c.transform.position, eye);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        // Carried at the belly, gripped low on the bottom crate; the camera looks over the shoulder so the whole stack stays in view.
        public static Vector3 HoldPos(int slot) => new(0, 1.12f + slot * (Crate.Size + 0.04f), 0.6f);

        // ---- server ----
        void FixedUpdate()
        {
            if (!IsServer) return;
            var p = transform.position;
            serverVel = Vector3.Lerp(serverVel, (p - lastServerPos) / Time.fixedDeltaTime, 0.3f);
            lastServerPos = p;
            if (TowedId.Value != 0)
            {
                // Towed crate is pulled to a point just ahead of the swimmer (it stays a floating physics body).
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(TowedId.Value, out var no) || !no.TryGetComponent<Crate>(out var c) || c.Attached.Value
                    || Vector3.Distance(c.transform.position, p) > 5f) TowedId.Value = 0;
                else c.TowTo(p + transform.forward * 1.1f + Vector3.up * SwimDepth);
            }
        }

        [Rpc(SendTo.Server)]
        void GrabRpc(NetworkObjectReference r)
        {
            if (!r.TryGet(out var no) || !no.TryGetComponent<Crate>(out var c) || stack.Count >= MaxStack) return;
            var parentTrolley = c.transform.parent != null ? c.transform.parent.GetComponent<Trolley>() : null;
            var parentPallet = c.transform.parent != null ? c.transform.parent.GetComponent<Pallet>() : null;
            if (c.Attached.Value && parentTrolley == null && parentPallet == null) return;
            if (parentPallet != null) parentPallet.Remove(c);
            if (Vector3.Distance(c.transform.position, transform.position) > 3.6f) return;
            if (parentTrolley != null) parentTrolley.Remove(c);
            c.Attach(NetworkObject, HoldPos(stack.Count), stack.Count);
            stack.Add(c);
            FxRpc("grab", c.transform.position);
        }

        Crate PopTop()
        {
            stack.RemoveAll(x => x == null || !x.IsSpawned || x.transform.parent != transform);
            if (stack.Count == 0) return null;
            var c = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return c;
        }

        // Q: the top crate goes where it obviously belongs: on the storage rack, into the trolley or onto a pallet
        // within reach; otherwise set down upright on the ground (or neatly on the crate below) just in front.
        [Rpc(SendTo.Server)]
        void PutDownRpc()
        {
            var c = PopTop();
            if (c == null) return;
            var at = c.transform.position;
            bool put = PutTarget(out _, out _) switch
            {
                PutSpot.Rack => Storage.Put(c, transform.position + Vector3.up * 1.1f),
                PutSpot.Trolley => Trolley.I.Put(c),
                PutSpot.Pallet => Pallet.All.Find(pa => !pa.Carried.Value && Vector3.Distance(pa.transform.position, transform.position) < 2.4f && pa.Load < Pallet.Capacity) is Pallet pl && pl.Put(c),
                _ => SetDown(c),
            };
            if (!put)
            {
                // Nowhere sensible (a wall in the way, the quay edge): let it go from the hands, as before.
                var p = transform.TransformPoint(new Vector3(0, 1.35f, 0.95f));
                c.Release(new Vector3(p.x, Mathf.Max(c.transform.position.y, transform.position.y + 1.25f), p.z), serverVel * 0.6f + transform.forward * 0.4f, Vector3.zero);
            }
            FxRpc("place", at);
        }

        // Server: stand the crate on whatever is just in front (ground, deck, another crate), squared to us.
        bool SetDown(Crate c)
        {
            var front = transform.TransformPoint(new Vector3(0, 0, 0.95f));
            if (!Physics.Raycast(front + Vector3.up * 2.2f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return false; // over the water
            if (hit.point.y > transform.position.y + 1.3f || hit.point.y < transform.position.y - 1.5f) return false; // a wall top, or a drop
            var pos = new Vector3(front.x, hit.point.y + Crate.Size / 2 + 0.02f, front.z);
            if (Physics.CheckBox(pos + Vector3.up * 0.03f, Vector3.one * (Crate.Size / 2 - 0.05f), transform.rotation, ~0, QueryTriggerInteraction.Ignore)) return false;
            c.Release(pos, Vector3.zero, Vector3.zero, Quaternion.Euler(0, transform.eulerAngles.y, 0));
            return true;
        }

        [Rpc(SendTo.Server)]
        void ThrowRpc(Vector3 dir)
        {
            var c = PopTop();
            if (c == null) return;
            dir.y = 0; dir.Normalize();
            var p = transform.position + dir * 0.75f + Vector3.up * Mathf.Max(1.4f, c.transform.position.y - transform.position.y);
            c.Release(p, dir * 8.5f + Vector3.up * 3.2f + serverVel * 0.8f, Random.insideUnitSphere * 3f);
        }

        [Rpc(SendTo.Server)]
        void ToppleRpc(Vector3 dir)
        {
            // Top crate (and sometimes the next one) slides off the way the stack leaned.
            int n = stack.Count >= 4 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                var c = PopTop();
                if (c == null) return;
                c.Release(c.transform.position + dir * 0.3f, dir * (2.5f + i) + Vector3.up * 1.2f + serverVel * 0.7f, Random.insideUnitSphere * 4f);
            }
        }

        [Rpc(SendTo.Server)]
        void DropAllRpc() => ReleaseAll(Vector3.up);

        void ReleaseAll(Vector3 v)
        {
            Crate c;
            while ((c = PopTop()) != null) c.Release(c.transform.position, v + Random.insideUnitSphere, Random.insideUnitSphere);
        }

        // Pushing: we only go where the trolley in front of us fits (walls, crates, people, the water's edge),
        // sliding along whatever stops it. (If it's already wedged, don't trap us.)
        Vector3 TrolleyClear(Vector3 v, float dt)
        {
            var t = Trolley.I;
            if (t == null || v.sqrMagnitude < 1e-6f) return v;
            var p = transform.position; float y = transform.eulerAngles.y;
            if (t.Fits(this, p + v * dt, y) || !t.Fits(this, p, y)) return v;
            var vx = new Vector3(v.x, 0, 0); if (t.Fits(this, p + vx * dt, y)) return vx;
            var vz = new Vector3(0, 0, v.z); if (t.Fits(this, p + vz * dt, y)) return vz;
            return Vector3.zero;
        }

        // Took hold of the trolley (or let go): step in behind the push bar, and don't collide with it meanwhile.
        bool wasPushing;
        void PushChanged(bool on)
        {
            var t = Trolley.I;
            if (t == null) return;
            foreach (var c in t.GetComponents<Collider>()) Physics.IgnoreCollision(cc, c, on);
            if (!on) return;
            var f = t.transform.forward; f.y = 0;
            if (f.sqrMagnitude < 0.01f) return;
            f.Normalize();
            var spot = t.transform.position - f * Trolley.HoldDist; spot.y = transform.position.y;
            var bottom = spot + Vector3.up * (cc.radius + 0.1f); var top = spot + Vector3.up * (cc.height - cc.radius);
            foreach (var c in Physics.OverlapCapsule(bottom, top, cc.radius * 0.9f, ~0, QueryTriggerInteraction.Ignore))
                if (c.GetComponentInParent<Player>() != this && !c.transform.IsChildOf(t.transform)) { PushRpc(false); GameUI.Toast("No room to get behind the trolley's handle."); return; }
            cc.enabled = false;
            transform.SetPositionAndRotation(spot, Quaternion.LookRotation(f));
            cc.enabled = true;
            hv = Vector3.zero;
        }

        // ---------- thrown off a boat ----------
        // The captain doesn't take passengers: anyone still aboard when she sails is slung back onto the jetty
        // (or over the side, once she's out). A ballistic flight to `target`; whatever we carry comes with us.
        const float FlingGravity = 24f;
        bool flung;
        [Rpc(SendTo.Everyone)]
        public void ThrownOffRpc(Vector3 target)
        {
            knockT = 0;
            Sfx.Play("whoosh", transform.position + Vector3.up, 1f, 0.8f);
            if (!IsOwner) return;
            var d = target - transform.position;
            float flat = new Vector2(d.x, d.z).magnitude, T = Mathf.Clamp(0.75f + flat * 0.045f, 0.8f, 1.4f);
            hv = new Vector3(d.x, 0, d.z) / T; vy = d.y / T + 0.5f * FlingGravity * T;
            flung = true; platform = null;
            stunnedUntil = Time.time + T + 0.4f;
            if (Pushing) PushRpc(false);
            Debug.Log($"[PB] thrown off a boat: {transform.position:F1} -> {target:F1}");
            Stats.Add("thrownOff");
            Shake(0.5f);
            GameUI.Toast(PortGame.Pick("Thrown off the boat! No stowaways.", "Yeeted by the captain. Fair enough.", "The captain doesn't do passengers."));
        }

        void Fly(float dt)
        {
            vy -= FlingGravity * dt;
            cc.Move((hv + Vector3.up * vy) * dt);
            if (cc.isGrounded && vy < 0)
            {
                flung = false; hv *= 0.2f; vy = -2f;
                Sfx.Play("land", transform.position, 1f, 0.85f);
                Shake(0.35f);
            }
        }

        [Rpc(SendTo.Server)]
        void PushRpc(bool on)
        {
            var t = Trolley.I;
            if (t == null) return;
            if (on && stack.Count == 0) t.SetPusher(this);
            else if (!on && t.PusherId.Value == OwnerClientId) t.SetPusher(null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void FxRpc(string sfx, Vector3 p)
        {
            Sfx.Play(sfx, p, 0.7f);
        }

        // ---- camera (owner) ----
        public static bool CamOverride; // test shots drive the camera themselves
        float carryCam;
        static float trauma;
        public static void Shake(float amount) { if (Settings.CameraShake) trauma = Mathf.Min(1f, trauma + amount); }

        void LateUpdate()
        {
            if (!IsOwner || !IsSpawned || CamOverride) return;
            var cam = Camera.main;
            if (cam == null) return;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            // Carrying: look over the right shoulder so the whole stack in front of the body stays in view.
            carryCam = Mathf.MoveTowards(carryCam, Carrying > 0 ? 1f : 0f, Time.deltaTime * 2f);
            var target = transform.position + Vector3.up * (1.7f + Carrying * 0.18f) + rot * Vector3.right * (0.95f * carryCam);
            float dist = 5.6f + Carrying * 0.45f;
            var want = target - rot * Vector3.forward * dist;
            if (Physics.SphereCast(target, 0.4f, (want - target).normalized, out var h, dist, ~0, QueryTriggerInteraction.Ignore)
                && h.collider.GetComponentInParent<Player>() == null)
            {
                // Blocked (quay wall, boat): come in closer but rise, so we look over the head instead of into the back.
                float d = Mathf.Max(0.8f, h.distance - 0.1f);
                if (h.normal.y > 0.7f) want = target + (want - target).normalized * d + h.normal * 0.35f; // the ground (looking up): slide along it
                else want = target + (want - target).normalized * d + Vector3.up * Mathf.Min(2.2f, (dist - d) * 0.5f);
            }
            want.y = Mathf.Max(want.y, Water.Height(want) + 0.5f);
            camPos = Vector3.Lerp(camPos, want, 1f - Mathf.Exp(-18f * Time.deltaTime));
            trauma = Mathf.Max(0, trauma - Time.deltaTime * 1.5f);
            float sh = trauma * trauma * 0.35f, tt = Time.time * 25f;
            var shake = new Vector3(Mathf.PerlinNoise(tt, 0) - 0.5f, Mathf.PerlinNoise(0, tt) - 0.5f, 0) * sh;
            cam.transform.SetPositionAndRotation(camPos + cam.transform.rotation * shake, Quaternion.LookRotation(target - camPos));
        }
    }
}
