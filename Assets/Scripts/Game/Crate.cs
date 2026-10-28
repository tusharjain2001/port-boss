using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Fish crate. Host simulates it while free; while held (hands, trolley, boat in transit) it is
    // parented + kinematic with its collider off, so it is never simulated in a hand.
    public class Crate : NetworkBehaviour
    {
        public const float Size = 0.55f, Spoiled = 0.35f, SpoilSeconds = 330f;
        public static readonly List<Crate> All = new();

        public NetworkVariable<float> Fresh = new(1f);
        public NetworkVariable<bool> Attached = new(false);
        public NetworkVariable<sbyte> Slot = new(-1); // index in a player's stack, -1 otherwise

        public bool InSea { get; private set; }
        public const float FreshAt = 0.75f;
        public int Value => Fresh.Value >= FreshAt ? 15 : Fresh.Value >= Spoiled ? 12 : 4;
        public string Grade => Fresh.Value >= FreshAt ? "fresh" : Fresh.Value >= Spoiled ? "okay" : "spoiled";
        public bool IsSpoiled => Fresh.Value < Spoiled;

        Rigidbody rb;
        Collider col;
        Transform vis;
        Renderer fishR;
        MaterialPropertyBlock mpb;
        ParticleSystem flies;
        float lastHit, fresh = 1f;
        bool countedSea;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            mpb = new MaterialPropertyBlock();
            vis = new GameObject("vis").transform;
            vis.SetParent(transform, false);
            float s = Size;
            Art.Box(vis, Vector3.zero, new Vector3(s * 0.96f, s * 0.9f, s * 0.96f), Art.Wood, false, "body");
            for (int i = -1; i <= 1; i += 2)
            {
                Art.Box(vis, new Vector3(0, i * s * 0.22f, s * 0.49f), new Vector3(s, s * 0.14f, 0.03f), Art.WoodDark, false, "slat");
                Art.Box(vis, new Vector3(0, i * s * 0.22f, -s * 0.49f), new Vector3(s, s * 0.14f, 0.03f), Art.WoodDark, false, "slat");
                Art.Box(vis, new Vector3(s * 0.49f, i * s * 0.22f, 0), new Vector3(0.03f, s * 0.14f, s), Art.WoodDark, false, "slat");
                Art.Box(vis, new Vector3(-s * 0.49f, i * s * 0.22f, 0), new Vector3(0.03f, s * 0.14f, s), Art.WoodDark, false, "slat");
            }
            // Fish on ice poking out of the top: this is what visibly spoils.
            fishR = Art.Box(vis, new Vector3(0, s * 0.47f, 0), new Vector3(s * 0.84f, 0.06f, s * 0.84f), Art.Fish, false, "fish", 0.6f).GetComponent<Renderer>();
            for (int i = 0; i < 3; i++)
            {
                var f = Art.Prim(PrimitiveType.Capsule, vis, new Vector3(-0.14f + i * 0.14f, s * 0.52f, 0), Quaternion.Euler(90, 0, 15 - i * 15), new Vector3(0.09f, 0.16f, 0.06f), Art.Fish, false, "fishy", 0.7f);
                f.GetComponent<Renderer>().sharedMaterial = fishR.sharedMaterial;
            }
        }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            Attached.OnValueChanged += (_, _) => ApplyState();
            ApplyState();
        }

        public override void OnNetworkDespawn() => All.Remove(this);

        void ApplyState()
        {
            bool att = Attached.Value;
            col.enabled = !att;
            rb.isKinematic = att || !IsServer;
            rb.interpolation = !att && IsServer ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            if (!att) { vis.localPosition = Vector3.zero; vis.localRotation = Quaternion.identity; }
        }

        // ---- server API ----
        public void Attach(NetworkObject parent, Vector3 localPos, int slot)
        {
            Attached.Value = true;
            ApplyState();
            NetworkObject.TrySetParent(parent, true);
            transform.localPosition = localPos;
            transform.localRotation = Quaternion.identity;
            Slot.Value = (sbyte)slot;
        }

        public void Release(Vector3 pos, Vector3 vel, Vector3 spin)
        {
            var rot = transform.rotation;
            NetworkObject.TryRemoveParent(true);
            transform.SetPositionAndRotation(pos, rot);
            Attached.Value = false;
            Slot.Value = -1;
            ApplyState();
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = vel;
            rb.angularVelocity = spin;
            Debug.Log($"[PB] release {name} at {pos:F2} v={vel:F1}");
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || Attached.Value) return;
            var game = PortGame.I;
            var p = rb.position;

            // Buoyancy + drift: crates float, bob and slowly drift out to sea.
            float depth = Water.Height(p) - (p.y - Size / 2);
            if (depth > 0)
            {
                float sub = Mathf.Clamp01(depth / Size);
                rb.AddForce(Vector3.up * (sub * 9.81f * 1.9f), ForceMode.Acceleration);
                rb.AddForce(-rb.linearVelocity * (1.5f * sub) + new Vector3(0.25f, 0, 0.5f) * sub, ForceMode.Acceleration);
                rb.AddTorque(-rb.angularVelocity * (1.2f * sub), ForceMode.Acceleration);
                if (!countedSea && depth > 0.15f)
                {
                    countedSea = true; InSea = true;
                    SplashRpc(p);
                    Debug.Log($"[PB] crate in sea at {p:F1}");
                    game?.CrateInSea();
                }
            }
            if ((InSea && new Vector2(p.x, p.z).magnitude > 160f) || p.y < -25f) { NetworkObject.Despawn(); return; }

            if (game != null && game.Phase.Value == PortGame.Ph.Day)
            {
                // Quantised so 30 crates don't sync a float every tick.
                fresh = Mathf.Min(fresh, Fresh.Value) - Time.fixedDeltaTime / SpoilSeconds;
                float q = Mathf.Max(0, Mathf.Ceil(fresh * 40) / 40);
                if (q < Fresh.Value) Fresh.Value = q;
            }

        }

        void OnCollisionEnter(Collision c)
        {
            if (!IsServer || c.relativeVelocity.sqrMagnitude < 9f || Time.time - lastHit < 0.15f) return;
            lastHit = Time.time;
            ThudRpc(transform.position, Mathf.Clamp01(c.relativeVelocity.magnitude / 10f));
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ThudRpc(Vector3 p, float v) => Sfx.Play("thud", p, 0.3f + 0.7f * v);

        [Rpc(SendTo.ClientsAndHost)]
        void SplashRpc(Vector3 p) => Sfx.Play("splash", p, 0.9f);

        // ---- visuals (every peer) ----
        void LateUpdate()
        {
            float f = Fresh.Value;
            mpb.SetColor("_BaseColor", Color.Lerp(Art.Rot, Art.Fish, Mathf.SmoothStep(0, 1, (f - 0.1f) / 0.7f)));
            fishR.SetPropertyBlock(mpb);
            foreach (Transform c in vis)
                if (c.name == "fishy") c.GetComponent<Renderer>().SetPropertyBlock(mpb);

            bool spoiled = f < Spoiled;
            if (spoiled && flies == null) flies = MakeFlies();
            if (flies != null && flies.gameObject.activeSelf != spoiled) flies.gameObject.SetActive(spoiled);

            // Wobble: carried crates sway with the carrier's stack sway (purely visual).
            if (Attached.Value && Slot.Value >= 0 && transform.parent != null && transform.parent.TryGetComponent<Player>(out var pl))
            {
                var s = pl.Sway;
                int i = Slot.Value;
                // The stack bends: higher crates swing further and tilt more.
                vis.localPosition = new Vector3(s.x, 0, s.y) * (0.07f * i * (i + 1));
                vis.localRotation = Quaternion.Euler(s.y * 12f * i, 0, -s.x * 12f * i);
            }
        }

        ParticleSystem MakeFlies()
        {
            var g = new GameObject("flies");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Vector3.up * 0.45f;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop();
            var m = ps.main;
            m.startLifetime = 1.6f; m.startSpeed = 0.25f; m.startSize = 0.045f; m.maxParticles = 14;
            m.startColor = new Color(0.08f, 0.08f, 0.06f); m.simulationSpace = ParticleSystemSimulationSpace.Local;
            var e = ps.emission; e.rateOverTime = 7;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var n = ps.noise; n.enabled = true; n.strength = 0.9f; n.frequency = 1.6f;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Art.Particle;
            ps.Play();
            var a = g.AddComponent<AudioSource>();
            a.clip = Sfx.Clip("flies"); a.loop = true; a.volume = 0.12f; a.spatialBlend = 1; a.maxDistance = 8; a.rolloffMode = AudioRolloffMode.Linear;
            a.Play();
            return ps;
        }
    }
}
