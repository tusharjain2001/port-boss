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
        public NetworkVariable<bool> Tied = new(false); // roped down against the storm
        public NetworkVariable<byte> Damage = new(0); // hard landings: each level knocks off a slat and 25% of the price
        public int Value => Mathf.Max(1, Mathf.RoundToInt((Fresh.Value >= FreshAt ? 15 : Fresh.Value >= Spoiled ? 12 : 4) * (1f - 0.25f * Damage.Value)));
        public string Grade => Fresh.Value >= FreshAt ? "fresh" : Fresh.Value >= Spoiled ? "okay" : "spoiled";
        public bool IsSpoiled => Fresh.Value < Spoiled;

        Rigidbody rb;
        Collider col;
        Transform vis;
        Renderer[] spoilers; // [0] = ice, rest = fish
        MaterialPropertyBlock mpb;
        ParticleSystem flies;
        float lastHit, fresh = 1f;
        bool fastLogged;
        bool countedSea;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            mpb = new MaterialPropertyBlock();
            vis = new GameObject("vis").transform;
            vis.SetParent(transform, false);
            float s = Size;
            if (Skin.On && Skin.I.Crate != null)
            {
                // Toon Harbor wooden crate (2 m, pivot at the base) scaled to ours, the catch laid on its lid.
                Art.Place(Skin.I.Crate, vis, new Vector3(0, -s / 2, 0), 90 * Random.Range(0, 4), s / 2f);
                spoilers = CrateLook.Fish(vis, s, s / 2 + 0.03f, s * 0.86f, new System.Random(Random.Range(0, 1 << 20)));
            }
            else spoilers = CrateLook.Build(vis, s, true, Random.Range(0, 1 << 20));
            // Rope tie-down (shown when tied).
            ropes = new GameObject("ropes");
            ropes.transform.SetParent(vis, false);
            Art.Box(ropes.transform, Vector3.zero, new Vector3(s * 1.04f, s * 1.04f, 0.05f), Art.Hex("#c9b07a"), false, "rope");
            Art.Box(ropes.transform, Vector3.zero, new Vector3(0.05f, s * 1.04f, s * 1.04f), Art.Hex("#c9b07a"), false, "rope");
            ropes.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            Attached.OnValueChanged += (_, _) => ApplyState();
            Tied.OnValueChanged += (_, _) => ApplyState();
            ApplyState();
        }

        public override void OnNetworkDespawn() => All.Remove(this);

        void ApplyState()
        {
            bool att = Attached.Value;
            col.enabled = !att;
            rb.isKinematic = att || !IsServer || Tied.Value;
            if (ropes != null) ropes.SetActive(Tied.Value && !att);
            rb.interpolation = !att && IsServer ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            if (!att) { vis.localPosition = Vector3.zero; vis.localRotation = Quaternion.identity; }
        }

        // ---- server API ----
        public void Attach(NetworkObject parent, Vector3 localPos, int slot)
        {
            Tied.Value = false;
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

        // Server: pull towards a point on the water (towed by a swimmer); buoyancy still handles height.
        public void TowTo(Vector3 target)
        {
            var d = target - rb.position; d.y = 0;
            var v = rb.linearVelocity; v.y = 0;
            rb.AddForce(d * 25f - v * 6f, ForceMode.Acceleration);
        }

        float dryT;
        GameObject ropes;

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || Attached.Value) return;
            var game = PortGame.I;
            var p = rb.position;
            if (!fastLogged && rb.linearVelocity.sqrMagnitude > 1600f) { fastLogged = true; Debug.Log($"[PB] FAST crate {NetworkObjectId} at {p:F1} v={rb.linearVelocity:F0} kin={rb.isKinematic} parent={(transform.parent ? transform.parent.name : "-")}"); }

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
            // The rival's wake: floating crates heave, loose crates on the deck/pier slide about.
            float wake = Speedboat.WakeAt(p);
            if (wake > 0)
            {
                if (depth > 0) rb.AddForce(Vector3.up * (Mathf.Sin(Time.time * 3.1f + p.x) * 7f * wake), ForceMode.Acceleration);
                else rb.AddForce(new Vector3(Mathf.Sin(Time.time * 1.7f + p.z), 0, Mathf.Cos(Time.time * 1.3f + p.x)) * (14f * wake), ForceMode.Acceleration);
            }

            // Storm wind: loose crates outside the shed get shoved across the pier (and over the side).
            if (game != null && game.StormPhase.Value == 2 && !Tied.Value && depth <= 0 && !PortGame.InShed(p))
            {
                float gust = 0.6f + 0.4f * Mathf.Sin(Time.time * 1.3f + p.x * 0.7f + p.z * 0.3f);
                rb.AddForce(Storm.WindDir * (13f * gust * Storm.Level), ForceMode.Acceleration);
            }

            // Towed or washed back onto land: no longer "in the sea".
            if (depth > 0) dryT = 0; else if (InSea && (dryT += Time.fixedDeltaTime) > 1.5f) InSea = false;
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
            if (IsServer && c.relativeVelocity.sqrMagnitude > 1600f) Debug.Log($"[PB] FAST hit crate {NetworkObjectId} by {c.collider.name} ({(c.collider.attachedRigidbody ? c.collider.attachedRigidbody.name : "static")}) rel={c.relativeVelocity:F0}");
            if (!IsServer || c.relativeVelocity.sqrMagnitude < 9f || Time.time - lastHit < 0.15f) return;
            lastHit = Time.time;
            float speed = c.relativeVelocity.magnitude;
            ThudRpc(transform.position, Mathf.Clamp01(speed / 10f));
            // A crate flying into someone knocks them off their feet.
            var pl = c.collider.GetComponentInParent<Player>();
            if (pl != null) { if (speed > 6f) pl.Knock(rb.linearVelocity); return; }
            if (speed > 7.5f && Damage.Value < 3) Damage.Value++;
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ThudRpc(Vector3 p, float v)
        {
            Sfx.Play("thud", p, 0.3f + 0.7f * v);
            if (Player.Local != null && (Player.Local.transform.position - p).sqrMagnitude < 16f) Player.Shake(0.25f * v);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void SplashRpc(Vector3 p) { Sfx.Play("splash", p, 0.9f); Fx.Splash(p, 0.7f); }

        // ---- visuals (every peer) ----
        void LateUpdate()
        {
            float f = Fresh.Value;
            // Spoiling: fish go olive, the ice goes grey.
            float fresh01 = Mathf.SmoothStep(0, 1, (f - 0.1f) / 0.7f);
            for (int i = 0; i < spoilers.Length; i++)
            {
                var baseC = Art.ColorOf(spoilers[i].sharedMaterial);
                Art.Tint(spoilers[i], mpb, i == 0 ? Color.Lerp(Art.Hex("#9a9a88"), baseC, fresh01) : Color.Lerp(Art.Rot, baseC, fresh01));
            }

            // Broken slats show the damage.
            int broken = 0;
            foreach (Transform ch in vis)
                if (ch.name == "slat") ch.gameObject.SetActive(broken++ >= Damage.Value * 3);

            bool spoiled = f < Spoiled;
            if (spoiled && flies == null) flies = MakeFlies();
            if (flies != null && flies.gameObject.activeSelf != spoiled) flies.gameObject.SetActive(spoiled);

            // Wobble: carried crates sway with the carrier's stack sway (purely visual).
            if (Attached.Value && Slot.Value >= 0 && transform.parent != null && transform.parent.TryGetComponent<Player>(out var pl))
            {
                var s = pl.Sway;
                int i = Slot.Value;
                // The stack bends: higher crates swing further and tilt more. A forward/back lean is hard to see
                // from behind, so the closer it is to toppling the more it also swings side to side.
                float mag = s.magnitude;
                var v = new Vector2(s.x + Mathf.Sin(Time.time * 6.5f + pl.OwnerClientId) * mag * 0.7f, s.y);
                // Slightly messy stack: a fixed per-crate stagger.
                float jx = ((NetworkObjectId * 37) % 9 - 4) * 0.01f, jyaw = ((NetworkObjectId * 53) % 13 - 6) * 1.2f;
                vis.localPosition = new Vector3(v.x * 0.075f * i * i + jx * i, 0, v.y * 0.075f * i * i);
                vis.localRotation = Quaternion.Euler(v.y * 14f * i, jyaw * Mathf.Min(i, 1), -v.x * 14f * i);
            }
        }

        ParticleSystem MakeFlies()
        {
            var g = new GameObject("flies");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = Vector3.up * 0.42f; // a little swarm just over the fish
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop();
            var m = ps.main;
            m.startLifetime = 1.4f; m.startSpeed = 0f; m.startSize = 0.03f; m.maxParticles = 18;
            m.startColor = new Color(0.08f, 0.08f, 0.06f); m.simulationSpace = ParticleSystemSimulationSpace.Local;
            var e = ps.emission; e.rateOverTime = 14;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.16f;
            // Circling tight over the fish (a loose cloud read as specks on the ground).
            var n = ps.noise; n.enabled = true; n.strength = 0.12f; n.frequency = 4f;
            var orb = ps.velocityOverLifetime; orb.enabled = true; orb.space = ParticleSystemSimulationSpace.Local;
            orb.orbitalY = 5.5f; orb.radial = -0.05f;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Art.Particle;
            ps.Play();
            var a = g.AddComponent<AudioSource>();
            a.clip = Sfx.Clip("flies"); a.loop = true; a.volume = 0.12f; a.spatialBlend = 1; a.maxDistance = 8; a.rolloffMode = AudioRolloffMode.Linear;
            a.Play();
            return ps;
        }
    }
}
