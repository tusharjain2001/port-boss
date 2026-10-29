using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Hand trolley: a platform cart with a push bar. Parked, it's a host-simulated rigidbody on frictionless
    // wheels: let go on the slipway and it rolls into the sea, spilling everything. Pushed, it's held: it sits
    // right in front of the pusher's hands on every peer (HeldPose, from the pusher's synced pose), wheels on the
    // ground, so it never lags, swings round or climbs over anyone. The pusher only moves where it fits (Fits).
    // Crates dropped into the bed are parented (kinematic).
    public class Trolley : NetworkBehaviour
    {
        public static Trolley I;
        public const int Capacity = 6;
        public const float HoldDist = 1.3f; // trolley centre ahead of the pusher's feet
        public NetworkVariable<ulong> PusherId = new(ulong.MaxValue);

        public Vector3 Handle => transform.TransformPoint(new Vector3(0, 1.05f, -0.8f));

        Rigidbody rb;
        Player pusher;
        readonly List<Crate> load = new();
        bool lost;
        float lostAt;
        Vector3 heldVel, lastHeld;

        static readonly Vector3 BedCenter = new(0, 0.62f, 0.05f), BedHalf = new(0.58f, 0.3f, 0.62f);
        // What has to stay clear while pushed: the bed and its sides, kept above kerbs and the gangplank's lip.
        static readonly Vector3 BodyCenter = new(0, 0.68f, 0.02f), BodyHalf = new(0.6f, 0.26f, 0.68f);
        static readonly RaycastHit[] rays = new RaycastHit[16];
        static readonly Collider[] cols = new Collider[16];

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var t = transform;
            Color frame = Art.Hex("#2f6fbf"), trim = Art.Hex("#f2b632"), tyre = Art.Hex("#262626"), hub = Art.Hex("#c8ccd0");
            // Steel chassis, a deck of four planks, low tube rails round three sides.
            Art.Box(t, new Vector3(0, 0.34f, 0.02f), new Vector3(1.22f, 0.08f, 1.38f), frame, false, "chassis");
            for (int i = 0; i < 4; i++)
                Art.Box(t, new Vector3(-0.45f + i * 0.3f, 0.405f, 0.02f), new Vector3(0.27f, 0.05f, 1.32f), i % 2 == 0 ? Art.WoodLight : Art.Hex("#c99a5b"), false, "plank");
            foreach (var x in new[] { -0.6f, 0.6f })
            {
                foreach (var z in new[] { -0.66f, 0.02f, 0.68f })
                    Art.Box(t, new Vector3(x, 0.6f, z), new Vector3(0.05f, 0.4f, 0.05f), frame, false, "post");
                foreach (var y in new[] { 0.62f, 0.8f })
                    Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, y, 0.01f), Quaternion.Euler(90, 0, 0), new Vector3(0.045f, 0.68f, 0.045f), frame, false, "rail");
            }
            foreach (var y in new[] { 0.62f, 0.8f })
                Art.Prim(PrimitiveType.Cylinder, t, new Vector3(0, y, 0.68f), Quaternion.Euler(0, 0, 90), new Vector3(0.045f, 0.6f, 0.045f), frame, false, "rail");
            Art.Box(t, new Vector3(0, 0.36f, 0.72f), new Vector3(1.26f, 0.1f, 0.04f), trim, false, "bumper");
            // Push bar: two uprights leaning back a touch, a cross bar with a black grip.
            foreach (var x in new[] { -0.55f, 0.55f })
                Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.74f, -0.73f), Quaternion.Euler(-12, 0, 0), new Vector3(0.06f, 0.34f, 0.06f), frame, false, "upright");
            Art.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.06f, -0.8f), Quaternion.Euler(0, 0, 90), new Vector3(0.06f, 0.6f, 0.06f), frame, false, "bar");
            Art.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.06f, -0.8f), Quaternion.Euler(0, 0, 90), new Vector3(0.075f, 0.4f, 0.075f), tyre, false, "grip");
            // Four wheels (fat tyres, grey hubs); the front pair on swivel forks.
            foreach (var x in new[] { -0.5f, 0.5f })
                foreach (var z in new[] { -0.5f, 0.52f })
                {
                    Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.17f, z), Quaternion.Euler(0, 0, 90), new Vector3(0.34f, 0.055f, 0.34f), tyre, false, "wheel");
                    Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x * 1.02f, 0.17f, z), Quaternion.Euler(0, 0, 90), new Vector3(0.16f, 0.06f, 0.16f), hub, false, "hub");
                    Art.Box(t, new Vector3(x, 0.3f, z), new Vector3(0.16f, 0.06f, 0.16f), z > 0 ? trim : frame, false, "fork");
                }
        }

        public override void OnNetworkSpawn()
        {
            I = this;
            rb.isKinematic = !IsServer;
            if (IsServer) rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public override void OnNetworkDespawn() { if (I == this) I = null; }

        // Server.
        public void SetPusher(Player p)
        {
            if (lost) p = null;
            pusher = p;
            PusherId.Value = p != null ? p.OwnerClientId : ulong.MaxValue;
            if (!IsServer) return;
            rb.isKinematic = p != null;
            rb.interpolation = p != null ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate; // held: posed every frame
            if (p == null) { rb.linearVelocity = Vector3.ClampMagnitude(heldVel, 4f); rb.angularVelocity = Vector3.zero; } // rolls on a little
            else lastHeld = rb.position;
        }

        public Player Pusher { get { foreach (var p in Player.All) if (p.OwnerClientId == PusherId.Value) return p; return null; } }

        // Where the trolley sits when pushed by someone standing at `pos` facing `yaw`: ahead of them, wheels on the
        // ground, pitched along it. False if a wheel would be off an edge (the water, a drop).
        public static bool HeldPose(Vector3 pos, float yaw, out Vector3 at, out Quaternion rot)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var c = pos + fwd * HoldDist;
            var side = Quaternion.Euler(0, yaw, 0) * Vector3.right * 0.5f;
            bool ok = Ground(c + fwd * 0.52f + side, pos.y, out float fr) & Ground(c + fwd * 0.52f - side, pos.y, out float fl)
                & Ground(c - fwd * 0.5f + side, pos.y, out float br) & Ground(c - fwd * 0.5f - side, pos.y, out float bl);
            float front = Mathf.Max(fr, fl), back = Mathf.Max(br, bl);
            at = new Vector3(c.x, (front + back) / 2, c.z);
            rot = Quaternion.Euler(Mathf.Atan2(back - front, 1.02f) * Mathf.Rad2Deg, yaw, 0);
            return ok;
        }

        // The ground under a wheel: the highest surface near the pusher's feet (not people, crates or the trolley).
        static bool Ground(Vector3 p, float feet, out float y)
        {
            int n = Physics.RaycastNonAlloc(new Vector3(p.x, feet + 0.8f, p.z), Vector3.down, rays, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            y = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                var c = rays[i].collider;
                if (c.GetComponentInParent<Trolley>() != null || c.GetComponentInParent<Player>() != null || c.GetComponentInParent<Crate>() != null) continue;
                y = Mathf.Max(y, rays[i].point.y);
            }
            if (y == float.MinValue) { y = feet; return false; }
            return true;
        }

        // Would the trolley, pushed by `who` from `pos` facing `yaw`, be clear of walls, crates, people and edges?
        public bool Fits(Player who, Vector3 pos, float yaw)
        {
            if (!HeldPose(pos, yaw, out var at, out var rot)) return false;
            int n = Physics.OverlapBoxNonAlloc(at + rot * BodyCenter, BodyHalf, cols, rot, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = cols[i];
                if (c.transform.IsChildOf(transform) || c.GetComponentInParent<Player>() == who) continue; // itself, its load, the pusher
                return false;
            }
            return true;
        }

        public void Remove(Crate c) => load.Remove(c);
        public bool NearBuyer => Vector3.Distance(transform.position, World.BuyerPos) < World.BuyerReach + 2.5f;
        public int Load { get { int n = 0; foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out _)) n++; return n; } } // every peer

        static Vector3 BedSlot(int i) => new(i % 2 == 0 ? -0.29f : 0.29f, 0.43f + Crate.Size / 2 + (i / 4) * (Crate.Size + 0.02f), (i / 2) % 2 == 0 ? -0.28f : 0.34f);

        // Server: set a crate straight into the bed (put down by a player standing next to it).
        public bool Put(Crate c)
        {
            if (load.Count >= Capacity || lost) return false;
            c.Attach(NetworkObject, BedSlot(FreeSlot()), -1);
            load.Add(c);
            ClunkRpc(c.transform.position);
            return true;
        }

        // The first bed slot no crate sits in (one taken from the middle leaves a gap; load.Count would land on a crate).
        int FreeSlot()
        {
            for (int i = 0; i < Capacity; i++)
                if (!load.Exists(c => c != null && (c.transform.localPosition - BedSlot(i)).sqrMagnitude < 0.01f)) return i;
            return load.Count;
        }

        // Every peer, every frame: a pushed trolley is drawn in its held pose from the pusher as this peer sees them.
        void LateUpdate()
        {
            if (!IsSpawned || PusherId.Value == ulong.MaxValue) return;
            var p = Pusher;
            if (p != null && HeldPose(p.transform.position, p.transform.eulerAngles.y, out var at, out var rot)) transform.SetPositionAndRotation(at, rot);
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            if (lost)
            {
                if (Time.time - lostAt > 8f) NetworkObject.Despawn();
                return;
            }
            if (pusher != null && (!pusher.IsSpawned || Vector3.Distance(pusher.transform.position, rb.position) > 3.2f)) SetPusher(null);

            if (pusher != null && HeldPose(pusher.transform.position, pusher.transform.eulerAngles.y, out var at, out var rot))
            {
                rb.MovePosition(at); rb.MoveRotation(rot);
                heldVel = Vector3.Lerp(heldVel, (at - lastHeld) / Time.fixedDeltaTime, 0.3f);
                lastHeld = at;
            }

            // Pick up crates that land in the bed.
            if (load.Count < Capacity)
            {
                int n = Physics.OverlapBoxNonAlloc(transform.TransformPoint(BedCenter), BedHalf, cols, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    if (load.Count >= Capacity) break;
                    var c = cols[i].GetComponent<Crate>();
                    if (c == null || c.Attached.Value || c.Stored || load.Contains(c)) continue;
                    Put(c);
                }
            }

            // Tipped over or in the water: everything spills out.
            bool tipped = Vector3.Angle(transform.up, Vector3.up) > 55f;
            bool wet = rb.position.y < Water.Height(rb.position) - 0.1f;
            if ((tipped || wet) && load.Count > 0) Spill();
            if (wet && !lost)
            {
                SetPusher(null);
                lost = true; lostAt = Time.time;
                PortGame.I?.TrolleyLost();
            }
        }

        void Spill()
        {
            foreach (var c in load)
                if (c != null && c.IsSpawned && c.transform.parent == transform)
                    c.Release(c.transform.position + Vector3.up * 0.2f, rb.linearVelocity + Random.insideUnitSphere * 1.5f + Vector3.up, Random.insideUnitSphere * 3f);
            load.Clear();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClunkRpc(Vector3 p) => Sfx.Play("thud", p, 0.6f, 1.3f);
    }
}
