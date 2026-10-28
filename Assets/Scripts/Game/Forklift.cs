using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PortBoss
{
    // Arcade forklift. The driver's client owns and simulates it (NetworkTransform Owner + NetworkRigidbody);
    // the host handles pallets (pick up / set down / spill when it tips) and losing it in the harbour.
    // Tipping: speed x turn rate x load height above a threshold rolls it over.
    public class Forklift : NetworkBehaviour
    {
        public static Forklift I;
        public const ulong NoDriver = ulong.MaxValue;
        const float ForkZ = 1.45f, MaxMast = 2.2f; // fork blades sit at height Mast above the ground

        public NetworkVariable<ulong> DriverId = new(NoDriver);
        public NetworkVariable<float> Mast = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> HasPallet = new(false);

        public static System.Func<Vector3> BotDrive; // test: (throttle, steer, mast)
        public Vector3 Seat => transform.TransformPoint(new Vector3(0, 0.95f, -0.35f));
        public bool Free => DriverId.Value == NoDriver;

        Rigidbody rb;
        Transform forks;
        Pallet carried;   // host
        float throttle, steer, mastInput;
        bool exitPressed, lost;
        float lostAt, lastBail;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = new Vector3(0, 0.35f, -0.25f);
            var t = transform;
            var yellow = Art.Hex("#e8b030");
            Art.Box(t, new Vector3(0, 0.62f, -0.1f), new Vector3(1.25f, 0.75f, 1.9f), yellow, false, "fl_body", 0.45f);
            Art.Box(t, new Vector3(0, 0.75f, -1.05f), new Vector3(1.3f, 0.9f, 0.45f), Art.Hex("#3a3a3a"), false, "fl_counterweight", 0.3f);
            Art.Box(t, new Vector3(0, 1.0f, -0.35f), new Vector3(0.6f, 0.12f, 0.55f), Art.Hex("#2b2b2b"), false, "fl_seat");
            foreach (var x in new[] { -0.55f, 0.55f })
                foreach (var z in new[] { -0.75f, 0.45f })
                    Art.Box(t, new Vector3(x, 1.55f, z), new Vector3(0.07f, 1.25f, 0.07f), Art.Hex("#2b2b2b"), false, "fl_guard_post");
            Art.Box(t, new Vector3(0, 2.2f, -0.15f), new Vector3(1.2f, 0.06f, 1.3f), Art.Hex("#2b2b2b"), false, "fl_guard_roof");
            foreach (var x in new[] { -0.4f, 0.4f })
                Art.Box(t, new Vector3(x, 1.45f, 0.95f), new Vector3(0.1f, 2.6f, 0.12f), Art.Hex("#4a4a4a"), false, "fl_mast");
            foreach (var x in new[] { -0.6f, 0.6f })
                foreach (var z in new[] { -0.7f, 0.55f })
                    Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.32f, z), Quaternion.Euler(0, 0, 90), new Vector3(0.64f, 0.12f, 0.64f), Art.Hex("#222222"), false, "fl_wheel");
            forks = new GameObject("forks").transform;
            forks.SetParent(t, false);
            Art.Box(forks, new Vector3(0, 0.25f, 1.02f), new Vector3(1.0f, 0.55f, 0.06f), Art.Hex("#4a4a4a"), false, "fl_carriage");
            foreach (var x in new[] { -0.32f, 0.32f })
                Art.Box(forks, new Vector3(x, 0, ForkZ), new Vector3(0.14f, 0.05f, 1.0f), Art.Hex("#555555"), false, "fl_fork");
        }

        public override void OnNetworkSpawn() => I = this;
        public override void OnNetworkDespawn() { if (I == this) I = null; }

        void Update()
        {
            forks.localPosition = new Vector3(0, Mast.Value, 0);
            if (!IsOwner || DriverId.Value != NetworkManager.LocalClientId) return;
            // Driver input (owner only).
            var k = Keyboard.current; var pad = Gamepad.current;
            Vector2 mv = Vector2.zero; float m = 0;
            if (!GameUI.InputBlocked)
            {
                if (k != null)
                {
                    mv = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                    var up = Settings.KeyFor("sprint"); var down = Settings.KeyFor("drop"); var exit = Settings.KeyFor("grab");
                    if (up != Key.None && k[up].isPressed) m += 1;
                    if (down != Key.None && k[down].isPressed) m -= 1;
                    if (exit != Key.None && k[exit].wasPressedThisFrame) exitPressed = true;
                }
                if (pad != null)
                {
                    var ls = pad.leftStick.ReadValue(); if (ls.sqrMagnitude > 0.04f) mv = ls;
                    m += pad.rightShoulder.isPressed ? 1 : 0; m -= pad.leftShoulder.isPressed ? 1 : 0;
                    if (pad.buttonWest.wasPressedThisFrame) exitPressed = true;
                }
            }
            if (BotDrive != null) { var b = BotDrive(); mv = new Vector2(b.y, b.x); m = b.z; }
            throttle = mv.y; steer = mv.x; mastInput = m;
            Mast.Value = Mathf.Clamp(Mast.Value + mastInput * 0.9f * Time.deltaTime, 0, MaxMast);
            if (exitPressed) { exitPressed = false; ExitRpc(); }
        }

        void FixedUpdate()
        {
            if (IsOwner && DriverId.Value != NoDriver && !lost) Drive();
            if (IsServer) HostUpdate();
        }

        void Drive()
        {
            float dt = Time.fixedDeltaTime;
            bool grounded = Physics.Raycast(rb.position + Vector3.up * 0.4f, -transform.up, 0.7f, ~0, QueryTriggerInteraction.Ignore);
            var v = rb.linearVelocity;
            var fwd = transform.forward;
            float speed = Vector3.Dot(v, fwd);
            if (grounded)
            {
                float target = throttle >= 0 ? throttle * 6.5f : throttle * 3f;
                rb.AddForce(fwd * Mathf.Clamp((target - speed) * 3f, -9f, 5f), ForceMode.Acceleration);
                // Tyres grip sideways (a little drift is fine).
                rb.AddForce(-transform.right * (Vector3.Dot(v, transform.right) * 8f), ForceMode.Acceleration);
                // Rear-wheel steering feel: turn rate scales with speed; reverse steers the other way.
                float yawRate = steer * Mathf.Clamp(speed / 2.5f, -1f, 1f) * 75f * Mathf.Deg2Rad;
                var av = rb.angularVelocity;
                rb.angularVelocity = new Vector3(av.x, Mathf.Lerp(av.y, yawRate, 0.5f), av.z);
                // Corner too fast (worse with a raised load) and it rolls over.
                float comH = 0.6f + Mast.Value * (HasPallet.Value ? 0.55f : 0.15f);
                float tip = Mathf.Abs(speed * yawRate) * comH;
                if (tip > 4.2f) rb.AddRelativeTorque(new Vector3(0, 0, -Mathf.Sign(speed * yawRate) * (tip - 4.2f) * 1.6f), ForceMode.VelocityChange);
            }
            if (rb.position.y < Water.Height(rb.position) - 0.3f && Time.time - lastBail > 1f) { lastBail = Time.time; ExitRpc(); } // into the harbour: jump clear
        }

        // ---- host ----
        void HostUpdate()
        {
            if (!IsSpawned) return;
            var vel = (transform.position - lastPos) / Time.fixedDeltaTime;
            lastPos = transform.position;
            var forkWorld = transform.TransformPoint(new Vector3(0, Mast.Value, ForkZ));
            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (carried == null)
            {
                // Slide the forks under a pallet and lift: it comes with you.
                if (Mast.Value > 0.08f && tilt < 15f)
                    foreach (var p in Pallet.All)
                    {
                        if (p.Carried.Value) continue;
                        var d = p.transform.position - forkWorld;
                        if (new Vector2(d.x, d.z).magnitude < 0.55f && d.y > -0.4f && d.y < 0.02f)
                        {
                            carried = p;
                            p.Attach(NetworkObject, new Vector3(0, Mast.Value + 0.03f, ForkZ));
                            HasPallet.Value = true;
                            break;
                        }
                    }
            }
            else if (!carried.IsSpawned) { carried = null; HasPallet.Value = false; }
            else
            {
                carried.transform.localPosition = new Vector3(0, Mast.Value + 0.03f, ForkZ);
                if (tilt > 14f) { carried.Spill(vel); DropPallet(vel); }          // tipped: load goes everywhere
                else if (Mast.Value < 0.04f) DropPallet(Vector3.zero);           // lowered all the way: set it down
            }
            if (!lost && transform.position.y < Water.Level - 0.6f)
            {
                lost = true; lostAt = Time.time;
                if (carried != null) { carried.Spill(vel); DropPallet(vel); }
                DriverId.Value = NoDriver;
                NetworkObject.RemoveOwnership();
                PortGame.I?.ForkliftLost();
            }
            if (lost && Time.time - lostAt > 8f) NetworkObject.Despawn();
        }
        Vector3 lastPos;

        void DropPallet(Vector3 vel)
        {
            carried.Release(vel);
            carried = null;
            HasPallet.Value = false;
        }

        [Rpc(SendTo.Server)]
        public void EnterRpc(RpcParams p = default)
        {
            ulong who = p.Receive.SenderClientId;
            if (!Free || lost || Vector3.Distance(Seat, PlayerPos(who)) > 3f) return;
            DriverId.Value = who;
            NetworkObject.ChangeOwnership(who);
        }

        [Rpc(SendTo.Server)]
        public void ExitRpc(RpcParams p = default)
        {
            if (DriverId.Value != p.Receive.SenderClientId) return;
            DriverId.Value = NoDriver;
            NetworkObject.RemoveOwnership();
        }

        static Vector3 PlayerPos(ulong id)
        {
            foreach (var pl in Player.All) if (pl.OwnerClientId == id) return pl.transform.position;
            return Vector3.one * 1e6f;
        }
    }
}
