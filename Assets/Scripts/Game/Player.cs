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
            public bool sprint, jump, grab, drop, throwP, push;
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
            BuildDockhand(model, c, (int)OwnerClientId);
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

        void Animate(float speed)
        {
            float dt = Time.deltaTime;
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
                i.jump = Down("jump"); i.grab = Down("grab"); i.drop = Down("drop"); i.push = Down("push"); i.throwP = Down("throw");
            }
            if (m != null)
            {
                i.look = m.delta.ReadValue() * (0.1f * Settings.Sensitivity);
                i.throwP |= m.leftButton.wasPressedThisFrame;
                i.grab |= m.rightButton.wasPressedThisFrame;
            }
            if (pad != null)
            {
                var ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.04f) i.move = ls;
                i.look += pad.rightStick.ReadValue() * (220f * Settings.PadSensitivity * Time.deltaTime);
                i.jump |= pad.buttonSouth.wasPressedThisFrame; i.grab |= pad.buttonWest.wasPressedThisFrame;
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
            Carrying = n;

            // Footsteps on every peer, from how far the body moved on the ground.
            var flat = transform.position - lastStepPos; flat.y = 0;
            stepDist += Mathf.Min(flat.magnitude, 1f);
            if (model != null) Animate(Mathf.Min(flat.magnitude, 1f) / Mathf.Max(Time.deltaTime, 1e-4f));
            lastStepPos = transform.position;
            if (stepDist > 1.7f && Mathf.Abs(transform.position.y - World.PierTop) < 1.2f) { stepDist = 0; Sfx.Play("step" + Random.Range(0, 5), transform.position, IsOwner ? 0.35f : 0.25f); }

            if (!IsOwner || !IsSpawned) return;
            float dt = Time.deltaTime;
            var input = GameUI.InputBlocked ? new In() : ReadInput();

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

            if (climbT >= 0) { Climb(dt); return; }
            float surface = Water.Height(transform.position);
            if (!swimming && transform.position.y < surface - SwimDepth + 0.3f && vy <= 0 && !Shallow()) EnterWater();
            if (swimming)
            {
                // Swimming: stay at the surface (head and shoulders out), bob with the waves, never sink.
                hv = Vector3.MoveTowards(hv, wish * (sprint ? 3.1f : 2.2f), 6f * dt);
                float dy = (surface - SwimDepth - transform.position.y) * Mathf.Min(1f, 5f * dt);
                cc.Move(hv * dt + Vector3.up * dy);
                vy = 0;
                if (Shallow()) LeaveWater(); // slipway / shore: walk out
            }
            else
            {
                float speed = (sprint ? 7.4f : 4.6f) * (1f - 0.07f * Carrying) * (Pushing ? 0.85f : 1f);
                hv = Vector3.MoveTowards(hv, wish * speed, (cc.isGrounded ? 30f : 10f) * dt);
                if (cc.isGrounded) { vy = -2f; if (input.jump) { vy = 5.6f; Sfx.Play("whoosh", transform.position, 0.3f); } }
                vy -= 19f * dt;
                cc.Move((hv + Vector3.up * vy) * dt);
            }

            if (wish.sqrMagnitude > 0.01f)
            {
                var face = Quaternion.LookRotation(wish);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, face, (Pushing ? 160f : 720f) * dt);
            }

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

            UpdateSway(dt, sprint);
            if (swimming) SwimInteract(input); else Interact(input);

            // Safety net: fell through the world somehow.
            if (transform.position.y < -12f) { swimming = false; Teleport(World.Spawn, 0); }
        }

        // ---------- swimming ----------
        public const float SwimDepth = 1.3f; // feet this far below the surface: head + shoulders out
        public bool Swimming => swimming;
        public bool Towing => TowedId.Value != 0;
        public NetworkVariable<ulong> TowedId = new(0); // server writes: the floating crate being towed
        bool swimming;
        float climbT = -1;
        Vector3 climbFrom, climbTo;

        // Ground close enough to stand on? Measured against the mean sea level so waves don't flip it back and forth.
        bool Shallow() => Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var gh, 2.8f, ~0, QueryTriggerInteraction.Ignore)
            && gh.point.y > Water.Level - 1.1f && gh.collider.GetComponentInParent<Crate>() == null;

        void EnterWater()
        {
            swimming = true;
            Sfx.Play("splash", transform.position + Vector3.up, 1f);
            Fx.Splash(new Vector3(transform.position.x, Water.Height(transform.position), transform.position.z), 1.2f);
            Shake(0.25f);
            if (Carrying > 0) DropAllRpc();
            if (Pushing) PushRpc(false);
            GameUI.Toast("Splash! Swim to a ladder (E) or the slipway.");
        }

        void LeaveWater()
        {
            swimming = false;
            if (Towing) TowRpc(default, false);
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
                    // Climb out: up the ladder and onto the deck.
                    if (Towing) TowRpc(default, false);
                    climbT = 0; climbFrom = transform.position; climbTo = l.Value.Top;
                    cc.enabled = false;
                    Sfx.Play("splash", transform.position, 0.5f, 1.4f);
                }
                else if (Towing) TowRpc(default, false);
                else { var c = FloatingCrateNear(); if (c != null) TowRpc(c.NetworkObject, true); }
            }
            if (input.drop && Towing) TowRpc(default, false);
        }

        void Climb(float dt)
        {
            climbT += dt / 0.9f;
            float u = Mathf.Clamp01(climbT);
            // Up the ladder first, then step over onto the deck.
            var up = new Vector3(climbFrom.x, Mathf.Lerp(climbFrom.y, climbTo.y + 0.3f, Mathf.SmoothStep(0, 1, Mathf.Min(1, u / 0.75f))), climbFrom.z);
            var p = u < 0.75f ? up : Vector3.Lerp(up, climbTo, (u - 0.75f) / 0.25f);
            transform.position = p;
            if (climbT >= 1)
            {
                climbT = -1; swimming = false; vy = 0; hv = Vector3.zero;
                cc.enabled = true;
            }
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
            float bob = 0.05f * n * Mathf.Sin(walkPhase) * Mathf.Clamp01(hv.magnitude / 7.4f) * 6f;
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
            return list;
        }

        public void RequestSell(Crate c) => SellRpc(c.NetworkObject);
        public void RequestSellAll() { foreach (var c in SellableCrates()) SellRpc(c.NetworkObject); }

        [Rpc(SendTo.Server)]
        void SellRpc(NetworkObjectReference r)
        {
            if (!r.TryGet(out var no) || !no.TryGetComponent<Crate>(out var c) || !NearBuyer || PortGame.I == null) return;
            var parent = c.transform.parent;
            if (parent == transform) { stack.Remove(c); PortGame.I.Sell(c); Restack(); }
            else if (parent != null && parent.TryGetComponent<Trolley>(out var t) && t.NearBuyer) { t.Remove(c); PortGame.I.Sell(c); }
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
            if (input.grab && Carrying < MaxStack && !Pushing)
            {
                var c = FindCrate();
                if (c != null) GrabRpc(c.NetworkObject);
            }
            if (input.drop && Carrying > 0) DropRpc();
            if (input.throwP && Carrying > 0)
            {
                var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                if (Vector3.Dot(dir, transform.forward) < 0.3f) dir = transform.forward;
                ThrowRpc(dir);
                throwT = 0;
                Sfx.Play("whoosh", transform.position, 0.6f);
            }
            if (input.push)
            {
                var t = Trolley.I;
                if (Pushing) PushRpc(false);
                else if (t != null && Carrying == 0 && Vector3.Distance(t.Handle, transform.position + Vector3.up) < 2.2f) PushRpc(true);
            }
        }

        public Crate FindCrate()
        {
            Crate best = null; float bestD = 2.4f;
            var eye = transform.position + Vector3.up * 0.9f + transform.forward * 0.8f;
            foreach (var c in Crate.All)
            {
                if (c.Attached.Value && !(c.transform.parent != null && c.transform.parent.GetComponent<Trolley>())) continue;
                var d = Vector3.Distance(c.transform.position, eye);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        public static Vector3 HoldPos(int slot) => new(0, 1.22f + slot * (Crate.Size + 0.04f), 0.72f);

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
            if (c.Attached.Value && parentTrolley == null) return;
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

        [Rpc(SendTo.Server)]
        void DropRpc()
        {
            var c = PopTop();
            if (c == null) return;
            var p = transform.TransformPoint(new Vector3(0, 1.35f, 0.95f));
            c.Release(new Vector3(p.x, Mathf.Max(c.transform.position.y, transform.position.y + 1.25f), p.z), serverVel * 0.6f + transform.forward * 0.4f, Vector3.zero);
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

        [Rpc(SendTo.Server)]
        void PushRpc(bool on)
        {
            var t = Trolley.I;
            if (t == null) return;
            if (on && stack.Count == 0) t.SetPusher(this);
            else if (!on && t.PusherId.Value == OwnerClientId) t.SetPusher(null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void FxRpc(string sfx, Vector3 p) => Sfx.Play(sfx, p, 0.7f);

        // ---- camera (owner) ----
        public static bool CamOverride; // test shots drive the camera themselves
        static float trauma;
        public static void Shake(float amount) { if (Settings.CameraShake) trauma = Mathf.Min(1f, trauma + amount); }

        void LateUpdate()
        {
            if (!IsOwner || !IsSpawned || CamOverride) return;
            var cam = Camera.main;
            if (cam == null) return;
            var target = transform.position + Vector3.up * (1.7f + Carrying * 0.18f);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            float dist = 5.6f + Carrying * 0.45f;
            var want = target - rot * Vector3.forward * dist;
            if (Physics.SphereCast(target, 0.25f, (want - target).normalized, out var h, dist, ~0, QueryTriggerInteraction.Ignore)
                && h.collider.GetComponentInParent<Player>() == null)
                want = target + (want - target).normalized * Mathf.Max(0.8f, h.distance - 0.1f);
            want.y = Mathf.Max(want.y, Water.Height(want) + 0.5f);
            camPos = Vector3.Lerp(camPos, want, 1f - Mathf.Exp(-18f * Time.deltaTime));
            trauma = Mathf.Max(0, trauma - Time.deltaTime * 1.5f);
            float sh = trauma * trauma * 0.35f, tt = Time.time * 25f;
            var shake = new Vector3(Mathf.PerlinNoise(tt, 0) - 0.5f, Mathf.PerlinNoise(0, tt) - 0.5f, 0) * sh;
            cam.transform.SetPositionAndRotation(camPos + cam.transform.rotation * shake, Quaternion.LookRotation(target - camPos));
        }
    }
}
