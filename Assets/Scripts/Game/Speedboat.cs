using UnityEngine;

namespace PortBoss
{
    // The rival's speedboat: every few minutes it roars over from the rival port, does doughnuts off the pier
    // and leaves. Its path is a pure function of network time, so every peer draws the same boat with no sync;
    // the host applies the wake (docked boat rocks harder, loose crates slide, floating crates bob).
    public class Speedboat : MonoBehaviour
    {
        public const float Period = 170f, Offset = 70f, Active = 30f;
        static readonly Vector3 Home = new(-20, 0, 215), Spin = new(20, 0, 42);
        const float SpinRadius = 13f;

        public static Speedboat I;
        public static bool IsActive { get; private set; }
        static Vector3 pos;

        ParticleSystem wake;
        AudioSource engine;

        public static void Create()
        {
            var g = new GameObject("RivalSpeedboat");
            I = g.AddComponent<Speedboat>();
            var t = g.transform;
            Art.Box(t, new Vector3(0, 0.25f, 0), new Vector3(1.9f, 0.7f, 5f), Art.Red, false, "hull", 0.45f);
            Art.Box(t, new Vector3(0, 0.62f, 0), new Vector3(1.95f, 0.06f, 5.05f), Art.Cream, false, "stripe");
            var bow = Art.Box(t, new Vector3(0, 0.25f, 2.7f), new Vector3(1.34f, 0.69f, 1.34f), Art.Red, false, "bow", 0.45f);
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Art.Box(t, new Vector3(0, 0.95f, 0.4f), new Vector3(1.4f, 0.12f, 1.1f), Art.Hex("#24323d"), false, "windscreen", 0.9f).transform.localRotation = Quaternion.Euler(-30, 0, 0);
            Art.Box(t, new Vector3(0, 0.75f, -2.3f), new Vector3(0.5f, 0.7f, 0.4f), Art.Hex("#2b2b2b"), false, "outboard");
            // Marlowe: the rival, in a captain's hat, standing at the wheel.
            Art.Prim(PrimitiveType.Capsule, t, new Vector3(0, 1.2f, -0.6f), Quaternion.identity, new Vector3(0.55f, 0.55f, 0.5f), Art.Cream, false, "marlowe");
            Art.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.95f, -0.6f), Quaternion.identity, Vector3.one * 0.34f, Art.Hex("#d6a27f"), false, "marlowe_head");
            Art.Box(t, new Vector3(0, 2.13f, -0.6f), new Vector3(0.4f, 0.12f, 0.42f), Art.Hex("#f2f2ee"), false, "marlowe_hat");

            var w = new GameObject("wake").transform;
            w.SetParent(t, false); w.localPosition = new Vector3(0, -0.1f, -2.6f);
            I.wake = w.gameObject.AddComponent<ParticleSystem>();
            I.wake.Stop();
            var m = I.wake.main;
            m.startLifetime = 2.5f; m.startSpeed = 0.6f; m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            m.startColor = new Color(0.96f, 0.97f, 0.95f, 0.85f); m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 400;
            var e = I.wake.emission; e.rateOverTime = 70;
            var sh = I.wake.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 0.05f, 0.4f);
            var sz = I.wake.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            var col = I.wake.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            w.gameObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = Art.Particle;

            I.engine = g.AddComponent<AudioSource>();
            I.engine.clip = Sfx.Clip("engine"); I.engine.loop = true; I.engine.spatialBlend = 1; I.engine.minDistance = 8; I.engine.maxDistance = 140;
            I.engine.rolloffMode = AudioRolloffMode.Linear; I.engine.volume = 0;
            if (I.engine.clip != null) I.engine.Play();
        }

        // Where the boat is at time t (and which way it faces). Inactive = parked out of sight at home.
        static bool Pose(float t, out Vector3 p, out Vector3 fwd)
        {
            float c = Mathf.Repeat(t - Offset, Period);
            fwd = Vector3.back;
            if (t < Offset || c > Active) { p = Home + Vector3.down * 5; return false; }
            var entry = Spin + new Vector3(0, 0, SpinRadius);
            if (c < 8f) { float u = Mathf.SmoothStep(0, 1, c / 8f); p = Vector3.Lerp(Home, entry, u); fwd = entry - Home; }
            else if (c < 22f)
            {
                // Two tight doughnuts.
                float a = (c - 8f) / 14f * Mathf.PI * 4f + Mathf.PI / 2;
                p = Spin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * SpinRadius;
                fwd = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
            }
            else { float u = Mathf.SmoothStep(0, 1, (c - 22f) / 8f); p = Vector3.Lerp(entry, Home, u); fwd = Home - entry; }
            return true;
        }

        // 0..1 wake strength felt at a point (host uses it for physics).
        public static float WakeAt(Vector3 at)
        {
            if (!IsActive) return 0;
            var d = at - pos; d.y = 0;
            return Mathf.Clamp01(1.2f - d.magnitude / 22f);
        }

        void Update()
        {
            float t = Water.T;
            bool was = IsActive;
            IsActive = Pose(t, out var p, out var fwd);
            var y = Water.Height(p.x, p.z, t);
            pos = new Vector3(p.x, y, p.z);
            fwd.y = 0;
            var rot = Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.back);
            // Nose up at speed, lean into the turns.
            float cyc = Mathf.Repeat(t - Offset, Period);
            float bank = cyc > 8 && cyc < 22 ? 18f : 0f;
            transform.SetPositionAndRotation(pos + Vector3.up * 0.1f, rot * Quaternion.Euler(-6f, 0, bank));
            if (IsActive && !wake.isPlaying) wake.Play();
            if (!IsActive && wake.isPlaying) wake.Stop();
            if (engine != null) engine.volume = Mathf.MoveTowards(engine.volume, IsActive ? 0.55f * Settings.Sfx : 0, Time.deltaTime);
            if (IsActive && !was) Arrived?.Invoke();
        }

        public static event System.Action Arrived;
    }
}
