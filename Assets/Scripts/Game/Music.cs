using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PortBoss
{
    // Music slots (menu/day/rush/night/storm) from StreamingAssets/Music/<slot>.mp3|ogg|wav, crossfaded.
    // A missing file just means that slot is silent. Files are Manan's Suno takes; nothing is fetched from elsewhere.
    public class Music : MonoBehaviour
    {
        public static readonly string[] Slots = { "menu", "day", "rush", "night", "storm" };
        static readonly Dictionary<string, AudioClip> clips = new();
        public static string Current { get; private set; } = "";

        AudioSource a, b; // b fades out, a fades in
        string want = "";
        float fade = 1f;

        public static Music Create()
        {
            var m = new GameObject("Music").AddComponent<Music>();
            DontDestroyOnLoad(m.gameObject);
            m.a = m.Src(); m.b = m.Src();
            m.StartCoroutine(m.LoadAll());
            return m;
        }

        AudioSource Src()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.spatialBlend = 0; s.playOnAwake = false; s.volume = 0;
            return s;
        }

        IEnumerator LoadAll()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, "Music");
            foreach (var slot in Slots)
                foreach (var (ext, type) in new[] { (".mp3", AudioType.MPEG), (".ogg", AudioType.OGGVORBIS), (".wav", AudioType.WAV) })
                {
                    var path = Path.Combine(dir, slot + ext);
                    if (!File.Exists(path)) continue;
                    using var req = UnityWebRequestMultimedia.GetAudioClip("file:///" + path.Replace(Path.DirectorySeparatorChar, '/'), type);
                    ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) { clips[slot] = DownloadHandlerAudioClip.GetContent(req); Debug.Log("[PB] music loaded: " + slot); }
                    else Debug.LogWarning($"[PB] music {slot}: {req.error}");
                    break;
                }
        }

        public void Play(string slot)
        {
            if (slot == want) return;
            want = slot;
            clips.TryGetValue(slot, out var clip);
            if (clip == a.clip) return;
            (a, b) = (b, a);
            a.clip = clip; a.volume = 0;
            if (clip != null) a.Play();
            fade = 0;
            Current = clip != null ? slot : "";
        }

        void Update()
        {
            // A clip that finished loading after its slot was requested starts now.
            if (want != "" && a.clip == null && clips.TryGetValue(want, out var late)) { var w = want; want = ""; Play(w); }
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 1.8f);
            float v = Settings.Music;
            a.volume = fade * v;
            b.volume = (1 - fade) * v;
            if (fade >= 1f && b.isPlaying) b.Stop();
        }
    }
}
