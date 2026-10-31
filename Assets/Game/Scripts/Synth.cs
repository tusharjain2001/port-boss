using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Fishing sounds, synthesised once at start (free, no samples to license). Each is a few lines of maths; every
    // number is a knob. Recorded CC0 sounds (waves, splash, gulls, thud) come from Resources/Audio.
    public static class Synth
    {
        const int SR = 44100;
        static readonly System.Random rng = new(7);
        static float N() => (float)(rng.NextDouble() * 2 - 1);

        static AudioClip Make(string name, float secs, System.Func<float, float> f, bool loop = false)
        {
            int n = Mathf.CeilToInt(secs * SR);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1, 1);
            if (!loop) { int fade = Mathf.Min(n, 200); for (int i = 0; i < fade; i++) d[n - 1 - i] *= i / (float)fade; }
            var c = AudioClip.Create(name, n, 1, SR, false); c.SetData(d, 0);
            return c;
        }

        static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Exp(-(t - a) / d);
        static float Sin(float hz, float t) => Mathf.Sin(2 * Mathf.PI * hz * t);

        // one-pole low-passed noise, state per clip
        static System.Func<float, float> LP(float cut)
        {
            float y = 0, a = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
            return _ => y += a * (N() - y);
        }

        static readonly Dictionary<string, AudioClip> cache = new();
        static AudioClip C(string k, System.Func<AudioClip> make) { if (!cache.TryGetValue(k, out var c) || c == null) cache[k] = c = make(); return c; }

        public static AudioClip Engine => C("engine", () =>
        {
            var lp = LP(300);
            return Make("engine", 2f, t =>
            {
                float f0 = 34; // putt-putt of a small two-stroke
                float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(2 * Mathf.PI * f0 * t)), 6);
                return 0.35f * pulse + 0.12f * Sin(f0 * 2, t) + 0.08f * lp(t);
            }, true);
        });

        public static AudioClip Plip => C("plip", () => { var lp = LP(2500); return Make("plip", 0.35f, t => 0.6f * Sin(900 - 1400 * t, t) * Env(t, 0.003f, 0.05f) + 0.25f * lp(t) * Env(t, 0.001f, 0.04f)); });
        public static AudioClip Plunk => C("plunk", () => { var lp = LP(1500); return Make("plunk", 0.6f, t => 0.7f * Sin(420 - 380 * t, t) * Env(t, 0.004f, 0.11f) + 0.35f * lp(t) * Env(t, 0.002f, 0.12f)); });
        public static AudioClip Bloop => C("bloop", () =>
        {
            var lp = LP(1100);
            return Make("bloop", 0.9f, t => 0.9f * Sin(Mathf.Lerp(230, 70, Mathf.Sqrt(t / 0.35f)), t) * Env(t, 0.006f, 0.16f) + 0.5f * lp(t) * Env(t, 0.01f, 0.25f));
        });
        public static AudioClip Tik => C("tik", () => { var lp = LP(5000); return Make("tik", 0.12f, t => 0.35f * (Sin(1150, t) * 0.6f + lp(t) * 0.5f) * Env(t, 0.0008f, 0.012f)); });
        public static AudioClip[] Ding => new[] { Bell("dingC", 523.25f), Bell("dingE", 659.25f), Bell("dingG", 783.99f) };
        static AudioClip Bell(string k, float hz) => C(k, () => Make(k, 1.1f, t => (0.55f * Sin(hz, t) + 0.22f * Sin(hz * 2.01f, t) + 0.1f * Sin(hz * 3.02f, t)) * Env(t, 0.002f, 0.32f)));
        public static AudioClip Fanfare => C("fanfare", () => Make("fanfare", 2.0f, t =>
        {
            float s = 0; float[] f = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < 4; i++) { float tt = t - i * 0.06f; if (tt > 0) s += (0.3f * Sin(f[i], tt) + 0.1f * Sin(f[i] * 2, tt)) * Env(tt, 0.01f, 0.7f); }
            return s * 0.7f;
        }));
        public static AudioClip Miss => C("miss", () => { var lp = LP(700); return Make("miss", 0.45f, t => (0.45f * Sin(180 - 120 * t, t) + 0.3f * lp(t)) * Env(t, 0.005f, 0.09f)); });
        public static AudioClip Whoosh => C("whoosh", () => { var lp = LP(1800); return Make("whoosh", 0.45f, t => 0.8f * lp(t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.45f))); });
        public static AudioClip Zing => C("zing", () => { var lp = LP(6000); return Make("zing", 0.9f, t => (0.25f * Sin(2600 - 1800 * t, t) + 0.25f * lp(t)) * Env(t, 0.01f, 0.35f)); });
        public static AudioClip Ratchet => C("ratchet", () => { var lp = LP(6500); return Make("ratchet", 0.5f, t => 0.6f * lp(t) * Mathf.Exp(-((t * 28f) % 1f) * 30f)); });
        public static AudioClip Reel => C("reel", () => { var lp = LP(7000); return Make("reel", 0.25f, t => 0.45f * lp(t) * Mathf.Exp(-((t * 40f) % 1f) * 25f), true); });
        public static AudioClip Creak => C("creak", () => Make("creak", 1f, t => 0.18f * (Mathf.Repeat(t * 110, 1) * 2 - 1) * (0.6f + 0.4f * Sin(7, t)) + 0.08f * Sin(220, t), true));
        public static AudioClip Ting => C("ting", () => Make("ting", 0.6f, t => (0.35f * Sin(1568, t) + 0.12f * Sin(3136, t)) * Env(t, 0.002f, 0.12f)));
        public static AudioClip Slap => C("slap", () => { var lp = LP(2200); return Make("slap", 0.3f, t => 0.9f * lp(t) * Env(t, 0.001f, 0.035f) + 0.3f * Sin(140, t) * Env(t, 0.001f, 0.05f)); });
        public static AudioClip Thup => C("thup", () => Make("thup", 0.25f, t => 0.6f * Sin(160 - 220 * t, t) * Env(t, 0.003f, 0.05f)));
        public static AudioClip Pluck => C("pluck", () => Make("pluck", 0.8f, t => (0.5f * Sin(392, t) + 0.2f * Sin(784, t)) * Env(t, 0.002f, 0.18f)));
        public static AudioClip Chime => C("chime", () => Make("chime", 1.4f, t => (0.35f * Sin(784, t) + 0.25f * Sin(1175, t - 0.08f) * (t > 0.08f ? 1 : 0)) * Env(t, 0.003f, 0.4f)));
        public static AudioClip Shimmer => C("shimmer", () => Make("shimmer", 1.8f, t => { float s = 0; for (int i = 0; i < 5; i++) { float tt = t - i * 0.07f; if (tt > 0) s += 0.16f * Sin(1046.5f * Mathf.Pow(1.1225f, i * 2), tt) * Env(tt, 0.003f, 0.35f); } return s; }));
        public static AudioClip Brass => C("brass", () => Make("brass", 2.4f, t =>
        {
            float a = Mathf.Clamp01(t / 0.25f) * Mathf.Exp(-Mathf.Max(0, t - 0.9f) / 0.6f), s = 0;
            foreach (var f in new[] { 261.6f, 329.6f, 392f, 523.2f }) for (int h = 1; h <= 5; h++) s += Sin(f * h, t) / (h * 1.6f);
            return 0.09f * s * a;
        }));
        public static AudioClip Snap => C("snap", () => { var lp = LP(3000); return Make("snap", 0.7f, t => (0.8f * lp(t) * Env(t, 0.001f, 0.02f) + 0.4f * Sin(95, t) * Env(t, 0.002f, 0.25f) * Sin(7, t))); });
        public static AudioClip Splash => C("splash", () => { var lp = LP(2800); return Make("splashS", 0.8f, t => 0.7f * lp(t) * Env(t, 0.01f, 0.2f)); });
    }

    // Pooled one-shots in the world (3D) or on the listener (2D).
    public static class Snd
    {
        static readonly List<AudioSource> pool = new();
        public static float Master = 1f;

        public static AudioSource Play(AudioClip c, Vector3 at, float vol = 1, float pitch = 1, float blend = 0.8f)
        {
            if (c == null) return null;
            AudioSource s = pool.Find(a => a != null && !a.isPlaying);
            if (s == null)
            {
                var g = new GameObject("snd"); Object.DontDestroyOnLoad(g);
                s = g.AddComponent<AudioSource>(); s.playOnAwake = false; s.minDistance = 3; s.maxDistance = 80; s.rolloffMode = AudioRolloffMode.Linear;
                pool.Add(s);
            }
            s.transform.position = at; s.clip = c; s.volume = vol * Master; s.pitch = pitch; s.spatialBlend = blend; s.Play();
            return s;
        }

        public static AudioSource Play2D(AudioClip c, float vol = 1, float pitch = 1) => Play(c, Camera.main != null ? Camera.main.transform.position : Vector3.zero, vol, pitch, 0);

        public static void Rumble(float low, float high, float secs)
        {
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(low, high);
            rumbleUntil = Time.unscaledTime + secs;
        }
        static float rumbleUntil;
        public static void TickRumble() { if (rumbleUntil > 0 && Time.unscaledTime > rumbleUntil) { rumbleUntil = 0; UnityEngine.InputSystem.Gamepad.current?.SetMotorSpeeds(0, 0); } }
    }
}
