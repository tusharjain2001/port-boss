using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Hand trolley: a host-simulated rigidbody on frictionless wheels. Crates dropped into the bed are
    // parented (kinematic). Let go on the slipway and it rolls into the sea, spilling everything.
    public class Trolley : NetworkBehaviour
    {
        public static Trolley I;
        public const int Capacity = 6;
        public NetworkVariable<ulong> PusherId = new(ulong.MaxValue);

        public Vector3 Handle => transform.TransformPoint(new Vector3(0, 1.0f, -0.85f));

        Rigidbody rb;
        Player pusher;
        readonly List<Crate> load = new();
        bool lost;
        float lostAt;

        static readonly Vector3 BedCenter = new(0, 0.62f, 0.05f), BedHalf = new(0.58f, 0.3f, 0.62f);

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var t = transform;
            Art.Box(t, new Vector3(0, 0.36f, 0.05f), new Vector3(1.2f, 0.08f, 1.3f), Art.WoodLight, false, "bed");
            Art.Box(t, new Vector3(0.62f, 0.5f, 0.05f), new Vector3(0.05f, 0.25f, 1.3f), Art.Steel, false, "side");
            Art.Box(t, new Vector3(-0.62f, 0.5f, 0.05f), new Vector3(0.05f, 0.25f, 1.3f), Art.Steel, false, "side");
            Art.Box(t, new Vector3(0, 0.5f, 0.72f), new Vector3(1.25f, 0.25f, 0.05f), Art.Steel, false, "front");
            Art.Box(t, new Vector3(0.55f, 0.75f, -0.68f), new Vector3(0.06f, 0.75f, 0.06f), Art.Red, false, "upright");
            Art.Box(t, new Vector3(-0.55f, 0.75f, -0.68f), new Vector3(0.06f, 0.75f, 0.06f), Art.Red, false, "upright");
            Art.Box(t, new Vector3(0, 1.1f, -0.75f), new Vector3(1.15f, 0.07f, 0.07f), Art.Hex("#222222"), false, "handle");
            foreach (var x in new[] { -0.55f, 0.55f })
                foreach (var z in new[] { -0.5f, 0.55f })
                    Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.18f, z), Quaternion.Euler(0, 0, 90), new Vector3(0.36f, 0.05f, 0.36f), Art.Hex("#2a2a2a"), false, "wheel");
        }

        public override void OnNetworkSpawn()
        {
            I = this;
            rb.isKinematic = !IsServer;
            if (IsServer) rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public override void OnNetworkDespawn() { if (I == this) I = null; }

        public void SetPusher(Player p)
        {
            pusher = p;
            PusherId.Value = p != null ? p.OwnerClientId : ulong.MaxValue;
        }

        public void Remove(Crate c) => load.Remove(c);
        public bool NearBuyer => Vector3.Distance(transform.position, World.BuyerPos) < World.BuyerReach + 2.5f;
        public int Load { get { int n = 0; foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out _)) n++; return n; } } // every peer

        static Vector3 BedSlot(int i) => new(i % 2 == 0 ? -0.29f : 0.29f, 0.4f + Crate.Size / 2 + (i / 4) * (Crate.Size + 0.02f), (i / 2) % 2 == 0 ? -0.28f : 0.34f);

        // Server: set a crate straight into the bed (put down by a player standing next to it).
        public bool Put(Crate c)
        {
            if (load.Count >= Capacity || lost) return false;
            c.Attach(NetworkObject, BedSlot(load.Count), -1);
            load.Add(c);
            ClunkRpc(c.transform.position);
            return true;
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

            if (pusher != null)
            {
                // Follow a point in front of the pusher; yaw to match them.
                var target = pusher.transform.position + pusher.transform.forward * 1.35f;
                var dv = target - rb.position;
                dv.y = 0;
                var want = Vector3.ClampMagnitude(dv * 9f, 9f);
                var v = rb.linearVelocity;
                rb.AddForce(new Vector3(want.x - v.x, 0, want.z - v.z) * 12f, ForceMode.Acceleration);
                float yawErr = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, pusher.transform.eulerAngles.y);
                var av = rb.angularVelocity;
                rb.angularVelocity = new Vector3(av.x, yawErr * Mathf.Deg2Rad * 8f, av.z);
            }

            // Pick up crates that land in the bed.
            if (load.Count < Capacity)
            {
                var hits = Physics.OverlapBox(transform.TransformPoint(BedCenter), BedHalf, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (load.Count >= Capacity) break;
                    var c = h.GetComponent<Crate>();
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
                lost = true; lostAt = Time.time;
                SetPusher(null);
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
