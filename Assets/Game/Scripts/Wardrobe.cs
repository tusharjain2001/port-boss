using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M5: the wardrobe (systems §1.9, mockup 07): pick one of the six crew (one each in co-op: a taken card says who has
    // it), then change clothes only: Hat, Top, Over, Legs, Boots, Gloves from what you own (the preset's own pieces,
    // Hartley's buys, the story's), dyes you bought at Hartley's. Faces, bodies and the player-colour band never change.
    // Opened with a tap of E at any bed, or at the office's wardrobe.
    //
    // Dressing a body (every body: you, your crewmates, the turntable, Hartley's mirror) is all PB_Toon channels on
    // the kit's own mesh (Channels.Clothes: channel 1 Hat .. 6 Gloves in the vertex alpha): a bought top recolours
    // channel 2 and lays its pattern over it (plaid, Breton stripe, hibiscus, cable knit); an over-layer the preset
    // hasn't got is a shell grown out of the top; a bought hat is its own model on the Hat bone with the preset's hat
    // folded away (_ChHide). No extra materials, ever.
    public class Wardrobe : MonoBehaviour
    {
        public static Wardrobe I;
        public static bool Open => I != null && I.root != null && I.root.gameObject.activeSelf;
        public static Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] wardrobe: " + s); OnLog?.Invoke(s); }
        static Company C => Firm.C;
        public static string TryOn; public static (int slot, int sw)? TryDye;   // Hartley's mirror: on me, here only

        // ---------------------------------------------------------------- the garments
        public struct Look { public string Base, Pat2; public int Pat; }
        static readonly Dictionary<string, Look> looks = new()
        {
            ["wear:beanie"] = new Look { Base = "C9483A" }, ["wear:flatcap"] = new Look { Base = "6E5A48" }, ["wear:souwester"] = new Look { Base = "F7C548" },
            ["wear:bucket"] = new Look { Base = "8DAF7A" }, ["wear:hardhat"] = new Look { Base = "F2F0E8" }, ["wear:captain"] = new Look { Base = "F7F4EC" },
            ["wear:straw"] = new Look { Base = "E8D49A" }, ["wear:bobble"] = new Look { Base = "3A6FC4" }, ["wear:walt"] = new Look { Base = "2E3F5C" },
            ["wear:tee"] = new Look { Base = "F3EBDD" }, ["wear:flannel"] = new Look { Base = "B8433A", Pat = 9, Pat2 = "2B2422" }, ["wear:breton"] = new Look { Base = "2E3F5C", Pat = 10, Pat2 = "F7F4EC" },
            ["wear:hawaiian"] = new Look { Base = "35A79C", Pat = 11, Pat2 = "F6A27A" }, ["wear:knit"] = new Look { Base = "E9DEC7", Pat = 12, Pat2 = "C9BBA0" },
            ["wear:hivis"] = new Look { Base = "D8F03C", Pat = 13, Pat2 = "D7DCE0" }, ["wear:oilskin"] = new Look { Base = "E0B43A" }, ["wear:puffer"] = new Look { Base = "C9483A", Pat = 14, Pat2 = "8E2E26" },
            ["wear:captainjk"] = new Look { Base = "22304F", Pat = 15, Pat2 = "E2B74A" },
            ["wear:cargos"] = new Look { Base = "9A8A66" }, ["wear:bib"] = new Look { Base = "3B5F94" }, ["wear:shorts"] = new Look { Base = "6E7F95", Pat = 16, Pat2 = "SKIN" },
            ["wear:rubber"] = new Look { Base = "2C3E50" }, ["wear:deck"] = new Look { Base = "8A5A3A", Pat = 18, Pat2 = "F3EBDD" }, ["wear:steel"] = new Look { Base = "5B4636", Pat = 17, Pat2 = "A8B2BC" },
            ["wear:rubberg"] = new Look { Base = "F7C548" }, ["wear:leather"] = new Look { Base = "8A5A3A" }, ["wear:gauntlet"] = new Look { Base = "3F4A5A" },
        };
        public static Color Colour(string item) => looks.TryGetValue(item, out var l) ? Ponyo.Hex(l.Base) : Ponyo.Paper3;

        // one body's dressing state
        class Dressed { public string Sig; public GameObject Hat; public SkinnedMeshRenderer Shell; public Vector4 PatK; public Color Skin; public bool HasOver; }
        static readonly Dictionary<GameObject, Dressed> dressed = new();
        static readonly Dictionary<Mesh, Mesh> withBind = new();
        public static string HatOf(GameObject body) => body != null && dressed.TryGetValue(body, out var d) && d.Hat != null ? d.Hat.name : null;   // (tests)
        public static string SigOf(GameObject body) => body != null && dressed.TryGetValue(body, out var d) ? d.Sig : null;

        // Dress a body (the GameObject holding the kit's SkinnedMeshRenderers) in an outfit (+ a try-on for the mirror)
        public static void Dress(GameObject body, Outfit o, string tryOn = null, (int slot, int sw)? tryDye = null)
        {
            if (body == null || o == null) return;
            var smrs = new List<SkinnedMeshRenderer>(); foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (r.name != "over shell") smrs.Add(r);
            if (smrs.Count == 0) return;
            string sig = string.Join(",", o.Wear) + "|" + string.Join(",", o.Dye) + "|" + tryOn + "|" + tryDye + "|" + smrs[0].GetHashCode();
            if (!dressed.TryGetValue(body, out var d)) { dressed[body] = d = Prepare(body, smrs); }
            if (d.Sig == sig) return; d.Sig = sig;
            var gs = new List<Channels.Garment>(); int hide = 0; string hat = null; Channels.Garment? over = null;
            for (int s = 0; s < 6; s++)
            {
                string item = o.Wear[s]; int dye = o.Dye[s];
                if (tryOn != null && Catalog.Get(tryOn)?.Slot == s) { item = tryOn; dye = -1; }
                if (tryDye is { } td && td.slot == s) dye = td.sw;
                if (item == null) { if (dye >= 0) gs.Add(new Channels.Garment { Ch = s + 1, Dye = true, Col = Channels.Lin(Catalog.Swatch[dye]) }); continue; }
                if (item == "wear:none") { if (s == 2) hide |= 1 << 3; continue; }
                var lk = looks.TryGetValue(item, out var l) ? l : new Look { Base = "F3EBDD" };
                var g = new Channels.Garment { Ch = s + 1, Dye = true, Col = dye >= 0 ? Channels.Lin(Catalog.Swatch[dye]) : Channels.Lin(lk.Base), Pat = lk.Pat, Pat2 = lk.Pat2 == "SKIN" ? d.Skin : lk.Pat2 != null ? Channels.Lin(lk.Pat2) : Color.black };
                if (s == 0) { hat = item; hide |= 1 << 1; gs.Add(g); continue; }
                if (s == 2 && !d.HasOver) { over = g; continue; }
                gs.Add(g);
            }
            var arr = gs.ToArray();
            foreach (var r in smrs) Channels.Clothes(r, arr, hide, d.PatK);
            // the over-layer shell (a preset with no coat of their own)
            if (d.Shell != null) { d.Shell.gameObject.SetActive(over.HasValue); if (over.HasValue) Channels.Clothes(d.Shell, new[] { over.Value }, 0, d.PatK); }
            // the hat on its bone
            if (d.Hat != null && (hat == null || d.Hat.name != hat)) { Destroy(d.Hat); d.Hat = null; }
            if (hat != null && d.Hat == null) d.Hat = HatOn(body, hat);
            if (d.Hat != null) foreach (var r in d.Hat.GetComponentsInChildren<MeshRenderer>()) Channels.Clothes(r, new[] { gs.Find(x => x.Ch == 1) }, 0, d.PatK);
        }
        // once per body: the bind pose in TEXCOORD1 (the patterns follow the cloth, not the world), the heights the leg and
        // boot patterns need, the skin tone (shorts), and the over-layer shell if the preset has no over-layer
        static Dressed Prepare(GameObject body, List<SkinnedMeshRenderer> smrs)
        {
            var d = new Dressed { PatK = new Vector4(0.5f, 0.15f, 0.1f, 0.1f), Skin = Channels.Lin("E9B48A") };
            float knee = 0, ankle = 0, toe = float.MinValue; int nk = 0; var skinSum = Vector3.zero; int ns = 0;
            foreach (var r in smrs)
            {
                var src = r.sharedMesh; if (src == null || !src.isReadable) continue;
                if (!withBind.TryGetValue(src, out var m))
                {
                    m = Instantiate(src); m.name = src.name + " (bind)"; var v = src.vertices; float k = src.bounds.size.y > 10 ? 0.01f : 1;
                    var q = new List<Vector3>(v.Length); foreach (var p in v) q.Add(new Vector3(p.x * k, p.y * k, -p.z * k));   // front = +z
                    m.SetUVs(1, q); withBind[src] = m; withBind[m] = m;
                }
                r.sharedMesh = m;
                var inf = Channels.Read(m); if (inf != null && inf.Count[3] > 0) d.HasOver = true;
                var cols = m.colors32; var uv = new List<Vector3>(); m.GetUVs(1, uv);
                for (int i = 0; i < cols.Length && i < uv.Count; i++)
                {
                    int ch = Channels.Of(cols[i]);
                    if (ch == 5) { toe = Mathf.Max(toe, uv[i].z); ankle = Mathf.Max(ankle, uv[i].y); }
                    if (ch == 0 && cols[i].r > 150 && cols[i].g > 90 && cols[i].g < 200 && cols[i].b < 170 && cols[i].r > cols[i].b + 40) { var l = ((Color)cols[i]).linear; skinSum += new Vector3(l.r, l.g, l.b); ns++; }
                }
            }
            var anim = body.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                var lk = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var root = anim.transform;
                if (lk != null) { knee = root.InverseTransformPoint(lk.position).y / Mathf.Max(0.01f, root.lossyScale.y) * root.lossyScale.y; nk = 1; }
            }
            if (nk > 0) d.PatK.x = knee + 0.04f;
            if (ankle > 0) d.PatK.w = Mathf.Max(0.05f, ankle - 0.1f);
            if (toe > float.MinValue) d.PatK.z = toe - 0.07f;
            if (ns > 0) d.Skin = new Color(skinSum.x / ns, skinSum.y / ns, skinSum.z / ns);
            if (!d.HasOver) d.Shell = Shell(smrs);
            return d;
        }
        // the over-layer: the top's triangles (channel 2) pushed out along their normals, skinned to the same bones, tagged 3
        static SkinnedMeshRenderer Shell(List<SkinnedMeshRenderer> smrs)
        {
            foreach (var r in smrs)
            {
                var m = r.sharedMesh; if (m == null || !m.isReadable) continue; var inf = Channels.Read(m); if (inf == null || inf.Count[2] == 0) continue;
                var cols = m.colors32; var v = m.vertices; var n = m.normals; var bw = m.boneWeights; var uv1 = new List<Vector3>(); m.GetUVs(1, uv1);
                var map = new Dictionary<int, int>(); var nv = new List<Vector3>(); var nc = new List<Color32>(); var nb = new List<BoneWeight>(); var nn = new List<Vector3>(); var nu = new List<Vector3>(); var tris = new List<int>();
                float push = (m.bounds.size.y > 10 ? 100 : 1) * 0.018f;
                for (int sub = 0; sub < m.subMeshCount; sub++)
                {
                    var t = m.GetTriangles(sub);
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        if (Channels.Of(cols[t[i]]) != 2 || Channels.Of(cols[t[i + 1]]) != 2 || Channels.Of(cols[t[i + 2]]) != 2) continue;
                        for (int j = 0; j < 3; j++)
                        {
                            int a = t[i + j];
                            if (!map.TryGetValue(a, out int b))
                            {
                                b = nv.Count; map[a] = b; nv.Add(v[a] + (n.Length > a ? n[a] : Vector3.zero) * push); nn.Add(n.Length > a ? n[a] : Vector3.up);
                                var c = cols[a]; c.a = Channels.Alpha(3); nc.Add(c); if (bw.Length > a) nb.Add(bw[a]); nu.Add(uv1.Count > a ? uv1[a] : Vector3.zero);
                            }
                            tris.Add(b);
                        }
                    }
                }
                if (tris.Count == 0) continue;
                var sm = new Mesh { name = m.name + " (over shell)" }; if (nv.Count > 65000) sm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                sm.SetVertices(nv); sm.SetNormals(nn); sm.SetColors(nc); sm.SetUVs(1, nu); sm.SetTriangles(tris, 0); sm.boneWeights = nb.ToArray(); sm.bindposes = m.bindposes; sm.RecalculateBounds();
                var g = new GameObject("over shell"); g.transform.SetParent(r.transform.parent, false); g.transform.localPosition = r.transform.localPosition; g.transform.localRotation = r.transform.localRotation; g.transform.localScale = r.transform.localScale;
                var s = g.AddComponent<SkinnedMeshRenderer>(); s.sharedMesh = sm; s.bones = r.bones; s.rootBone = r.rootBone; s.sharedMaterial = r.sharedMaterial; s.updateWhenOffscreen = true;
                g.SetActive(false);
                return s;
            }
            return null;
        }
        // a Hartley's hat on the Hat bone (the kit's hats wobble on it); its main colour tagged channel 1 so dyes take
        static GameObject HatOn(GameObject body, string item)
        {
            var pf = M1Assets.I.M5Prefab("Hat_" + item.Substring(5)); var bone = Util.Find(body.transform, "Hat");
            var anim = body.GetComponentInChildren<Animator>(); var head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            var at = bone != null ? bone : head; if (at == null) return null;
            GameObject g;
            if (pf != null) { g = Instantiate(pf); Util.Colliders(g); Util.Toon(g, Game.ToonMat); foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c); }
            else { g = new GameObject(item); var b = Firm.Box(g.transform, "crown", new Vector3(0, 0.07f, 0), new Vector3(0.24f, 0.12f, 0.26f), Colour(item)); Firm.Box(g.transform, "brim", new Vector3(0, 0.01f, 0.04f), new Vector3(0.3f, 0.02f, 0.34f), Colour(item)); }
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>()) TagMain(mf);
            g.name = item; g.transform.SetParent(at, false);
            // sit it on the crown: the head's top (from the body's measured head) is above the hat bone by the kit's offset
            var me = body.GetComponentInParent<Angler>(); float size = me != null ? me.Size : 1;
            g.transform.localPosition = bone != null ? Vector3.zero : new Vector3(0, 0.16f, 0); g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, at.lossyScale.x)) * 1.0f;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return g;
        }
        static readonly Dictionary<Mesh, Mesh> tagged = new();
        static void TagMain(MeshFilter mf)
        {
            var src = mf.sharedMesh; if (src == null || !src.isReadable) return;
            if (!tagged.TryGetValue(src, out var m))
            {
                m = Instantiate(src); var cols = m.colors32;
                if (cols.Length == 0) { cols = new Color32[m.vertexCount]; for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(200, 200, 200, Channels.Alpha(1)); }
                else
                {
                    var count = new Dictionary<int, int>(); foreach (var c in cols) { int k = c.r << 16 | c.g << 8 | c.b; count[k] = count.TryGetValue(k, out var x) ? x + 1 : 1; }
                    int best = 0, bn = -1; foreach (var kv in count) if (kv.Value > bn) { bn = kv.Value; best = kv.Key; }
                    for (int i = 0; i < cols.Length; i++) if ((cols[i].r << 16 | cols[i].g << 8 | cols[i].b) == best || Channels.Of(cols[i]) == 1) cols[i].a = Channels.Alpha(1);
                }
                m.colors32 = cols; tagged[src] = m;
            }
            mf.sharedMesh = m;
        }

        // every body in the world, every frame: dressed from the company (the host's, synced)
        void LateUpdate()
        {
            if (C == null) return;
            var me = Angler.I;
            if (me != null && me.BodyGo != null) Dress(me.BodyGo, C.OutfitOf(Firm.MyKey), TryOn, TryDye);
            if (Coop.On && Coop.I != null)
                foreach (var e in Coop.I.Roster)
                    if (Coop.I.Mates.TryGetValue(e.Id, out var m) && m != null && m.BodyGo != null)
                        Dress(m.BodyGo, C.OutfitOf(e.Key ?? Coop.MyKey(e.Name)));
            if (turn != null) Dress(turn, Preview(), null, null);
        }

        // ---------------------------------------------------------------- the screen (mockup 07, 1920x1080 pixels)
        RectTransform root, cards, items, swatches, left, right; Text crewName, crewLine, colourT, dyeCap, hint, resetT; Image colourDot;
        RawImage turnImg; Camera tcam, cam; RenderTexture trt; Transform stage; GameObject turn; int turnFor = -1; float yaw = Front, yawV; const float Front = 0;   // the turntable body faces the camera at 0 (its idle turns it; the portrait bake, no idle, uses 180)
        readonly RenderTexture[] portraits = new RenderTexture[6];
        int tab, preset, sel = -1;
        readonly List<(RectTransform r, Action a)> btns = new(); int focus = -1; string key;
        static readonly string[] Blurb = { "round · fish expert", "stocky · quiet strongman", "lanky · eager apprentice", "broad · ex-crane boss", "wiry · old salt", "the diver · silent" };
        static readonly string[] Line = { "Cheerful fish expert. Hums shanties, knows every buyer by name.", "Says little, lifts a lot. Has never once dropped a crate.", "All elbows and enthusiasm. Learning fast, falling often.", "Ran the cranes at Cannery Flats for twenty years. Still points at things.", "Fished this bay before the lighthouse went dark. Remembers Walt young.", "Came up out of the bay one morning in that helmet. Nobody asks." };
        static readonly string[] ColourName = { "orange", "blue", "green", "red" };

        public static void Create() { var g = new GameObject("Wardrobe"); I = g.AddComponent<Wardrobe>(); I.Build(); }
        static Text T(RectTransform p, string s, Font f, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft)
        { var t = UiKit.Txt(p, s, f, size, c, a); t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return t; }
        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            root = UiKit.Rect("wardrobe", hud); root.Stretch(); UiKit.Fit(root, 0, 0);
            var oc = root.gameObject.AddComponent<Canvas>(); oc.overrideSorting = true; oc.sortingOrder = 20; root.gameObject.AddComponent<GraphicRaycaster>();   // above the toasts
            var sky = UiKit.Img(root, null, Ponyo.Hex("D7E6F2"), "sky"); sky.rectTransform.Stretch(); sky.raycastTarget = true;
            var low = UiKit.Img(root, null, Ponyo.Hex("F6EEDF"), "low"); low.rectTransform.Place(new Vector2(0.5f, 0), Vector2.zero, new Vector2(1920, 420), new Vector2(0.5f, 0)); low.color = new Color(0.965f, 0.933f, 0.875f, 0.85f);
            T(root, "Wardrobe", UiKit.DisplayBold, 46, Ponyo.Ink, 64, 46, 600, 60);
            T(root, "Pick one of the six crew. Faces and bodies come with them; only the clothes change. Open it at any bed or the office wardrobe.", UiKit.Body, 19, Ponyo.InkSoft, 64, 108, 1300, 28);
            // left: the crew
            left = UiKit.Card(root, "crew", 26, Ponyo.Paper, true, Ponyo.Line, 4); left.Place(new Vector2(0, 1), new Vector2(64, -150), new Vector2(380, 872), new Vector2(0, 1));
            T(left, "CREW MEMBER", UiKit.Bold, 13, Ponyo.InkSoft, 24, 26, 300, 18);
            cards = UiKit.Rect("cards", left).Place(new Vector2(0, 1), new Vector2(24, -54), new Vector2(332, 690), new Vector2(0, 1));
            var note = UiKit.Card(left, "note", 12, Ponyo.Hex("FBF1DE"), false, Ponyo.Hex("E5D3B3"), 0); note.Place(new Vector2(0, 0), new Vector2(24, 24), new Vector2(332, 84), new Vector2(0, 0));
            hint = T(note, "In co-op, each crew member can be picked by only one player at a time. If two want Pip, the second picks another.", UiKit.Body, 14, Ponyo.InkSoft, 12, 10, 308, 70);
            // centre: the turntable
            var tt = UiKit.Rect("turntable", root).Place(new Vector2(0, 1), new Vector2(560, -200), new Vector2(800, 680), new Vector2(0, 1));
            turnImg = UiKit.Rect("rt", tt).gameObject.AddComponent<RawImage>(); turnImg.rectTransform.Stretch(); turnImg.raycastTarget = false;
            var lA = UiKit.Card(root, "turn left", 26, Ponyo.Paper, true, Ponyo.Line, 3); lA.Place(new Vector2(0, 1), new Vector2(610, -808), new Vector2(52, 52), new Vector2(0, 1));
            UiKit.Img(lA, Glyphs.Get("back"), Ponyo.Ink, "g").rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24)); Btn(lA, "turn left", () => yawV -= 160);
            var rA = UiKit.Card(root, "turn right", 26, Ponyo.Paper, true, Ponyo.Line, 3); rA.Place(new Vector2(0, 1), new Vector2(1080, -808), new Vector2(52, 52), new Vector2(0, 1));
            var rg = UiKit.Img(rA, Glyphs.Get("back"), Ponyo.Ink, "g"); rg.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24)); rg.rectTransform.localScale = new Vector3(-1, 1, 1); Btn(rA, "turn right", () => yawV += 160);
            var nc = UiKit.Card(root, "name card", 22, Ponyo.Paper, true, Ponyo.Line, 4); nc.Place(new Vector2(0, 1), new Vector2(628, -886), new Vector2(482, 136), new Vector2(0, 1));
            crewName = T(nc, "", UiKit.DisplayBold, 38, Ponyo.Ink, 0, 12, 482, 48, TextAnchor.UpperCenter);
            crewLine = T(nc, "", UiKit.Body, 16, Ponyo.InkSoft, 16, 62, 450, 24, TextAnchor.UpperCenter);
            var band = UiKit.Card(nc, "colour", 15, Ponyo.Hex("EAF7EC"), false, Ponyo.Hex("A9D3B1"), 0); band.Place(new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(380, 30), new Vector2(0.5f, 0));
            colourDot = UiKit.Img(band, UiKit.Circle, Ponyo.Leaf, "dot"); colourDot.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(16, 16), new Vector2(0, 0.5f));
            colourT = UiKit.Txt(band, "", UiKit.Body, 14, Ponyo.Ink, TextAnchor.MiddleLeft); colourT.rectTransform.Stretch(); colourT.rectTransform.offsetMin = new Vector2(38, 0);
            // right: the clothes
            right = UiKit.Card(root, "clothes", 26, Ponyo.Paper, true, Ponyo.Line, 4); right.Place(new Vector2(0, 1), new Vector2(1298, -150), new Vector2(558, 872), new Vector2(0, 1));
            items = UiKit.Rect("items", right).Place(new Vector2(0, 1), new Vector2(24, -24), new Vector2(510, 560), new Vector2(0, 1));
            swatches = UiKit.Rect("dyes", right).Place(new Vector2(0, 1), new Vector2(24, -420), new Vector2(510, 300), new Vector2(0, 1));
            var reset = UiKit.Card(right, "reset", 18, Ponyo.Paper, true, Ponyo.Line, 4); reset.Place(new Vector2(0, 0), new Vector2(24, 24), new Vector2(186, 56), new Vector2(0, 0));
            var rk = UiKit.Key(reset, "R", 30); rk.Place(new Vector2(0, 0.5f), new Vector2(18, 0), rk.sizeDelta, new Vector2(0, 0.5f));
            resetT = UiKit.Txt(reset, "Reset", UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleLeft); resetT.rectTransform.Stretch(); resetT.rectTransform.offsetMin = new Vector2(60, 0);
            Btn(reset, "reset", ResetClothes);
            var wear = UiKit.Card(right, "wear", 18, Ponyo.Sunflower, true, Ponyo.Hex("B88A1E"), 5); wear.Place(new Vector2(0, 0), new Vector2(222, 24), new Vector2(312, 56), new Vector2(0, 0));
            var ek = UiKit.Key(wear, "E", 30); ek.Place(new Vector2(0.5f, 0.5f), new Vector2(-50, 0), ek.sizeDelta);
            var wt = UiKit.Txt(wear, "Wear it", UiKit.DisplayBold, 22, Ponyo.Ink, TextAnchor.MiddleLeft); wt.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-26, 0), new Vector2(200, 40), new Vector2(0, 0.5f));
            Btn(wear, "wear", WearIt);
            var esc = UiKit.Key(root, "Esc", 34); esc.Place(new Vector2(1, 1), new Vector2(-170, -40), esc.sizeDelta, new Vector2(1, 1));
            T(root, "Done", UiKit.Body, 18, Ponyo.InkSoft, 1920 - 160, 44, 100, 26);
            root.gameObject.SetActive(false);
            // the turntable's stage, far under the bay
            stage = new GameObject("WardrobeStage").transform; stage.position = new Vector3(40, -600, 0);
            var floor = new GameObject("floor"); floor.transform.SetParent(stage, false);
            Firm.Box(stage, "disc", new Vector3(0, -0.03f, 0), new Vector3(2.4f, 0.06f, 2.4f), Ponyo.Hex("EFE4D0"));
            var lt = new GameObject("key light").AddComponent<Light>(); lt.transform.SetParent(stage, false); lt.transform.localPosition = new Vector3(-1.2f, 2.4f, -2.2f); lt.type = LightType.Point; lt.range = 8; lt.intensity = 3.5f; lt.color = Ponyo.Hex("FFF1DC");
            trt = new RenderTexture(800, 680, 24) { name = "PB_Wardrobe", antiAliasing = 4 };
            var cg = new GameObject("WardrobeCam"); cg.transform.SetParent(stage, false); tcam = cg.AddComponent<Camera>(); tcam.targetTexture = trt; tcam.fieldOfView = 26; tcam.clearFlags = CameraClearFlags.SolidColor; tcam.backgroundColor = Ponyo.Hex("D7E6F2"); tcam.enabled = false;
            cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            cg.transform.localPosition = new Vector3(0, 1.05f, -4.6f); cg.transform.localRotation = Quaternion.Euler(4, 0, 0);
            turnImg.texture = trt;
            stage.gameObject.SetActive(false);
        }
        RectTransform Btn(RectTransform r, string label, Action a)
        {
            r.name = "btn " + label;
            var f = UiKit.Card(r, "focus", 20, new Color(0, 0, 0, 0), false, Ponyo.Sunflower); f.Stretch(-5); UiKit.Edge(f).color = Ponyo.Hex("E0A82E"); f.gameObject.SetActive(false);
            btns.Add((r, a)); return r;
        }

        public static void Show() => I?.ShowI();
        void ShowI()
        {
            if (Open) return;
            preset = Mathf.Clamp(Mine.Preset >= 0 ? Mine.Preset : Angler.Body, 0, 5); tab = 0; sel = -1; key = null; focus = -1; yaw = Front; yawV = 0;
            root.gameObject.SetActive(true); stage.gameObject.SetActive(true); tcam.enabled = true; Ctl.Ui = 2;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Snd.Play2D(Synth.Creak, 0.25f, 1.4f); Snd.Play2D(Synth.Tik, 0.3f, 0.9f);
            BakePortraits();
            Log("open");
        }
        public void Hide()
        {
            if (!Open) return; root.gameObject.SetActive(false); stage.gameObject.SetActive(false); tcam.enabled = false; Ctl.Ui = 0;
            if (turn != null) { dressed.Remove(turn); Destroy(turn); turn = null; turnFor = -1; }
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            Log("close");
        }
        static Outfit Mine => C.OutfitOf(Firm.MyKey);
        static int MySlot => Coop.On && Coop.I != null ? (Coop.I.Roster.Find(e => e.Id == Coop.Me)?.Slot ?? 0) % Coop.Colours.Length : 0;
        // what the turntable wears: mine if it's my crew member, else theirs as they come (their own clothes)
        Outfit Preview()
        {
            var o = (preset == (Mine.Preset >= 0 ? Mine.Preset : Angler.Body)) ? Mine.Copy() : new Outfit();
            if (sel >= 0 && Choices().Count > sel) { var c = Choices()[sel]; if (c.own) o.Wear[tab] = null; else if (c.owned) o.Wear[tab] = c.id; }
            return o;
        }

        // ---- the six portraits: each crew member on the stage once, into a small texture
        void BakePortraits()
        {
            if (portraits[0] != null) return; var a = M1Assets.I; if (a.Crew == null) return;
            var cam0 = tcam.targetTexture; var pos = tcam.transform.localPosition; var rot = tcam.transform.localRotation; float fov = tcam.fieldOfView;
            for (int i = 0; i < 6 && i < a.Crew.Length; i++)
            {
                var b = Body(i); b.transform.localRotation = Quaternion.Euler(0, 180 + 12, 0);
                var head = b.GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Head);
                float hy = head != null ? head.position.y - stage.position.y : 1.5f;
                portraits[i] = new RenderTexture(320, 300, 24) { antiAliasing = 4 };
                tcam.targetTexture = portraits[i]; tcam.fieldOfView = 22; tcam.backgroundColor = Ponyo.Hex("C9D6E2");
                tcam.transform.localPosition = new Vector3(0, hy - 0.12f, -2.0f); tcam.transform.localRotation = Quaternion.Euler(3, 0, 0);
                b.GetComponentInChildren<Animator>()?.Update(0.5f);
                tcam.Render();
                DestroyImmediate(b);
            }
            tcam.targetTexture = cam0; tcam.transform.localPosition = pos; tcam.transform.localRotation = rot; tcam.fieldOfView = fov; tcam.backgroundColor = Ponyo.Hex("D7E6F2");
        }
        GameObject Body(int i)
        {
            var a = M1Assets.I; var b = Instantiate(a.Crew[i], stage, false); b.name = "turntable " + a.CrewNames[i];
            foreach (var r in b.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterial = PortBoss.Art.Lit; r.updateWhenOffscreen = true; Angler.PaintBand(r, Coop.Colours[MySlot]); }
            foreach (var c in b.GetComponentsInChildren<Collider>()) Destroy(c);
            b.transform.localPosition = Vector3.zero;
            var an = b.GetComponentInChildren<Animator>();
            if (an != null && a.Idle != null)
            {
                var skin = ScriptableObject.CreateInstance<PortBoss.Skin>(); skin.Idle = a.Idle; skin.Walk = a.Walk; skin.Run = a.Run; skin.Sprint = a.Sprint; skin.Jump = a.Jump; skin.Land = a.Land; skin.SwimIdle = a.SwimIdle; skin.Swim = a.Swim; skin.Climb = a.Climb; skin.Drive = a.Idle; skin.Push = a.Idle;
                var ca = an.gameObject.AddComponent<CharAnim>(); ca.Init(an, skin); ca.Facing = b.transform; ca.GripLean = 0; ca.enabled = true;
            }
            return b;
        }

        // ---- every frame
        void Update()
        {
            if (!Open) return;
            var kb = Keyboard.current; var pad = Gamepad.current;
            if (Ctl.Escape || kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame || Angler.I == null) { Ctl.UiEsc = Time.frameCount; Hide(); return; }
            if (turnFor != preset) { if (turn != null) { dressed.Remove(turn); Destroy(turn); } turn = Body(preset); turnFor = preset; StartCoroutine(Works.Pop(turn)); }
            // the turntable: a slow drift, arrows / mouse drag spin it
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed && RectTransformUtility.RectangleContainsScreenPoint(turnImg.rectTransform, mouse.position.ReadValue(), cam)) yawV = -mouse.delta.ReadValue().x * 25;
            // they face you and sway a little (a slow full spin showed their backs half the time); drag or the arrows turn them
            yaw += yawV * Time.unscaledDeltaTime; yawV = Mathf.Lerp(yawV, 0, 1 - Mathf.Exp(-3 * Time.unscaledDeltaTime));
            if (turn != null) { turn.transform.localRotation = Quaternion.Euler(0, yaw + 18 * Mathf.Sin(Time.unscaledTime * 0.7f), 0); var ca = turn.GetComponentInChildren<CharAnim>(); if (ca != null) ca.Tick(CharAnim.Slot.Idle, 0); }
            string k = $"{preset}|{tab}|{sel}|{string.Join(",", Mine.Wear)}|{string.Join(",", Mine.Dye)}|{Mine.Owned.Count}|{Mine.Preset}|{(Coop.On ? Coop.I.Roster.Count + string.Join(",", Coop.I.Roster.ConvertAll(e => e.Crew)) : "")}|{Angler.Body}";
            if (k != key) { key = k; Draw(); }
            Nav(kb, pad);
            if (kb != null && kb.rKey.wasPressedThisFrame || pad != null && pad.buttonNorth.wasPressedThisFrame) ResetClothes();
            if (kb != null && kb.tabKey.wasPressedThisFrame) { tab = (tab + 1) % 6; sel = -1; key = null; }
        }
        void Nav(Keyboard kb, Gamepad pad)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var mp = mouse.position.ReadValue();
                if (mouse.delta.ReadValue().sqrMagnitude > 0) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { if (focus != i) { focus = i; Mark(); } break; }
                if (mouse.leftButton.wasPressedThisFrame) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { Press(i); return; }
            }
            int dy = 0, dx = 0;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dy = -1; if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy = 1; if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1; if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) dy = -1; if (pad.dpad.down.wasPressedThisFrame) dy = 1; if (pad.dpad.left.wasPressedThisFrame) dx = -1; if (pad.dpad.right.wasPressedThisFrame) dx = 1; if (pad.leftShoulder.wasPressedThisFrame) { tab = (tab + 5) % 6; sel = -1; key = null; } if (pad.rightShoulder.wasPressedThisFrame) { tab = (tab + 1) % 6; sel = -1; key = null; } }
            if ((dx != 0 || dy != 0) && btns.Count > 0)
            {
                if (focus < 0) focus = 0;
                else
                {
                    var at = Centre(btns[focus].r); var move = new Vector2(dx, -dy); int best = -1; float bd = 1e9f;
                    for (int i = 0; i < btns.Count; i++) { if (i == focus || !btns[i].r.gameObject.activeInHierarchy) continue; var d = Centre(btns[i].r) - at; if (Vector2.Dot(d.normalized, move) < 0.5f) continue; float s = d.magnitude * (1 + 2 * Mathf.Abs(Vector2.Dot(d.normalized, new Vector2(move.y, -move.x)))); if (s < bd) { bd = s; best = i; } }
                    if (best >= 0) focus = best;
                }
                Mark();
            }
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame) && focus >= 0 && focus < btns.Count) Press(focus);
        }
        bool Hit(RectTransform r, Vector2 sp) => r != null && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, sp, cam);
        static Vector2 Centre(RectTransform r) { var c = r.TransformPoint(r.rect.center); return new Vector2(c.x, c.y); }
        void Press(int i) { focus = i; Snd.Play2D(Synth.Tik, 0.25f, 1.2f); var a = btns[i].a; key = null; a(); }
        void Mark() { for (int i = 0; i < btns.Count; i++) { var f = btns[i].r != null ? btns[i].r.Find("focus") : null; if (f != null) f.gameObject.SetActive(i == focus); } }
        public bool Tap(string label) { foreach (var b in btns) if (b.r != null && b.r.name.StartsWith("btn " + label)) { key = null; b.a(); return true; } return false; }

        // ---- what's on the right for this tab: the preset's own, then what I own, the story's, then Hartley's (greyed)
        struct Choice { public string id, name, sub; public bool own, owned, story, locked; public int price; }
        List<Choice> Choices()
        {
            var l = new List<Choice>(); string pn = Catalog.Preset[preset];
            l.Add(new Choice { id = null, name = pn + "'s own", sub = "as they came", own = true, owned = true });
            foreach (var w in Catalog.All)
            {
                if (w.Fit != Fit.Wear || w.Slot != tab) continue;
                bool have = Mine.Owned.Contains(w.Id) || w.Id == "wear:none";
                if (w.Id == "wear:walt") { l.Add(new Choice { id = w.Id, name = "Walt's cap", sub = "from Walt's journal", story = true, owned = have, locked = !have }); continue; }
                l.Add(new Choice { id = w.Id, name = w.Name, sub = have ? (w.Id == "wear:none" ? "just the top" : "owned") : $"Hartley's · ${w.Price}", owned = have, locked = !have, price = w.Price });
            }
            l.Sort((a, b) => (a.own ? 0 : a.owned ? 1 : a.story ? 2 : 3).CompareTo(b.own ? 0 : b.owned ? 1 : b.story ? 2 : 3));
            return l;
        }
        void Draw()
        {
            btns.RemoveAll(b => b.r == null || b.r.IsChildOf(cards) || b.r.IsChildOf(items) || b.r.IsChildOf(swatches));
            foreach (Transform t in cards) Destroy(t.gameObject); foreach (Transform t in items) Destroy(t.gameObject); foreach (Transform t in swatches) Destroy(t.gameObject);
            int mine = Mine.Preset >= 0 ? Mine.Preset : Angler.Body;
            // the crew cards: 2 x 3
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 2) * 172, y = (i / 2) * 232; string taken = TakenBy(i); bool me = i == mine, on = i == preset;
                var c = UiKit.Card(cards, "card", 18, on ? Ponyo.Hex("FFF4D6") : Ponyo.Paper, !string.IsNullOrEmpty(taken) ? false : true, on ? Ponyo.Ink : Ponyo.Hex("E2D3BB"), on ? 4 : 2);
                c.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(160, 222), new Vector2(0, 1));
                var pic = UiKit.Rect("pic", c).Place(new Vector2(0, 1), new Vector2(4, -4), new Vector2(152, 142), new Vector2(0, 1)).gameObject.AddComponent<RawImage>(); pic.texture = portraits[i]; pic.raycastTarget = false;
                if (!string.IsNullOrEmpty(taken)) pic.color = new Color(1, 1, 1, 0.55f);
                T(c, Catalog.Preset[i] == "Gus" ? "Old Gus" : Catalog.Preset[i], UiKit.DisplayBold, 22, string.IsNullOrEmpty(taken) ? Ponyo.Ink : Ponyo.InkSoft, 12, 152, 140, 28);
                T(c, Blurb[i], UiKit.Body, 13, Ponyo.InkSoft, 12, 180, 140, 38);
                if (me || !string.IsNullOrEmpty(taken))
                {
                    string lab = me ? "YOU" : taken; var bc = me ? Ponyo.Leaf : Ponyo.Poppy;
                    var bd = UiKit.Card(c, "badge", 11, bc, false, Color.Lerp(bc, Ponyo.Line, 0.4f), 0); float bw = 18 + lab.Length * 8.5f; bd.Place(new Vector2(1, 1), new Vector2(-8, -10), new Vector2(bw, 24), new Vector2(1, 1));
                    var bt = UiKit.Txt(bd, lab, UiKit.Bold, 12, Ponyo.Paper, TextAnchor.MiddleCenter); bt.rectTransform.Stretch();
                }
                int ii = i; Btn(c, "crew " + Catalog.Preset[i], () => { if (preset != ii) { preset = ii; sel = -1; yaw = Front; } else Pick(ii); });
            }
            crewName.text = Catalog.Preset[preset] == "Gus" ? "Old Gus" : Catalog.Preset[preset]; crewLine.text = Line[preset];
            int slot = MySlot; colourDot.color = Coop.Colours[slot];
            colourT.text = $"Your colour: {ColourName[slot % 4]} · armband + back patch, always on";
            resetT.text = "Reset to " + (Catalog.Preset[mine] == "Gus" ? "Gus" : Catalog.Preset[mine]);
            // the tabs
            float tx = 0;
            for (int s = 0; s < 6; s++)
            {
                int ss = s; bool on = s == tab; float w = 70 + (s == 5 ? 10 : 0);
                var p = UiKit.Card(items, "tab", 18, on ? Ponyo.Paper : new Color(0, 0, 0, 0), on, on ? Ponyo.Ink : new Color(0, 0, 0, 0), on ? 3 : 0); p.Place(new Vector2(0, 1), new Vector2(tx, -4), new Vector2(w, 42), new Vector2(0, 1));
                var t = UiKit.Txt(p, Catalog.SlotName[s], on ? UiKit.Bold : UiKit.Body, 17, on ? Ponyo.Ink : Ponyo.InkSoft, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
                Btn(p, "tab " + Catalog.SlotName[s], () => { tab = ss; sel = -1; });
                tx += w + 14;
            }
            // the item cards: 3 across
            var ch = Choices(); string worn = mine == preset ? Mine.Wear[tab] : null; float iy = 64; int shown = 0;
            for (int i = 0; i < ch.Count && shown < 6; i++, shown++)
            {
                var c = ch[i]; float x = (shown % 3) * 172, y = iy + (shown / 3) * 162; bool wearing = (c.own && worn == null) || (!c.own && c.id == worn); bool on = i == sel || (sel < 0 && wearing);
                var r = UiKit.Card(items, "item", 18, c.locked ? Ponyo.Hex("F7EEDD") : on ? Ponyo.Hex("FFF4D6") : Ponyo.Paper, !c.locked, on ? Ponyo.Ink : c.locked ? Ponyo.Hex("DCCBB0") : Ponyo.Hex("E2D3BB"), on ? 4 : 2);
                r.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(160, 152), new Vector2(0, 1));
                var g = UiKit.Img(r, Glyphs.Get(new[] { "hat", "shirt", "coat", "legs", "boot", "glove" }[tab]), c.locked ? Ponyo.Hex("CDBFA8") : c.own ? Ponyo.InkSoft : Colour(c.id ?? ""), "g"); g.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(64, 64), new Vector2(0.5f, 1));
                T(r, c.name, UiKit.Bold, 16, c.locked ? Ponyo.InkSoft : Ponyo.Ink, 8, 90, 144, 22, TextAnchor.UpperCenter);
                T(r, wearing ? "wearing" : c.sub, UiKit.Body, 13, c.locked ? Ponyo.Hex("B07A1E") : Ponyo.InkSoft, 8, 114, 144, 20, TextAnchor.UpperCenter);
                if (c.story) { var b = UiKit.Card(r, "story", 10, Ponyo.Sunflower, false, Ponyo.Hex("B88A1E"), 0); b.Place(new Vector2(0, 1), new Vector2(8, -8), new Vector2(60, 20), new Vector2(0, 1)); var bt = UiKit.Txt(b, "STORY", UiKit.Bold, 11, Ponyo.Ink, TextAnchor.MiddleCenter); bt.rectTransform.Stretch(); }
                int ii = i; Btn(r, "item " + (c.id ?? "own"), () => { sel = ii; });
            }
            // dyes: as made + this slot's colour now; more at Hartley's
            var dy = UiKit.Rect("dye", swatches).Place(new Vector2(0, 1), new Vector2(0, -24), new Vector2(510, 200), new Vector2(0, 1));
            string what = worn != null ? Catalog.Get(worn)?.Name : Catalog.Preset[preset] + "'s " + Catalog.SlotName[tab].ToLower();
            T(dy, UiKit.Caps("Dye · " + what), UiKit.Bold, 13, Ponyo.InkSoft, 0, 0, 500, 18);
            int cur = mine == preset ? Mine.Dye[tab] : -1;
            Swatch(dy, 0, 30, Colour(worn ?? ""), cur < 0, "as made", () => { if (mine == preset && Mine.Dye[tab] >= 0) Works.Wear(Mine.Wear[tab], tab); });
            if (cur >= 0) Swatch(dy, 56, 30, Ponyo.Hex(Catalog.Swatch[cur]), true, Catalog.SwatchName[cur], () => { });
            T(dy, "+ dyes at Hartley's, $25 each", UiKit.Body, 14, Ponyo.InkSoft, cur >= 0 ? 116 : 60, 40, 260, 22);
            var info = UiKit.Card(dy, "info", 12, Ponyo.Hex("FBF1DE"), false, Ponyo.Hex("E5D3B3"), 0); info.Place(new Vector2(0, 1), new Vector2(0, -92), new Vector2(510, 62), new Vector2(0, 1));
            UiKit.Img(info, Glyphs.Get("pin"), Ponyo.InkSoft, "pin").rectTransform.Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(20, 20), new Vector2(0, 0.5f));
            T(info, "Greyed items are sold at <b>Hartley's Outfitters</b>, Old Saltmouth, 9 AM – 7 PM. Clothes only: they never change how you fish.", UiKit.Body, 14, Ponyo.InkSoft, 44, 10, 450, 46);
            Mark();
        }
        void Swatch(RectTransform p, float x, float y, Color c, bool on, string label, Action a)
        {
            var s = UiKit.Card(p, "sw", 22, c, false, on ? Ponyo.Ink : Ponyo.Hex("CDBFA8"), 0); s.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(44, 44), new Vector2(0, 1));
            Btn(s, "dye " + label, a);
        }
        string TakenBy(int c)
        {
            if (!Coop.On || Coop.I == null) return null;
            foreach (var e in Coop.I.Roster) if (e.Crew == c && e.Id != Coop.Me) return $"{e.Name} (P{e.Slot + 1})";
            return null;
        }

        // ---- actions
        void Pick(int p)
        {
            var t = TakenBy(p); if (t != null) { Alerts.Push("Wardrobe", $"{t} is {Catalog.Preset[p]}. Pick another.", Alerts.Kind.Info); Snd.Play2D(Synth.Miss, 0.3f, 1); return; }
            var why = Works.Pick(p); if (!string.IsNullOrEmpty(why)) { Alerts.Push("Wardrobe", why, Alerts.Kind.Info); return; }
            Snd.Play2D(Synth.Chime, 0.35f, 1.1f); Log("picked " + Catalog.Preset[p]);
        }
        void WearIt()
        {
            int mine = Mine.Preset >= 0 ? Mine.Preset : Angler.Body;
            if (preset != mine) { Pick(preset); return; }
            var ch = Choices(); if (sel < 0 || sel >= ch.Count) { Hide(); return; }
            var c = ch[sel];
            if (c.locked) { Alerts.Push("Wardrobe", c.story ? "Walt's cap is in his journal somewhere." : $"Hartley's sells it: ${c.price}.", Alerts.Kind.Info); Snd.Play2D(Synth.Miss, 0.3f, 1); return; }
            Works.Wear(c.own ? null : c.id, tab); Snd.Play2D(Synth.Pluck, 0.3f, 1.2f); sel = -1;
            Log($"wears {(c.own ? Catalog.Preset[mine] + "'s own" : c.id)} ({Catalog.SlotName[tab]})");
        }
        void ResetClothes() { for (int s = 0; s < 6; s++) Works.Wear(null, s); Snd.Play2D(Synth.Whoosh, 0.25f, 1.2f); sel = -1; Log("reset to the preset"); }
    }
}
