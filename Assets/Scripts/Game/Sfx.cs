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
                case "tick": len = 0.04f; f = t => Mathf.Exp(-t * 160) * (0.5f * Mathf.Sin(2 * Mathf.PI * 1850 * t) + 0.2f * Noise()); break;
                case "grab": len = 0.12f; f = t => Mathf.Exp(-t * 30) * Mathf.Sin(t * 2 * Mathf.PI * 220) * 0.6f; break;
                case "buy": len = 0.5f; f = t => Mathf.Exp(-t * 5) * 0.4f * Mathf.Sin(t * 2 * Mathf.PI * (t < 0.12f ? 784 : 1175)); break;
                case "flies": len = 2f; f = t => 0.15f * Mathf.Sin(t * 2 * Mathf.PI * (210 + 25 * Mathf.Sin(t * 7))) * (0.6f + 0.4f * Mathf.Sin(t * 3.3f)); break;
                case "creak": len = 0.7f; f = t => Env(t, 0.05f, 0.7f, 0.3f) * 0.35f * Saw(t * (180 + 40 * Mathf.Sin(t * 13))) * (0.6f + 0.4f * Noise()); break;
                case "engine": len = 1f; f = t => 0.3f * (Saw(t * 96f) * 0.6f + Saw(t * 192.5f) * 0.25f + Noise() * 0.15f) * (0.85f + 0.15f * Mathf.Sin(t * 2 * Mathf.PI * 8)); break;
                case "siren": len = 3.5f; f = t => Env(t, 0.2f, 3.5f, 0.5f) * 0.35f * Mathf.Sin(2 * Mathf.PI * (520f * t + 90f * Mathf.Sin(t * 2.2f) / 2.2f)); break;
                case "rain": return Rain();
                case "tauntHorn": return TauntHorn();
                case "boom": len = 1.6f; f = t => Mathf.Exp(-t * 3.2f) * (0.7f * Mathf.Sin(2 * Mathf.PI * (55 - 20 * t) * t) + 0.35f * Noise() * Mathf.Exp(-t * 9f)) + (t > 0.25f ? 0.12f * Noise() * Mathf.Exp(-(t - 0.25f) * 6f) * (Noise() > 0.6f ? 1 : 0) : 0); break;
                case "plane": len = 2f; f = t => 0.3f * (Saw(t * 88f) * 0.5f + Saw(t * 132f) * 0.25f + Noise() * 0.1f) * (0.8f + 0.2f * Mathf.Sin(t * 2 * Mathf.PI * 22f)); break;
                case "wind": return Wind();
                case "thunder": return Thunder();
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

        static float[] Normalize(float[] d, float peak)
        {
            float m = 1e-6f; foreach (var x in d) m = Mathf.Max(m, Mathf.Abs(x));
            for (int i = 0; i < d.Length; i++) d[i] *= peak / m;
            return d;
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Loops: render a little extra and fold it back over the start, so the seam can't be heard.
        static float[] Seamless(float[] d, int fade)
        {
            int n = d.Length - fade;
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = d[i];
            for (int i = 0; i < fade; i++) { float k = i / (float)fade; o[i] = d[i] * k + d[n + i] * (1 - k); }
            return o;
        }

        // Rain: a soft filtered hiss with a slow swell, and thousands of single drops (short bright ticks, some lower plips).
        static AudioClip Rain()
        {
            var rng = new System.Random(77);
            float Noise() => (float)rng.NextDouble() * 2 - 1;
            int fade = Rate / 2, n = Rate * 7 + fade;
            var d = new float[n];
            float lp = 0, lp2 = 0, hp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, x = Noise();
                lp += (x - lp) * 0.35f; lp2 += (lp - lp2) * 0.5f; // ~3 kHz roll-off
                hp += (lp2 - hp) * 0.02f;                            // drop the rumble
                d[i] = (lp2 - hp) * 0.22f * (0.85f + 0.15f * Mathf.Sin(t * 0.9f) * Mathf.Sin(t * 0.37f + 2f));
            }
            int drops = 7 * 260;
            for (int k = 0; k < drops; k++)
            {
                int at = rng.Next(n);
                bool plip = rng.NextDouble() < 0.12;
                float fq = plip ? 500f + (float)rng.NextDouble() * 700f : 2200f + (float)rng.NextDouble() * 4200f;
                float decay = plip ? 90f : 260f + (float)rng.NextDouble() * 400f, amp = (plip ? 0.07f : 0.025f) + (float)rng.NextDouble() * (plip ? 0.06f : 0.05f);
                for (int i = 0; i < Rate / 20 && at + i < n; i++)
                {
                    float t = i / (float)Rate;
                    d[at + i] += Mathf.Sin(2 * Mathf.PI * fq * t * (plip ? 1f - t * 4f : 1f)) * Mathf.Exp(-t * decay) * amp;
                }
            }
            return Make("rain", Normalize(Seamless(d, fade), 0.6f));
        }

        // Marlowe's horn: the first bars of a cheeky tune (G G G C E, G G G C E) on a two-tone air horn.
        static AudioClip TauntHorn()
        {
            var notes = new (float f, float len)[] { (392, 0.13f), (392, 0.13f), (392, 0.13f), (523, 0.3f), (659, 0.42f), (0, 0.12f), (392, 0.13f), (392, 0.13f), (392, 0.13f), (523, 0.3f), (659, 0.6f) };
            float total = 0; foreach (var nn in notes) total += nn.len + 0.03f;
            var d = new float[(int)(total * Rate)];
            int at = 0;
            foreach (var (fq, len) in notes)
            {
                int n = (int)(len * Rate);
                for (int i = 0; i < n && at + i < d.Length; i++)
                {
                    float t = i / (float)Rate, env = Mathf.Clamp01(t / 0.015f) * Mathf.Clamp01((len - t) / 0.04f);
                    d[at + i] = fq <= 0 ? 0 : env * 0.32f * (Saw(t * fq) * 0.6f + Saw(t * fq * 1.26f) * 0.3f + Saw(t * fq * 2.01f) * 0.1f);
                }
                at += n + (int)(0.03f * Rate);
            }
            return Make("tauntHorn", d);
        }

        // Wind: a whoosh (noise band-passed around a few hundred Hz) that brightens and swells with each gust,
        // over a low rumble.
        static AudioClip Wind()
        {
            var rng = new System.Random(31);
            float Noise() => (float)rng.NextDouble() * 2 - 1;
            int fade = Rate, n = Rate * 9 + fade;
            var d = new float[n];
            float br = 0, lp1 = 0, lp2 = 0, hp = 0, lo = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, x = Noise();
                float gust = 0.5f + 0.32f * Mathf.Sin(t * 0.7f) + 0.18f * Mathf.Sin(t * 1.9f + 1f);
                lp1 += (x - lp1) * (0.03f + 0.07f * gust); lp2 += (lp1 - lp2) * 0.4f; // ~250-700 Hz, up with the gust
                hp += (lp2 - hp) * 0.012f;
                br = br * 0.995f + x * 0.06f; lo += (br - lo) * 0.03f;
                d[i] = ((lp2 - hp) * (0.4f + 0.6f * gust) * 4f + lo * 0.6f) * gust;
            }
            return Make("wind", Normalize(Seamless(d, fade), 0.55f));
        }

        // Thunder: a sharp crack, then a long low rumble that rolls in waves.
        static AudioClip Thunder()
        {
            var rng = new System.Random(13);
            float Noise() => (float)rng.NextDouble() * 2 - 1;
            int n = (int)(Rate * 4.5f);
            var d = new float[n];
            float lp = 0, lp2 = 0, crack = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, x = Noise();
                lp += (x - lp) * 0.012f; lp2 += (lp - lp2) * 0.03f;
                crack += (x - crack) * 0.4f;
                float roll = Mathf.Clamp01(t * 6f) * Mathf.Exp(-t * 0.8f) * (0.6f + 0.4f * Mathf.Sin(t * 5.5f) * Mathf.Sin(t * 2.1f + 0.5f));
                d[i] = Mathf.Clamp(lp2 * 9f * roll + crack * 0.5f * Mathf.Exp(-t * 14f), -1, 1);
            }
            return Make("thunder", Normalize(d, 0.85f));
        }

        static float Saw(float x) => 2 * (x - Mathf.Floor(x + 0.5f));
        static float Env(float t, float a, float len, float r) => Mathf.Clamp01(t / a) * Mathf.Clamp01((len - t) / r);
    }
}
