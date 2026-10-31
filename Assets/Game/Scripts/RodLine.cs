using UnityEngine;

namespace PortBoss.M1
{
    // The rod: a tapered 2.4 m bamboo rod as a chain of 6 segments that bends tip-heavy with line tension (gdd fishing
    // §3.11: bone weights .05/.08/.12/.18/.25/.32, spring 8 Hz, damping 0.4), cork grip, a little reel. Its pose
    // (elevation, yaw, where the butt sits) is set by Fishing; it lags those targets on a spring so the tip trails the
    // hands and sways with the boat.
    public class RodView : MonoBehaviour
    {
        public Transform Root, Tip;
        readonly Transform[] seg = new Transform[6];
        static readonly float[] W = { 0.05f, 0.08f, 0.12f, 0.18f, 0.25f, 0.32f };
        public float Tension;              // 0..1 target
        public Vector3 BendDir = Vector3.down; // world direction the line pulls
        float bend, bendV;
        public const float Length = 2.4f;
        public static readonly Vector3 GripR = new(0, -0.02f, 0.42f), GripL = new(0, -0.02f, 0.1f);

        public static RodView Make(Material toon, int tier, float thick = 1f)
        {
            var g = new GameObject("Rod"); var r = g.AddComponent<RodView>();
            r.Root = g.transform;
            Color blank = tier switch { 1 => Ponyo.Hex("D8B070"), 2 => Ponyo.Hex("F2E2C0"), 3 => Ponyo.Hex("3A3A48"), 4 => Ponyo.Hex("2160B8"), _ => Ponyo.Hex("6A4A30") };
            Color wrap = tier == 5 ? Ponyo.Hex("F7C548") : Ponyo.Hex("D9483B");
            // cork handle + reel seat
            Util.MeshObj("cork", Util.Cylinder(10, 0.02f * thick, 0.017f * thick, 0.5f, Ponyo.Hex("C9A27A"), Ponyo.Hex("B88E66")), toon, g.transform);
            var reel = Util.MeshObj("reel", Util.Cylinder(12, 0.045f, 0.045f, 0.035f, Ponyo.Hex("5E5A66"), Ponyo.Hex("7A7684")), toon, g.transform);
            reel.transform.localPosition = new Vector3(0, -0.06f, 0.36f); reel.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var handle = Util.MeshObj("crank", Util.Cylinder(6, 0.006f, 0.006f, 0.06f, Ponyo.Hex("F7C548"), Ponyo.Hex("F7C548")), toon, reel.transform);
            handle.transform.localPosition = new Vector3(0, 0.03f, 0.035f);
            var foot = Util.MeshObj("foot", Util.Cylinder(6, 0.008f, 0.008f, 0.06f, Ponyo.Hex("5E5A66"), Ponyo.Hex("5E5A66")), toon, g.transform);
            foot.transform.localPosition = new Vector3(0, -0.005f, 0.36f); foot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            float len = (Length - 0.5f) / 6f, r0 = 0.016f * thick;
            Transform parent = g.transform; float z = 0.5f;
            for (int i = 0; i < 6; i++)
            {
                var s = new GameObject("seg" + i).transform; s.SetParent(parent, false); s.localPosition = new Vector3(0, 0, i == 0 ? z : len);
                float r1 = Mathf.Lerp(0.016f, 0.0035f, (i + 1) / 6f) * thick;
                Util.MeshObj("blank", Util.Cylinder(8, r0, r1, len, blank, blank), toon, s);
                // a guide ring + wrap at each joint
                var gw = Util.MeshObj("wrap", Util.Cylinder(8, r0 + 0.003f, r0 + 0.003f, 0.025f, wrap, wrap), toon, s);
                var gd = Util.MeshObj("guide", Util.Cylinder(6, 0.004f, 0.004f, 0.025f, Ponyo.Hex("C8CCD8"), Ponyo.Hex("C8CCD8")), toon, s);
                gd.transform.localPosition = new Vector3(0, -r0, 0.012f); gd.transform.localRotation = Quaternion.Euler(90, 0, 0);
                r.seg[i] = s; parent = s; r0 = r1;
            }
            r.Tip = new GameObject("tip").transform; r.Tip.SetParent(parent, false); r.Tip.localPosition = new Vector3(0, 0, len);
            foreach (var mr in g.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return r;
        }

        void LateUpdate()
        {
            // spring 8 Hz, damping 0.4 toward the tension's tip angle (0..70°)
            float target = Mathf.Clamp01(Tension) * 70f, w = 2 * Mathf.PI * 8f, dt = Mathf.Min(Time.deltaTime, 0.05f);
            bendV += (-(w * w) * (bend - target) - 2 * 0.4f * w * bendV) * dt;
            bend += bendV * dt;
            // bend toward the line: rotate each joint about the axis perpendicular to the rod and the pull
            var axisW = Vector3.Cross(Root.forward, BendDir);
            if (axisW.sqrMagnitude < 1e-4f) axisW = -Root.right;
            for (int i = 0; i < 6; i++)
            {
                seg[i].localRotation = Quaternion.identity;
            }
            for (int i = 0; i < 6; i++)
            {
                var ax = seg[i].InverseTransformDirection(axisW.normalized);
                seg[i].localRotation = Quaternion.AngleAxis(bend * W[i], ax);
            }
        }
    }

    // Verlet fishing line, 32 points from the rod tip to the bobber: sags when slack, straight under tension, never
    // through Biscuit (points inside the hull footprint are lifted over the gunwale).
    public class LineView : MonoBehaviour
    {
        const int N = 32;
        readonly Vector3[] p = new Vector3[N], prev = new Vector3[N];
        LineRenderer lr;
        public Transform A; public Vector3 B; public float Slack = 0.04f; public bool Live;

        public static LineView Make()
        {
            var g = new GameObject("Line"); var l = g.AddComponent<LineView>();
            l.lr = g.AddComponent<LineRenderer>(); l.lr.positionCount = N; l.lr.useWorldSpace = true;
            l.lr.widthMultiplier = 0.012f; l.lr.numCapVertices = 2;
            var m = new Material(M1Assets.I.Particle); m.SetColor("_BaseColor", new Color(1f, 0.98f, 0.92f, 0.9f));
            m.SetFloat("_Surface", 1); m.renderQueue = 3050; m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            l.lr.sharedMaterial = m; l.lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.lr.enabled = false;
            return l;
        }

        public void Reset(Vector3 a, Vector3 b)
        {
            for (int i = 0; i < N; i++) p[i] = prev[i] = Vector3.Lerp(a, b, i / (N - 1f));
        }

        void LateUpdate()
        {
            lr.enabled = Live && A != null;
            if (!lr.enabled) return;
            float dt = Mathf.Min(Time.deltaTime, 1 / 30f);
            var a = A.position;
            float rest = Vector3.Distance(a, B) * (1 + Slack) / (N - 1);
            for (int i = 1; i < N - 1; i++)
            {
                var v = (p[i] - prev[i]) * 0.96f; prev[i] = p[i];
                p[i] += v + Vector3.down * 9.8f * 0.6f * dt * dt;
            }
            p[0] = a; p[N - 1] = B;
            var s = Skiff.I;
            for (int it = 0; it < 12; it++)
            {
                for (int i = 0; i < N - 1; i++)
                {
                    var d = p[i + 1] - p[i]; float len = d.magnitude; if (len < 1e-5f) continue;
                    var c = d * (1 - rest / len) * 0.5f;
                    if (i > 0) p[i] += c; if (i + 1 < N - 1) p[i + 1] -= c;
                }
                for (int i = 1; i < N - 1; i++)
                {
                    if (p[i].y < Sea.Level - 0.25f) p[i].y = Sea.Level - 0.25f; // floats just under the surface
                    if (s != null && s.Inside(p[i], 0.05f)) { float gy = s.GunwaleY(p[i]) + 0.04f; if (p[i].y < gy) p[i].y = gy; }
                }
            }
            lr.SetPositions(p);
            // ~1.5 px at any distance (screen-constant width, fishing §3.3)
            var cam = Camera.main;
            if (cam != null)
            {
                float d0 = Vector3.Distance(cam.transform.position, a), d1 = Vector3.Distance(cam.transform.position, B);
                float px = 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1, cam.pixelHeight) * 2.6f;
                lr.widthCurve = new AnimationCurve(new Keyframe(0, Mathf.Max(0.006f, d0 * px)), new Keyframe(1, Mathf.Max(0.006f, d1 * px)));
                lr.widthMultiplier = 1;
            }
        }
    }

    // The red-and-white float (MUST): Ø 6 cm x 2.5 for readability, rides the wave height and normal.
    public class BobberView : MonoBehaviour
    {
        public Vector3 Pos;            // resting point on the sea (xz drifts)
        public float Dip, Yank, Spin;  // metres under (nibble / bite), extra sideways drag, spin
        public bool Floating;
        float rippleT; Transform halo;

        public static BobberView Make(Material toon)
        {
            var g = new GameObject("Bobber"); var b = g.AddComponent<BobberView>();
            Color red = Ponyo.Hex("E2553F"), white = Ponyo.Hex("FFFBF2");
            Util.MeshObj("float", Util.Sphere(10, 0.075f, v => v.y > 0.08f ? red : v.y > -0.05f ? Ponyo.Hex("F4F0E8") : white), toon, g.transform).transform.localScale = new Vector3(1, 1.15f, 1);
            var stick = Util.MeshObj("stick", Util.Cylinder(6, 0.01f, 0.006f, 0.09f, red, red), toon, g.transform);
            stick.transform.localPosition = new Vector3(0, 0.07f, 0); stick.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var band = Util.MeshObj("band", Util.Cylinder(12, 0.0765f, 0.0765f, 0.018f, Ponyo.Hex("3F2C27"), Ponyo.Hex("3F2C27")), toon, g.transform);
            band.transform.localPosition = new Vector3(0, 0.0f, 0); band.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            // bright: a little self-light and a strong white rim, plus a soft camera-facing halo
            var bright = new Material(toon); bright.SetFloat("_Emit", 0.35f); bright.SetColor("_RimColor", new Color(1, 1, 1, 0.8f)); bright.SetFloat("_RimPower", 2.5f); bright.SetFloat("_PaintNoise", 0);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = bright;
            var halo = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(halo.GetComponent<Collider>()); halo.name = "halo";
            halo.transform.SetParent(g.transform, false); halo.transform.localScale = Vector3.one * 0.42f;
            var hm = Util.Soft(new Color(1f, 0.6f, 0.45f, 0.45f)); hm.renderQueue = 3150;
            halo.GetComponent<MeshRenderer>().sharedMaterial = hm; halo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.halo = halo.transform;
            g.transform.localScale = Vector3.one * 1.25f;
            b.gameObject.SetActive(false);
            return b;
        }

        void LateUpdate()
        {
            if (!Floating) return;
            float h = Sea.Height(Pos);
            var n = Sea.Normal(Pos.x, Pos.z, 0.25f);
            float bob = Mathf.Sin(Time.time * 2 * Mathf.PI * 0.8f) * 0.015f;
            transform.position = new Vector3(Pos.x, h + 0.02f + bob - Dip - Yank, Pos.z);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0, Spin, 0);
            // never lost at range: grows so it stays ~10 px tall beyond 15 m
            var cam = Camera.main;
            float d = cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 10;
            transform.localScale = Vector3.one * Mathf.Max(1.25f, d / 12f);
            if (halo != null && cam != null) { halo.rotation = Quaternion.LookRotation(halo.position - cam.transform.position); halo.gameObject.SetActive(Yank < 0.05f); }
            rippleT += Time.deltaTime;
            if (rippleT > 1.6f && Yank < 0.05f) { rippleT = 0; Fx.Ring(new Vector3(Pos.x, h, Pos.z), 1.2f, 1.0f, 0.45f); }
        }
    }

    // A fish model, scaled to its length (FBX fish are 1 m nose to tail, head +Z), swimming by vertex bend.
    public static class FishView
    {
        public static GameObject Make(Fish f, Transform parent)
        {
            var a = M1Assets.I;
            var pf = a.FishPrefab(f.Species.Id);
            GameObject g;
            if (pf != null) { g = Object.Instantiate(pf, parent, false); foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c); }
            else { g = Util.MeshObj("fish", Util.Sphere(8, 0.5f, _ => Ponyo.Hex("9FB0C8")), null, parent); }
            var mat = Game.FishMat(f.Species);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            g.transform.localScale = Vector3.one * (f.Cm / 100f);
            return g;
        }

        public static void Swim(GameObject g, float amp, float freq)
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) { r.GetPropertyBlock(mpb); mpb.SetFloat("_Swim", amp); mpb.SetFloat("_SwimFreq", freq); r.SetPropertyBlock(mpb); }
        }
    }
}
