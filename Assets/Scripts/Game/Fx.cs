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
    }
}
