using UnityEngine;

namespace PortBoss.M1
{
    // Third-person camera (feel spec: 7.5 m + 0.35 a crate up to 9, 30°, FOV 55 (+4 sprinting), aimed 1.2 m up and
    // ahead of the run, following 0.15 s across and 0.3 s up and down from the ground stood on, so a jump doesn't
    // lift it; a dip on a big landing; on a deck 6 m). Fishing framings from gdd
    // fishing §3 are offsets from that baseline, eased in and back: shoulder cam while charging a cast, the "wait"
    // framing biased toward the bobber, a 3/4-front close-up for the reveal, and the boat cam at the helm.
    [DefaultExecutionOrder(2000)]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public const int UiLayer = 5;
        public Camera UiCam => Cam.transform.childCount > 0 ? Cam.transform.GetChild(0).GetComponent<Camera>() : Cam;
        public Camera Cam;
        public Transform Target;
        public float Yaw, Pitch = 30f;
        public enum Mode { Follow, Shoulder, Wait, Fight, Reveal, Helm }
        public Mode M;
        public Vector3 Focus;             // marker / bobber / fish
        public bool OnDeck;
        public float FovPunch;            // the bite's -3° punch
        public float RevealSide = -1;     // which 3/4 side the reveal frames from (Fishing picks the one with a clear view)

        public string Blocker;            // what the camera is pulled in by this frame (tests)
        Vector3 pivot, pos, focus;
        float dist = 6, fov = 55, side, pitchOff, focusW, lastLook, roll, reach = 99, distV, fovV, sideV, pitchV, focusV, height = 1.2f, heightV;
        Vector3 pivotV, ahead;
        static float shake, dipT = 9, dipA;

        public static CameraRig Create(Transform target)
        {
            var g = new GameObject("Camera"); g.tag = "MainCamera";
            var c = g.AddComponent<Camera>(); c.nearClipPlane = 0.08f; c.farClipPlane = 4000; c.fieldOfView = 55;
            var data = g.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true; data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            g.AddComponent<AudioListener>();
            // the UI draws through an overlay camera after the post stack, so the grade, bloom and grain never tint the paper
            c.cullingMask &= ~(1 << UiLayer);
            var ug = new GameObject("UiCamera"); ug.transform.SetParent(g.transform, false);
            var uc = ug.AddComponent<Camera>(); uc.cullingMask = 1 << UiLayer; uc.clearFlags = CameraClearFlags.Depth; uc.nearClipPlane = 0.05f; uc.farClipPlane = 10;
            var ud = ug.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay; ud.renderPostProcessing = false;
            data.cameraStack.Add(uc);
            var r = g.AddComponent<CameraRig>(); r.Cam = c; r.Target = target; I = r;
            r.Yaw = target.eulerAngles.y; r.pivot = target.position + Vector3.up * 1.5f;
            return r;
        }

        bool cut;
        public void Cut() => cut = true; // the next frame jumps straight to the target (a join, under the cover: JoinView)
        public static void Shake(float a) => shake = Mathf.Max(shake, a);
        public static void Dip(float m) { dipT = 0; dipA = m; } // a big landing: down and back up in 0.3 s

        void LateUpdate()
        {
            if (Target == null) return;
            float dt = Time.deltaTime;
            var look = Ctl.Look;
            if (look.sqrMagnitude > 0.0001f) lastLook = Time.time;
            bool aim = M == Mode.Shoulder;
            Yaw += look.x;
            if (M != Mode.Reveal && M != Mode.Fight) Pitch = Mathf.Clamp(Pitch - look.y, 10f, 60f);

            // targets per mode
            var a = Angler.I;
            // (the feel spec's distances were for the 1.78 m worker: scaled to the body, so she fills the frame the same)
            float tDist = (OnDeck ? 6f : Mathf.Min(9f, 7.5f + 0.35f * (a != null ? a.Load : 0))) * (a != null ? a.Size : 1), tFov = 55 + 4 * (a != null ? a.SprintW : 0), tSide = 0, tPitchOff = 0, tFocus = 0;
            switch (M)
            {
                case Mode.Shoulder: tDist = 4.5f; tSide = 0.6f; tFov = 50; tPitchOff = 20 - Pitch; tFocus = 0.25f; break;
                case Mode.Wait: tDist = 6.5f; tFocus = 0.3f; break;   // the player's own pitch: free to look up, or at her face
                case Mode.Fight: tDist = 5.5f; tPitchOff = 18 - Pitch; tFocus = 0.35f; tSide = 0.4f; break;
                case Mode.Reveal: tDist = 3.0f; tFov = 45; tPitchOff = 6 - Pitch; tFocus = 0; break;
                case Mode.Helm:
                    var s = Skiff.I; tDist = 9; tPitchOff = 18 - Pitch; tFov = 55 + 6 * (s != null ? Mathf.Clamp01(s.Speed / Skiff.MaxSpeed) : 0);
                    if (s != null && Time.time - lastLook > 2f) Yaw = Mathf.LerpAngle(Yaw, s.transform.eulerAngles.y, 1 - Mathf.Exp(-1.2f * dt)); // recentre
                    break;
            }
            // The bobber / the fish pulls the view only while it's in front of the camera: orbit round to her face and the
            // camera looks at her, not back over its own shoulder down into the water (Tushar 2 Nov: stuck looking at the sea).
            if (tFocus > 0)
            {
                var to = Focus - Target.position; to.y = 0;
                float ahead = to.sqrMagnitude > 0.01f ? Vector3.Dot(Quaternion.Euler(0, Yaw, 0) * Vector3.forward, to.normalized) : 1;
                tFocus *= Mathf.Clamp01(ahead + 0.3f);
            }
            // springs, not exponential eases: a framing change (aim -> wait on the bobber) starts and stops gently instead
            // of setting off at full speed in one frame
            dist = Mathf.SmoothDamp(dist, tDist, ref distV, 0.3f, Mathf.Infinity, dt); fov = Mathf.SmoothDamp(fov, tFov, ref fovV, 0.3f, Mathf.Infinity, dt);
            side = Mathf.SmoothDamp(side, tSide, ref sideV, 0.3f, Mathf.Infinity, dt);
            pitchOff = Mathf.SmoothDamp(pitchOff, tPitchOff, ref pitchV, 0.35f, Mathf.Infinity, dt); focusW = Mathf.SmoothDamp(focusW, tFocus, ref focusV, 0.35f, Mathf.Infinity, dt);
            if (M == Mode.Reveal)
            {
                // 3/4 front: swing round to face the player and the fish they hold up
                float front = Target.eulerAngles.y + 180 + RevealSide * 32;
                Yaw = Mathf.LerpAngle(Yaw, front, 1 - Mathf.Exp(-4f * dt));
            }

            // the pivot: 1.2 m up walking about (the fishing framings were made round 1.55), 0.8 m ahead of a run
            height = Mathf.SmoothDamp(height, M == Mode.Follow ? 1.2f : M == Mode.Reveal ? 1.35f : 1.55f, ref heightV, 0.3f, Mathf.Infinity, dt);
            var run = M == Mode.Follow && a != null ? Vector3.ClampMagnitude(new Vector3(a.Vel.x, 0, a.Vel.z) * (0.8f / 4.2f), 0.8f) : Vector3.zero;
            ahead = Vector3.Lerp(ahead, run, 1 - Mathf.Exp(-4f * dt));
            float groundY = a != null && Target == a.transform ? a.CamY : Target.position.y;
            var want = new Vector3(Target.position.x, groundY + height, Target.position.z) + ahead;
            // well below where it wants to be (up a ladder from the water): rise first, then in over the deck (Tushar 2 Nov:
            // from right by the pier ladder the pivot slid in under the deck edge before it rose, the view back crossed the
            // piles and the camera snapped in close behind her, then crept out again)
            float below = want.y - pivot.y, inT = below > 0.5f ? Mathf.Lerp(0.15f, 0.6f, Mathf.InverseLerp(0.5f, 1f, below)) : 0.15f;
            var pv = new Vector2(pivotV.x, pivotV.z);
            var flat = Vector2.SmoothDamp(new Vector2(pivot.x, pivot.z), new Vector2(want.x, want.z), ref pv, inT, Mathf.Infinity, dt);
            float py = Mathf.SmoothDamp(pivot.y, want.y, ref pivotV.y, 0.3f, Mathf.Infinity, dt);
            pivotV.x = pv.x; pivotV.z = pv.y;
            pivot = new Vector3(flat.x, py, flat.y);
            if (cut) { cut = false; pivot = want; pivotV = Vector3.zero; reach = 99; dist = tDist; distV = 0; }
            dipT += dt; var dip = Vector3.down * (dipA * Mathf.Sin(Mathf.PI * Mathf.Clamp01(dipT / 0.3f)));
            var rot = Quaternion.Euler(Pitch + pitchOff, Yaw, 0);
            var piv = pivot + dip;
            var p = piv + rot * new Vector3(side, 0, -dist);

            // keep off the land and the pier (the boat itself never pushes the camera: a 5 m deck would jitter it)
            // (pulled in over a few frames and let out slowly; thin posts are ignored: the lamp post at the pier end
            // squashed the camera into his head and swung the view 7° in a frame while casting there; anything that
            // can fade, the shack, fades instead: feel spec, pull in only for the ground and big hulls)
            var dir = p - piv; float len = dir.magnitude, room = len;
            Blocker = null;
            foreach (var hit in Physics.SphereCastAll(piv, 0.3f, dir / len, len, ~0, QueryTriggerInteraction.Ignore))
            {
                var bs = hit.collider.bounds.size;
                if (hit.distance <= 0 || bs.x < 0.5f && bs.z < 0.5f) continue; // (SphereCast never saw start overlaps either)
                if (Skiff.I != null && hit.transform.IsChildOf(Skiff.I.transform) || hit.collider.GetComponentInParent<Angler>() != null) continue;
                if (Fadeable(hit.collider).Count > 0) continue;
                if (hit.distance < room) { room = Mathf.Max(0.6f, hit.distance); Blocker = hit.collider.name; }
            }
            reach = Mathf.Min(reach, len);
            reach = Mathf.Lerp(reach, room, 1 - Mathf.Exp(-dt / (room < reach ? 0.05f : 0.35f)));
            p = piv + dir / len * reach;
            // never under the water
            float wy = Sea.Height(p) + 0.35f; if (p.y < wy) p.y = wy;

            // The focus glides (a jump in Focus, e.g. the marker 12 m out -> the bobber at the rod tip on release, snapped
            // the view: Tushar's "the screen stutters when I cast"), and the look blends into the focus framing instead
            // of switching to it at a threshold.
            if (focusW < 0.01f) focus = Focus;
            else focus = Vector3.Lerp(focus, Focus, 1 - Mathf.Exp(-dt / 0.25f));
            var aimAt = Vector3.Lerp(piv, focus, focusW);
            var fwd = M == Mode.Reveal ? (aimAt - p) : Vector3.Lerp(rot * Vector3.forward, (aimAt - p).normalized, Mathf.Clamp01(focusW / 0.05f));
            if (M == Mode.Reveal) fwd = (Target.position + Vector3.up * 1.3f + Target.forward * 0.25f) - p;
            // a little roll with the deck (systems §1.6.1: roll follow is a setting; calm default)
            float tRoll = OnDeck && Skiff.I != null ? -Mathf.DeltaAngle(0, Skiff.I.transform.eulerAngles.z) * 0.25f : 0;
            roll = Mathf.Lerp(roll, tRoll, 1 - Mathf.Exp(-3f * dt));
            var q = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(0, 0, roll);

            SeeThrough(p);

            if (shake > 0.001f)
            {
                p += Random.insideUnitSphere * shake * 0.15f; shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
            }
            FovPunch = Mathf.MoveTowards(FovPunch, 0, dt * 6f);
            transform.SetPositionAndRotation(p, q);
            Cam.fieldOfView = fov - FovPunch;
            if (UiCam != Cam) UiCam.fieldOfView = Cam.fieldOfView;
        }

        // Walls between the camera and the player fade (PB_Toon's _Fade dither, 65% gone in 0.15 s, back in 0.3 s): what
        // the line from the lens to her chest passes through, if it is a separate model under 8 m (the shack; the boat
        // when she's in the water beside it), never what she stands on or in. The pier and the land are single huge
        // models: the camera pulls in for those instead.
        readonly System.Collections.Generic.Dictionary<Renderer, float> fading = new();
        readonly System.Collections.Generic.Dictionary<Collider, System.Collections.Generic.List<Renderer>> fadeable = new();
        readonly System.Collections.Generic.HashSet<Renderer> blocking = new();
        MaterialPropertyBlock fadeMpb;
        public int Fading => fading.Count; // (tests)

        System.Collections.Generic.List<Renderer> Fadeable(Collider c)
        {
            if (fadeable.TryGetValue(c, out var list)) return list;
            list = new System.Collections.Generic.List<Renderer>();
            var cb = c.bounds.size;
            if (Mathf.Max(cb.x, cb.y, cb.z) > 8f) { fadeable[c] = list; return list; } // the ground, the pier: never (the camera pulls in)
            foreach (var r in c.transform.root.GetComponentsInChildren<MeshRenderer>())
            {
                var b = r.bounds;
                if (Mathf.Max(b.size.x, b.size.y, b.size.z) <= 8f && b.Intersects(c.bounds) && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Fade") && r.GetComponentInParent<Angler>() == null)
                    list.Add(r);
            }
            fadeable[c] = list;
            return list;
        }

        void SeeThrough(Vector3 cam)
        {
            blocking.Clear();
            var a = Angler.I;
            var at = Target.position + Vector3.up * (a != null && Target == a.transform ? 0.95f : 0.3f);
            var d = at - cam; float len = d.magnitude;
            if (len > 0.3f)
                foreach (var hit in Physics.SphereCastAll(cam, 0.2f, d / len, len - 0.25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Angler>() != null) continue;
                    if (a != null && a.OnBoat && Skiff.I != null && hit.transform.IsChildOf(Skiff.I.transform)) continue; // aboard: the boat stays
                    foreach (var r in Fadeable(hit.collider))
                        if (!r.bounds.Contains(Target.position + Vector3.up * 0.1f)) blocking.Add(r);
                }
            foreach (var r in blocking) if (!fading.ContainsKey(r)) fading[r] = 0;
            fadeMpb ??= new MaterialPropertyBlock();
            var keys = new System.Collections.Generic.List<Renderer>(fading.Keys);
            foreach (var r in keys)
            {
                if (r == null) { fading.Remove(r); continue; }
                bool on = blocking.Contains(r);
                float f = Mathf.MoveTowards(fading[r], on ? 0.65f : 0, Time.deltaTime * 0.65f / (on ? 0.15f : 0.3f));
                r.GetPropertyBlock(fadeMpb); fadeMpb.SetFloat("_Fade", f); r.SetPropertyBlock(fadeMpb);
                if (f <= 0 && !on) fading.Remove(r); else fading[r] = f;
            }
        }

        // Where the aim ray from the screen centre meets the sea (for the cast marker), flattened to the player's yaw.
        public Vector3 AimDir => Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
    }
}
