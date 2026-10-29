using UnityEngine;

namespace PortBoss
{
    // The opening flythrough, at the start of every game (skippable): Marlowe's giant port across the sea, his
    // name on the HQ, then a sweep back over the water to Grandpa's shabby little pier, what we do there, and
    // what it could become. Captions are drawn by GameUI (DrawIntro). While it plays the host's day clock waits.
    public class Intro : MonoBehaviour
    {
        public static bool Playing => I != null;
        public static Intro I;
        public float T => Time.unscaledTime - t0;
        float t0;

        // Camera path: (time, position, look-at). The last key is replaced by the player's own camera at the end.
        static (float t, Vector3 p, Vector3 look)[] Keys => new[]
        {
            (0f, new Vector3(-300, 55, 380), new Vector3(0, 30, 700)),
            (7f, new Vector3(-90, 105, 500), new Vector3(30, 80, 820)), // high: his gantries move about below
            (12.5f, new Vector3(25, 112, 740), HarbourMap.MarloweSign),
            (16.5f, new Vector3(28, 108, 770), HarbourMap.MarloweSign),
            (21f, new Vector3(12, 40, 330), new Vector3(0, 2, 30)),
            (25.5f, new Vector3(7, 8, 62), new Vector3(0, 1, 18)),
            (30f, new Vector3(-15, 6, 28), new Vector3(3, 1, 6)),
            (35f, new Vector3(-12, 10, -20), new Vector3(-6, 1, -105)),
            (40f, new Vector3(22, 26, 48), new Vector3(0, 4, -40)), // the whole little port, town and mountains behind (title)
            (44f, Vector3.zero, Vector3.zero),
        };
        public const float Length = 44f;

        // (from, to, text, who): who "" = narration, "MARLOWE" = his line, "TITLE" = the closing title card.
        public static readonly (float a, float b, string text, string who)[] Captions =
        {
            (0.8f, 6.8f, "This whole coast belongs to one man: MARLOWE.", ""),
            (7f, 12.4f, "Mega freighters. Giant cranes. Fireworks every time a ship comes in. And he always wants more.", ""),
            (12.6f, 17.6f, "\"One little pier on this coast still isn't mine. Not for long!\"", "MARLOWE"),
            (17.9f, 23.6f, "That little pier over there was Grandpa's. He left it to you and your crew.", ""),
            (23.8f, 28.9f, "Grandpa had one dream: turn this shabby fish berth into a real port.", ""),
            (29.1f, 34f, "Unload the fishing boats. Haul the fish across the road to the market. Every crate counts.", ""),
            (34.2f, 39.2f, "Save up in the crew tin and build: Concrete quay > Crane + warehouse > Container berth > MEGA PORT!", ""),
            (39.4f, Length, "Show Marlowe who the real PORT BOSS is.", "TITLE"),
        };

        Vector3 endPos, endLook;
        bool fireworks;

        public static void Begin()
        {
            if (I != null) return;
            I = new GameObject("Intro").AddComponent<Intro>();
            I.t0 = Time.unscaledTime;
            Player.CamOverride = true;
            Debug.Log("[PB] intro: start");
        }

        public static void Skip() { if (I != null) I.Finish("skipped"); }

        void Finish(string how)
        {
            Debug.Log($"[PB] intro: {how} at {T:F1}s");
            Player.CamOverride = false;
            I = null;
            Destroy(gameObject);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var pl = Player.Local;
            if (cam == null || pl == null) { Finish("no player"); return; }
            float t = T;
            if (t >= Length) { Finish("done"); return; }
            if (!fireworks && t > 8.5f) { fireworks = true; Fireworks.Show(Fireworks.Over); }
            // End on the player's usual camera (behind and above them).
            endLook = pl.transform.position + Vector3.up * 1.7f;
            endPos = endLook - pl.transform.forward * 5.6f + Vector3.up * 0.9f;
            var k = Keys;
            k[^1] = (Length, endPos, endLook);
            int i = 0;
            while (i < k.Length - 2 && t >= k[i + 1].t) i++;
            float u = Mathf.Clamp01((t - k[i].t) / (k[i + 1].t - k[i].t));
            if (i == 0) u = u * u; else if (i == k.Length - 2) u = 1 - (1 - u) * (1 - u); // ease off the start, settle at the end
            Vector3 P(int j) => k[Mathf.Clamp(j, 0, k.Length - 1)].p;
            Vector3 L(int j) => k[Mathf.Clamp(j, 0, k.Length - 1)].look;
            var pos = CatmullRom(P(i - 1), P(i), P(i + 1), P(i + 2), u);
            var look = CatmullRom(L(i - 1), L(i), L(i + 1), L(i + 2), u);
            pos.y = Mathf.Max(pos.y, Water.Height(pos) + 1.2f);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
        }

        static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u) =>
            0.5f * (2 * b + (c - a) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (3 * b - a - 3 * c + d) * u * u * u);
    }
}
