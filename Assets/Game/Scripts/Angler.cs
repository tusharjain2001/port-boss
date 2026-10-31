using UnityEngine;

namespace PortBoss.M1
{
    // The player in M1 (solo): walks the pier, the shore and Biscuit's deck, swims, climbs ladders, takes the helm and
    // fishes. On the deck the body moves in the boat's own space (systems §1.6.1): each frame it is first carried by
    // the boat's move (position and turn), then walks, so a rocking, moving boat never slides it.
    // Feel numbers from replan/feel: walk 1.4, run 4.2, sprint 5.8 m/s on land; on Biscuit's small deck no sprint and a
    // calmer 2.6 m/s; eased starts and stops; jump 0.45 m on a deck.
    [DefaultExecutionOrder(0)]
    public class Angler : MonoBehaviour
    {
        public static Angler I;
        public CharacterController Cc;
        public CharAnim Anim;
        public Transform Model;
        public Hotbar Bar = new();
        public Fishing Fish;

        public bool OnBoat { get; private set; }
        public bool Swimming { get; private set; }
        public bool AtHelm => helm;
        public bool Climbing => climbT >= 0;
        public bool Busy;                 // fishing owns the body (fight, reveal): no walking
        public float FaceYaw = float.NaN; // fishing faces the aim
        public Vector3 Vel;               // own walking velocity (not the boat's)
        public const float SwimDepth = 1.3f;

        Vector3 localOnBoat; float localYaw;
        float vy, coyote, lastGround;
        bool helm;
        float climbT = -1; Vector3 climbFrom, climbTo, climbOut; Transform climbRef; float climbLen;
        Vector3 lastPos; float animSpeed;
        float stepT; int stepI;
        bool wasSwim; float dunkT = -1;

        public static Angler Create(M1Assets a, Vector3 at, float yaw)
        {
            var g = new GameObject("Angler");
            g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var p = g.AddComponent<Angler>(); I = p;
            p.Cc = g.AddComponent<CharacterController>();
            p.Cc.height = 1.75f; p.Cc.radius = 0.3f; p.Cc.center = new Vector3(0, 0.875f, 0); p.Cc.stepOffset = 0.42f; p.Cc.slopeLimit = 50; p.Cc.skinWidth = 0.03f;
            p.Model = new GameObject("model").transform; p.Model.SetParent(g.transform, false);
            // Our own dock worker (Assets/Art/Worker, Blender + Mixamo rig) in the toon shader with his palette texture.
            PortBoss.Art.Lit = new Material(a.Toon);
            PortBoss.Art.Lit.SetFloat("_PaintNoise", 0.12f);
            var w = PortBoss.Worker.Make(a.Worker, p.Model);
            w.Apply(new PortBoss.Look
            {
                Set = 1, Face = 0, Hair = 3, Beard = 2, Hat = 2, Top = 2, Gloves = 0,
                SkinTone = 2, HairColor = 2, TopColor = 1, AccentColor = 4, LegsColor = 4, BootsColor = 7, GloveColor = 4, HatColor = 2,
            });
            foreach (var c in w.GetComponentsInChildren<Collider>()) Destroy(c);
            var rs = w.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            if (bb.size.y > 0.5f) w.transform.localScale = Vector3.one * Mathf.Clamp(1.78f / bb.size.y, 0.8f, 1.25f);
            p.Bar.Selected = 1; // empty hands at the start: "1" draws the rod (F1)
            var anim = w.GetComponentInChildren<Animator>();
            var skin = ScriptableObject.CreateInstance<PortBoss.Skin>();
            skin.Idle = a.Idle; skin.Walk = a.Walk; skin.Run = a.Run; skin.Sprint = a.Sprint; skin.Jump = a.Jump; skin.Land = a.Land;
            skin.SwimIdle = a.SwimIdle; skin.Swim = a.Swim; skin.Climb = a.Climb; skin.Drive = a.Drive ?? a.Idle; skin.Push = a.Idle;
            p.Anim = anim.gameObject.AddComponent<CharAnim>();
            p.Anim.Init(anim, skin); p.Anim.Facing = g.transform; p.Anim.GripLean = 0;
            p.Fish = g.AddComponent<Fishing>();
            p.lastPos = at;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var s = Skiff.I;
            // carried by the boat
            if (OnBoat && s != null)
            {
                var carried = s.transform.TransformPoint(localOnBoat);
                Cc.enabled = false; transform.position = carried; Cc.enabled = true;
                transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y + localYaw, 0);
            }

            if (climbT >= 0) { Climb(dt); Track(); Animate(dt); return; }
            if (helm) { Helm(dt); Track(); Animate(dt); return; }

            // hotbar
            if (!Busy && !Fish.Locked)
            {
                int k = Ctl.SlotKey; if (k >= 0) Select(k);
                int sc = Ctl.SlotScroll; if (sc != 0) Select((Bar.Selected + sc + Hotbar.Slots) % Hotbar.Slots);
            }

            float surface = Sea.Height(transform.position);
            Swimming = transform.position.y < surface - SwimDepth + 0.35f && !OnBoat;
            if (Swimming && !wasSwim) { dunkT = 0; Splash(surface, 1f); }
            wasSwim = Swimming;

            // walking
            var cam = CameraRig.I;
            var input = Busy ? Vector2.zero : Ctl.Move;
            var fwd = cam != null ? Quaternion.Euler(0, cam.Yaw, 0) : transform.rotation;
            var wish = fwd * new Vector3(input.x, 0, input.y);
            bool sprint = Ctl.Sprint && !OnBoat && !Swimming;
            float max = Swimming ? (Ctl.Sprint ? 2.2f : 1.4f) : Ctl.Walk ? 1.4f : OnBoat ? 2.6f : sprint ? 5.8f : 4.2f;
            if (Fish.Holding && !Swimming) max = Mathf.Min(max, OnBoat ? 2.2f : 3.6f); // careful with a rod out
            var target = wish * max;
            float rate = target.sqrMagnitude > Vel.sqrMagnitude ? 1f / 0.12f : 1f / 0.15f;
            Vel = Util.Damp(Vel, target, rate, dt);
            if (sprint && Fish.Holding) Fish.Stow();

            // facing: where we walk, or the fishing aim
            float wantYaw = !float.IsNaN(FaceYaw) ? FaceYaw : Vel.sqrMagnitude > 0.05f ? Mathf.Atan2(Vel.x, Vel.z) * Mathf.Rad2Deg : transform.eulerAngles.y;
            float turnRate = !float.IsNaN(FaceYaw) ? 540f : 600f;
            transform.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(transform.eulerAngles.y, wantYaw, turnRate * dt), 0);

            bool grounded = Cc.isGrounded;
            if (grounded) lastGround = Time.time;
            if (Swimming)
            {
                // float with head + shoulders out; a short dunk on the way in
                float wantY = surface - SwimDepth;
                if (dunkT >= 0) { dunkT += dt / 0.6f; wantY -= 0.3f * Mathf.Sin(Mathf.Clamp01(dunkT) * Mathf.PI); if (dunkT >= 1) dunkT = -1; }
                vy = (wantY - transform.position.y) * 6f;
            }
            else
            {
                vy -= (vy > 0 ? 20f : 30f) * dt;
                if (grounded && vy < 0) vy = -2f;
                if (!Busy && Ctl.Jump && Time.time - lastGround < 0.12f) { vy = Mathf.Sqrt(2 * 20f * (OnBoat ? 0.6f : 0.6f)); /* on Biscuit: just clears the 0.5 m gunwale when you mean it */ lastGround = -1; }
            }
            Cc.Move((Vel + Vector3.up * vy) * dt);

            Interact();
            Track();
            Animate(dt);
        }

        // Put the player somewhere (tests, ladders): also re-anchors them in the boat's space so the deck carry doesn't undo it.
        public void Teleport(Vector3 p, float yaw)
        {
            Cc.enabled = false; transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); Cc.enabled = true;
            Vel = Vector3.zero; vy = 0; Track();
        }

        public void TakeHelm() { var s = Skiff.I; if (s == null) return; helm = true; s.AtHelm = true; Fish.Stow(); }
        public void LeaveHelm() { var s = Skiff.I; helm = false; if (s != null) { s.AtHelm = false; s.Steer = 0; } if (CameraRig.I != null) CameraRig.I.M = CameraRig.Mode.Follow; }

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
            if (helm || climbT >= 0) on = climbT >= 0 ? climbRef != null && climbRef.IsChildOf(s.transform) : true;
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
            if (s == null) return;
            if (OnBoat && s.Helm != null && Near(s.Helm.position, 1.3f)) { Prompt = s.Moored ? "Take the tiller" : "Take the tiller"; if (Ctl.Interact) { helm = true; s.AtHelm = true; Fish.Stow(); } return; }
            if (OnBoat && s.AnchorPt != null && Near(s.AnchorPt.position, 1.4f) && !s.Moored) { Prompt = s.AnchorT >= 0 ? "…" : s.Anchored ? "Raise the anchor" : "Drop the anchor"; if (Ctl.Interact) s.ToggleAnchor(); return; }
            var pick = FishPickup.Nearest(transform.position, 1.6f);
            if (pick != null) { Prompt = Bar.Full ? "Hotbar full" : "Pick up " + pick.F.Species.Name; if (Ctl.Interact && !Bar.Full) pick.Take(this); return; }
        }

        bool Near(Vector3 p, float r) { var d = p - transform.position; d.y *= 0.5f; return d.magnitude < r; }

        void Helm(float dt)
        {
            var s = Skiff.I;
            Prompt = s.Moored ? "W  Cast off   ·   E  Leave the tiller" : s.CanMoor ? "F  Moor   ·   E  Leave the tiller" : "W/S  Throttle   ·   A/D  Steer   ·   E  Leave";
            Cc.enabled = false;
            transform.position = s.Helm.position; transform.rotation = Quaternion.Euler(0, s.transform.eulerAngles.y, 0);
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

        void StartClimb(Transform bottom, Transform top, Transform reference)
        {
            climbRef = reference; climbT = 0;
            climbFrom = reference.InverseTransformPoint(transform.position);
            climbTo = reference.InverseTransformPoint(top.position);
            // step in over the gunwale / onto the pier: 0.6 m past the top, toward the deck / pier middle
            var inward = reference.InverseTransformPoint(top.position) - reference.InverseTransformPoint(bottom.position); inward.y = 0;
            var toCentre = -reference.InverseTransformPoint(top.position); toCentre.y = 0;
            climbOut = climbTo + (toCentre.sqrMagnitude > 0.01f && reference == Skiff.I?.transform ? toCentre.normalized : -inward.normalized) * 0.7f;
            climbLen = 1.2f;
            Fish.Stow();
            Snd.Play(M1Assets.I.Splash, transform.position, 0.3f, 1.4f);
        }

        void Climb(float dt)
        {
            climbT += dt / climbLen;
            float u = Mathf.Clamp01(climbT);
            Vector3 l = u < 0.75f ? Vector3.Lerp(climbFrom, climbTo, Mathf.SmoothStep(0, 1, u / 0.75f)) : Vector3.Lerp(climbTo, climbOut, Mathf.SmoothStep(0, 1, (u - 0.75f) / 0.25f));
            var w = climbRef.TransformPoint(l);
            Cc.enabled = false; transform.position = w; Cc.enabled = true;
            var face = climbRef.TransformPoint(climbOut) - climbRef.TransformPoint(climbFrom); face.y = 0;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face);
            if (climbT >= 1) { climbT = -1; vy = 0; Vel = Vector3.zero; Swimming = false; }
        }

        void Splash(float surface, float size)
        {
            Fx.Splash(new Vector3(transform.position.x, surface, transform.position.z), size);
            Snd.Play(M1Assets.I.Splash, transform.position, 0.8f);
        }

        void Animate(float dt)
        {
            var d = transform.position - lastPos; lastPos = transform.position;
            // own speed only (the boat's motion carries us, it isn't walking)
            float sp = new Vector3(Vel.x, 0, Vel.z).magnitude;
            animSpeed = sp;
            CharAnim.Slot slot;
            Model.localPosition = Vector3.zero; Model.localRotation = Quaternion.identity;
            Anim.Ladder = null; Anim.WaterY = float.NaN;
            if (climbT >= 0) slot = CharAnim.Slot.Climb;
            else if (helm) slot = CharAnim.Slot.Idle;
            else if (Swimming)
            {
                bool stroke = sp > 0.6f;
                slot = stroke ? CharAnim.Slot.Swim : CharAnim.Slot.SwimIdle;
                Model.localPosition = new Vector3(0, Anim.WaterLift(stroke, SwimDepth), 0);
                if (!stroke) Anim.WaterY = Sea.Height(transform.position);
            }
            else slot = Cc.isGrounded || Time.time - lastGround < 0.15f ? CharAnim.Slot.Idle : CharAnim.Slot.Jump;
            // hands: the rod (Fishing sets Grip) or the tiller
            if (helm && Skiff.I.TillerGrip != null)
            {
                Anim.Grip = Skiff.I.TillerGrip; Anim.GripL = new Vector3(-0.04f, 0, -0.06f); Anim.GripR = new Vector3(0.04f, 0, 0.02f);
                Anim.GripElbow = new Vector3(0, -0.8f, -0.2f); Anim.GripFingers = new Vector3(0, -0.3f, 1);
            }
            else if (!Fish.Holding || Swimming || climbT >= 0) Anim.Grip = null;
            Anim.Tick(slot, sp);

            // footsteps on the stride (wood on the deck and pier)
            if (!Swimming && climbT < 0 && sp > 0.5f && Cc.isGrounded)
            {
                stepT += dt * sp / 0.75f;
                if (stepT >= 1) { stepT -= 1; Snd.Play(M1Assets.I.Steps != null && M1Assets.I.Steps.Length > 0 ? M1Assets.I.Steps[stepI++ % M1Assets.I.Steps.Length] : null, transform.position, 0.25f, Random.Range(0.92f, 1.08f)); }
            }
        }
    }
}
