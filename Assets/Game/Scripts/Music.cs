using UnityEngine;

namespace PortBoss.M1
{
    // M7 step 8: the music player, with empty slots (Tushar 8 Nov: "leave a music slot"; Manan supplies the tracks, no
    // Suno downloads here: HANDOVER rule 4). A track per mood, chosen from the clock and the weather, crossfaded over 3 s:
    //   Resources/Music/morning, day, evening, night, storm (any Unity audio format: .ogg is best)
    //   Resources/Music/mari   (Mari's radio: from her boat, 3D, 09:00-22:00 while she's moored)
    // A missing file is a silent slot (logged once). The Music slider (Settings.Music) sets the level. AI-made music must
    // be disclosed on the Steam page (rule 4): docs/MUSIC-SLOTS.md says how to drop tracks in.
    public class Music : MonoBehaviour
    {
        public static Music I;
        public static readonly string[] Moods = { "morning", "day", "evening", "night", "storm" };
        public const float Level = 0.5f, Fade = 3f;
        AudioSource a, b; AudioSource radio; string now; float mix = 1; bool aOn = true;
        readonly System.Collections.Generic.Dictionary<string, AudioClip> clips = new();
        public string Mood => now;                                      // (tests)
        public bool Playing(string mood) => now == mood && Clip(mood) != null;

        public static void Create()
        {
            var g = new GameObject("Music"); I = g.AddComponent<Music>();
            AudioSource Src() { var s = g.AddComponent<AudioSource>(); s.loop = true; s.spatialBlend = 0; s.playOnAwake = false; s.volume = 0; return s; }
            I.a = Src(); I.b = Src();
            int have = 0; foreach (var m in Moods) if (I.Clip(m) != null) have++;
            Debug.Log($"[PB] music: {have} of {Moods.Length} mood tracks in Resources/Music ({(have == 0 ? "all slots empty: silent" : "the rest silent")}), Mari's radio {(I.Clip("mari") != null ? "in" : "empty")}");
        }

        AudioClip Clip(string id)
        {
            if (!clips.TryGetValue(id, out var c)) clips[id] = c = Resources.Load<AudioClip>("Music/" + id);
            return c;
        }

        // the mood now: a storm wins; else the time of day (morning 05-10, day 10-18, evening 18-21, night 21-05)
        public static string MoodAt(double m, Wx w)
        {
            if (w == Wx.Storm) return "storm";
            double h = DayTime.OfDay(m) / 60.0;
            return h >= 5 && h < 10 ? "morning" : h >= 10 && h < 18 ? "day" : h >= 18 && h < 21 ? "evening" : "night";
        }

        void Update()
        {
            if (Sky.W == null) return;
            var (d, blk) = Weather.BlockAt(Sky.Minutes);
            string want = MoodAt(Sky.Minutes, Sky.W.Block(d, blk));
            if (want != now)
            {
                now = want; var next = aOn ? b : a; aOn = !aOn;                // the other source takes the new mood
                next.clip = Clip(want); if (next.clip != null) next.Play(); else next.Stop();
                mix = 0; Debug.Log($"[PB] music: {want}{(next.clip == null ? " (empty slot)" : "")}");
            }
            mix = Mathf.MoveTowards(mix, 1, Time.unscaledDeltaTime / Fade);
            float lv = Level * Settings.Music; var on = aOn ? a : b; var off = aOn ? b : a;
            on.volume = lv * mix; off.volume = lv * (1 - mix); if (off.volume <= 0.001f && off.isPlaying) off.Stop();
            Radio();
        }

        // Mari's radio, from her boat while she's here and open
        void Radio()
        {
            var c = Firm.C; var st = Firm.I?.Stalls.Find(s => s.K == Firm.Kind.Mari);
            bool on = c != null && st != null && c.MariHere(Sky.Minutes) && st.IsOpen(Sky.Minutes) && Clip("mari") != null;
            if (on && radio == null)
            {
                var g = new GameObject("Mari's radio"); g.transform.position = st.At.position + Vector3.up * 1.5f;
                radio = g.AddComponent<AudioSource>(); radio.clip = Clip("mari"); radio.loop = true; radio.spatialBlend = 1; radio.minDistance = 4; radio.maxDistance = 30; radio.rolloffMode = AudioRolloffMode.Linear; radio.Play();
            }
            if (radio != null) { radio.volume = on ? 0.6f * Settings.Music : 0; if (!on && radio.isPlaying) radio.Stop(); else if (on && !radio.isPlaying) radio.Play(); }
        }
    }
}
