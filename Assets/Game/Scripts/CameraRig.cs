using UnityEngine;

namespace PortBoss.M1
{
    // Third-person camera (feel spec: 7.5 m, 30°, FOV 55, 0.15 s damping; on a deck 6 m). Fishing framings from gdd
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
        public float Yaw, Pitch = 24f;
        public enum Mode { Follow, Shoulder, Wait, Fight, Reveal, Helm }
        public Mode M;
        public Vector3 Focus;             // marker / bobber / fish
        public bool OnDeck;
        public float FovPunch;            // the bite's -3° punch
        public float RevealSide = -1;     // which 3/4 side the reveal frames from (Fishing picks the one with a clear view)

        public string Blocker;            // what the camera is pulled in by this frame (tests)
        Vector3 pivot, pos, focus;
        float dist = 6, fov = 55, side, pitchOff, focusW, lastLook, roll, reach = 99, distV, fovV, sideV, pitchV, focusV;
        static float shake;

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

        public static void Shake(float a) => shake = Mathf.Max(shake, a);

        void LateUpdate()
        {
            if (Target == null) return;
            float dt = Time.deltaTime;
            var look = Ctl.Look;
            if (look.sqrMagnitude > 0.0001f) lastLook = Time.time;
            bool aim = M == Mode.Shoulder;
            Yaw += look.x;
            if (M != Mode.Reveal && M != Mode.Fight) Pitch = Mathf.Clamp(Pitch - look.y, -8f, 65f);

            // targets per mode
            float tDist = OnDeck ? 6f : 7.5f, tFov = 55, tSide = 0, tPitchOff = 0, tFocus = 0;
            switch (M)
            {
                case Mode.Shoulder: tDist = 4.5f; tSide = 0.6f; tFov = 50; tPitchOff = 20 - Pitch; tFocus = 0.25f; break;
                case Mode.Wait: tDist = 6.5f; tPitchOff = 25 - Pitch; tFocus = 0.3f; break;
                case Mode.Fight: tDist = 5.5f; tPitchOff = 18 - Pitch; tFocus = 0.35f; tSide = 0.4f; break;
                case Mode.Reveal: tDist = 3.0f; tFov = 45; tPitchOff = 6 - Pitch; tFocus = 0; break;
                case Mode.Helm:
                    var s = Skiff.I; tDist = 9; tPitchOff = 18 - Pitch; tFov = 55 + 6 * (s != null ? Mathf.Clamp01(s.Speed / Skiff.MaxSpeed) : 0);
                    if (s != null && Time.time - lastLook > 2f) Yaw = Mathf.LerpAngle(Yaw, s.transform.eulerAngles.y, 1 - Mathf.Exp(-1.2f * dt)); // recentre
                    break;
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

            var want = Target.position + Vector3.up * (M == Mode.Reveal ? 1.35f : 1.55f);
            pivot = Vector3.Lerp(pivot, want, 1 - Mathf.Exp(-dt / 0.15f));
            var rot = Quaternion.Euler(Pitch + pitchOff, Yaw, 0);
            var p = pivot + rot * new Vector3(side, 0, -dist);

            // keep off the land and the pier (the boat itself never pushes the camera: a 5 m deck would jitter it)
            // (pulled in over a few frames and let out slowly; thin posts are ignored: the lamp post at the pier end
            // squashed the camera into his head and swung the view 7° in a frame while casting there)
            var dir = p - pivot; float len = dir.magnitude, room = len;
            Blocker = null;
            foreach (var hit in Physics.SphereCastAll(pivot, 0.3f, dir / len, len, ~0, QueryTriggerInteraction.Ignore))
            {
                var bs = hit.collider.bounds.size;
                if (hit.distance <= 0 || bs.x < 0.5f && bs.z < 0.5f) continue; // (SphereCast never saw start overlaps either)
                if (Skiff.I != null && hit.transform.IsChildOf(Skiff.I.transform) || hit.collider.GetComponentInParent<Angler>() != null) continue;
                if (hit.distance < room) { room = Mathf.Max(0.6f, hit.distance); Blocker = hit.collider.name; }
            }
            reach = Mathf.Min(reach, len);
            reach = Mathf.Lerp(reach, room, 1 - Mathf.Exp(-dt / (room < reach ? 0.05f : 0.35f)));
            p = pivot + dir / len * reach;
            // never under the water
            float wy = Sea.Height(p) + 0.35f; if (p.y < wy) p.y = wy;

            // The focus glides (a jump in Focus, e.g. the marker 12 m out -> the bobber at the rod tip on release, snapped
            // the view: Tushar's "the screen stutters when I cast"), and the look blends into the focus framing instead
            // of switching to it at a threshold.
            if (focusW < 0.01f) focus = Focus;
            else focus = Vector3.Lerp(focus, Focus, 1 - Mathf.Exp(-dt / 0.25f));
            var aimAt = Vector3.Lerp(pivot, focus, focusW);
            var fwd = M == Mode.Reveal ? (aimAt - p) : Vector3.Lerp(rot * Vector3.forward, (aimAt - p).normalized, Mathf.Clamp01(focusW / 0.05f));
            if (M == Mode.Reveal) fwd = (Target.position + Vector3.up * 1.3f + Target.forward * 0.25f) - p;
            // a little roll with the deck (systems §1.6.1: roll follow is a setting; calm default)
            float tRoll = OnDeck && Skiff.I != null ? -Mathf.DeltaAngle(0, Skiff.I.transform.eulerAngles.z) * 0.25f : 0;
            roll = Mathf.Lerp(roll, tRoll, 1 - Mathf.Exp(-3f * dt));
            var q = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(0, 0, roll);

            if (shake > 0.001f)
            {
                p += Random.insideUnitSphere * shake * 0.15f; shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
            }
            FovPunch = Mathf.MoveTowards(FovPunch, 0, dt * 6f);
            transform.SetPositionAndRotation(p, q);
            Cam.fieldOfView = fov - FovPunch;
            if (UiCam != Cam) UiCam.fieldOfView = Cam.fieldOfView;
        }

        // Where the aim ray from the screen centre meets the sea (for the cast marker), flattened to the player's yaw.
        public Vector3 AimDir => Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
    }
}
