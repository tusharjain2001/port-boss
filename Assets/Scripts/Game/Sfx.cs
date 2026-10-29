using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Sound bank. A clip in Resources/Audio/<name> overrides the synthesized placeholder,
    // so CC0 sounds can be dropped in without code changes.
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> clips = new();
        const int Rate = 44100;

        public static AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name) ?? Synth(name);
            clips[name] = c;
            return c;
        }

        public static void Play(string name, Vector3 pos, float vol = 1f, float pitch = 1f, bool ambience = false)
        {
            var clip = Clip(name);
            if (clip == null) return;
            var g = new GameObject("sfx_" + name);
            g.transform.position = pos;
            var s = g.AddComponent<AudioSource>();
            s.clip = clip; s.volume = vol * (ambience ? Settings.Ambience : Settings.Sfx); s.pitch = pitch * Random.Range(0.94f, 1.06f);
            s.spatialBlend = 0.85f; s.minDistance = 4f; s.maxDistance = 120f; s.rolloffMode = AudioRolloffMode.Linear;
            s.Play();
            Object.Destroy(g, clip.length / Mathf.Max(0.1f, s.pitch) + 0.1f);
        }

        public static void Play2D(string name, float vol = 1f)
        {
            var clip = Clip(name);
            if (clip == null || Camera.main == null) return;
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, vol * Settings.Sfx);
        }

        static AudioClip Synth(string name)
        {
            float len; System.Func<float, float> f;
            var rng = new System.Random(name.GetHashCode());
            float Noise() => (float)rng.NextDouble() * 2 - 1;
            switch (name)
            {
                case "horn": len = 2.2f; f = t => Env(t, 0.35f, 2.2f, 0.5f) * 0.45f * (Saw(t * 70f) * 0.35f + Saw(t * 70.9f) * 0.35f + Saw(t * 140f) * 0.2f + Saw(t * 141.3f) * 0.1f); break;
                case "kaching": len = 0.7f; f = t => Mathf.Exp(-t * 6) * 0.4f * (Mathf.Sin(t * 2 * Mathf.PI * 2093) + (t > 0.08f ? Mathf.Sin(t * 2 * Mathf.PI * 2637) : 0) + 0.5f * Mathf.Sin(t * 2 * Mathf.PI * 3136)); break;
                case "thud": len = 0.25f; f = t => Mathf.Exp(-t * 25) * (Mathf.Sin(t * 2 * Mathf.PI * (90 - t * 120)) * 0.8f + Noise() * 0.2f); break;
                case "splash": len = 0.9f; f = t => Env(t, 0.01f, 0.9f, 0.7f) * Noise() * 0.5f * (1 - t); break;
                case "whoosh": len = 0.35f; f = t => Mathf.Sin(t / 0.35f * Mathf.PI) * Noise() * 0.25f; break;
                case "jump": len = 0.2f; f = t => Env(t, 0.006f, 0.2f, 0.14f) * (0.3f * Mathf.Sin(2 * Mathf.PI * (240 * t + 700 * t * t)) + 0.08f * Mathf.Sin(4 * Mathf.PI * (240 * t + 700 * t * t)) + 0.05f * Noise() * Mathf.Exp(-t * 40)); break;
                case "land": len = 0.18f; f = t => Mathf.Exp(-t * 26) * (0.55f * Mathf.Sin(2 * Mathf.PI * (105 * t - 120 * t * t)) + 0.2f * Noise() * Mathf.Exp(-t * 60)); break;
                case "place": len = 0.2f; f = t => Mathf.Exp(-t * 45) * (0.5f * Mathf.Sin(2 * Mathf.PI * 180 * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * 410 * t)) + 0.25f * Noise() * Mathf.Exp(-t * 90); break;
                case "grab": len = 0.12f; f = t => Mathf.Exp(-t * 30) * Mathf.Sin(t * 2 * Mathf.PI * 220) * 0.6f; break;
                case "buy": len = 0.5f; f = t => Mathf.Exp(-t * 5) * 0.4f * Mathf.Sin(t * 2 * Mathf.PI * (t < 0.12f ? 784 : 1175)); break;
                case "flies": len = 2f; f = t => 0.15f * Mathf.Sin(t * 2 * Mathf.PI * (210 + 25 * Mathf.Sin(t * 7))) * (0.6f + 0.4f * Mathf.Sin(t * 3.3f)); break;
                case "creak": len = 0.7f; f = t => Env(t, 0.05f, 0.7f, 0.3f) * 0.35f * Saw(t * (180 + 40 * Mathf.Sin(t * 13))) * (0.6f + 0.4f * Noise()); break;
                case "engine": len = 1f; f = t => 0.3f * (Saw(t * 96f) * 0.6f + Saw(t * 192.5f) * 0.25f + Noise() * 0.15f) * (0.85f + 0.15f * Mathf.Sin(t * 2 * Mathf.PI * 8)); break;
                case "siren": len = 3.5f; f = t => Env(t, 0.2f, 3.5f, 0.5f) * 0.35f * Mathf.Sin(2 * Mathf.PI * (520f * t + 90f * Mathf.Sin(t * 2.2f) / 2.2f)); break;
                case "wind": len = 4f; f = t => 0.35f * Noise() * (0.55f + 0.45f * Mathf.Sin(t * 1.6f) * Mathf.Sin(t * 0.7f + 1f)); break;
                case "thunder": len = 3f; f = t => Mathf.Exp(-t * 1.4f) * Mathf.Clamp01(t * 20f) * 0.8f * Noise() * (0.6f + 0.4f * Mathf.Sin(t * 37f)); break;
                case "clank": len = 0.3f; f = t => Mathf.Exp(-t * 16) * 0.3f * (Mathf.Sin(t * 2 * Mathf.PI * 523) * 0.5f + Mathf.Sin(t * 2 * Mathf.PI * 1307) * 0.35f + Mathf.Sin(t * 2 * Mathf.PI * 2093) * 0.2f) + (t < 0.008f ? Noise() * 0.3f : 0); break;
                case "blip": len = 0.3f; f = t => Env(t, 0.01f, 0.3f, 0.12f) * 0.4f * (Saw(t * 233f) * 0.5f + Saw(t * 235.4f) * 0.3f + Saw(t * 466f) * 0.2f); break;
                case "gull": len = 0.6f; f = t => Env(t, 0.05f, 0.6f, 0.3f) * 0.25f * Mathf.Sin(t * 2 * Mathf.PI * (1400 - 900 * t)); break;
                default: return null;
            }
            int n = (int)(len * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1, 1);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Saw(float x) => 2 * (x - Mathf.Floor(x + 0.5f));
        static float Env(float t, float a, float len, float r) => Mathf.Clamp01(t / a) * Mathf.Clamp01((len - t) / r);
    }
}
