using UnityEngine;

namespace PortBoss
{
    // Marlowe's banner plane: every few minutes a little red plane drones low over our yard towing one of his
    // banners. Like the speedboat, it flies on a timetable of network time, so every peer sees the same pass.
    public class BannerPlane : MonoBehaviour
    {
        const float Period = 290f, Offset = 150f, Fly = 42f, Height = 30f, Span = 460f, BannerLen = 30f;
        static readonly string[] Banners =
        {
            "MARLOWE PORT: 10,000 CRATES A DAY. YOU? 12",
            "HONK IF YOUR PIER IS SINKING",
            "MARLOWE PORT IS HIRING. (NOT YOU)",
            "GRANDPA'S PIER? MORE LIKE GRANDPA'S PUDDLE",
            "SELL TO MARLOWE! $1.50 CASH OFFER",
        };

        Transform plane, prop, banner, rope;
        Renderer[] faces;
        RenderTexture[] texs;
        AudioSource drone;
        int shown = -1;

        public static void Create(Transform parent)
        {
            var b = new GameObject("BannerPlane").AddComponent<BannerPlane>();
            b.transform.SetParent(parent, false);
            b.Build();
        }

        void Build()
        {
            plane = new GameObject("plane").transform; plane.SetParent(transform, false);
            Color red = Art.Hex("#c8342a"), cream = Art.Hex("#f2ead8"), dark = Art.Hex("#2b2b2b");
            // A stubby cartoon plane, nose along +z: body, wings, tail, wheels, a spinning propeller.
            Art.Box(plane, new Vector3(0, 0, 0), new Vector3(1.3f, 1.3f, 5.5f), red, false, "body");
            Art.Box(plane, new Vector3(0, 0.15f, 2.9f), new Vector3(1.1f, 1.0f, 0.5f), cream, false, "nose");
            Art.Box(plane, new Vector3(0, 0.75f, 0.9f), new Vector3(1.0f, 0.5f, 1.2f), Art.Hex("#24323d"), false, "canopy");
            Art.Box(plane, new Vector3(0, 0.35f, 0.6f), new Vector3(10f, 0.18f, 1.6f), cream, false, "wing");
            Art.Box(plane, new Vector3(0, 0.25f, -2.6f), new Vector3(3.4f, 0.12f, 0.9f), cream, false, "tailplane");
            Art.Box(plane, new Vector3(0, 0.9f, -2.6f), new Vector3(0.14f, 1.4f, 1f), red, false, "fin");
            foreach (var x in new[] { -0.8f, 0.8f }) Art.Prim(PrimitiveType.Cylinder, plane, new Vector3(x, -0.95f, 1.2f), Quaternion.Euler(0, 0, 90), new Vector3(0.5f, 0.08f, 0.5f), dark, false, "wheel");
            prop = new GameObject("prop").transform; prop.SetParent(plane, false); prop.localPosition = new Vector3(0, 0.15f, 3.2f);
            Art.Box(prop, Vector3.zero, new Vector3(0.18f, 2.6f, 0.06f), dark, false, "blade");
            Art.Box(prop, Vector3.zero, new Vector3(2.6f, 0.18f, 0.06f), dark, false, "blade");
            plane.localScale = Vector3.one * 1.6f;
            // The banner (both faces painted) on a tow rope.
            rope = Art.Box(transform, Vector3.zero, new Vector3(0.06f, 0.06f, 1f), Art.Hex("#4a3b2a"), false, "rope").transform;
            banner = new GameObject("banner").transform; banner.SetParent(transform, false);
            texs = new RenderTexture[Banners.Length];
            for (int i = 0; i < Banners.Length; i++) texs[i] = HarbourMap.SignTexture(Banners[i], new Color(1f, 0.98f, 0.92f), Art.Hex("#b5402f"), BannerLen / 3.4f, true);
            faces = new Renderer[2];
            for (int k = 0; k < 2; k++)
            {
                var q = Art.Prim(PrimitiveType.Quad, banner, Vector3.zero, Quaternion.Euler(0, k == 0 ? 90 : -90, 0), new Vector3(BannerLen, 3.4f, 1), Color.white, false, "banner_face");
                faces[k] = q.GetComponent<Renderer>();
                faces[k].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            drone = gameObject.AddComponent<AudioSource>();
            drone.clip = Sfx.Clip("plane"); drone.loop = true; drone.spatialBlend = 1; drone.minDistance = 20; drone.maxDistance = 420;
            drone.rolloffMode = AudioRolloffMode.Linear; drone.volume = 0;
            if (drone.clip != null) drone.Play();
            plane.gameObject.SetActive(false); banner.gameObject.SetActive(false); rope.gameObject.SetActive(false);
        }

        void Update()
        {
            float t = Water.T, c = Mathf.Repeat(t - Offset, Period);
            bool on = t >= Offset && c < Fly;
            if (plane.gameObject.activeSelf != on) { plane.gameObject.SetActive(on); banner.gameObject.SetActive(on); rope.gameObject.SetActive(on); }
            if (drone != null) drone.volume = Mathf.MoveTowards(drone.volume, on ? 0.6f * Settings.Sfx : 0, Time.deltaTime);
            if (!on) return;
            int cycle = Mathf.FloorToInt((t - Offset) / Period), idx = Mathf.Abs(cycle) % Banners.Length;
            if (idx != shown && texs[idx] != null)
            {
                shown = idx;
                var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_TextureSample", texs[idx]);
                foreach (var f in faces) f.SetPropertyBlock(mpb);
            }
            // East to west over the yard, with a lazy weave.
            float u = c / Fly, x = Mathf.Lerp(Span, -Span, u), z = -25f + 14f * Mathf.Sin(u * Mathf.PI * 2f);
            var p = new Vector3(x, Height + Mathf.Sin(t * 0.8f) * 1.5f, z);
            var dir = new Vector3(-1, 0, 14f * Mathf.Cos(u * Mathf.PI * 2f) * Mathf.PI * 2f / (2 * Span)).normalized;
            plane.SetPositionAndRotation(p, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, -dir.z * 60f));
            prop.localRotation = Quaternion.Euler(0, 0, t * 1800f);
            // The banner trails behind on the rope, rippling a little.
            var tail = p - dir * 12f + Vector3.down * 2.5f;
            var bc = tail - dir * (BannerLen / 2);
            // Rolled about its length to face whoever is watching (from below it would be edge-on), never quite flat.
            float roll = 0;
            var cam = Camera.main;
            if (cam != null)
            {
                var to = Vector3.ProjectOnPlane(cam.transform.position - bc, dir);
                var side = Vector3.Cross(Vector3.up, dir).normalized; // the face normal with no roll
                roll = Mathf.Clamp(Vector3.SignedAngle(Vector3.Dot(to, side) >= 0 ? side : -side, to, dir), -65f, 65f);
            }
            banner.SetPositionAndRotation(bc, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, roll + Mathf.Sin(t * 3.1f) * 5f));
            var mid = (p + tail) / 2;
            rope.SetPositionAndRotation(mid, Quaternion.LookRotation(tail - p));
            rope.localScale = new Vector3(0.06f, 0.06f, (tail - p).magnitude);
        }
    }

    // Fireworks over Marlowe Port: he celebrates every freighter that docks there, and the whole coast can see it.
    public class Fireworks : MonoBehaviour
    {
        static readonly Color[] Colors = { new(1f, 0.35f, 0.3f), new(1f, 0.85f, 0.3f), new(0.4f, 0.8f, 1f), new(0.5f, 1f, 0.5f), new(1f, 0.5f, 0.95f), Color.white };
        public static Vector3 Over => new(HarbourMap.MarloweSign.x, 70f, 660f); // over his quay front, closer to us than the HQ
        Vector3 at;
        float t0;
        int fired;
        const int Shells = 12;

        public static void Show(Vector3 at)
        {
            var f = new GameObject("Fireworks").AddComponent<Fireworks>();
            f.at = at; f.t0 = Time.time;
            Destroy(f.gameObject, 12f);
        }

        void Update()
        {
            // A shell every ~0.45 s, the last three together for the finale.
            while (fired < Shells && Time.time - t0 >= (fired < Shells - 3 ? fired * 0.45f : (Shells - 3) * 0.45f + 0.6f))
            {
                var p = at + new Vector3(Random.Range(-110f, 110f), Random.Range(20f, 110f), Random.Range(-25f, 25f));
                Shell(p, Colors[Random.Range(0, Colors.Length)], fired >= Shells - 3 ? 1.4f : 1f);
                fired++;
            }
        }

        static void Shell(Vector3 p, Color c, float size)
        {
            var g = new GameObject("shell");
            g.transform.position = p;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop();
            var m = ps.main;
            m.duration = 0.1f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 2.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(36f * size, 45f * size); // a ~100 m ball, seen from 600 m
            m.startSize = new ParticleSystem.MinMaxCurve(7f * size, 11f * size);
            m.startColor = c;
            m.gravityModifier = 0.5f; m.maxParticles = 300; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(140 * size)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1f; sh.radiusThickness = 0f; // all from the shell, out to the same distance
            var shrink = ps.sizeOverLifetime; shrink.enabled = true; shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.4f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Art.Spark != null ? Art.Spark : Art.Particle; // glowing, not fogged
            ps.Play();
            Destroy(g, 3f);
            // The bang arrives later than the flash (sound crosses the sea at ~343 m/s), quiet from that far.
            var cam = Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, p) : 800f;
            var snd = new GameObject("boom").AddComponent<Delayed>();
            snd.at = Time.time + dist / 343f; snd.vol = Mathf.Clamp(260f / Mathf.Max(dist, 1f), 0.08f, 0.6f);
        }

        class Delayed : MonoBehaviour
        {
            public float at, vol;
            void Update() { if (Time.time >= at) { Sfx.Play2D("boom", vol); Destroy(gameObject); } }
        }
    }
}
