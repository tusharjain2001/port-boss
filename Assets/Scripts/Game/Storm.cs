using UnityEngine;

namespace PortBoss
{
    // Local weather presentation on every peer, driven by the host's PortGame.Severity (0 calm .. 1 storm):
    // bigger, choppier waves (Water.Storm), wind, rain around the camera, thunder + lightning at the top end.
    // No sirens or warnings: the weather is something you read from the sky and the sea.
    public class Storm : MonoBehaviour
    {
        public static Storm I;
        public static float Level;     // weather severity 0 calm .. 1 full storm (smoothed, every peer)
        public static float Rain => Mathf.Clamp01((Level - 0.45f) / 0.4f);   // rain from "overcast" up
        public static float Gust { get { float g = Mathf.Clamp01((Level - 0.45f) / 0.55f); return g * g; } } // wind strong enough to shove things (from rain up)
        public static float Flash;     // lightning flash 0..1
        public static readonly Vector3 WindDir = new Vector3(1f, 0, 0.25f).normalized; // across the pier: loose crates go over the side

        ParticleSystem rain, caps;
        AudioSource wind;
        float nextThunder;

        public static void Create()
        {
            I = new GameObject("Storm").AddComponent<Storm>();
            var r = new GameObject("rain").transform;
            r.SetParent(I.transform, false);
            I.rain = r.gameObject.AddComponent<ParticleSystem>();
            I.rain.Stop();
            var m = I.rain.main;
            m.startLifetime = 1.1f; m.startSpeed = 26f; m.startSize = 0.025f; m.maxParticles = 6000; // thin, fast streaks
            m.startColor = new Color(0.75f, 0.8f, 0.88f, 0.55f); m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = I.rain.emission; e.rateOverTime = 0;
            var sh = I.rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40, 1, 40);
            sh.rotation = new Vector3(90, 0, 0); // emit downwards
            var vel = I.rain.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = WindDir.x * 6f; vel.y = 0; vel.z = WindDir.z * 6f;
            var pr = r.gameObject.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 2f; pr.velocityScale = 0.06f;
            pr.sharedMaterial = Art.Particle;
            I.rain.Play();
            // Whitecaps: foam patches blown across the sea around the camera, more and bigger as it roughens.
            var fc = new GameObject("whitecaps").transform;
            fc.SetParent(I.transform, false);
            I.caps = fc.gameObject.AddComponent<ParticleSystem>();
            I.caps.Stop();
            var cm = I.caps.main;
            cm.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3f); cm.startSpeed = 0f; cm.startSize = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            cm.startColor = new Color(0.95f, 0.97f, 1f, 0.75f); cm.simulationSpace = ParticleSystemSimulationSpace.World; cm.maxParticles = 900;
            var ce = I.caps.emission; ce.rateOverTime = 0;
            var cs = I.caps.shape; cs.shapeType = ParticleSystemShapeType.Box; cs.scale = new Vector3(90, 0.1f, 90);
            var cv = I.caps.velocityOverLifetime; cv.enabled = true; cv.space = ParticleSystemSimulationSpace.World; cv.x = WindDir.x * 3f; cv.y = 0; cv.z = WindDir.z * 3f;
            var csz = I.caps.sizeOverLifetime; csz.enabled = true; csz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.3f, 1, 1.4f));
            var cc = I.caps.colorOverLifetime; cc.enabled = true;
            var cg = new Gradient(); cg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0, 1) });
            cc.color = cg;
            var cr = fc.gameObject.GetComponent<ParticleSystemRenderer>(); cr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; cr.sharedMaterial = Art.Particle;
            I.caps.Play();
            I.wind = I.gameObject.AddComponent<AudioSource>();
            I.wind.clip = Sfx.Clip("wind"); I.wind.loop = true; I.wind.spatialBlend = 0; I.wind.volume = 0;
            if (I.wind.clip != null) I.wind.Play();
        }

        void Update()
        {
            var g = PortGame.I;
            float target = g != null ? g.Severity.Value : 0f;
            Level = Mathf.MoveTowards(Level, target, Time.deltaTime / 4f);
            Water.Storm = 1f + Level * Level * Level * 3.5f; // calm stays glassy, rough is choppy, a storm really heaves (x4.5)

            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position + Vector3.up * 14f + WindDir * -6f;
            var e = rain.emission; e.rateOverTime = Rain * 5000f;
            if (cam != null) caps.transform.position = new Vector3(cam.transform.position.x, Water.Level + 0.35f, cam.transform.position.z) + cam.transform.forward * 30f;
            var ce = caps.emission; ce.rateOverTime = Mathf.Clamp01((Level - 0.35f) / 0.65f) * 260f;
            if (wind != null) wind.volume = Mathf.Clamp01(Level * 1.2f - 0.1f) * 0.6f * Settings.Ambience;

            if (Level > 0.8f && Time.time > nextThunder)
            {
                nextThunder = Time.time + Random.Range(6f, 14f);
                Flash = 1;
                Sfx.Play2D("thunder", 0.8f);
            }
            Flash = Mathf.MoveTowards(Flash, 0, Time.deltaTime * 3f);
        }
    }
}
