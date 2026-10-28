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
        float respawnAt = -1, stepDist;
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
            var m = model;
            Art.Prim(PrimitiveType.Capsule, m, new Vector3(0, 1.08f, 0), Quaternion.identity, new Vector3(0.66f, 0.6f, 0.6f), c, false, "body");
            Art.Box(m, new Vector3(0, 0.98f, 0.04f), new Vector3(0.7f, 0.45f, 0.62f), Art.Hex("#3b4a5a"), false, "overalls");
            Art.Prim(PrimitiveType.Sphere, m, new Vector3(0, 1.66f, 0), Quaternion.identity, Vector3.one * 0.4f, Art.Hex("#c99b7a"), false, "head");
            Art.Box(m, new Vector3(0, 1.86f, 0.03f), new Vector3(0.44f, 0.12f, 0.46f), c * 0.8f, false, "cap");
            Art.Box(m, new Vector3(0, 1.82f, 0.3f), new Vector3(0.38f, 0.04f, 0.2f), c * 0.8f, false, "visor");
            legL = Limb(m, new Vector3(-0.15f, 0.64f, 0), new Vector3(0.2f, 0.64f, 0.22f), Art.Hex("#3b4a5a"));
            legR = Limb(m, new Vector3(0.15f, 0.64f, 0), new Vector3(0.2f, 0.64f, 0.22f), Art.Hex("#3b4a5a"));
            armL = Limb(m, new Vector3(-0.42f, 1.38f, 0), new Vector3(0.14f, 0.6f, 0.14f), c);
            armR = Limb(m, new Vector3(0.42f, 1.38f, 0), new Vector3(0.14f, 0.6f, 0.14f), c);
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

        // A limb pivots at its top (hip/shoulder).
        static Transform Limb(Transform parent, Vector3 pivot, Vector3 size, Color c)
        {
            var p = new GameObject("limb").transform;
            p.SetParent(parent, false);
            p.localPosition = pivot;
            Art.Box(p, new Vector3(0, -size.y / 2, 0), size, c, false, "limb_mesh");
            return p;
        }

        void Animate(float speed)
        {
            float k = Mathf.Clamp01(speed / 4f);
            walkPhase += speed * Time.deltaTime * 2.2f;
            float sw = Mathf.Sin(walkPhase) * k * 38f;
            legL.localRotation = Quaternion.Euler(sw, 0, 0);
            legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            if (Carrying > 0 || Pushing) armL.localRotation = armR.localRotation = Quaternion.Euler(-80, 0, 0);
            else { armL.localRotation = Quaternion.Euler(-sw * 0.8f, 0, 0); armR.localRotation = Quaternion.Euler(sw * 0.8f, 0, 0); }
            model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.05f * k, 0);
        }

        void Teleport(Vector3 pos, float newYaw, bool viaNet = true)
        {
            yaw = newYaw; hv = Vector3.zero; vy = 0; platform = null;
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, newYaw, 0));
            cc.enabled = true;
            if (viaNet && nt != null && nt.CanCommitToTransform) nt.Teleport(pos, transform.rotation, transform.localScale);
        }

        public void DebugTeleport(Vector3 p, float yawDeg) => Teleport(p, yawDeg);

        static In ReadInput()
        {
            if (BotInput != null) return BotInput();
            var k = Keyboard.current; var m = Mouse.current;
            var i = new In();
            if (k == null) return i;
            i.move = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            i.sprint = k.leftShiftKey.isPressed;
            i.jump = k.spaceKey.wasPressedThisFrame;
            i.grab = k.eKey.wasPressedThisFrame;
            i.drop = k.qKey.wasPressedThisFrame;
            i.push = k.fKey.wasPressedThisFrame;
            if (m != null)
            {
                i.look = m.delta.ReadValue() * 0.1f;
                i.throwP = m.leftButton.wasPressedThisFrame;
                i.grab |= m.rightButton.wasPressedThisFrame;
            }
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
            float speed = (sprint ? 7.4f : 4.6f) * (1f - 0.07f * Carrying) * (Pushing ? 0.85f : 1f);
            hv = Vector3.MoveTowards(hv, wish * speed, (cc.isGrounded ? 30f : 10f) * dt);

            if (cc.isGrounded) { vy = -2f; if (input.jump) { vy = 5.6f; Sfx.Play("whoosh", transform.position, 0.3f); } }
            vy -= 19f * dt;
            var prevHv = hv;
            cc.Move((hv + Vector3.up * vy) * dt);

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
            Interact(input);

            // Fell in the sea: drop everything, climb back out on the quay.
            if (transform.position.y < Water.Level - 0.6f)
            {
                if (respawnAt < 0)
                {
                    respawnAt = Time.time + 1.2f;
                    Sfx.Play("splash", transform.position, 1f);
                    if (Carrying > 0) DropAllRpc();
                    GameUI.Toast("Splash! You fell in.");
                }
                else if (Time.time > respawnAt) { respawnAt = -1; Teleport(World.Spawn, 0); }
            }
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
        void LateUpdate()
        {
            if (!IsOwner || !IsSpawned) return;
            var cam = Camera.main;
            if (cam == null) return;
            var target = transform.position + Vector3.up * (1.7f + Carrying * 0.18f);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            float dist = 5.6f + Carrying * 0.45f;
            var want = target - rot * Vector3.forward * dist;
            if (Physics.SphereCast(target, 0.25f, (want - target).normalized, out var h, dist, ~0, QueryTriggerInteraction.Ignore)
                && h.collider.GetComponentInParent<Player>() == null)
                want = target + (want - target).normalized * Mathf.Max(0.8f, h.distance - 0.1f);
            camPos = Vector3.Lerp(camPos, want, 1f - Mathf.Exp(-18f * Time.deltaTime));
            cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(target - camPos));
        }
    }
}
