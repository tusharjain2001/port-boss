using UnityEngine;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M3.1 (Manan + Tushar's co-op test, 3 Nov): a leak you can see, and a patch you can watch. On every copy of Biscuit
    // (Leaking rides in the boat's pose): water spurting in at the hole, a pulsing red ring round it, a pin over it on the
    // screen (an arrow at the edge when it's off screen) that fills with the patch's progress, a gurgle now and then.
    // Whoever patches kneels at the hole with a plank and a hammer (Angler.RepairT: State 4 in the pose, so everyone sees
    // it); the plank stays nailed over the hole once she's patched.
    public class LeakFx : MonoBehaviour
    {
        Skiff s; ParticleSystem spout; Transform ring, plank; Material ringMat;
        RectTransform pin, arrow; Image prog; Text label; CanvasGroup pinG;
        bool wasLeak, patched; float gurgleT;
        public static float Progress;          // 0..1: the patch's progress (whoever is patching), for the pin and the tests
        public static bool PinShown, ArrowShown, PlankShown; // (tests)
        static LeakFx I;
        public static void PlaceNow() { if (I != null && I.s != null) I.Pin(I.s.Leaking, Angler.I); } // (a test picture from another camera)

        public static void Ensure(Skiff s) { if (s.GetComponent<LeakFx>() == null) s.gameObject.AddComponent<LeakFx>().s = s; }

        void Start()
        {
            if (s == null) s = GetComponent<Skiff>(); I = this;
            // the spout: droplets jetting up out of the hole, riding with the boat
            var g = new GameObject("leakSpout"); g.transform.SetParent(transform, false); g.transform.localPosition = s.HoleLocal;
            spout = g.AddComponent<ParticleSystem>();
            var m = spout.main; m.loop = true; m.simulationSpace = ParticleSystemSimulationSpace.Local; m.maxParticles = 200;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f); m.gravityModifier = 1.1f; m.startColor = new Color(0.85f, 0.97f, 1f, 0.95f);
            var e = spout.emission; e.rateOverTime = 0;
            var sh = spout.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14; sh.radius = 0.05f; sh.rotation = new Vector3(-90, 0, 0);
            var sz = spout.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.4f));
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.Soft(Color.white); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1.2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spout.Play();
            // the ring round the hole, pulsing
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "leakRing";
            ringMat = Util.Soft(Ponyo.Poppy, RingTex()); ringMat.renderQueue = 3200;
            var qr = q.GetComponent<MeshRenderer>(); qr.sharedMaterial = ringMat; qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring = q.transform; ring.SetParent(transform, false); ring.localPosition = s.HoleLocal + Vector3.up * 0.025f; ring.localRotation = Quaternion.Euler(90, 0, 0);
            // the plank nailed over it (shown while someone patches, and after)
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(p.GetComponent<Collider>()); p.name = "leakPlank";
            p.GetComponent<Renderer>().sharedMaterial = Ponyo.Toon(M1Assets.I, Ponyo.Hex("C89B6A"));
            plank = p.transform; plank.SetParent(transform, false); plank.localPosition = s.HoleLocal + Vector3.up * 0.018f;
            plank.localRotation = Quaternion.Euler(0, 12, 0); plank.localScale = new Vector3(0.36f, 0.03f, 0.2f);
            for (int i = -1; i <= 1; i += 2) // two nail heads
            {
                var n = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(n.GetComponent<Collider>()); n.name = "nail";
                n.GetComponent<Renderer>().sharedMaterial = Ponyo.Toon(M1Assets.I, Ponyo.Hex("5B5E6E"));
                n.transform.SetParent(plank, false); n.transform.localPosition = new Vector3(0.38f * i, 0.55f, 0); n.transform.localScale = new Vector3(0.07f, 0.12f, 0.11f);
            }
            BuildPin();
        }

        void BuildPin()
        {
            var hud = Hud.I.transform;
            pin = UiKit.Rect("leakPin", hud); pin.sizeDelta = new Vector2(64, 64); pin.anchorMin = pin.anchorMax = new Vector2(0.5f, 0.5f);
            pinG = pin.gameObject.AddComponent<CanvasGroup>(); pinG.blocksRaycasts = false;
            var halo = UiKit.Img(pin, UiKit.Circle, new Color(1, 1, 1, 0.9f), "halo"); halo.rectTransform.Stretch(-5);
            var bg = UiKit.Img(pin, UiKit.Circle, Ponyo.Poppy, "bg"); bg.rectTransform.Stretch();
            prog = UiKit.Img(pin, UiKit.RingSprite, Ponyo.Sunflower, "progress"); prog.rectTransform.Stretch(-9);
            prog.type = Image.Type.Filled; prog.fillMethod = Image.FillMethod.Radial360; prog.fillOrigin = (int)Image.Origin360.Top; prog.fillClockwise = true;
            var t = UiKit.Txt(pin, "!", UiKit.DisplayBold, 40, Color.white, TextAnchor.MiddleCenter); t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(0, 2);
            label = UiKit.Txt(pin, "Leak", UiKit.Bold, 19, Color.white, TextAnchor.UpperCenter); label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.rectTransform.anchorMin = new Vector2(0.5f, 0); label.rectTransform.anchorMax = new Vector2(0.5f, 0); label.rectTransform.pivot = new Vector2(0.5f, 1);
            label.rectTransform.sizeDelta = new Vector2(200, 30); label.rectTransform.anchoredPosition = new Vector2(0, -8);
            UiKit.Shade(label, new Color(0.25f, 0.17f, 0.15f, 0.85f), new Vector2(1.5f, -1.5f));
            // the arrow at the screen's edge (off screen): a triangle pointing the way, just outside the pin
            arrow = UiKit.Img(pin, Triangle(), Ponyo.Poppy, "arrow").rectTransform; arrow.sizeDelta = new Vector2(30, 26);
            pin.gameObject.SetActive(false);
        }

        // a bold ring with a soft glow inside (Util.Ring is a thin ripple)
        static Texture2D ringTex;
        static Texture2D RingTex()
        {
            if (ringTex != null) return ringTex;
            int n = 128; ringTex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float fx = (x + 0.5f) / n * 2 - 1, fy = (y + 0.5f) / n * 2 - 1, r = Mathf.Sqrt(fx * fx + fy * fy);
                    float band = Mathf.Clamp01(1 - Mathf.Abs(r - 0.82f) / 0.09f), glow = r < 0.82f ? 0.22f * r / 0.82f : 0;
                    ringTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(band, glow)));
                }
            ringTex.Apply(); return ringTex;
        }

        static Sprite tri;
        static Sprite Triangle()
        {
            if (tri != null) return tri;
            int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n; // pointing up: wide at the bottom, the tip at the top
                    float half = 0.5f * (1 - v), d = half - Mathf.Abs(u - 0.5f);
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(d * n * 0.9f) * (v > 0.06f ? 1 : 0)));
                }
            t.Apply(); return tri = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        void Update()
        {
            if (s == null) return;
            bool leak = s.Leaking;
            if (leak && !wasLeak) patched = false;
            if (!leak && wasLeak) patched = true;
            wasLeak = leak;
            // whoever is patching: me, or a crewmate (from their pose)
            float best = -1; var me = Angler.I;
            if (me != null) best = me.RepairT;
            var c = Coop.I; if (c != null) foreach (var m in c.Mates.Values) if (m != null) best = Mathf.Max(best, m.RepairT);
            bool repairing = best >= 0;
            Progress = leak && repairing ? Mathf.Clamp01(best / Skiff.PatchTime) : 0;

            var em = spout.emission; em.rateOverTime = leak ? (repairing ? 30f : 80f) : 0; // (half covered by the plank while it's nailed)
            ring.gameObject.SetActive(leak);
            if (leak)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                ring.localScale = Vector3.one * (0.62f + 0.16f * k);
                ringMat.SetColor("_BaseColor", Color.Lerp(Ponyo.Poppy, Ponyo.Sunflower, Progress) * new Color(1, 1, 1, 0.55f + 0.4f * k));
                if ((gurgleT -= Time.deltaTime) <= 0) { gurgleT = 1.4f; Snd.Play(M1Assets.I.Splash, s.Hole, 0.16f, 1.6f); }
            }
            PlankShown = repairing && leak || patched;
            plank.gameObject.SetActive(PlankShown);
            Pin(leak, me);
        }

        // the pin over the hole: there on screen, or at the edge with an arrow pointing the way
        void Pin(bool leak, Angler me)
        {
            var cam = CameraRig.I != null ? CameraRig.I.Cam : null;
            bool show = leak && cam != null && me != null && !Title.Shown && Vector3.Distance(me.transform.position, s.Hole) < 80;
            if (pin.gameObject.activeSelf != show) pin.gameObject.SetActive(show);
            PinShown = show; ArrowShown = false;
            if (!show) return;
            pin.SetAsLastSibling();
            var hud = (RectTransform)Hud.I.transform; var ui = CameraRig.I.UiCam;
            var sp = cam.WorldToScreenPoint(s.Hole + Vector3.up * 0.55f);
            float w = Screen.width, h = Screen.height, margin = 70 * w / 1920f;
            bool behind = sp.z < 0; if (behind) { sp.x = w - sp.x; sp.y = h - sp.y; }
            bool off = behind || sp.x < margin || sp.x > w - margin || sp.y < margin || sp.y > h - margin;
            Vector2 at = sp, dir = Vector2.zero;
            if (off)
            {
                var ctr = new Vector2(w / 2, h / 2); dir = (at - ctr); if (behind && dir.sqrMagnitude < 1) dir = Vector2.down; dir.Normalize();
                // along the line from the centre to where it is, to the edge of the inset screen
                float kx = dir.x != 0 ? (w / 2 - margin) / Mathf.Abs(dir.x) : float.MaxValue, ky = dir.y != 0 ? (h / 2 - margin) / Mathf.Abs(dir.y) : float.MaxValue;
                at = ctr + dir * Mathf.Min(kx, ky);
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, at, ui, out var lp);
            pin.anchoredPosition = lp;
            arrow.gameObject.SetActive(off); ArrowShown = off;
            if (off) { arrow.anchoredPosition = dir * 50; arrow.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90); }
            prog.fillAmount = Progress; prog.enabled = Progress > 0;
            float dist = Vector3.Distance(me.transform.position, s.Hole);
            label.text = Progress > 0 ? $"Patching {Mathf.RoundToInt(Progress * 100)}%" : off || dist > 4 ? $"Leak · {dist:0} m" : "Leak";
            float pulse = Progress > 0 ? 1 : 1 + 0.08f * Mathf.Sin(Time.time * 6f);
            pin.localScale = Vector3.one * pulse;
        }

        void OnDestroy() { if (pin != null) Destroy(pin.gameObject); }
    }

    // The hammer in a patcher's right hand, swung by the arm's IK (Angler.RepairLift). After CharAnim's IK (1000), so it
    // sits in the hand as posed this frame.
    [DefaultExecutionOrder(1010)]
    public class Hammer : MonoBehaviour
    {
        public Angler A; Transform h;
        void LateUpdate()
        {
            bool on = A != null && A.RepairT >= 0 && A.Anim != null && A.Anim.HandR != null && !A.Down;
            if (h == null) { if (!on) return; h = Make(); }
            if (h.gameObject.activeSelf != on) h.gameObject.SetActive(on);
            if (!on) return;
            var across = Quaternion.AngleAxis(-25, Vector3.up) * A.transform.forward;              // toward the plank, under the body
            var axis = Vector3.Cross(Vector3.up, across);
            var dir = Quaternion.AngleAxis(Mathf.Lerp(50, -40, A.RepairLift), axis) * across;     // head down on the plank, up at the top
            h.SetPositionAndRotation(A.Anim.HandR.position + dir * 0.03f, Quaternion.LookRotation(dir, Vector3.up));
        }
        Transform Make()
        {
            var root = new GameObject("hammer").transform;
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().sharedMaterial = Ponyo.Toon(M1Assets.I, Ponyo.Hex("A8754A"));
            handle.transform.SetParent(root, false); handle.transform.localPosition = new Vector3(0, 0, 0.09f); handle.transform.localScale = new Vector3(0.028f, 0.028f, 0.29f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = Ponyo.Toon(M1Assets.I, Ponyo.Hex("5B5E6E"));
            head.transform.SetParent(root, false); head.transform.localPosition = new Vector3(0, -0.012f, 0.23f); head.transform.localScale = new Vector3(0.048f, 0.115f, 0.052f);
            return root;
        }
        void OnDestroy() { if (h != null) Destroy(h.gameObject); }
    }
}
