using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Shop signs and the port's signpost (Tushar 7 Nov: "a new player can't find a shop, it looks like every other
    // house"). Over each shop's door a big painted board with its name and its icon in the shop's colour; out from the
    // wall beside it a hanging blade sign with the icon, so it reads from up and down the street. At the port, where the
    // pier meets the yard, a signpost with an arrow board to each shop (its name and how far). Code-built: boards are
    // toon boxes, words TextMesh (Firm.Sign's depth-tested font), icons the phone's painted glyphs.
    public class Signs : MonoBehaviour
    {
        public static Signs I;
        public struct Shop { public string Id, Name, Glyph; public Color Col; }
        public static readonly Shop[] Shops =
        {
            new() { Id = "tackle", Name = "Hook, Line & Sinker", Glyph = "hook", Col = Ponyo.Hex("2FA79C") },
            new() { Id = "pratts", Name = "Pratt's Fish Market", Glyph = "fish", Col = Ponyo.Hex("3A86E0") },
            new() { Id = "doyle", Name = "Doyle Boatworks", Glyph = "boat", Col = Ponyo.Hex("3B4C7A") },
            new() { Id = "plank", Name = "Plank & Post", Glyph = "saw", Col = Ponyo.Hex("5C9A4E") },
            new() { Id = "pub", Name = "The Mooring Line", Glyph = "mug", Col = Ponyo.Hex("E2553F") },
            new() { Id = "post", Name = "Post office", Glyph = "letter", Col = Ponyo.Hex("D9483B") },
            new() { Id = "hartleys", Name = "Hartley's Outfitters", Glyph = "shirt", Col = Ponyo.Hex("8E5C8A") },
            new() { Id = "surf", Name = "Swell Surf Co.", Glyph = "surf", Col = Ponyo.Hex("E3A83C") },
        };
        public readonly Dictionary<string, Transform> Boards = new();      // (tests) shop id -> its door board
        public Transform Post;                                             // the port's way-to-town board
        public readonly List<(string id, Transform board)> Arrows = new();

        public static void Create()
        {
            var g = new GameObject("Signs"); I = g.AddComponent<Signs>();
            foreach (var s in Shops) { var a = Firm.Anchor(s.Id); if (a != null) I.Boards[s.Id] = I.DoorSign(s, a); }
            I.BuildPost();
            Debug.Log($"[PB] signs: {I.Boards.Count} shop boards, signpost {(I.Post != null ? "up" : "missing")} with {I.Arrows.Count} arrows");
        }

        static readonly Color Paper = Ponyo.Paper, Wood = Ponyo.Hex("9A6A48"), Ink = Ponyo.Ink, Deep = Ponyo.Hex("2A1C18");   // (Deep: board words read light at a distance in Ink)

        // an icon (white glyph, tinted) on a board face: local +Z out of the face, like the board's root
        static void Icon(Transform root, string glyph, Vector3 p, float size, Color col)
        {
            var g = new GameObject("icon " + glyph); g.transform.SetParent(root, false);
            g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0, 180, 0); g.transform.localScale = Vector3.one * (size / 0.96f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = Glyphs.Get(glyph); sr.color = col;
        }

        // Over the door: a cream board, the shop's colour as a frame, the icon in a coloured disc on the left, the name.
        // The anchor stands 1 m out from the door (fwd = out of it): the board hangs on the wall above the door.
        Transform DoorSign(Shop s, Transform anchor)
        {
            var root = new GameObject("sign " + s.Id).transform; root.SetParent(transform, false);
            var fwd = anchor.forward; fwd.y = 0; fwd.Normalize();
            float h = s.Id == "pratts" ? 4.6f : 3.5f;                     // (Pratt's door is up its steps)
            root.SetPositionAndRotation(anchor.position - fwd * 0.62f + Vector3.up * h, Quaternion.LookRotation(fwd));
            float w = Mathf.Clamp(0.95f + s.Name.Length * 0.2f, 3.0f, 5.2f);
            Firm.Box(root, "frame", new Vector3(0, 0, -0.04f), new Vector3(w + 0.22f, 1.12f, 0.1f), s.Col);
            Firm.Box(root, "board", new Vector3(0, 0, 0.02f), new Vector3(w, 0.9f, 0.08f), Paper);
            Firm.Box(root, "disc", new Vector3(-w / 2 + 0.55f, 0, 0.065f), new Vector3(0.78f, 0.78f, 0.02f), s.Col);
            Icon(root, s.Glyph, new Vector3(-w / 2 + 0.55f, 0, 0.08f), 0.64f, Paper);
            Firm.Sign(root, s.Name, new Vector3(0.45f, 0.02f, 0.075f), Deep, 0.046f);
            for (int k = -1; k <= 1; k += 2) Firm.Box(root, "bracket", new Vector3(k * (w / 2 - 0.3f), 0.66f, -0.18f), new Vector3(0.07f, 0.3f, 0.32f), Wood);
            // the blade sign: out from the wall beside the door, the icon both sides, on an iron arm
            var blade = new GameObject("blade").transform; blade.SetParent(root, false);
            blade.localPosition = new Vector3(w / 2 + 0.9f, -0.3f, 0.35f); blade.localRotation = Quaternion.Euler(0, 90, 0);
            Firm.Box(blade, "arm", new Vector3(0, 0.62f, 0), new Vector3(0.05f, 0.05f, 1.3f), Ponyo.Hex("5E5A66"));
            Firm.Box(blade, "plate", new Vector3(0, 0, 0), new Vector3(0.86f, 0.86f, 0.07f), s.Col);
            Firm.Box(blade, "face", new Vector3(0, 0, 0), new Vector3(0.74f, 0.74f, 0.09f), Paper);
            Icon(blade, s.Glyph, new Vector3(0, 0, 0.05f), 0.6f, s.Col);
            var back = new GameObject("back").transform; back.SetParent(blade, false); back.localRotation = Quaternion.Euler(0, 180, 0);
            Icon(back, s.Glyph, new Vector3(0, 0, 0.05f), 0.6f, s.Col);
            return root;
        }

        // The way-to-town board at the pier's head, facing down the pier (Tushar 7 Nov: a signpost with arrows). One row a
        // shop you can walk to, nearest first: its icon, its name, how far, and an arrow turned the way it lies as you read
        // the board (up = straight on into town, left = north along the waterfront, right = south).
        void BuildPost()
        {
            var l = Landing.I; if (l == null) return;
            // landing metres (2.6, 10.8): the yard side of the pier's head, clear of the office and the warehouse doors;
            // its face to the pier (landing -Y)
            var at = l.transform.TransformPoint(new Vector3(-2.6f, 0, -10.8f));
            if (Physics.Raycast(at + Vector3.up * 30, Vector3.down, out var hit, 60, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y; else at.y = 1.3f;
            var face = l.transform.TransformDirection(Vector3.forward); face.y = 0; face.Normalize();     // landing -Y in Blender = Unity +Z: down the pier
            Post = new GameObject("Signpost").transform; Post.SetParent(transform, false);
            Post.SetPositionAndRotation(at, Quaternion.LookRotation(face));
            var rows = new List<(Shop s, float dist, Vector3 dir)>();
            foreach (var s in Shops)
            {
                var a = Firm.Anchor(s.Id); if (a == null) continue;
                var to = a.position - at; to.y = 0; if (to.magnitude > 400) continue;              // (the surf shop: by boat)
                rows.Add((s, to.magnitude, to.normalized));
            }
            rows.Sort((x, y) => x.dist.CompareTo(y.dist));
            const float W = 4.4f, RH = 0.54f; float H = 0.86f + rows.Count * RH;   // (look check: 3.4 m let the long names run into the distances)
            float top = 1.0f + H;
            foreach (var x in new[] { -W / 2 + 0.12f, W / 2 - 0.12f }) Firm.Box(Post, "leg", new Vector3(x, top / 2 + 0.1f, -0.06f), new Vector3(0.14f, top + 0.2f, 0.14f), Wood);
            Firm.Box(Post, "frame", new Vector3(0, 1.0f + H / 2, -0.03f), new Vector3(W + 0.16f, H + 0.16f, 0.08f), Wood);
            Firm.Box(Post, "board", new Vector3(0, 1.0f + H / 2, 0.02f), new Vector3(W, H, 0.06f), Paper);
            Firm.Box(Post, "roof", new Vector3(0, top + 0.2f, -0.02f), new Vector3(W + 0.5f, 0.1f, 0.42f), Ponyo.Hex("805C52"));
            Firm.Sign(Post, "To the town", new Vector3(0, top - 0.32f, 0.06f), Deep, 0.036f);
            var col = Post.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, top / 2, -0.03f); col.size = new Vector3(W + 0.2f, top + 0.3f, 0.2f);
            // reading the board (facing up the pier, inland): its right hand is the board's -X seen from the front
            var right = Vector3.Cross(Vector3.up, -face);                                            // the reader's right
            float y = top - 0.86f;
            foreach (var (s, dist, dir) in rows)
            {
                var row = new GameObject("row " + s.Id).transform; row.SetParent(Post, false); row.localPosition = new Vector3(0, y, 0.06f);
                Firm.Box(row, "disc", new Vector3(W / 2 - 0.38f, 0, 0), new Vector3(0.42f, 0.42f, 0.02f), s.Col);
                Icon(row, s.Glyph, new Vector3(W / 2 - 0.38f, 0, 0.015f), 0.34f, Paper);
                Firm.Sign(row, s.Name, new Vector3(0.42f, 0, 0.01f), Deep, 0.027f);
                Firm.Sign(row, $"{Mathf.RoundToInt(dist / 10f) * 10} m", new Vector3(-W / 2 + 1.12f, 0, 0.01f), Ponyo.Hex("5E4A44"), 0.022f);
                // the arrow: the chevron glyph points left at 0°; turned so up = away from the reader, right = their right
                float ang = Mathf.Atan2(Vector3.Dot(dir, right), Vector3.Dot(dir, -face)) * Mathf.Rad2Deg;   // 0 = straight on, + = to the right
                var arrow = new GameObject("arrow").transform; arrow.SetParent(row, false); arrow.localPosition = new Vector3(-W / 2 + 0.36f, 0, 0.012f);
                arrow.localRotation = Quaternion.Euler(0, 0, 90 + ang);    // (the chevron points the board's +X, the reader's left: +90 = up, + ang = to their right)
                Icon(arrow, "back", Vector3.zero, 0.36f, s.Col);
                Arrows.Add((s.Id, row)); y -= RH;
            }
        }
    }
}
