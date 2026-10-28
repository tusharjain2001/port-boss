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

        ParticleSystem rain;
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
            m.startLifetime = 1.1f; m.startSpeed = 22f; m.startSize = 0.05f; m.maxParticles = 3000;
            m.startColor = new Color(0.75f, 0.8f, 0.88f, 0.55f); m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = I.rain.emission; e.rateOverTime = 0;
            var sh = I.rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40, 1, 40);
            sh.rotation = new Vector3(90, 0, 0); // emit downwards
            var vel = I.rain.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = WindDir.x * 6f; vel.y = 0; vel.z = WindDir.z * 6f;
            var pr = r.gameObject.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 4f; pr.velocityScale = 0.03f;
            pr.sharedMaterial = Art.Particle;
            I.rain.Play();
            I.wind = I.gameObject.AddComponent<AudioSource>();
            I.wind.clip = Sfx.Clip("wind"); I.wind.loop = true; I.wind.spatialBlend = 0; I.wind.volume = 0;
            if (I.wind.clip != null) I.wind.Play();
        }

        void Update()
        {
            var g = PortGame.I;
            float target = g != null ? g.Severity.Value : 0f;
            Level = Mathf.MoveTowards(Level, target, Time.deltaTime / 4f);
            Water.Storm = 1f + Level * Level * 1.8f; // calm stays glassy, a storm really heaves

            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position + Vector3.up * 14f + WindDir * -6f;
            var e = rain.emission; e.rateOverTime = Rain * 2600f;
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
