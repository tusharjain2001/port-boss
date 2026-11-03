using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // The player in M1 (solo): walks the pier, the shore and Biscuit's deck, swims, climbs ladders, takes the helm and
    // fishes. On the deck the body moves in the boat's own space (systems §1.6.1): each frame it is first carried by
    // the boat's move (position and turn), then walks, so a rocking, moving boat never slides it.
    // Feel numbers from replan/feel: walk 1.4, run 4.2, sprint 5.8 m/s on land (ramping up); on Biscuit's small deck no
    // sprint and a calmer 2.6 m/s; eased starts, stops and turns that get heavier with the fish carried; a skid on a hard
    // reverse; a lean from acceleration; a crouch before every jump.
    [DefaultExecutionOrder(0)]
    public class Angler : MonoBehaviour
    {
        public static Angler I;           // the local player (co-op crewmates are Remote copies, driven by their poses)
        public CharacterController Cc;
        public CharAnim Anim;
        public Transform Model, Head;     // Head: the head bone (co-op name tags sit over it, standing or lying)
        public Hotbar Bar = new();
        public Fishing Fish;

        public bool OnBoat { get; private set; }
        public bool Swimming { get; private set; }
        public bool AtHelm => helm;
        public bool Busy;                 // fishing owns the body (fight, reveal): no walking
        public float Lie, LieH;           // M3: 0..1 lying down on a bed, the bed's height
        public float FaceYaw = float.NaN; // fishing faces the aim
        public Vector3 Vel;               // own walking velocity (not the boat's)
        public float Size = 1;            // body height / the 1.78 m worker the rod and hold poses were made on
        public float BellyFront, BellyHalf; // this body's belly (m): the rod is held just in front of it
        public Vector3 HeadC; public float HeadR, HeadLo, HeadHi; // head + hat as an upright cylinder (Model space, rest pose): the rod keeps out
        public const float SwimDepth = 1.3f;
        // co-op (M2): which crew member and player colour this body wears; a Remote body is someone else's, placed by
        // the poses they send (Coop) instead of input and physics
        public bool Remote; public int Crew; public Color Colour = Coop.Colours[0]; public string NetName;
        public readonly NetTrack<Pose> Net = new();

        // feel (replan/feel spec): the sprint's run-up, where the camera follows height from, the carried load
        public float SprintW { get; private set; }  // 0..1: run 4.2 -> sprint 5.8 m/s over 0.6 s, back in 0.2 s
        public float CamY { get; private set; }     // the ground we last stood on (a jump doesn't lift the camera)
        public bool Skidding => skidT > 0;
        // Fish carried, as the feel spec's crates: every 10 kg is one (M1's tier-1 fish weigh grams; a big conger counts).
        public float Load { get { float kg = 0; foreach (var s in Bar.S) foreach (var f in s.Fish) kg += f.Kg; return Mathf.Clamp(kg / 10f, 0, 5); } }

        Vector3 localOnBoat; float localYaw;
        float vy, lastGround;
        bool helm;
        Vector3 lastPos; float animSpeed;
        int stepI; float lastPhase;
        bool wasSwim; float dunkT = -1;
        float sit, lastYaw;
        Vector3 velV, lastVel;                    // the walking spring, last frame's walk (lean from acceleration)
        float skidT;                              // > 0: skidding to a stop on a hard reverse
        float jumpBuf = -9, crouchT = -1, jumpCut, airT, peakY; bool rising, wasAir;
        float leanP, leanPV, leanR, leanRV;       // body lean forward / sideways (degrees) on a spring
        float sq, sqV;                            // squash (-) and stretch (+) of the body on take-off and landing
        float standT;                             // how long she has stood still aboard
        byte hop;                                 // counts her jumps to a new place (Teleport): co-op never blends across one

        public static Angler Create(M1Assets a, Vector3 at, float yaw)
        {
            var g = new GameObject("Angler");
            g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var p = g.AddComponent<Angler>(); I = p;
            p.Cc = g.AddComponent<CharacterController>();
            p.Cc.height = 1.75f; p.Cc.radius = 0.3f; p.Cc.center = new Vector3(0, 0.875f, 0); p.Cc.stepOffset = 0.42f; p.Cc.slopeLimit = 50; p.Cc.skinWidth = 0.03f;
            p.Cc.minMoveDistance = 0; // the default 1 mm drops every step at high frame rates (uncapped test runs: the swimmer barely moved)
            p.Model = new GameObject("model").transform; p.Model.SetParent(g.transform, false);
            PortBoss.Art.Lit = new Material(a.Toon);
            PortBoss.Art.Lit.SetFloat("_PaintNoise", 0.12f);
            p.Bar.Selected = 1; // empty hands at the start: "1" draws the rod (F1)
            // which body: -pb-body <name> (tests), else Pip
            var args = System.Environment.GetCommandLineArgs();
            int bi = System.Array.IndexOf(args, "-pb-body");
            if (bi >= 0 && bi + 1 < args.Length && a.CrewNames != null)
                Body = Mathf.Max(0, System.Array.FindIndex(a.CrewNames, n => n.Equals(args[bi + 1], System.StringComparison.OrdinalIgnoreCase)));
            p.Wear(a, Body, Coop.Colours[0]);
            p.Fish = g.AddComponent<Fishing>();
            p.lastPos = at;
            return p;
        }

        // A crewmate's body in co-op: the same body and clips, no controller of its own (it goes where their poses say).
        public static Angler CreateRemote(M1Assets a, int crew, Color colour, Vector3 at)
        {
            var g = new GameObject("Mate"); g.transform.position = at;
            var p = g.AddComponent<Angler>(); p.Remote = true;
            p.Cc = g.AddComponent<CharacterController>(); p.Cc.height = 1.75f; p.Cc.radius = 0.3f; p.Cc.center = new Vector3(0, 0.875f, 0); p.Cc.enabled = false;
            p.Model = new GameObject("model").transform; p.Model.SetParent(g.transform, false);
            p.Bar.Selected = 1;
            p.Wear(a, crew, colour);
            p.Fish = g.AddComponent<Fishing>();
            p.lastPos = at;
            return p;
        }

        public static int Body;    // which of M1Assets.Crew she is (F9 in game steps through them)
        GameObject body;
        float BodyK => Size / (1.47f / 1.78f); // this body against Pip's, which the clip lifts and the get-up creep were sampled on

        public void Wear(int index, Color colour) => Wear(M1Assets.I, index, colour);

        // The head and hat (brim, helmet) as a cylinder round the head joint, from the mesh as built (the rest pose, before
        // any clip): from the neck up, as wide as the mesh is above the head joint (the brim, the helmet: the T-posed arms
        // are lower) within 0.3 x the height of the head's axis.
        // Look audit 3 Nov: on the cast's wind-up the rod lay through Pip's sou'wester brim, the mitt inside it.
        void MeasureHead(Animator anim)
        {
            HeadR = 0; if (Head == null) return;
            var neck = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Neck) : null; float h = Size * 1.78f;
            var c = Model.InverseTransformPoint(Head.position);
            float lo = neck != null ? Model.InverseTransformPoint(neck.position).y : c.y - 0.1f * h, hi = c.y, r = 0, cap = 0.3f * h; // (Sully's rig has no neck)
            foreach (var smr in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = smr.sharedMesh; if (m == null || !m.isReadable) continue;
                var toModel = Model.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                foreach (var v in m.vertices)
                {
                    var q = toModel.MultiplyPoint3x4(v); if (q.y < c.y - 0.03f) continue;
                    float d = new Vector2(q.x - c.x, q.z - c.z).magnitude; if (d > cap) continue;
                    r = Mathf.Max(r, d); hi = Mathf.Max(hi, q.y);
                }
            }
            HeadC = c; HeadLo = lo; HeadHi = hi; HeadR = r;
            Debug.Log($"[PB] head of {body.name}: {r:F2} m round, {lo:F2}-{hi:F2} m up");
        }

        // Put on a body: the crew member's model under Model (or the placeholder worker when the kit isn't built), its clip
        // driver, ragdoll and springs. Swapped in place by F9, so it builds everything a body needs.
        void Wear(M1Assets a, int index, Color colour)
        {
            if (body != null) { body.SetActive(false); Destroy(body); }
            rag = null; if (Remote) { shown = 0; downT = getUpT = -1; }
            if (a.Crew != null && a.Crew.Length > 0)
            {
                // a crew member from the character kit (art-src/kit): vertex-coloured like the boat, in the toon shader
                index = (index % a.Crew.Length + a.Crew.Length) % a.Crew.Length; Crew = index; if (!Remote) Body = index;
                body = Instantiate(a.Crew[index], Model, false); body.name = a.CrewNames[index].ToLowerInvariant();
                body.transform.localRotation = Quaternion.Euler(0, 180, 0); // exported facing -Z, like the worker
                foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = PortBoss.Art.Lit; r.updateWhenOffscreen = true; Paint(r, colour); }
                Colour = colour;
                // size against the 1.78 m worker the rod poses were made on, from the Head joint (at 82% of the height)
                var head = body.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                Size = head != null ? (head.position.y - Model.position.y) / (0.82f * 1.78f) : 1;
                var belly = a.CrewBelly != null && index < a.CrewBelly.Length ? a.CrewBelly[index] : Vector2.zero;
                BellyFront = belly.x; BellyHalf = belly.y;
            }
            else
            {
                // the placeholder dock worker (Assets/Art/Worker) with his palette texture
                var w = PortBoss.Worker.Make(a.Worker, Model);
                w.Apply(new PortBoss.Look
                {
                    Set = 1, Face = 0, Hair = 3, Beard = 2, Hat = 2, Top = 2, Gloves = 0,
                    SkinTone = 2, HairColor = 2, TopColor = 1, AccentColor = 4, LegsColor = 4, BootsColor = 7, GloveColor = 4, HatColor = 2,
                });
                var rs = w.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                if (bb.size.y > 0.5f) w.transform.localScale = Vector3.one * Mathf.Clamp(1.78f / bb.size.y, 0.8f, 1.25f);
                body = w.gameObject; Size = 1; BellyFront = BellyHalf = 0;
            }
            foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            var anim = body.GetComponentInChildren<Animator>();
            Head = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            MeasureHead(anim);
            var skin = ScriptableObject.CreateInstance<PortBoss.Skin>();
            skin.Idle = a.Idle; skin.Walk = a.Walk; skin.Run = a.Run; skin.Sprint = a.Sprint; skin.Jump = a.Jump; skin.Land = a.Land;
            skin.SwimIdle = a.SwimIdle; skin.Swim = a.Swim; skin.Climb = a.Climb; skin.Drive = a.Drive ?? a.Idle; skin.Push = a.Idle;
            Anim = anim.gameObject.AddComponent<CharAnim>();
            Anim.Init(anim, skin); Anim.Facing = transform; Anim.GripLean = 0;
            Anim.LandFrom = 0.25f; // Falling To Landing's feet touch down here: before it, it's still falling
            if (a.Crew != null && a.Crew.Length > 0) rag = Ragdoll.Build(anim, Cc); // (a crewmate's only ever lies as they send it)
            // a floppy head, and the hat (its own bone in the kit) wobbling on top of it
            var jig = Jiggle.On(anim);
            jig.Bone(anim.GetBoneTransform(HumanBodyBones.Head), Vector3.up, 0.25f * Size / 0.83f, 220f, 0.55f, 8f);
            jig.Bone(Util.Find(body.transform, "Hat"), Vector3.up, 0.15f * Size / 0.83f, 140f, 0.35f, 12f, false);
            overW = 0; overClip = null;
        }

        // The player colour (orange, blue, green, red by join slot) on the armband and back patch: the kit paints them the
        // slot-0 orange (kitlib `player`), swapped here on a copy of the mesh.
        static void Paint(SkinnedMeshRenderer r, Color to)
        {
            var src = r.sharedMesh; var cols = src.colors32; if (cols == null || cols.Length == 0) return;
            Color32 key = Coop.Colours[0], c = to; int n = 0;
            for (int i = 0; i < cols.Length; i++)
                if (Mathf.Abs(cols[i].r - key.r) < 10 && Mathf.Abs(cols[i].g - key.g) < 10 && Mathf.Abs(cols[i].b - key.b) < 10) { cols[i] = new Color32(c.r, c.g, c.b, cols[i].a); n++; }
            if (n == 0 || to == Coop.Colours[0]) return;
            var m = Instantiate(src); m.name = src.name + " (player colour)"; m.colors32 = cols; r.sharedMesh = m;
        }

        // ---- co-op: what this player sends, and how a crewmate's body follows what they sent ----
        public struct Pose
        {
            public float T; public byte Flags; public Vector3 P, Vel; public float Yaw, AirT, Vy, SprintW;
            // State 0 up, 1 knocked over (Hips + Rot: where the ragdoll's parts lie), 2 getting up (Up: which get-up, +128 on
            // her back; UpT: how far into it), 3 tossing a fish (Up: its species; UpT: how far into the throw), 4 patching
            // the hole (UpT: s into it), 5 shoving a crewmate (UpT: s into it). Hop: her
            // Teleport count (a ladder, the end of a get-up: jumps, never blends).
            public byte State, Hop, Up; public float UpT; public Vector3 Hips; public Quaternion[] Rot;
            public Fishing.Net F; // their fishing (M2 step 3)
            public bool OnBoat => (Flags & 1) != 0; public bool Swim => (Flags & 2) != 0; public bool Helm => (Flags & 4) != 0; public bool Grounded => (Flags & 8) != 0;
            public bool Jumped => (Flags & 16) != 0; public bool FallIn => (Flags & 32) != 0; public bool Crouch => (Flags & 64) != 0; public bool Skid => (Flags & 128) != 0;
            public void Write(ref Unity.Netcode.FastBufferWriter w)
            {
                w.WriteValueSafe(T); w.WriteValueSafe(Flags); w.WriteValueSafe(P); w.WriteValueSafe(Vel); w.WriteValueSafe(Yaw); w.WriteValueSafe(AirT); w.WriteValueSafe(Vy); w.WriteValueSafe(SprintW);
                w.WriteValueSafe(State); w.WriteValueSafe(Hop);
                if (State == 1) { w.WriteValueSafe(Hips); w.WriteValueSafe((byte)Rot.Length); foreach (var q in Rot) w.WriteValueSafe(q); }
                else if (State >= 2) { w.WriteValueSafe(Up); w.WriteValueSafe(UpT); }
                F.Write(ref w);
            }
            public static Pose Read(ref Unity.Netcode.FastBufferReader r)
            {
                var p = new Pose(); r.ReadValueSafe(out p.T); r.ReadValueSafe(out p.Flags); r.ReadValueSafe(out p.P); r.ReadValueSafe(out p.Vel);
                r.ReadValueSafe(out p.Yaw); r.ReadValueSafe(out p.AirT); r.ReadValueSafe(out p.Vy); r.ReadValueSafe(out p.SprintW);
                r.ReadValueSafe(out p.State); r.ReadValueSafe(out p.Hop);
                if (p.State == 1) { r.ReadValueSafe(out p.Hips); r.ReadValueSafe(out byte n); p.Rot = new Quaternion[n]; for (int i = 0; i < n; i++) r.ReadValueSafe(out p.Rot[i]); }
                else if (p.State >= 2) { r.ReadValueSafe(out p.Up); r.ReadValueSafe(out p.UpT); }
                p.F = Fishing.Net.Read(ref r);
                return p;
            }
        }

        // Aboard: the place and facing in the boat's own space, so the receiver puts them on its copy of the deck (the
        // ragdoll's parts too). Swimming: the height over her float line, so she floats on the receiver's own sea.
        public Pose NetPose(float t)
        {
            int f = (OnBoat ? 1 : 0) | (Swimming ? 2 : 0) | (helm ? 4 : 0) | (Grounded ? 8 : 0) | (jumped ? 16 : 0) | (fallIn ? 32 : 0) | (crouchT >= 0 ? 64 : 0) | (skidT > 0 ? 128 : 0);
            var p = new Pose { T = t, Flags = (byte)f, P = OnBoat ? localOnBoat : transform.position, Yaw = OnBoat ? localYaw : transform.eulerAngles.y, Vel = Vel, AirT = airT, Vy = vy, SprintW = SprintW, Hop = hop };
            if (Swimming) p.P.y -= Sea.Height(transform.position) - SwimDepth;
            var s = Skiff.I; bool boat = OnBoat && s != null;
            if (downT >= 0 && rag != null)
            {
                var inv = boat ? Quaternion.Inverse(s.transform.rotation) : Quaternion.identity;
                p.State = 1; p.Hips = boat ? s.transform.InverseTransformPoint(rag.Hips.position) : rag.Hips.position;
                p.Rot = new Quaternion[rag.Parts.Count]; for (int i = 0; i < p.Rot.Length; i++) p.Rot[i] = inv * rag.Parts[i].rotation;
            }
            else if (getUpT >= 0) { p.State = 2; p.Up = (byte)(upI | (upBack ? 128 : 0)); p.UpT = getUpT; }
            else if (ThrowT >= 0) { p.State = 3; p.Up = (byte)Mathf.Max(0, throwSp); p.UpT = ThrowT; }
            else if (RepairT >= 0) { p.State = 4; p.UpT = RepairT; }
            else if (ShoveT >= 0) { p.State = 5; p.UpT = ShoveT; }
            if (Fish != null) p.F = Fish.NetState();
            return p;
        }

        bool netGrounded = true;
        bool Grounded => Remote ? netGrounded : Cc.isGrounded;

        byte shown; // what a crewmate's body shows: 0 up, 1 knocked over, 2 getting up (Pose.State)

        void RemoteUpdate(float dt)
        {
            if (!Net.Sample(out var a, out var b, out float k)) return;
            var s = Skiff.I;
            // boarding, leaving, the tiller, into or out of the sea, a jump to a new place, going down or getting up: no
            // blend between two places
            if (a.OnBoat != b.OnBoat || a.Helm != b.Helm || a.Swim != b.Swim || a.Hop != b.Hop || a.State != b.State) a = b;
            if (b.State != 4) RepairT = -1;
            if (Fish != null) { Fish.NetA = a.F; Fish.NetB = b.F; Fish.NetK = k; } // their fishing plays from the same moment
            Vector3 p = Vector3.Lerp(a.P, b.P, k); float yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, k);
            OnBoat = b.OnBoat && s != null;
            if (b.Swim) p.y += Sea.Height(p) - SwimDepth;
            // up again: the ragdoll's pose blends out from where it lay (before the root moves, as GetUp does)
            if (shown == 1 && b.State != 1) { rag.Stop(b.State == 2 && (b.Up & 128) != 0 ? 0.45f : 0.3f); downT = -1; }
            if (shown == 2 && b.State != 2) { getUpT = -1; Anim.Over = null; Anim.OverW = overW = 0; }
            if (OnBoat) { localOnBoat = p; localYaw = yaw; transform.SetPositionAndRotation(s.transform.TransformPoint(p), Quaternion.Euler(0, s.transform.eulerAngles.y + yaw, 0)); }
            else transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            if (b.State == 1 && rag != null && b.Rot != null) { shown = 1; RemoteLying(a, b, k, dt); return; }
            var ups = M1Assets.I.GetUps;
            if (b.State == 2 && ups != null && ups.Length > 0)
            {
                if (shown != 2) { upI = (b.Up & 127) % ups.Length; up = ups[upI]; }
                shown = 2; getUpT = Mathf.Lerp(a.UpT, b.UpT, k); UpPose(getUpT);
                return;
            }
            shown = 0;
            if (b.State == 3) { ThrowT = a.State == 3 ? Mathf.Lerp(a.UpT, b.UpT, k) : b.UpT; throwSp = b.Up; } else ThrowT = -1;
            RepairT = b.State == 4 ? (a.State == 4 ? Mathf.Lerp(a.UpT, b.UpT, k) : b.UpT) : -1;
            float shoveWas = ShoveT; ShoveT = b.State == 5 ? (a.State == 5 ? Mathf.Lerp(a.UpT, b.UpT, k) : b.UpT) : -1;
            if (shoveWas < 0 && ShoveT >= 0) Snd.Play(Synth.Whoosh, transform.position, 0.4f, 1.5f);
            Vel = Vector3.Lerp(a.Vel, b.Vel, k); SprintW = b.SprintW; airT = b.AirT; vy = b.Vy;
            Swimming = b.Swim; helm = b.Helm; netGrounded = b.Grounded; jumped = b.Jumped; fallIn = b.FallIn;
            crouchT = b.Crouch ? Mathf.Max(0, crouchT) + dt : -1; skidT = b.Skid ? 0.1f : 0;
            if (netGrounded) lastGround = Time.time;
            if (Swimming && !wasSwim) Splash(Sea.Height(transform.position), 1f);
            wasSwim = Swimming;
            Animate(dt);
            wasAir = !netGrounded && !Swimming;
        }

        // A crewmate knocked over: their ragdoll's parts as their machine has them (in my copy of the boat when aboard).
        void RemoteLying(Pose a, Pose b, float k, float dt)
        {
            if (downT < 0)
            {
                // over they go: as Knock, with the thud (the shake and the hit-stop are theirs alone)
                Anim.Over = null; Anim.OverW = overW = 0; Anim.Flail = false; Anim.Grip = null; Anim.Act = null;
                Model.localPosition = Vector3.zero; Model.localRotation = Quaternion.identity; Model.localScale = Vector3.one;
                leanP = leanR = leanPV = leanRV = sq = sqV = 0; crouchT = -1;
                rag.Puppet(); downT = 0; Snd.Play(M1Assets.I.Thud, transform.position, 0.6f, 1.2f);
            }
            downT += dt;
            bool blend = a.Rot != null && a.Rot.Length == b.Rot.Length;
            var s = Skiff.I; bool boat = OnBoat && s != null;
            var q0 = boat ? s.transform.rotation : Quaternion.identity;
            var rot = new Quaternion[b.Rot.Length];
            for (int i = 0; i < rot.Length; i++) rot[i] = q0 * (blend ? Quaternion.Slerp(a.Rot[i], b.Rot[i], k) : b.Rot[i]);
            var h = blend ? Vector3.Lerp(a.Hips, b.Hips, k) : b.Hips;
            rag.SetPose(boat ? s.transform.TransformPoint(h) : h, rot);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            if (Remote) { RemoteUpdate(dt); return; }
            var s = Skiff.I;
            if (downT >= 0) { Lying(dt); return; } // the ragdoll has the body (and the root stays put under it)
            // carried by the boat
            if (OnBoat && s != null)
            {
                var carried = s.transform.TransformPoint(localOnBoat);
                bool cc = Cc.enabled; Cc.enabled = false; transform.position = carried; Cc.enabled = cc;
                transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y + localYaw, 0);
            }
            if (getUpT >= 0) { GettingUp(dt); return; }
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F8) && !Busy) { Knock(transform.forward * 2.5f + Vector3.up * 1.5f); return; } // debug: fall over
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F9) && !Busy && !helm && !Fish.Locked && M1Assets.I.Crew != null && M1Assets.I.Crew.Length > 1)
            {
                // debug: the next crew member (in co-op the host picks the next free one: one each)
                if (Coop.On) Coop.Request(Coop.Ask.NextCrew);
                else { Fish.Stow(); Wear(M1Assets.I, Body + 1, Colour); Alerts.Push("Debug", "Body: " + M1Assets.I.CrewNames[Body] + " (F9 for the next)", Alerts.Kind.Info); }
            }

            if (helm) { Helm(dt); Track(); CamY = transform.position.y; Animate(dt); return; }

            // hotbar
            if (!Busy && !Fish.BarLocked)
            {
                int k = Ctl.SlotKey; if (k >= 0) Select(k);
                int sc = Ctl.SlotScroll; if (sc != 0) Select(((Bar.Selected + sc) % Hotbar.Slots + Hotbar.Slots) % Hotbar.Slots);
            }
            Items(dt);

            float surface = Sea.Height(transform.position);
            Swimming = transform.position.y < surface - SwimDepth + 0.35f && !OnBoat;
            if (Swimming && !wasSwim) { dunkT = 0; Splash(surface, 1f); }
            wasSwim = Swimming;

            // walking
            var cam = CameraRig.I;
            var input = Busy || Sleep.Frozen || RepairT >= 0 || ShoveT >= 0 ? Vector2.zero : Ctl.Move;
            var fwd = cam != null ? Quaternion.Euler(0, cam.Yaw, 0) : transform.rotation;
            var wish = fwd * new Vector3(input.x, 0, input.y);
            float load = Load;
            // Sprint ramps up over 0.6 s (a run-up, not a gear change) and drops back to a run in 0.2 s; never with 5
            // crates' worth, and every crate's worth is 8% slower (5: 2.5 m/s).
            bool sprint = Ctl.Sprint && !OnBoat && !Swimming && !Ctl.Walk && load < 5 && wish.sqrMagnitude > 0.01f;
            SprintW = Mathf.MoveTowards(SprintW, sprint ? 1 : 0, dt / (sprint ? 0.6f : 0.2f));
            float run = Mathf.Lerp(4.2f, 5.8f, Mathf.SmoothStep(0, 1, SprintW)) * (1 - 0.08f * load);
            float max = Swimming ? (Ctl.Sprint ? 2.2f : 1.4f) : Ctl.Walk ? 1.4f : OnBoat ? 2.6f : run;
            if (Fish.Holding && !Swimming) max = Mathf.Min(max, OnBoat ? 2.2f : 3.6f); // careful with a rod out
            var target = wish * max * Sleep.WalkMul * (Braced ? 0.4f : 1); // (M3: -10% from 01:30, tired; braced: a slow shuffle)
            bool grounded = Cc.isGrounded;
            // A hard reverse at speed (> 120°, over 60% of a run): a 0.15 s skid, feet planted, leaning back, then the turn.
            if (skidT <= 0 && grounded && !Swimming && Vel.magnitude > 0.6f * 4.2f && wish.sqrMagnitude > 0.25f && Vector3.Angle(wish, Vel) > 120f)
            { skidT = 0.15f; Dust(6, 1.2f); Snd.Play(Synth.Scuff, transform.position, 0.5f, 0.9f); }
            if (skidT > 0) { skidT -= dt; target = Vector3.zero; }
            // Eased starts and stops (a critically damped spring: 90% of the way in 1.94 x its time), slower per crate:
            // 0 -> 90% in 0.22 s + 0.05 s a crate, 90% -> stopped in 0.18 s + 0.07 s a crate; a skid stops in 0.15 s.
            bool speeding = target.sqrMagnitude > Vel.sqrMagnitude + 0.01f;
            float t90 = skidT > 0 ? 0.1f : Swimming ? 0.45f : speeding ? 0.22f + 0.05f * load : 0.18f + 0.07f * load;
            Vel = Vector3.SmoothDamp(Vel, target, ref velV, t90 / 1.94f, Mathf.Infinity, dt);
            if (sprint && Fish.Holding) Fish.Stow();

            // facing: where we walk, or the fishing aim; 600°/s empty, 70°/s less per crate; none while skidding
            float wantYaw = !float.IsNaN(FaceYaw) ? FaceYaw : Vel.sqrMagnitude > 0.05f ? Mathf.Atan2(Vel.x, Vel.z) * Mathf.Rad2Deg : transform.eulerAngles.y;
            float turnRate = !float.IsNaN(FaceYaw) ? 540f : skidT > 0 ? 0 : 600f - 70f * load;
            transform.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(transform.eulerAngles.y, wantYaw, turnRate * dt), 0);

            if (grounded) lastGround = Time.time;
            if (Swimming)
            {
                // float with head + shoulders out; a short dunk on the way in
                float wantY = surface - SwimDepth;
                if (dunkT >= 0) { dunkT += dt / 0.6f; wantY -= 0.3f * Mathf.Sin(Mathf.Clamp01(dunkT) * Mathf.PI); if (dunkT >= 1) dunkT = -1; }
                vy = (wantY - transform.position.y) * 6f;
                crouchT = -1; rising = false;
            }
            float g = Swimming ? 0 : Jump(dt, grounded, load);
            var step = Crowd(Vel * dt, dt, target.sqrMagnitude > 0.01f); var shove = step / dt - Vel;
            Cc.Move(step + Vector3.up * (vy - 0.5f * g * dt) * dt); // the exact arc at any frame rate
            vy -= g * dt;
            Land();
            // Stood still aboard: the controller creeps on a rolling, moving deck (sideways down the roll, and up to 15 cm up
            // off the planks as she speeds up: M2's co-op test). The deck is rigid, so standing still she keeps her exact spot.
            // (after 0.2 s stood: the spot it keeps is one she stood on, never a moment of a landing). Once held, a frame the
            // controller loses the ground doesn't let go: that's the frame it steps her up onto a bench she stands beside
            // (M2 step 2's test: 30-40 cm up at full speed).
            bool still = OnBoat && s != null && vy <= 0 && crouchT < 0 && target.sqrMagnitude < 1e-4f && Vel.sqrMagnitude < 4e-4f && shove.sqrMagnitude < 1e-4f;
            standT = still && (grounded || standT > 0.2f) ? standT + dt : 0;
            if (standT > 0.2f)
            {
                Cc.enabled = false; transform.position = s.transform.TransformPoint(localOnBoat); Cc.enabled = true;
            }

            bool wasRepair = RepairT >= 0; RepairT = -1;
            Interact();
            if (RepairT >= 0) { if (!wasRepair) { Fish.Stow(); kneelAt = KneelSpot(); } KneelAtHole(dt); }
            Shoving(dt);
            Track();
            Animate(dt);
        }

        // Co-op (M3.1, Manan + Tushar's test: "we walk through each other"): crewmates are solid. Their bodies go where their
        // poses say (no collider here), and each machine moves only its own player, so: my step into a crewmate stops at
        // their capsule (two radii apart) and slides round it, my speed into them goes too; I never move them, so walking
        // can't shove anyone off the boat. Overlapping anyway (a late pose, a teleport): out at 0.8 m/s, no jolt.
        public const float Apart = 0.6f;
        Vector3 Crowd(Vector3 step, float dt, bool pressing)
        {
            var c = Coop.I; if (c == null) return step;
            foreach (var m in c.Mates.Values)
            {
                if (m == null || m.AtHelm || m.Down || helm) continue;
                var d = transform.position - m.transform.position; float dy = Mathf.Abs(d.y); d.y = 0; float r = d.magnitude;
                if (dy > 1.6f || r > Apart + step.magnitude + 0.05f) continue;
                var n = r > 1e-3f ? d / r : -transform.forward;
                float into = -Vector3.Dot(step, n), gap = Mathf.Max(0, r - Apart);
                if (into > gap) step += n * (into - gap);                               // up to their side, then along it
                if (r < Apart + 0.02f) { float v = -Vector3.Dot(Vel, n); if (v > 0) Vel += n * v; }
                // out of an overlap: only if I'm the one pressing to move, or it's deep (a teleport, a spawn). A few cm from their
                // pose running ahead of where they stopped mustn't nudge someone standing still (crowd test: 5-11 cm, the
                // walker's pose overshot to 0.49 m on the stander's screen while it stopped at 0.60 on its own).
                if (r < Apart && (pressing || r < Apart - 0.25f)) step += n * Mathf.Min(Apart - r, 0.8f * dt);
            }
            return step;
        }

        // Put the player somewhere (tests, ladders): also re-anchors them in the boat's space so the deck carry doesn't undo it.
        public void Teleport(Vector3 p, float yaw)
        {
            Cc.enabled = false; transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); Cc.enabled = true; hop++;
            Vel = lastVel = velV = Vector3.zero; vy = 0; skidT = 0; crouchT = -1; rising = wasAir = jumped = fallIn = false; overW = 0; CamY = p.y; Track();
        }

        // Jump (feel spec): a 0.08 s crouch first (the press is buffered 0.12 s, with 0.12 s of coyote time, so it still
        // answers at once), then up: 0.8 m held, 0.45 m tapped (0.6 m held on Biscuit's deck: it clears the 0.5 m
        // gunwale when you mean it), a 0.3 m hop with 3+ crates' worth. Rises at g 18, falls at 35 (a heavy body).
        // Returns this frame's gravity.
        float Jump(float dt, bool grounded, float load)
        {
            if (!Busy && Ctl.Jump) jumpBuf = Time.time;
            if (crouchT < 0 && !Busy && Time.time - jumpBuf < 0.12f && Time.time - lastGround < 0.12f) { crouchT = 0; jumpBuf = -9; }
            if (crouchT >= 0 && (crouchT += dt) >= 0.08f)
            {
                float h = load >= 3 ? 0.3f : OnBoat ? 0.6f : 0.8f;
                vy = Mathf.Sqrt(2 * 18f * h); jumpCut = load >= 3 ? 18f : vy * vy / (2 * 0.45f);
                crouchT = -1; rising = jumped = true; lastGround = -1; sqV += 1.1f;
            }
            if (vy <= 0) rising = false;
            if (grounded && vy <= 0) { vy = -2f; return 0; }
            return vy > 0 ? (rising && !Ctl.JumpHeld ? jumpCut : 18f) : 35f;
        }

        // Landing: soft under a 1 m drop (a step, a small puff), hard above (a thud, a bigger puff, the camera dips).
        // The camera follows height from the ground stood on, not the jump, unless it's a real fall (off the pier).
        void Land()
        {
            bool air = !Cc.isGrounded && !Swimming;
            if (air && !wasAir) { airT = 0; peakY = transform.position.y; fallIn = !jumped && SeaBelow(); }
            if (air) { airT += Time.deltaTime; peakY = Mathf.Max(peakY, transform.position.y); }
            else if (wasAir && !Swimming && airT > 0.08f)
            {
                float drop = peakY - transform.position.y;
                if (drop > 1f) { Snd.Play(M1Assets.I.Thud, transform.position, 0.7f, 0.8f); Dust(10, 1.6f); CameraRig.Dip(0.1f); sqV -= 1.5f; }
                else if (drop > 0.15f) { Step(0.35f); Dust(4, 1f); sqV -= 0.9f; }
            }
            if (!air) jumped = fallIn = false;
            wasAir = air;
            if (!air) CamY = transform.position.y;
            else if (transform.position.y < CamY - 1f) CamY = transform.position.y + 1f;
        }

        // Stepping off something high over open water (the pier's edge): more than 0.9 m down to the sea, deep below.
        bool SeaBelow()
        {
            var p = transform.position; float sea = Sea.Height(p);
            bool ground = Physics.Raycast(p + Vector3.up * 0.2f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y > sea - 0.3f;
            return !ground && p.y - sea > 0.9f;
        }

        void Dust(int n, float size) { if (!Swimming) Fx.Dust(transform.position + Vector3.up * 0.04f, n, size); }
        void Step(float vol) { var st = M1Assets.I.Steps; if (st != null && st.Length > 0) Snd.Play(st[stepI++ % st.Length], transform.position, vol, Random.Range(0.92f, 1.08f)); }
        static void Spring(ref float x, ref float v, float target, float k, float zeta, float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            v += (k * (target - x) - 2 * zeta * Mathf.Sqrt(k) * v) * dt; x += v * dt;
        }

        public void TakeHelm() { var s = Skiff.I; if (s == null) return; BeginHelm(s); Fish.Stow(); }
        void BeginHelm(Skiff s)
        {
            helm = true; s.AtHelm = true; helmT = 0;
            Coop.Request(Coop.Ask.Helm); // co-op: the boat moves to the driver (its copy becomes the real one)
            helmFrom = s.transform.InverseTransformPoint(transform.position); helmFromYaw = transform.eulerAngles.y - s.transform.eulerAngles.y;
        }
        float helmT = 1, helmFromYaw; Vector3 helmFrom;

        // Where he sits at the tiller: on the stern bench, starboard of the tiller (Manan: the LEFT hand always on the
        // tiller, the right hand free), placed from the tiller's centred position so a full swing stays clear of him.
        Vector3 SeatLocal(Skiff s)
        {
            var m = s.Motor; var g = s.TillerGrip;
            Vector3 rest = m != null && g != null
                ? s.transform.InverseTransformPoint(m.parent.TransformPoint(m.localPosition + Vector3.Scale(m.localScale, m.InverseTransformPoint(g.position))))
                : s.transform.InverseTransformPoint(s.Helm.position);
            return new Vector3(rest.x + 0.6f, s.transform.InverseTransformPoint(s.Helm.position).y, rest.z - 0.02f);
        }
        public void LeaveHelm() { var s = Skiff.I; helm = false; if (s != null) { s.AtHelm = false; s.Steer = 0; } if (CameraRig.I != null) CameraRig.I.M = CameraRig.Mode.Follow; if (!Coop.IsHost) Coop.Request(Coop.Ask.LeaveHelm); }

        public void Select(int i)
        {
            if (i == Bar.Selected) return;
            Bar.Selected = i;
            Hud.I?.Selected(i);
        }

        // Which deck are we on? Re-anchor in the boat's space after every move.
        void Track()
        {
            var s = Skiff.I;
            bool on = false;
            if (s != null && !Swimming && Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out var hit, 0.9f, ~0, QueryTriggerInteraction.Ignore))
                on = hit.transform.IsChildOf(s.transform);
            if (helm) on = true;
            OnBoat = on;
            if (on) { localOnBoat = s.transform.InverseTransformPoint(transform.position); localYaw = transform.eulerAngles.y - s.transform.eulerAngles.y; }
            if (CameraRig.I != null) CameraRig.I.OnDeck = on;
        }

        // ---- E: helm, anchor, ladders, pickups ----
        public string Prompt { get; private set; }

        void Interact()
        {
            Prompt = null;
            var s = Skiff.I;
            if (Busy || Fish.Locked) return;
            // ladders from the water
            if (Swimming)
            {
                if (s != null && s.LadderBottom != null && Near(s.LadderBottom.position, 1.8f)) { Prompt = "Climb aboard"; if (Ctl.Interact) StartClimb(s.LadderBottom, s.LadderTop, s.transform); return; }
                var l = Landing.I;
                if (l != null && l.LadderBottom != null && Near(l.LadderBottom.position, 1.8f)) { Prompt = "Climb the ladder"; if (Ctl.Interact) StartClimb(l.LadderBottom, l.LadderTop, l.transform); return; }
                return;
            }
            // give (co-op): hold E 0.4 s looking at a crewmate within 3 m with a fish in hand; Shift: the whole stack
            // (before the tiller and the anchor: with a fish in hand, looking at a crewmate, that's what E is for)
            var (mateId, mate) = GiveTarget();
            if (mate != null && Bar.Sel.Fish.Count > 0 && ThrowT < 0)
            {
                bool stack = Ctl.Sprint && Bar.Sel.Fish.Count > 1;
                Prompt = $"Hold to give {mate.NetName} {(stack ? "the stack" : "a " + Bar.Sel.Fish[0].Species.Name)}{(stack || Bar.Sel.Fish.Count < 2 ? "" : " (Shift: the stack)")}";
                if (Ctl.InteractHeld)
                {
                    giveT += Time.deltaTime;
                    if (giveT >= GiveHold && !giveDone) { giveDone = true; Coop.Give(mateId, stack ? new List<Fish>(Bar.Sel.Fish) : new List<Fish> { Bar.Sel.Fish[0] }); }
                }
                else { giveT = 0; giveDone = false; }
                return;
            }
            giveT = 0; giveDone = false;
            if (s == null) return;
            if (OnBoat && s.Helm != null && Near(s.Helm.position, 1.3f))
            {
                if (s.Remote && s.AtHelm) { Prompt = "A crewmate has the tiller"; return; } // co-op: one driver
                Prompt = "Take the tiller"; if (Ctl.Interact) { BeginHelm(s); Fish.Stow(); } return;
            }
            if (OnBoat && s.AnchorPt != null && Near(s.AnchorPt.position, 1.4f) && !s.Moored) { Prompt = s.AnchorT >= 0 ? "…" : s.Anchored ? "Raise the anchor" : "Drop the anchor"; if (Ctl.Interact) { if (s.Remote) Coop.Request(Coop.Ask.Anchor); else s.ToggleAnchor(); } return; }
            var leak = s.LeakPrompt(this); if (leak != null) { Prompt = leak; return; }  // M3: patch the leak, bail
            var bed = Sleep.BedPrompt(this); if (bed != null) { Prompt = bed; return; } // M3: Walt's cot
            var pick = FishPickup.Nearest(transform.position, 1.6f);
            if (pick != null) { Prompt = Bar.Full ? "Hotbar full" : "Pick up " + pick.F.Species.Name; if (Ctl.Interact && !Bar.Full) pick.Take(this); return; }
        }

        bool Near(Vector3 p, float r) { var d = p - transform.position; d.y *= 0.5f; return d.magnitude < r; }

        // ---- the hotbar's fish: Q drops one (a tap) or tosses it (hold, aim, let go); hold E on a crewmate gives it
        //      (gdd systems §1.1.3, §1.1.7). Co-op: the host's book says what happened (Coop); solo, here. ----
        public const float GiveHold = 0.4f, TossHold = 0.25f;
        public float ThrowT = -1;                  // s into the throw (Fishing's Frisbee Throw clip); a crewmate's as they send it
        public bool Aiming => qT >= TossHold;
        float qT = -1, giveT; bool giveDone, thrown; Fish throwFish; Vector3 throwTo; int throwSp = -1; GameObject throwView; Transform aimT;

        // the crewmate I'm looking at, within 3 m (the camera's line, 35° either side)
        (ulong, Angler) GiveTarget()
        {
            var c = Coop.I; if (c == null || !Coop.On) return (0, null);
            var cam = CameraRig.I; var fwd = Quaternion.Euler(0, cam != null ? cam.Yaw : transform.eulerAngles.y, 0) * Vector3.forward;
            (ulong, Angler) best = (0, null); float bd = 3f;
            foreach (var kv in c.Mates)
            {
                var m = kv.Value; if (m == null) continue;
                var d = m.transform.position - transform.position; float dy = Mathf.Abs(d.y); d.y = 0;
                if (d.magnitude < bd && dy < 1.5f && Vector3.Angle(fwd, d) < 35f) { best = (kv.Key, m); bd = d.magnitude; }
            }
            return best;
        }

        void Items(float dt)
        {
            bool fish = Bar.Sel.Fish.Count > 0 && !Busy && !Fish.Holding && ThrowT < 0 && !Swimming && !helm;
            if (fish && Ctl.DropHeld) { qT = qT < 0 ? 0 : qT + dt; Aim(Aiming ? TossTarget() : null); }
            else if (qT >= 0)
            {
                if (fish) { if (qT < TossHold) DropOne(); else TossAt(TossTarget()); }
                qT = -1; Aim(null);
            }
            if (ThrowT >= 0) Throwing(dt);
        }

        // throw the selected slot's oldest fish at a spot (letting go of Q; tests aim it straight)
        public void TossAt(Vector3 to)
        {
            if (Bar.Sel.Fish.Count == 0 || ThrowT >= 0) return;
            throwFish = Bar.Sel.Fish[0]; throwTo = to; throwSp = FishDb.All.IndexOf(throwFish.Species); ThrowT = 0; thrown = false;
        }

        // where a toss would land: 2 m out at once, 6 m after ~1 s more, along the camera's line
        Vector3 TossTarget()
        {
            float d = Mathf.Lerp(2f, 6f, Mathf.Clamp01((qT - TossHold) / 0.9f));
            var cam = CameraRig.I; float yaw = cam != null ? cam.Yaw : transform.eulerAngles.y;
            var p = transform.position + Quaternion.Euler(0, yaw, 0) * Vector3.forward * d;
            return Coop.Ground(p, out var at) ? at : new Vector3(p.x, Sea.Height(p), p.z);
        }
        void Aim(Vector3? p)
        {
            if (aimT == null) { aimT = Hud.MakeMarker(); aimT.gameObject.SetActive(false); }
            aimT.gameObject.SetActive(p != null);
            if (p is Vector3 at) { aimT.position = at + Vector3.up * 0.04f; aimT.rotation = Quaternion.Euler(90, Time.time * 20f, 0); }
        }

        // one fish (the oldest of the stack) at your feet; over the side it swims away
        void DropOne()
        {
            var f = Bar.Sel.Fish[0]; var at = transform.TransformPoint(new Vector3(0, 0.3f, 0.75f));
            bool ground = Coop.Ground(at, out var g);
            if (Coop.On && f.Id != 0) { if (ground) Coop.DropFish(f, g); else { Coop.FreeFish(f); Freed(at, f); } }
            else { Bar.Remove(f); if (ground) FishPickup.Drop(f, g + Vector3.up * 0.05f, transform.rotation); else Freed(at, f); }
            Snd.Play(Synth.Slap, at, 0.4f);
        }
        static void Freed(Vector3 at, Fish f)
        {
            Fx.Splash(new Vector3(at.x, Sea.Height(at), at.z), 0.6f, 10); Snd.Play(M1Assets.I.Splash, at, 0.5f);
            Alerts.Push("It's free!", $"The {f.Species.Name} swam away.", Alerts.Kind.Info);
        }

        // the throw: the fish leaves the hand on the clip's release frame and arcs to the aim
        void Throwing(float dt)
        {
            ThrowT += dt;
            float release = (Fishing.TossRelease - Fishing.TossFrom) / Fishing.TossSpeed, len = (Fishing.TossTo - Fishing.TossFrom) / Fishing.TossSpeed;
            if (!thrown && ThrowT >= release)
            {
                thrown = true; var from = Anim.HandR != null ? Anim.HandR.position : transform.TransformPoint(new Vector3(0.3f, 1.4f, 0.3f));
                if (throwFish != null && Coop.On && throwFish.Id != 0) Coop.Toss(throwFish, from, throwTo);
                else if (throwFish != null && Bar.Remove(throwFish))
                {
                    var f = throwFish; var to = throwTo;
                    FishFlight.Launch(f, from, to, Coop.TossTime, 0).Landed = () => { if (Coop.Ground(to, out var g)) FishPickup.Drop(f, g + Vector3.up * 0.05f, Quaternion.identity); else Freed(to, f); };
                }
                throwFish = null;
            }
            if (ThrowT > len + 0.2f) ThrowT = -1;
        }

        void OnDestroy() { if (aimT != null) Destroy(aimT.gameObject); if (throwView != null) Destroy(throwView); if (repairGrip != null) Destroy(repairGrip.gameObject); if (shoveGrip != null) Destroy(shoveGrip.gameObject); } // (in the world, not under me)

        // ---- a friendly shove (M3.1, Manan + Tushar's test): G with a crewmate within 1.5 m in front (where you look or
        //      face): both hands out, a lean into it, and they go over (the knock-down, on their machine: the host passes it
        //      on); near the rail the push is a fling that can tip them in. 3 s between shoves (a miss: just the swing). ----
        public const float ShoveReach = 1.5f, ShoveCool = 3f, ShoveHit = 0.14f, ShoveLen = 0.55f;
        public float ShoveT = -1;                         // s into the shove (its pose)
        public static int ShovesSent;                     // (tests)
        float shoveReady; ulong shoveTo; Angler shoveAt; bool shoveSent; Transform shoveGrip;

        void Shoving(float dt)
        {
            if (ShoveT >= 0)
            {
                ShoveT += dt;
                if (!shoveSent && ShoveT >= ShoveHit) { shoveSent = true; if (shoveAt != null && !shoveAt.Down) { Coop.Shove(shoveTo, ShovePush(shoveAt)); ShovesSent++; } }
                if (ShoveT >= ShoveLen) ShoveT = -1;
                return;
            }
            if (!Ctl.Shove || Time.time < shoveReady || !Coop.On || Busy || Down || Swimming || helm || RepairT >= 0 || Fish.Locked || Sleep.Frozen) return;
            var look = CameraRig.I != null ? Quaternion.Euler(0, CameraRig.I.Yaw, 0) * Vector3.forward : transform.forward;
            shoveAt = null; float bd = ShoveReach; string seen = "";
            foreach (var kv in Coop.I.Mates)
            {
                var m = kv.Value; if (m == null || m.Down) continue;
                var d = m.transform.position - transform.position; float dy = d.y; d.y = 0; float r = d.magnitude;
                seen += $" {m.name} {r:F2} m (up {dy:+0.00;-0.00}, {Vector3.Angle(d, look):0}° off the view, {Vector3.Angle(d, transform.forward):0}° off me)";
                if (Mathf.Abs(dy) > 1f) continue;
                if (r > bd || r < 1e-3f || Vector3.Angle(d, look) > 55 && Vector3.Angle(d, transform.forward) > 55) continue;
                shoveAt = m; shoveTo = kv.Key; bd = r;
            }
            Debug.Log($"[PB] shove: G: {(shoveAt != null ? "at " + shoveAt.name : "no one in reach")};{seen}");
            ShoveT = 0; shoveSent = false; shoveReady = Time.time + (shoveAt != null ? ShoveCool : ShoveLen + 0.1f);
            if (shoveAt != null) { var d = shoveAt.transform.position - transform.position; transform.rotation = Quaternion.Euler(0, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0); }
            Snd.Play(Synth.Whoosh, transform.position, 0.4f, 1.5f);
        }

        // how hard: a stagger-and-fall; aboard, at the rail and pushed outward, a fling (up and over the gunwale)
        public static bool NearRail(Angler m, Vector3 dir)
        {
            var s = Skiff.I; if (s == null || !m.OnBoat) return false;
            var l = s.transform.InverseTransformPoint(m.transform.position); var ld = s.transform.InverseTransformDirection(dir);
            return Mathf.Abs(l.x) > 0.3f && Mathf.Sign(l.x) == Mathf.Sign(ld.x) && Mathf.Abs(ld.x) > 0.5f;
        }
        Vector3 ShovePush(Angler m)
        {
            var d = m.transform.position - transform.position; d.y = 0; d = d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward;
            return NearRail(m, d) ? d * 3.4f + Vector3.up * 2.6f : d * 2.6f + Vector3.up * 1.2f;
        }

        // shoved over by a crewmate (their message, via the host)
        public void Shoved(Vector3 push, string by)
        {
            if (rag == null || Down || Swimming || !Fish.Knocked()) { Debug.Log($"[PB] shove: {by} shoved me: kept my feet ({(Down ? "down already" : Swimming ? "swimming" : "holding a catch")})"); return; }
            Debug.Log($"[PB] shove: {by} shoved me over ({push.magnitude:F1} m/s)");
            Knock(push);
            Alerts.Push(by, "Shoved you over!", Alerts.Kind.Info);
        }

        // the pose (mine or a crewmate's, from ShoveT): both palms out at the chest, thrust on ShoveHit, a lean into it
        void ShovePose()
        {
            if (ShoveT < 0 || Down || Swimming || helm)
            {
                if (shoveGrip != null && Anim.Grip == shoveGrip) { Anim.Grip = null; Anim.GripLean = 0; }
                return;
            }
            if (shoveGrip == null) shoveGrip = new GameObject("shoveGrip").transform;
            float u = ShoveT < ShoveHit ? Mathf.SmoothStep(0, 1, ShoveT / ShoveHit) : 1 - Mathf.SmoothStep(0, 1, (ShoveT - ShoveHit - 0.1f) / (ShoveLen - ShoveHit - 0.1f));
            shoveGrip.SetPositionAndRotation(transform.TransformPoint(new Vector3(0, 1.22f * Size, 0)), transform.rotation);
            Anim.Grip = shoveGrip; Anim.GripLean = 14f * u;
            Anim.GripL = new Vector3(-0.17f, 0.03f, 0.2f + 0.34f * u); Anim.GripR = new Vector3(0.17f, 0.03f, 0.2f + 0.34f * u);
            Anim.GripElbow = new Vector3(0, -0.6f, -0.4f); Anim.GripFingers = new Vector3(0, 1, 0.3f);
        }

        // ---- patching the hole (M3.1, Manan + Tushar's test: "add a repair animation"): down at the hole, the plank under
        //      the left hand, the hammer in the right (Hammer, LeakFx), a knock with every blow; a crewmate's from their pose ----
        public const float RepairStroke = 0.5f;           // s a hammer blow (Fish_Repair's nod loops with it)
        public float RepairT = -1;                        // s into patching (Skiff.LeakPrompt sets it while E is held at the hole)
        public float RepairLift { get; private set; }     // the hammer: 0 on the plank, 1 at the top of the swing
        Transform repairGrip; bool repairLook; float lastStrokePh = 1; Hammer hammer; Vector3 kneelAt; public Vector3 KneelAt => kneelAt; // (tests)
        public const float KneelOff = 0.38f;              // from the hole: the hands reach the plank from a deep crouch

        // where to kneel (boat space): about 0.38 m from the hole on the open floor. M3.1: the first try knelt on the bench
        // beside it, hammering the air; the second checked only the spot's centre, so the 0.3 m capsule round it caught the
        // thwart and the controller stepped up onto it (the leak test: feet 0.35-0.5 m over the hole, the patch dropping).
        // So a spot counts only if the floor under it is the hole's, the whole capsule fits there clear of the boat's
        // benches and sides, and it's clear of crewmates; of those, the one nearest where I stand.
        Vector3 KneelSpot()
        {
            var s = Skiff.I; var h = s.HoleLocal; var me = s.transform.InverseTransformPoint(transform.position);
            Vector3 best = h + (me - h).normalized * KneelOff; float bd = float.MaxValue;
            foreach (float off in new[] { KneelOff, KneelOff - 0.06f, KneelOff + 0.07f })
                for (int i = 0; i < 12; i++)
                {
                    float a = i * 30 * Mathf.Deg2Rad; var c = h + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * off;
                    float floor = float.NegativeInfinity;
                    foreach (var hit in Physics.RaycastAll(s.transform.TransformPoint(c + Vector3.up * 1.2f), -s.transform.up, 2f, ~0, QueryTriggerInteraction.Ignore))
                        if (Ours(hit.collider)) floor = Mathf.Max(floor, s.transform.InverseTransformPoint(hit.point).y);
                    if (Mathf.Abs(floor - h.y) > 0.06f) continue; // a thwart, the gunwale, or off the boat
                    float bad = 0;
                    var w = s.transform.TransformPoint(c); var up = s.transform.up; float r = Cc.radius - 0.02f;
                    foreach (var col in Physics.OverlapCapsule(w + up * (r + 0.1f), w + up * (Cc.height - r), r, ~0, QueryTriggerInteraction.Ignore))
                        if (Ours(col)) { bad += 100; break; }       // a bench or the side in the way: the controller would climb it
                    if (Coop.I != null) foreach (var m in Coop.I.Mates.Values)
                            if (m != null && !m.Down && Vector3.ProjectOnPlane(m.transform.position - w, up).magnitude < Apart) bad += 10;
                    float d = (c - me).sqrMagnitude + bad + Mathf.Abs(off - KneelOff);
                    if (d < bd) { bd = d; best = c; }
                }
            return best;
            bool Ours(Collider col) => col.transform.IsChildOf(s.transform) && col.GetComponentInParent<Angler>() == null;
        }

        // ease to the kneeling spot, facing the hole
        void KneelAtHole(float dt)
        {
            var s = Skiff.I; if (s == null) return;
            var d = s.Hole - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.0025f) transform.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(transform.eulerAngles.y, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f * dt), 0);
            var mv = s.transform.TransformPoint(kneelAt) - transform.position; mv.y = 0;
            if (mv.sqrMagnitude > 1e-4f) Cc.Move(Crowd(mv * Mathf.Min(1, dt * 6), dt, true)); // (round a crewmate, never into them)
        }

        void Repairing()
        {
            var a = M1Assets.I; var s = Skiff.I;
            bool on = RepairT >= 0 && s != null && !helm && !Swimming && a.Repair != null;
            if (!on)
            {
                if (a.Repair != null && Anim.Act == a.Repair) Anim.Act = null;
                if (repairLook) { repairLook = false; Anim.LookAt = new Vector3(float.NaN, 0, 0); }
                if (repairGrip != null && Anim.Grip == repairGrip) Anim.Grip = null;
                RepairLift = 0; lastStrokePh = 1; return;
            }
            if (repairGrip == null) repairGrip = new GameObject("repairGrip").transform;
            if (hammer == null) { hammer = gameObject.AddComponent<Hammer>(); hammer.A = this; }
            repairGrip.SetPositionAndRotation(s.Hole, Quaternion.Euler(0, transform.eulerAngles.y, 0));
            float L = a.Repair.length, stroke = L / Mathf.Max(1, Mathf.Round(L / RepairStroke));
            Anim.Act = a.Repair; Anim.ActTime = RepairT % L;
            float ph = RepairT % stroke / stroke;  // 0: the blow lands; up slowly, down fast
            RepairLift = ph < 0.7f ? Mathf.SmoothStep(0, 1, ph / 0.7f) : 1 - (ph - 0.7f) * (ph - 0.7f) / 0.09f;
            if (ph < lastStrokePh && RepairT > 0.1f) { Snd.Play(M1Assets.I.Thud, s.Hole, 0.5f, Random.Range(1.6f, 1.85f)); Fx.Dust(s.Hole + Vector3.up * 0.04f, 3, 0.45f); }
            lastStrokePh = ph;
            Anim.Grip = repairGrip; Anim.GripLean = 0;
            Anim.GripL = new Vector3(-0.13f, 0.05f, 0.02f);                                           // flat on the plank
            Anim.GripR = new Vector3(0.10f, 0.22f + 0.2f * RepairLift, -0.15f - 0.05f * RepairLift);   // the hammer hand (its head on the plank)
            Anim.GripElbow = new Vector3(0, -0.4f, -0.6f); Anim.GripFingers = new Vector3(0, -0.7f, 1);
            Anim.LookAt = s.Hole; repairLook = true;
        }

        // the fish in the throwing hand until it goes (mine, or a crewmate's from their pose)
        void HoldThrown()
        {
            float release = (Fishing.TossRelease - Fishing.TossFrom) / Fishing.TossSpeed;
            bool on = ThrowT >= 0 && ThrowT < release && throwSp >= 0 && throwSp < FishDb.All.Count && Anim.HandR != null;
            if (!on) { if (throwView != null) Destroy(throwView); throwView = null; return; }
            if (throwView == null) { var sp = FishDb.All[throwSp]; throwView = FishView.Make(new Fish { Species = sp, Cm = (sp.SizeMin + sp.SizeMax) / 2 }, null); }
            throwView.transform.SetPositionAndRotation(Anim.HandR.position + Anim.HandR.up * 0.04f, Quaternion.LookRotation(transform.right, Vector3.up) * Quaternion.Euler(0, 0, 90));
        }

        void Helm(float dt)
        {
            var s = Skiff.I;
            Prompt = s.Moored ? "W  Cast off   ·   E  Leave the tiller" : s.CanMoor ? "F  Moor   ·   E  Leave the tiller" : "W/S  Throttle   ·   A/D  Steer   ·   E  Leave";
            Cc.enabled = false;
            // slide onto the seat (0.35 s) instead of jumping there
            helmT = Mathf.Min(1, helmT + dt / 0.35f); float k = Mathf.SmoothStep(0, 1, helmT);
            transform.position = s.transform.TransformPoint(Vector3.Lerp(helmFrom, SeatLocal(s), k));
            transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y + Mathf.LerpAngle(helmFromYaw, 0, k), 0);
            Cc.enabled = true;
            int th = Ctl.Throttle;
            if (s.Moored) { if (th > 0) { s.CastOff(); s.Notch = 1; Snd.Play2D(Synth.Ratchet, 0.4f); } }
            else if (th != 0) { s.Notch = Mathf.Clamp(s.Notch + th, -1, 3); Snd.Play2D(Synth.Tik, 0.6f, th > 0 ? 1.2f : 0.9f); }
            s.Steer = Mathf.Clamp(Ctl.Steer, -1, 1);
            if (s.CanMoor && Ctl.Key_(UnityEngine.InputSystem.Key.F)) s.Moor();
            if (Ctl.Interact || Ctl.Escape) LeaveHelm();
            if (CameraRig.I != null) CameraRig.I.M = helm ? CameraRig.Mode.Helm : CameraRig.Mode.Follow;
            Vel = Vector3.zero;
        }

        // A ladder from the water: E puts you straight on top (Tushar 1 Nov: the climb animation went up and down the
        // rungs a few times before he got off; he wants E = up). Onto the pier, or in over the gunwale toward the middle
        // of the boat, standing on whatever is under that spot.
        void StartClimb(Transform bottom, Transform top, Transform reference)
        {
            var inward = top.position - bottom.position; inward.y = 0;
            var toCentre = reference.position - top.position; toCentre.y = 0;
            bool boat = reference == Skiff.I?.transform;
            var dir = (boat && toCentre.sqrMagnitude > 0.01f ? toCentre : inward).normalized;
            var p = top.position + dir * 0.7f;
            if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Angler>() == null)
                p.y = hit.point.y;
            Fish.Stow();
            Teleport(p + Vector3.up * 0.02f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
            vy = -2f; Swimming = false; wasSwim = false; lastGround = Time.time; Prompt = null;
            Snd.Play(M1Assets.I.Splash, transform.position, 0.3f, 1.4f);
        }

        void Splash(float surface, float size)
        {
            Fx.Splash(new Vector3(transform.position.x, surface, transform.position.z), size);
            Snd.Play(M1Assets.I.Splash, transform.position, 0.8f);
        }

        // ---- knocked over (ragdoll-lite, replan/feel §4): F8, or thrown off the tiller by a crash ----
        public bool Down => downT >= 0 || getUpT >= 0;
        public bool Braced => !Remote && OnBoat && !helm && !Swimming && !Down && Ctl.Brace;   // M3 footing: a rogue wave can't take you
        public bool Limp => downT >= 0;
        M1Assets.GetUpClip up; int upI; bool upBack; // the get-up playing (one of M1Assets.GetUps, at random), and on her back?
        Ragdoll rag; float downT = -1, getUpT = -1; bool jumped, fallIn; AnimationClip overClip; float overT, overW;

        // Over she goes, limp from the pose she's in, flung by `push` (m/s) and tipping the way she was pushed.
        public void Knock(Vector3 push)
        {
            if (rag == null || Down || Swimming || Fish.Locked) return; // (never with a line out)
            if (helm) { LeaveHelm(); if (Skiff.I != null) Skiff.I.Notch = 0; }
            Fish.Stow(); RepairT = -1;
            Anim.Over = null; Anim.OverW = overW = 0; Anim.Flail = false; Anim.Grip = null; Anim.Act = null;
            Model.localPosition = Vector3.zero; Model.localRotation = Quaternion.identity; Model.localScale = Vector3.one;
            leanP = leanR = leanPV = leanRV = sq = sqV = 0; crouchT = -1; jumped = fallIn = false;
            var flat = new Vector3(push.x, 0, push.z);
            rag.Go(Vel + Vector3.up * Mathf.Max(0, vy) + push, flat.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, flat.normalized) * 5f : Vector3.zero);
            Cc.enabled = false; Vel = velV = Vector3.zero; vy = 0; downT = 0; Prompt = null;
            if (CameraRig.I != null) CameraRig.I.Target = rag.Hips;
            CameraRig.Shake(0.3f); Snd.Play(M1Assets.I.Thud, transform.position, 0.6f, 1.2f);
            StartCoroutine(HitStop(0.05f));
        }

        static System.Collections.IEnumerator HitStop(float s)
        {
            float ts = Time.timeScale; Time.timeScale = ts * 0.05f;
            yield return new WaitForSecondsRealtime(s);
            Time.timeScale = ts;
        }

        void Lying(float dt)
        {
            downT += dt; Prompt = null;
            var h = rag.Hips.position;
            if (h.y < Sea.Height(h) - 0.25f) { Overboard(); return; } // flung into the sea
            if (downT > 1.0f && rag.Speed < 0.6f || downT > 1.8f) GetUp(); // (a rocking deck never lets her lie quite still)
        }

        // In the sea: straight up into treading water where the hips are.
        void Overboard()
        {
            var h = rag.Hips.position;
            rag.Stop(0.3f); downT = -1;
            if (CameraRig.I != null) CameraRig.I.Target = transform;
            Teleport(new Vector3(h.x, Sea.Height(h) - SwimDepth, h.z), transform.eulerAngles.y);
        }

        // Up again, with one of the get-ups at random: the body is put where its first frame lines up with where she lies
        // (hips on the hips, head the same way), the clip blends in from the ragdoll pose (0.3 s; 0.45 s on her back: a
        // quick roll onto her front, both clips start face down).
        void GetUp()
        {
            var a = M1Assets.I;
            if (a.GetUps == null || a.GetUps.Length == 0) { Overboard(); return; } // (no clips: never stuck lying there)
            upI = Random.Range(0, a.GetUps.Length); up = a.GetUps[upI];
            var h = rag.Hips.position; var spine = rag.Head.position - h; spine.y = 0;
            bool onBack = upBack = rag.Chest.forward.y > 0;
            float yaw = Mathf.Atan2(spine.x, spine.z) * Mathf.Rad2Deg - Mathf.Atan2(up.Spine.x, up.Spine.z) * Mathf.Rad2Deg;
            var bodyRot = Quaternion.Euler(0, yaw, 0);
            float ground = float.NegativeInfinity; // the highest floor under the hips (not the rail she may lie against)
            foreach (var hit in Physics.RaycastAll(h + Vector3.up * 0.8f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Angler>() == null && hit.point.y <= h.y + 0.15f && hit.point.y > ground) ground = hit.point.y;
            if (float.IsNegativeInfinity(ground)) { Overboard(); return; } // draped over the gunwale, hips over the sea: she slides in
            var at = new Vector3(h.x, ground, h.z) - bodyRot * new Vector3(up.Hips.x, 0, up.Hips.z) * BodyK;
            rag.Stop(onBack ? 0.45f : 0.3f);
            transform.SetPositionAndRotation(at, bodyRot * Quaternion.Euler(0, 180, 0)); // the body sits turned round under the model
            downT = -1; getUpT = 0; CamY = ground;
            Track(); // on the deck: the boat carries her while she gets up
            if (CameraRig.I != null) CameraRig.I.Target = transform;
            Anim.Over = up.Clip; Anim.OverW = 1; Anim.OverTime = up.From;
        }

        // Getting up: the clip's creep along the deck moves the body (feet planted), the controller follows it at the end.
        void GettingUp(float dt)
        {
            Prompt = null;
            getUpT += dt;
            if (UpPose(getUpT) >= 1)
            {
                getUpT = -1; Anim.Over = null; Anim.OverW = 0;
                var p = transform.TransformPoint(Model.localPosition); Model.localPosition = Vector3.zero;
                Teleport(p, transform.eulerAngles.y);
            }
        }

        // The get-up `time` s in (here, or a crewmate's as they send it): returns how far it has blended into the idle.
        float UpPose(float time)
        {
            float len = (up.To - up.From) / up.Speed, t = Mathf.Min(up.To, up.From + time * up.Speed);
            float done = Mathf.Clamp01((time - len) / 0.25f); // then 0.25 s into the idle
            Anim.Over = up.Clip; Anim.OverTime = t; Anim.OverW = 1 - done;
            var creep = Quaternion.Euler(0, 180, 0) * new Vector3(up.MoveX.Evaluate(t), 0, up.MoveZ.Evaluate(t)) * BodyK; // body space -> model space
            Model.localPosition = creep; Model.localRotation = Quaternion.identity; Model.localScale = Vector3.one;
            Anim.Tick(CharAnim.Slot.Idle, 0);
            return done;
        }

        void Animate(float dt)
        {
            var d = transform.position - lastPos; lastPos = transform.position;
            // own speed only (the boat's motion carries us, it isn't walking)
            float sp = new Vector3(Vel.x, 0, Vel.z).magnitude;
            animSpeed = sp;
            CharAnim.Slot slot;
            // at the tiller: sat on the stern bench (Fish_Helm sits the hips down and back; this slides the body the
            // rest of the way back onto the bench), easing in and out so standing up is a move, not a pop
            sit = Util.Damp(sit, helm ? 1 : 0, 9f, dt);
            Model.localPosition = new Vector3(0, 0, -0.17f * sit);
            // Lean (feel spec): forward / back from the walk's acceleration, banked into turns, on a spring with a little
            // overshoot (k 70); max 12° (15° back in a skid), 10° banked; less with the rod out. Squash 0.92 in the
            // jump's crouch, stretch on take-off, squash on landing (a stiff spring, a few frames). The body only: the
            // root and the camera stay put.
            var la = transform.InverseTransformDirection((Vel - lastVel) / dt); lastVel = Vel;
            float lk = helm || Swimming ? 0 : Fish.Holding ? 0.4f : 1;
            float tp = skidT > 0 ? -15f : Mathf.Clamp(la.z * 0.6f, -12f, 12f) * lk, tr = Mathf.Clamp(la.x * 0.4f, -10f, 10f) * lk;
            Spring(ref leanP, ref leanPV, tp, 70f, 0.6f, dt); Spring(ref leanR, ref leanRV, tr, 70f, 0.6f, dt);
            Spring(ref sq, ref sqV, crouchT >= 0 ? -0.08f : Braced ? -0.11f : 0, 300f, 0.45f, dt); sq = Mathf.Clamp(sq, -0.12f, 0.1f);
            Model.localRotation = Quaternion.Euler(Mathf.Clamp(leanP, -15f, 12f), 0, -Mathf.Clamp(leanR, -10f, 10f));
            Model.localScale = new Vector3(1 - sq * 0.5f, 1 + sq, 1 - sq * 0.5f);
            if (Lie > 0) { Model.localRotation = Quaternion.Euler(-86f * Lie, 0, 0); Model.localPosition += new Vector3(0, LieH * Lie, 0); } // M3: lying on a bed (Sleep.cs)
            Anim.Ladder = null; Anim.WaterY = float.NaN;
            if (helm) { Anim.Act = M1Assets.I.Helm; Anim.ActTime = -1; }
            else if (sit > 0.02f && Anim.Act == M1Assets.I.Helm) Anim.Act = null;
            // turning on the spot: the feet step round with it (a little walk-cycle), instead of sliding
            float yawNow = OnBoat && Skiff.I != null ? transform.eulerAngles.y - Skiff.I.transform.eulerAngles.y : transform.eulerAngles.y;
            float turnRate = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yawNow)) / dt; lastYaw = yawNow;
            float animSp = sp;
            if (!helm && !Swimming) animSp = Mathf.Max(sp, Mathf.Clamp01((turnRate - 60f) / 300f) * 0.8f);
            if (skidT > 0) animSp = 0; // feet planted through the skid
            Anim.Flail = wasAir && airT > 0.6f && vy < 0 && !helm && !fallIn; // a real fall: arms going
            if (helm) slot = CharAnim.Slot.Idle;
            else if (Swimming)
            {
                bool stroke = sp > 0.6f;
                slot = stroke ? CharAnim.Slot.Swim : CharAnim.Slot.SwimIdle;
                Model.localPosition = new Vector3(0, Anim.WaterLift(stroke, SwimDepth), 0);
                if (!stroke) Anim.WaterY = Sea.Height(transform.position);
            }
            else slot = Grounded || Time.time - lastGround < 0.15f ? CharAnim.Slot.Idle : CharAnim.Slot.Jump;
            // hands: the rod (Fishing sets Grip) or the tiller
            if (helm && Skiff.I.TillerGrip != null)
            {
                // left hand on the tiller's grip, right hand resting on the right knee
                var tg = Skiff.I.TillerGrip;
                Anim.Grip = tg; Anim.GripL = new Vector3(0.0f, 0.01f, 0.0f);
                Anim.GripR = tg.InverseTransformPoint(transform.TransformPoint(new Vector3(0.15f, 0.52f, -0.02f)));
                // elbows down and back; +x cancels the left arm's built-in outward hint, so its elbow stays off the tiller bar
                Anim.GripElbow = new Vector3(0.62f, -0.8f, -0.35f); Anim.GripFingers = new Vector3(0, -0.3f, 1); // left elbow tucked in to his side
            }
            else if (!Fish.Holding || Swimming) Anim.Grip = null;
            Repairing();
            ShovePose();
            // Mixamo clips the game drives over everything: the jump (its crouch fitted into our 0.08 s, its push and tuck
            // over the first 0.25 s of the rise, then held) and stepping off the pier into the sea (Falling Into Pool: the
            // topple and the drop). Their own rise is taken back out of the body: the controller does the rising.
            var a = M1Assets.I;
            if (ThrowT >= 0 && a.Throw != null)
            {
                float len = (Fishing.TossTo - Fishing.TossFrom) / Fishing.TossSpeed;
                Anim.Over = a.Throw; Anim.OverTime = Mathf.Min(Fishing.TossTo, Fishing.TossFrom + ThrowT * Fishing.TossSpeed);
                Anim.OverW = Mathf.Clamp01(ThrowT / 0.1f) * (1 - Mathf.Clamp01((ThrowT - len) / 0.2f)); overW = 0; Anim.Grip = null;
            }
            else if (!Fish.Tossing)
            {
                AnimationClip oc = null;
                if (crouchT >= 0 && a.JumpUp != null) { oc = a.JumpUp; overT = Mathf.Lerp(0, 0.21f, crouchT / 0.08f); }
                else if (jumped && wasAir && a.JumpUp != null) { oc = a.JumpUp; overT = Mathf.Lerp(0.42f, 0.83f, Mathf.Clamp01(airT / 0.25f)); }
                else if (fallIn && wasAir && a.FallIn != null) { oc = a.FallIn; overT = Mathf.Lerp(0.85f, 1.5f, Mathf.Clamp01(airT / 0.4f)); }
                if (oc != null) overClip = oc;
                overW = Mathf.MoveTowards(overW, oc != null ? 1 : 0, dt / (oc != null ? 0.05f : 0.08f));
                Anim.Over = overW > 0 ? overClip : null; Anim.OverTime = overT; Anim.OverW = overW;
                var lift = overClip == a.JumpUp ? a.JumpLift : a.FallLift;
                if (overW > 0 && lift != null && lift.length > 0) Model.localPosition -= Vector3.up * (Mathf.Max(0, lift.Evaluate(overT)) * overW * BodyK);
            }
            Anim.Tick(slot, animSp);

            // footsteps on the heel strikes (the shared stride: left at 0, right at ~0.5), wood on the deck and pier;
            // a little dust off the feet at a sprint
            float ph = Anim.Phase;
            if (!Swimming && !helm && Grounded && animSp > 0.3f && skidT <= 0 && Mathf.FloorToInt(ph * 2) != Mathf.FloorToInt(lastPhase * 2))
            { Step(Mathf.Lerp(0.16f, 0.32f, sp / 5.8f)); if (SprintW > 0.6f) Dust(2, 0.7f); }
            lastPhase = ph;
            HoldThrown();
        }
    }
}
