using UnityEngine;

namespace PortBoss
{
    // One-shot particle effects (local, cosmetic).
    public static class Fx
    {
        public static void Splash(Vector3 p, float size)
        {
            var g = new GameObject("splash");
            g.transform.position = p;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop();
            var m = ps.main;
            m.duration = 0.2f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * size, 5f * size);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f * size, 0.22f * size);
            m.startColor = new Color(0.92f, 0.96f, 1f, 0.9f);
            m.gravityModifier = 1.6f;
            m.maxParticles = 60;
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(28 * size)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 28; sh.radius = 0.3f * size;
            sh.rotation = new Vector3(-90, 0, 0);
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = Art.Particle;
            ps.Play();
            Object.Destroy(g, 2f);
        }

        // A sheet of spray thrown one way (Marlowe's power turn): a wide fan of drops up and out along dir.
        public static void Spray(Vector3 p, Vector3 dir, float size)
        {
            var g = new GameObject("spray");
            g.transform.SetPositionAndRotation(p, Quaternion.LookRotation(dir + Vector3.up * 0.55f));
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop();
            var m = ps.main;
            m.duration = 0.35f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f * size, 9f * size);
            m.startSize = new ParticleSystem.MinMaxCurve(0.15f * size, 0.45f * size);
            m.startColor = new Color(0.93f, 0.97f, 1f, 0.9f);
            m.gravityModifier = 1.4f; m.maxParticles = 600; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(140 * size)), new ParticleSystem.Burst(0.12f, (short)(80 * size)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 38; sh.radius = 1.2f * size;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.95f, 0), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = Art.Particle;
            ps.Play();
            Object.Destroy(g, 3f);
        }
    }
}
