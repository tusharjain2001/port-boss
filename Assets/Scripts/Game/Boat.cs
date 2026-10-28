using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Kinematic fishing boat, host-driven: sails in on a curve, docks rocking, shouts a countdown,
    // and leaves with whatever is still on deck.
    public class Boat : NetworkBehaviour
    {
        public enum St : byte { Arriving, Docked, Leaving }
        public static Boat Docked; // the boat at the pier right now (every peer)
        public static readonly List<Boat> All = new();

        public NetworkVariable<St> State = new(St.Arriving);
        public NetworkVariable<short> SecondsLeft = new(-1); // while docked
        public const float DeckY = 1.6f, HalfW = 2.3f, HalfL = 6f, DockSeconds = 120f, SailSeconds = 34f;
        public static readonly Vector3 DockPos = new(7.6f, 0, 16f);
        // In from the open sea to the north-east, round the lighthouse, alongside the pier; out the same way.
        static readonly Vector3[] InPath = { new(200, 0, 330), new(70, 0, 170), new(7.6f, 0, 78), DockPos };
        static readonly Vector3[] OutPath = { DockPos, new(7.6f, 0, 2), new(70, 0, 40), new(210, 0, 340) };

        [HideInInspector] public int CargoCount = 8; // set by spawner before Spawn
        Rigidbody rb;
        float t, countdown;
        int shoutIdx;
        readonly List<Crate> cargo = new();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var v = transform;
            // Hull: navy topsides, rust bottom, cream wheelhouse at the stern (-z), mast forward.
            Art.Box(v, new Vector3(0, 0.6f, 0), new Vector3(HalfW * 2, 2.0f, HalfL * 2), Art.Navy, true, "hull");
            Art.Box(v, new Vector3(0, -0.45f, 0.2f), new Vector3(HalfW * 2 + 0.04f, 0.5f, HalfL * 2 + 0.04f), Art.Rust, false, "antifoul");
            Art.Box(v, new Vector3(0, 1.45f, 0), new Vector3(HalfW * 2 + 0.06f, 0.18f, HalfL * 2 + 0.06f), Art.Cream, false, "gunwale_stripe");
            var bow = Art.Box(v, new Vector3(0, 0.75f, HalfL + 0.9f), new Vector3(HalfW * 1.42f, 1.7f, HalfW * 1.42f), Art.Navy, true, "bow");
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Art.Box(v, new Vector3(0, DeckY - 0.03f, 0), new Vector3(HalfW * 2 - 0.1f, 0.06f, HalfL * 2 - 0.1f), Art.WoodLight, false, "deck");
            // Wheelhouse
            Art.Box(v, new Vector3(0, DeckY + 1.1f, -HalfL + 1.2f), new Vector3(3.0f, 2.2f, 2.2f), Art.Cream, true, "wheelhouse");
            Art.Box(v, new Vector3(0, DeckY + 2.3f, -HalfL + 1.2f), new Vector3(3.3f, 0.2f, 2.5f), Art.Red, false, "wh_roof");
            Art.Box(v, new Vector3(0, DeckY + 1.5f, -HalfL + 2.31f), new Vector3(2.4f, 0.6f, 0.04f), Art.Hex("#24323d"), false, "wh_window", 0.9f);
            // Mast + boom
            Art.Prim(PrimitiveType.Cylinder, v, new Vector3(0, DeckY + 2.6f, HalfL - 2.2f), Quaternion.identity, new Vector3(0.18f, 2.6f, 0.18f), Art.WoodDark, false, "mast");
            Art.Box(v, new Vector3(0, DeckY + 3.4f, HalfL - 3.6f), new Vector3(0.12f, 0.12f, 3f), Art.WoodDark, false, "boom").transform.localRotation = Quaternion.Euler(-25, 0, 0);
            // Captain by the wheelhouse door.
            Art.Prim(PrimitiveType.Capsule, v, new Vector3(-1.0f, DeckY + 0.85f, -HalfL + 2.8f), Quaternion.identity, new Vector3(0.6f, 0.85f, 0.6f), Art.Hex("#f2c230"), true, "captain");
            Art.Prim(PrimitiveType.Sphere, v, new Vector3(-1.0f, DeckY + 1.85f, -HalfL + 2.8f), Quaternion.identity, Vector3.one * 0.38f, Art.Hex("#b98668"), false, "captain_head");
            Art.Box(v, new Vector3(-1.0f, DeckY + 2.08f, -HalfL + 2.8f), new Vector3(0.44f, 0.14f, 0.44f), Art.Navy, false, "captain_cap");
            // Rails with an opening on the +x side (that side faces the pier when docked) for the gangplank.
            Rail(new Vector3(-HalfW + 0.06f, 0, 0), new Vector3(0.1f, 0.45f, HalfL * 2));
            Rail(new Vector3(HalfW - 0.06f, 0, -HalfL / 2 - 0.6f), new Vector3(0.1f, 0.45f, HalfL - 1.2f));
            Rail(new Vector3(HalfW - 0.06f, 0, HalfL / 2 + 0.6f), new Vector3(0.1f, 0.45f, HalfL - 1.2f));
            Rail(new Vector3(0, 0, HalfL - 0.06f), new Vector3(HalfW * 2, 0.45f, 0.1f));
            Rail(new Vector3(0, 0, -HalfL + 0.06f), new Vector3(HalfW * 2, 0.45f, 0.1f));
        }

        void Rail(Vector3 p, Vector3 s) => Art.Box(transform, p + Vector3.up * (DeckY + s.y / 2), s, Art.Cream, true, "rail");

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            State.OnValueChanged += (_, s) => OnState(s);
            OnState(State.Value);
            if (!IsServer) return;
            Pose(Bezier(InPath, 0), Tangent(InPath, 0));
            // Cargo rides in stacked on deck, parented (not simulated) until we dock.
            for (int i = 0; i < CargoCount; i++)
            {
                var c = Instantiate(PortGame.I.CratePrefab).GetComponent<Crate>();
                c.NetworkObject.Spawn(true);
                int col = i % 3, row = (i / 3) % 4, layer = i / 12;
                c.Attach(NetworkObject, new Vector3(-1.2f + col * 1.2f, DeckY + Crate.Size / 2 + 0.02f + layer * (Crate.Size + 0.02f), -1.6f + row * 1.3f), -1);
                cargo.Add(c);
            }
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (Docked == this) Docked = null;
        }

        void OnState(St s)
        {
            if (s == St.Docked) Docked = this;
            else if (Docked == this) Docked = null;
            Sfx.Play("horn", transform.position, 1f);
        }

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            float dt = Time.fixedDeltaTime;
            t += dt;
            var game = PortGame.I;
            switch (State.Value)
            {
                case St.Arriving:
                {
                    float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / SailSeconds));
                    Pose(Bezier(InPath, u), Tangent(InPath, Mathf.Min(u, 0.995f)));
                    if (t >= SailSeconds)
                    {
                        State.Value = St.Docked; t = 0; countdown = game != null ? game.Today.DockSeconds : DockSeconds; shoutIdx = 0;
                        while (shoutIdx < Marks.Length && Marks[shoutIdx] >= countdown) shoutIdx++;
                        foreach (var c in cargo)
                            if (c != null && c.IsSpawned && c.transform.parent == transform)
                                c.Release(c.transform.position, Vector3.zero, Vector3.zero);
                        cargo.Clear();
                        game?.Shout(game.CaptainName, countdown < 100 ? PortGame.Pick(CaptainLinesFast) : PortGame.Pick(CaptainLines), 4.5f);
                    }
                    break;
                }
                case St.Docked:
                {
                    Pose(DockPos, Vector3.back);
                    countdown -= dt;
                    short secs = (short)Mathf.CeilToInt(Mathf.Max(0, countdown));
                    if (secs != SecondsLeft.Value) SecondsLeft.Value = secs;
                    if (shoutIdx < Marks.Length && countdown <= Marks[shoutIdx])
                    {
                        game?.Shout(game.CaptainName, PortGame.Pick(MarkLines[shoutIdx]), 3.5f);
                        if (Marks[shoutIdx] <= 10) game?.TootRpc(transform.position);
                        shoutIdx++;
                    }
                    if (countdown <= 0)
                    {
                        int left = TakeAboard();
                        State.Value = St.Leaving; t = 0;
                        game?.BoatLeaving(left);
                    }
                    break;
                }
                case St.Leaving:
                {
                    float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / SailSeconds));
                    Pose(Bezier(OutPath, u), Tangent(OutPath, Mathf.Max(u, 0.005f)));
                    if (t >= SailSeconds)
                    {
                        foreach (var c in cargo) if (c != null && c.IsSpawned) c.NetworkObject.Despawn();
                        game?.BoatGone();
                        NetworkObject.Despawn();
                    }
                    break;
                }
            }
        }

        static readonly string[] CaptainLines =
        {
            "Two minutes, I've got a tide to catch!",
            "Fresh catch! Two minutes and I'm gone!",
            "Two minutes! Whatever's still aboard comes back out with me!",
            "Morning, boss! Two minutes. The fish won't carry themselves.",
        };
        static readonly string[] CaptainLinesFast =
        {
            "Ninety seconds, not one more! Tide's turning!",
            "Big catch, short tide! Ninety seconds, GO!",
            "Bjorn doesn't wait. Ninety seconds!",
        };
        static readonly float[] Marks = { 90, 60, 30, 10 };
        static readonly string[][] MarkLines =
        {
            new[] { "Ninety seconds! Chop chop!", "A minute and a half, boss. I'm timing you." },
            new[] { "One minute! My nets won't mend themselves!", "Sixty seconds! The tide waits for no one!", "A minute left! Then this fish goes back to sea!" },
            new[] { "Thirty seconds! I can feel the tide turning!", "Half a minute! Move those legs!", "Thirty! Why is nobody running?!" },
            new[] { "Ten! Untying the ropes!", "Ten seconds! Mind the gangplank!", "TEN! Off my boat or come fishing!" },
        };

        // Crates still on deck get lashed down and sail away with us.
        int TakeAboard()
        {
            int n = 0;
            foreach (var c in Crate.All.ToArray())
            {
                if (c.Attached.Value) continue;
                var l = transform.InverseTransformPoint(c.transform.position);
                if (Mathf.Abs(l.x) < HalfW + 0.1f && Mathf.Abs(l.z) < HalfL + 0.1f && l.y > DeckY - 0.3f && l.y < DeckY + 4f)
                {
                    c.Attach(NetworkObject, l, -1);
                    cargo.Add(c);
                    n++;
                }
            }
            return n;
        }

        void Pose(Vector3 p, Vector3 fwd)
        {
            float tw = Water.T;
            fwd.y = 0;
            var rot = Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.back);
            var right = rot * Vector3.right * 2f;
            var ahead = rot * Vector3.forward * 4f;
            float y = Water.Height(p.x, p.z, tw) + 0.1f;
            float roll = (Water.Height(p.x + right.x, p.z + right.z, tw) - Water.Height(p.x - right.x, p.z - right.z, tw)) * 9f + Mathf.Sin(tw * 0.9f) * 2.5f
                + Speedboat.WakeAt(p) * 7f * Mathf.Sin(tw * 2.4f); // rival's wake rocks the boat
            float pitch = (Water.Height(p.x - ahead.x, p.z - ahead.z, tw) - Water.Height(p.x + ahead.x, p.z + ahead.z, tw)) * 5f + Mathf.Sin(tw * 0.63f + 1f) * 1.2f;
            rb.MovePosition(new Vector3(p.x, y, p.z));
            rb.MoveRotation(rot * Quaternion.Euler(pitch, 0, roll));
        }

        static Vector3 Bezier(Vector3[] c, float u)
        {
            float v = 1 - u;
            return v * v * v * c[0] + 3 * v * v * u * c[1] + 3 * v * u * u * c[2] + u * u * u * c[3];
        }

        static Vector3 Tangent(Vector3[] c, float u)
        {
            float v = 1 - u;
            return 3 * v * v * (c[1] - c[0]) + 6 * v * u * (c[2] - c[1]) + 3 * u * u * (c[3] - c[2]);
        }
    }

    // Plank from the docked boat's rail opening to the pier; tilts as the boat rocks. Local on every peer.
    public class Gangplank : MonoBehaviour
    {
        Transform plank;

        public static void Create()
        {
            var g = new GameObject("Gangplank");
            var gp = g.AddComponent<Gangplank>();
            gp.plank = Art.Box(g.transform, Vector3.zero, new Vector3(1.1f, 0.1f, 1f), Art.WoodLight, true, "plank_gp").transform;
            for (int i = 0; i < 5; i++)
                Art.Box(gp.plank, new Vector3(0, 0.6f, -0.4f + i * 0.2f), new Vector3(1f, 0.6f, 0.04f), Art.WoodDark, false, "cleat");
        }

        void LateUpdate()
        {
            var b = Boat.Docked;
            plank.gameObject.SetActive(b != null);
            if (b == null) return;
            var a = b.transform.TransformPoint(new Vector3(Boat.HalfW - 0.05f, Boat.DeckY + 0.05f, 0));
            var p = World.PierGangPoint + Vector3.up * 0.05f;
            var mid = (a + p) / 2;
            transform.SetPositionAndRotation(mid, Quaternion.LookRotation(a - p));
            plank.localScale = new Vector3(1.1f, 0.1f, (a - p).magnitude + 0.3f);
            // Cleats are children of the plank; keep them square despite the z-scale.
            foreach (Transform c in plank) c.localScale = new Vector3(1f, 0.6f, 0.04f / plank.localScale.z);
        }
    }
}
