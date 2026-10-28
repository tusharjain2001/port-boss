using UnityEngine;

namespace PortBoss
{
    // Local storm presentation on every peer, driven by the host's PortGame.StormPhase/StormLevel:
    // rain around the camera, wind + siren + thunder, lightning flashes, bigger waves (Water.Storm).
    public class Storm : MonoBehaviour
    {
        public static Storm I;
        public static float Level;     // 0 calm .. 1 full storm (smoothed, every peer)
        public static float Flash;     // lightning flash 0..1
        public static readonly Vector3 WindDir = new Vector3(1f, 0, 0.25f).normalized; // across the pier: loose crates go over the side

        ParticleSystem rain;
        AudioSource wind;
        float nextThunder;
        int lastPhase;

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
            int phase = g != null ? g.StormPhase.Value : 0;
            if (phase != lastPhase)
            {
                if (phase == 1) { Sfx.Play2D("siren", 1f); }
                if (phase == 2) { Sfx.Play2D("thunder", 1f); Flash = 1; }
                lastPhase = phase;
            }
            float target = phase == 2 ? 1f : phase == 1 ? 0.25f : 0f;
            Level = Mathf.MoveTowards(Level, target, Time.deltaTime / 8f);
            Water.Storm = 1f + Level * 1.4f;

            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position + Vector3.up * 14f + WindDir * -6f;
            var e = rain.emission; e.rateOverTime = Level * 2600f;
            if (wind != null) wind.volume = Level * 0.6f * Settings.Ambience;

            if (Level > 0.7f && Time.time > nextThunder)
            {
                nextThunder = Time.time + Random.Range(6f, 14f);
                Flash = 1;
                Sfx.Play2D("thunder", 0.8f);
            }
            Flash = Mathf.MoveTowards(Flash, 0, Time.deltaTime * 3f);
        }
    }
}
