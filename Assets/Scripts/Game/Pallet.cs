using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Wooden pallet: holds 6 crates (parented), lifted by the forklift (then it is parented to the forklift).
    // Host-simulated when loose; floats if it ends up in the harbour.
    public class Pallet : NetworkBehaviour
    {
        public static readonly List<Pallet> All = new();
        public const int Capacity = 6;
        public const float W = 1.8f, D = 1.2f, H = 0.14f;

        public NetworkVariable<bool> Carried = new(false);
        public bool NearBuyer => Vector3.Distance(transform.position, World.BuyerPos) < World.BuyerReach + 2.5f;

        Rigidbody rb;
        readonly List<Crate> load = new();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var t = transform;
            for (int i = 0; i < 5; i++)
                Art.Box(t, new Vector3(-W / 2 + 0.18f + i * (W - 0.36f) / 4, H - 0.025f, 0), new Vector3(0.32f, 0.05f, D), Color.Lerp(Art.WoodLight, Art.Wood, (i % 2) * 0.4f), false, "pallet_top");
            foreach (var z in new[] { -D / 2 + 0.06f, 0f, D / 2 - 0.06f })
                Art.Box(t, new Vector3(0, H / 2 - 0.02f, z), new Vector3(W, H - 0.04f, 0.12f), Art.WoodDark, false, "pallet_runner");
        }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            Carried.OnValueChanged += (_, _) => Apply();
            Apply();
        }

        public override void OnNetworkDespawn() => All.Remove(this);

        void Apply() => rb.isKinematic = !IsServer || Carried.Value;

        // ---- server ----
        public void Attach(NetworkObject parent, Vector3 localPos)
        {
            Carried.Value = true; Apply();
            NetworkObject.TrySetParent(parent, true);
            transform.localPosition = localPos;
            transform.localRotation = Quaternion.identity;
        }

        public void Release(Vector3 vel)
        {
            var p = transform.position; var r = transform.rotation;
            NetworkObject.TryRemoveParent(true);
            transform.SetPositionAndRotation(p, r);
            Carried.Value = false; Apply();
            rb.position = p; rb.rotation = r; rb.linearVelocity = vel;
        }

        public void Spill(Vector3 vel)
        {
            foreach (var c in load)
                if (c != null && c.IsSpawned && c.transform.parent == transform)
                    c.Release(c.transform.position + Vector3.up * 0.15f, vel + Random.insideUnitSphere * 2f + Vector3.up, Random.insideUnitSphere * 3f);
            load.Clear();
        }

        public void Remove(Crate c) => load.Remove(c);
        public int Load { get { int n = 0; foreach (Transform c in transform) if (c.TryGetComponent<Crate>(out _)) n++; return n; } } // every peer

        // Server: set a crate straight onto the deck (3 x 2 grid).
        public bool Put(Crate c)
        {
            if (load.Count >= Capacity) return false;
            int i = load.Count;
            c.Attach(NetworkObject, new Vector3(-0.6f + (i % 3) * 0.6f, H + Crate.Size / 2 + 0.01f, i < 3 ? -0.3f : 0.3f), -1);
            load.Add(c);
            return true;
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            // Crates set down on the deck get stacked into a 3 x 2 grid.
            if (load.Count < Capacity)
            {
                var hits = Physics.OverlapBox(transform.TransformPoint(new Vector3(0, H + 0.3f, 0)), new Vector3(W / 2, 0.3f, D / 2), transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (load.Count >= Capacity) break;
                    var c = h.GetComponent<Crate>();
                    if (c == null || c.Attached.Value || c.Stored || load.Contains(c)) continue;
                    Put(c);
                }
            }
            if (Carried.Value) return;
            // Floats in the harbour.
            float depth = Water.Height(rb.position) - rb.position.y;
            if (depth > -0.1f)
            {
                rb.AddForce(Vector3.up * (9.81f * 1.6f * Mathf.Clamp01((depth + 0.1f) / 0.3f)) - rb.linearVelocity * 1.5f, ForceMode.Acceleration);
                if (load.Count > 0) Spill(rb.linearVelocity);
            }
        }
    }
}
