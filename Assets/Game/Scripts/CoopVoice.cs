using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M2 step 5: proximity voice (gdd systems §3.6). Steam records and compresses the mic (Facepunch: VoiceRecord,
    // ReadVoiceDataBytes; it sends nothing in silence, so an open mic is voice-activated), the bytes go to the host on an
    // unreliable channel and on to everyone else ("pb.voice"), and each crewmate's voice is decoded (DecompressVoice) and
    // played from their head: full to 3 m, fading to silence at 35 m, muffled when their head is under the sea.
    // Open mic by default; push-to-talk (V) and off are Settings.Voice (the settings screen comes later: Manan 2 Nov).
    // -pb-voicetest: a made-up voice (never the mic) through the same path, μ-law at 8 kHz, so the tests need no Steam.
    public partial class Coop
    {
        public static bool SteamVoice;                           // Steam is running here: the mic can be heard
        public static float MyLevel;                             // my voice going out (0..1), for the HUD's mic chip
        public static bool Talking => Time.time - lastSent < 0.3f;
        static float lastSent = -9;
        ushort voiceSeq; float testT; bool testVoice; public static bool TestTalk; // (tests) the made-up voice speaks
        public int VoiceSent, VoiceBytes;                       // (tests)

        void StartVoice()
        {
            testVoice = Array.IndexOf(Environment.GetCommandLineArgs(), "-pb-voicetest") >= 0;
            if (testVoice) { Log("voice: the test voice (not the mic)"); return; }
            SteamUp(); SteamVoice = SteamOk;
            Log(SteamOk ? "voice: Steam is running, the mic is " + Settings.Voice : "voice: Steam isn't running, so no mic");
        }
        void StopVoice()
        {
            if (!SteamVoice) return;
            try { Steamworks.SteamUser.VoiceRecord = false; } catch { } // (Steam itself stays up: the lobby; Game shuts it at quit)
            SteamVoice = false;
        }

        // every frame: what my mic (or the test voice) has, out to the others
        void TickVoice()
        {
            bool on = Settings.Voice == Settings.VoiceMode.Open || Settings.Voice == Settings.VoiceMode.PushToTalk && Ctl.PushToTalk;
            if (SteamVoice)
            {
                Steamworks.SteamUser.VoiceRecord = on;
                if (on && Steamworks.SteamUser.HasVoiceData) { var b = Steamworks.SteamUser.ReadVoiceDataBytes(); if (b != null && b.Length > 0) SendVoice(0, b, 0.3f); }
            }
            else if (testVoice)
            {
                // 50 ms of the made-up voice, 20 times a second, while it's "speaking" (gaps between words aren't sent)
                testT += Time.unscaledDeltaTime;
                while (testT >= 0.05f)
                {
                    testT -= 0.05f;
                    if (!on || !TestTalk) continue;
                    var pcm = TestVoice.Next(VoiceOut.TestRate / 20, out float level);
                    if (level > 0.02f) SendVoice(1, Mulaw.Encode(pcm), level);
                }
            }
            MyLevel = Mathf.MoveTowards(MyLevel, Talking ? MyLevel : 0, Time.unscaledDeltaTime * 2);
        }

        // Over ~1.27 KB NGO won't send one unsplit message (it throws: code audit 3 Nov). Only a burst after a long hitch gets
        // that big, and a Steam voice blob can't be cut up and decoded in pieces, so it's dropped (a moment of silence).
        const int VoiceMax = 1000; public static int VoiceDropped;
        public void TestBurst() => SendVoice(1, new byte[1500], 0.5f); // (cooptest: a burst too big for one message)
        void SendVoice(byte codec, byte[] data, float level)
        {
            if (data.Length > VoiceMax) { if (VoiceDropped++ == 0) Debug.Log($"[PB] coop: voice burst of {data.Length} bytes dropped (over {VoiceMax})"); return; }
            lastSent = Time.time; MyLevel = Mathf.Max(MyLevel * 0.8f, level);
            voiceSeq++; VoiceSent++; VoiceBytes += data.Length;
            var seq = voiceSeq;
            if (nm.IsServer) RelayVoice(nm.LocalClientId, seq, codec, data);
            else Send("pb.voice", w => WriteVoice(ref w, seq, codec, data), NetworkDelivery.Unreliable);
        }
        static void WriteVoice(ref FastBufferWriter w, ushort seq, byte codec, byte[] data) { w.WriteValueSafe(seq); w.WriteValueSafe(codec); w.WriteValueSafe((ushort)data.Length); w.WriteBytesSafe(data); }

        void RelayVoice(ulong who, ushort seq, byte codec, byte[] data)
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId && id != who) SendTo(id, "pb.voice", w => { w.WriteValueSafe(who); WriteVoice(ref w, seq, codec, data); }, NetworkDelivery.Unreliable);
        }

        void OnVoice(ulong from, FastBufferReader r)
        {
            ulong who = from;
            if (!nm.IsServer) r.ReadValueSafe(out who);
            r.ReadValueSafe(out ushort seq); r.ReadValueSafe(out byte codec); r.ReadValueSafe(out ushort n);
            var data = new byte[n]; r.ReadBytesSafe(ref data, n);
            Deliver(() =>
            {
                if (nm.IsServer) RelayVoice(who, seq, codec, data);
                if (Mates.TryGetValue(who, out var m) && m != null) VoiceOut.Of(m).Take(seq, codec, data);
            }, false);
        }
    }

    // A crewmate's voice: decoded into a little buffer, played from their head as a streaming clip.
    public class VoiceOut : MonoBehaviour
    {
        public const int SteamRate = 24000, TestRate = 8000;
        public const float Full = 3f, Silent = 35f;              // full volume to 3 m, silence at 35 m (systems §3.6)
        public float Level;                                      // how loud they are now (0..1), what's playing
        public bool Speaking => Time.time - lastHeard < 0.3f && Level > 0.01f;
        public int Packets, Lost, Samples; public float Mute = 1; // (Mute: per-player volume, 0 = muted: settings later)
        public AudioSource Src;
        Angler who; AudioLowPassFilter low; float lastHeard = -9; int seqLast = -1; int rate;
        // the jitter buffer: written as packets come, read by the audio thread
        float[] ring = new float[TestRate * 2]; int wr, rd, have; bool primed; readonly object gate = new();
        readonly MemoryStream dec = new();

        public static VoiceOut Of(Angler m) { var v = m.GetComponent<VoiceOut>(); if (v == null) { v = m.gameObject.AddComponent<VoiceOut>(); v.who = m; } return v; }

        // the volume at a distance, the same curve the audio source uses
        public static float Gain(float d) => d <= Full ? 1 : d >= Silent ? 0 : 1 - Mathf.Log(d / Full) / Mathf.Log(Silent / Full);

        void Setup(int r)
        {
            if (rate == r && Src != null) return;
            rate = r; lock (gate) { ring = new float[r * 2]; wr = rd = have = 0; primed = false; }
            if (Src == null)
            {
                var g = new GameObject("voice"); g.transform.SetParent(transform, false);
                Src = g.AddComponent<AudioSource>(); low = g.AddComponent<AudioLowPassFilter>(); low.cutoffFrequency = 22000;
                Src.spatialBlend = 1; Src.dopplerLevel = 0; Src.rolloffMode = AudioRolloffMode.Custom; Src.minDistance = Full; Src.maxDistance = Silent;
                var curve = new AnimationCurve(); for (int i = 0; i <= 24; i++) { float d = Silent * i / 24f; curve.AddKey(d / Silent, Gain(d)); }
                Src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
            }
            Src.Stop();
            Src.clip = AudioClip.Create("voice " + name, r, 1, r, true, Read); Src.loop = true; Src.Play();
        }

        public void Take(ushort seq, byte codec, byte[] data)
        {
            float[] pcm;
            if (codec == 0)
            {
                if (!Coop.SteamVoice) return; // (no Steam here to decode Steam's voice)
                dec.SetLength(0);
                try { Steamworks.SteamUser.DecompressVoice(data, dec); } catch { return; }
                var b = dec.GetBuffer(); int n = (int)dec.Length / 2; pcm = new float[n];
                for (int i = 0; i < n; i++) pcm[i] = (short)(b[2 * i] | b[2 * i + 1] << 8) / 32768f;
                Setup(SteamRate);
            }
            else { pcm = Mulaw.Decode(data); Setup(TestRate); }
            if (seqLast >= 0) { int gap = (ushort)(seq - seqLast) - 1; if (gap > 0 && gap < 1000) Lost += gap; }
            seqLast = seq; Packets++; Samples += pcm.Length; lastHeard = Time.time;
            float sum = 0; foreach (var s in pcm) sum += s * s;
            Level = Mathf.Max(Level * 0.6f, Mathf.Sqrt(sum / Mathf.Max(1, pcm.Length)) * 3f);
            lock (gate)
            {
                foreach (var s in pcm) { ring[wr] = s; wr = (wr + 1) % ring.Length; if (have < ring.Length) have++; else rd = (rd + 1) % ring.Length; }
                if (have > rate / 8) primed = true; // 125 ms in hand before it starts (the jitter buffer)
            }
        }

        // the audio thread
        void Read(float[] data)
        {
            lock (gate)
                for (int i = 0; i < data.Length; i++)
                {
                    if (!primed || have == 0) { data[i] = 0; primed = false; continue; }
                    data[i] = ring[rd] * Mute; rd = (rd + 1) % ring.Length; have--;
                }
        }

        void LateUpdate()
        {
            if (Src == null || who == null) return;
            var head = who.Head != null ? who.Head.position : who.transform.position + Vector3.up * 1.4f * who.Size;
            Src.transform.position = head;
            low.cutoffFrequency = head.y < Sea.Height(head) - 0.05f ? 800 : 22000; // under the sea: muffled
            Level = Mathf.MoveTowards(Level, 0, Time.deltaTime * (Time.time - lastHeard > 0.2f ? 4 : 0.5f));
        }
    }

    // G.711 μ-law: the test voice's codec (8 bits a sample)
    public static class Mulaw
    {
        public static byte[] Encode(float[] pcm)
        {
            var o = new byte[pcm.Length];
            for (int i = 0; i < pcm.Length; i++)
            {
                int s = Mathf.Clamp((int)(pcm[i] * 32767), -32635, 32635), sign = s < 0 ? 0x80 : 0; if (s < 0) s = -s;
                s += 0x84; int exp = 7; for (int m = 0x4000; (s & m) == 0 && exp > 0; m >>= 1) exp--;
                o[i] = (byte)~(sign | exp << 4 | (s >> (exp + 3)) & 0x0F);
            }
            return o;
        }
        public static float[] Decode(byte[] b)
        {
            var o = new float[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                int u = ~b[i] & 0xFF, sign = u & 0x80, exp = u >> 4 & 7, man = u & 0x0F;
                int s = ((man << 3) + 0x84 << exp) - 0x84;
                o[i] = (sign != 0 ? -s : s) / 32768f;
            }
            return o;
        }
    }

    // A made-up voice for the tests: a buzzy vowel at ~140 Hz with two formants, in syllables of ~4 a second and short
    // pauses between words. Never the mic.
    public static class TestVoice
    {
        static double t; static readonly System.Random r = new(7);
        public static float[] Next(int n, out float level)
        {
            var o = new float[n]; float sum = 0; double dt = 1.0 / VoiceOut.TestRate;
            for (int i = 0; i < n; i++, t += dt)
            {
                double word = t % 1.6, syl = 0.5 - 0.5 * Math.Cos(2 * Math.PI * 4 * t);
                double env = word < 1.2 ? syl : 0;
                double f0 = 140 + 15 * Math.Sin(2 * Math.PI * 0.7 * t), ph = t * f0 % 1;
                double buzz = ph < 0.4 ? Math.Sin(Math.PI * ph / 0.4) : 0;
                double v = buzz * (0.6 * Math.Sin(2 * Math.PI * 700 * t) + 0.4 * Math.Sin(2 * Math.PI * 1200 * t)) + 0.02 * (r.NextDouble() - 0.5);
                o[i] = (float)(0.5 * env * v); sum += o[i] * o[i];
            }
            level = Mathf.Sqrt(sum / n) * 3f;
            return o;
        }
    }
}
