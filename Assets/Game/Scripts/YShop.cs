using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M5: one shop screen for Doyle Boatworks, Plank & Post and Hartley's Outfitters (mockup 04-shop, in its 1920x1080
    // pixels). Left: the keeper, the sign, their line; category tabs (Q / LB RB); a grid of item cards (price, what it
    // does, OWNED / 07:00 / NEED $ / locked). Right: a live preview of the thing (the real boat for Doyle's, the item on a
    // turntable or its set spot for Plank & Post, a mirror camera on you at Hartley's), the stats before -> after, the
    // colour / pattern / slot pickers, the price and the bank before -> after, and the big Buy button (hold E 0.6 s, or
    // click). Over the crew's limit it says "Ask the crew" and asks instead. A buy: ka-ching, confetti-lite, the keeper's
    // bark, and in the world the thing pops into its spot / Doyle's lads walk over with their rollers.
    public class YShop : MonoBehaviour
    {
        public static YShop I;
        public static bool Open => I != null && I.shop != null;
        public static Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] yshop: " + s); OnLog?.Invoke(s); }
        static Company C => Firm.C;
        const float View = 548, W = 1724, H = 936, L = 40, GridW = 1172, RX = 1288, RW = 402;   // card, left pad, grid width, right pane x / width

        Works.Counter5 shop; string cat; public Ware Sel; int sw = -1, pat, slot; bool rush;
        RectTransform root, left, grid, right, opts, quote, buyBtn; Text title, caption, quoteWho, quoteTxt, nameT, descT, totalL, totalT, bankT, buyT, footL, footR, footLC, footRC;
        Image avatar, avatarIco, buyFill; RawImage preview; Camera cam, pcam; RenderTexture rt; Transform stage, stageItem; string stageFor;
        readonly List<(RectTransform r, Action a, bool hold)> btns = new(); int focus = -1; float hold; bool holdDone;
        string key; float scroll, contentH; float barkT; readonly List<string> cats = new();
        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        static string Money(int v) => "$" + v.ToString("N0", Inv);

        public static void Create() { var g = new GameObject("YShop"); I = g.AddComponent<YShop>(); I.Build(); }

        // ---------------------------------------------------------------- the frame
        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            root = UiKit.Card(hud, "yshop", 36, Ponyo.Paper, true, Ponyo.Line, 6); root.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(W, H));
            // above the HUD's toasts and tutorial hints (they used to sit over the shop header)
            var oc = root.gameObject.AddComponent<Canvas>(); oc.overrideSorting = true; oc.sortingOrder = 20; root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var esc = UiKit.Key(root, "Esc", 34); esc.Place(new Vector2(1, 1), new Vector2(-122, 52), esc.sizeDelta, new Vector2(1, 0.5f));
            T(root, "Leave shop", UiKit.Body, 17, Ponyo.Paper, W - 116, -40, 120, 26).alignment = TextAnchor.MiddleLeft;
            // the right pane: a slightly cooler paper, a dashed rule between
            var rp = UiKit.Img(root, null, Ponyo.Hex("F4F1EA"), "right bg"); rp.rectTransform.Place(new Vector2(0, 1), new Vector2(RX - 34, -2), new Vector2(W - RX + 32, H - 4), new Vector2(0, 1));
            for (float y = 20; y < H - 20; y += 22) UiKit.Img(root, null, Ponyo.Hex("D8C7AE"), "dash").rectTransform.Place(new Vector2(0, 1), new Vector2(RX - 36, -y), new Vector2(2, 12), new Vector2(0, 1));
            // the keeper
            var av = UiKit.Card(root, "avatar", 42, Ponyo.Peach, false, Ponyo.Line, 0); av.Place(new Vector2(0, 1), new Vector2(L, -52), new Vector2(84, 84), new Vector2(0, 1));
            avatar = UiKit.Fill(av); avatarIco = UiKit.Img(av, Glyphs.Get("anchor"), Ponyo.Ink, "ico"); avatarIco.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
            caption = T(root, "", UiKit.Bold, 14, Ponyo.InkSoft, L + 104, 62, 640, 20);
            title = T(root, "", UiKit.DisplayBold, 46, Ponyo.Ink, L + 102, 82, 700, 60);
            quote = UiKit.Card(root, "quote", 14, Ponyo.Hex("FBF1DE"), false, Ponyo.Hex("E5D3B3"), 0); quote.Place(new Vector2(0, 1), new Vector2(782, -36), new Vector2(430, 116), new Vector2(0, 1));
            quoteWho = T(quote, "", UiKit.Bold, 13, Ponyo.InkSoft, 18, 14, 390, 18); quoteTxt = T(quote, "", UiKit.Body, 16, Ponyo.Ink, 18, 36, 396, 76);
            // the grid (scrolls under a mask)
            var body = UiKit.Rect("body", root).Place(new Vector2(0, 1), new Vector2(L - 6, -236), new Vector2(GridW + 12, View), new Vector2(0, 1)); body.gameObject.AddComponent<RectMask2D>();
            grid = UiKit.Rect("grid", body).Place(new Vector2(0, 1), new Vector2(6, 0), new Vector2(GridW, 10), new Vector2(0, 1));
            // the bottom cards
            var fl = UiKit.Card(root, "foot left", 18, Ponyo.Hex("EAF7F4"), false, Ponyo.Hex("9FD8CF"), 2); fl.Place(new Vector2(0, 0), new Vector2(L, 30), new Vector2(660, 104), new Vector2(0, 0));
            footLC = T(fl, "", UiKit.Bold, 13, Ponyo.InkSoft, 22, 16, 620, 18); footL = T(fl, "", UiKit.Body, 17, Ponyo.Ink, 22, 38, 620, 60);
            var fr = UiKit.Card(root, "foot right", 18, Ponyo.Paper, false, Ponyo.Hex("E2D3BB"), 2); fr.Place(new Vector2(0, 0), new Vector2(L + 680, 30), new Vector2(GridW - 680, 104), new Vector2(0, 0));
            footRC = T(fr, "THE COMPANY", UiKit.Bold, 13, Ponyo.InkSoft, 22, 16, 440, 18); footR = T(fr, "", UiKit.Body, 17, Ponyo.Ink, 22, 38, 450, 60);
            // the right pane
            right = UiKit.Rect("right", root).Place(new Vector2(0, 1), new Vector2(RX, 0), new Vector2(RW, H), new Vector2(0, 1));
            var pv = UiKit.Card(right, "preview", 22, Ponyo.Hex("CFE9F6"), false, Ponyo.Line, 0); pv.Place(new Vector2(0, 1), new Vector2(0, -36), new Vector2(RW, 226), new Vector2(0, 1));
            preview = UiKit.Rect("rt", pv).gameObject.AddComponent<RawImage>(); preview.rectTransform.Stretch(4); preview.raycastTarget = false; preview.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UiKit.Edge(pv).transform.SetAsLastSibling();
            nameT = T(right, "", UiKit.DisplayBold, 34, Ponyo.Ink, 0, 280, RW, 46);
            descT = T(right, "", UiKit.Body, 16, Ponyo.InkSoft, 0, 330, RW, 70);
            opts = UiKit.Rect("opts", right).Place(new Vector2(0, 1), new Vector2(0, -408), new Vector2(RW, 380), new Vector2(0, 1));
            totalL = T(right, "", UiKit.Body, 16, Ponyo.InkSoft, 0, 790, 260, 24); totalL.alignment = TextAnchor.LowerLeft;
            totalT = T(right, "", UiKit.DisplayBold, 38, Ponyo.Ink, RW - 200, 772, 200, 46); totalT.alignment = TextAnchor.LowerRight;
            buyBtn = UiKit.Card(right, "buy", 18, Ponyo.Sunflower, true, Ponyo.Hex("B88A1E"), 5); buyBtn.Place(new Vector2(0, 1), new Vector2(0, -826), new Vector2(RW, 62), new Vector2(0, 1));
            buyFill = UiKit.Fill(buyBtn);
            var hk = UiKit.Key(buyBtn, "E", 34); hk.Place(new Vector2(0.5f, 0.5f), new Vector2(-62, 0), hk.sizeDelta);
            buyT = UiKit.Txt(buyBtn, "Buy", UiKit.DisplayBold, 24, Ponyo.Ink, TextAnchor.MiddleLeft); buyT.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-38, 0), new Vector2(260, 40), new Vector2(0, 0.5f));
            bankT = T(right, "", UiKit.Body, 15, Ponyo.InkSoft, 0, 900, RW, 22); bankT.alignment = TextAnchor.UpperCenter;
            Btn(buyBtn, "Buy", Buy, true);
            root.gameObject.SetActive(false);
            BuildPreview();
            Works.Done += OnDone;
        }

        // ---------------------------------------------------------------- the preview camera
        void BuildPreview()
        {
            rt = new RenderTexture(804, 452, 24) { name = "PB_ShopPreview", antiAliasing = 4 };
            var g = new GameObject("ShopPreviewCam"); g.transform.SetParent(transform, false); pcam = g.AddComponent<Camera>();
            pcam.targetTexture = rt; pcam.fieldOfView = 30; pcam.nearClipPlane = 0.05f; pcam.farClipPlane = 400; pcam.enabled = false;
            var main = CameraRig.I != null ? CameraRig.I.Cam : Camera.main;
            if (main != null) { var d = main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); var pd = g.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (d != null) { pd.renderPostProcessing = true; pd.antialiasing = d.antialiasing; } pcam.cullingMask = main.cullingMask & ~(1 << 5); }
            preview.texture = rt;
            // the turntable: far under the bay, its own little floor
            stage = new GameObject("ShopStage").transform; stage.position = new Vector3(0, -600, 0);
            var floor = Firm.Box(stage, "floor", new Vector3(0, -0.05f, 0), new Vector3(5, 0.1f, 5), Ponyo.Hex("EADBC0"));
            var lt = new GameObject("stage light").AddComponent<Light>(); lt.transform.SetParent(stage, false); lt.transform.localPosition = new Vector3(1.5f, 3, -2); lt.type = LightType.Point; lt.range = 12; lt.intensity = 3; lt.color = Ponyo.Hex("FFF1DC");
            stage.gameObject.SetActive(false);
        }
        enum PMode { None, Boat, Spot, Stage, Mirror }
        PMode pmode; Skiff pBoat; Transform pSpot; float orbit;
        void SetPreview()
        {
            var w = Sel; pmode = PMode.None; pBoat = null; pSpot = null; Works.TryPaint = null; Wardrobe.TryOn = null; Wardrobe.TryDye = null;
            if (shop == null) return;
            if (shop.S == Store.Hartley) pmode = PMode.Mirror;
            else if (w != null && w.At == Store.Doyle && w.Lock == null) { pBoat = Skiff.Of(w.Boat ?? (w.Id == "boat:kestrel" ? "kestrel" : "biscuit")); pmode = pBoat != null ? PMode.Boat : PMode.None; }
            else if (w != null && w.At == Store.Plank)
            {
                var spot = Works.SpotOf(w.Id);
                bool prop = w.Id.StartsWith("furn:") || w.Id.StartsWith("bed:");
                if (prop) { pmode = PMode.Stage; StageItem(w.Id); }
                else if (spot != null && Works.I != null && Works.I.Spots.TryGetValue(spot, out var t)) { pmode = PMode.Spot; pSpot = t; }
                else if (w.Id.StartsWith("office:") && Works.I?.Office != null) { pmode = PMode.Spot; pSpot = Works.I.Office; }
            }
            if (w != null && w.Fit == Fit.Paint && sw >= 0) Works.TryPaint = (w.Boat, w.Ch, sw, pat);
            if (w != null && w.Fit == Fit.Wear) Wardrobe.TryOn = w.Id;
            if (w != null && w.Fit == Fit.Dye && sw >= 0) Wardrobe.TryDye = (slot, sw);
            stage.gameObject.SetActive(pmode == PMode.Stage);
            pcam.enabled = pmode != PMode.None;
            preview.color = pmode == PMode.None ? new Color(1, 1, 1, 0) : Color.white;
        }
        void StageItem(string item)
        {
            if (stageFor == item) return; stageFor = item;
            if (stageItem != null) Destroy(stageItem.gameObject);
            string id = item.Substring(item.IndexOf(':') + 1);
            var pf = Works.PropOf(item);
            GameObject g;
            if (pf != null) { g = Instantiate(pf); Util.Colliders(g); Util.Toon(g, Game.ToonMat);   // Colliders: hides the _colBox meshes (they showed as a white box)
                foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c); }
            else { g = new GameObject(item); Firm.Box(g.transform, "stand-in", new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.8f, 0.8f), Ponyo.Hex("C9A27A")); }
            stageItem = g.transform; stageItem.SetParent(stage, false); stageItem.localPosition = Vector3.zero;
            StartCoroutine(Works.Pop(g));
        }
        void LateUpdate()
        {
            if (shop == null || !pcam.enabled) return;
            orbit += Time.unscaledDeltaTime * 14; pcam.nearClipPlane = 0.05f;
            switch (pmode)
            {
                case PMode.Boat:
                {
                    var b = pBoat; var h = b.Hull; var c = b.transform.TransformPoint(h.center); float r = Mathf.Max(h.size.z, h.size.x) * 1.05f + 2.2f;
                    float a = (orbit + 35) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    var p = c + b.transform.rotation * dir * r + Vector3.up * (r * 0.42f);
                    pcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(c - p + Vector3.up * 0.2f));
                    break;
                }
                case PMode.Spot:
                {
                    var t = pSpot; if (t == null) break; var rs = t.GetComponentsInChildren<Renderer>(); var bb = new Bounds(t.position + Vector3.up, Vector3.one * 2);
                    foreach (var rr in rs) bb.Encapsulate(rr.bounds);
                    float r = Mathf.Clamp(bb.extents.magnitude * 2.2f, 4, 30); float a = (orbit * 0.6f) * Mathf.Deg2Rad + Mathf.Atan2(t.forward.x, t.forward.z);
                    var p = bb.center + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r + Vector3.up * r * 0.5f;
                    pcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(bb.center - p));
                    break;
                }
                case PMode.Stage:
                {
                    if (stageItem == null) break; stageItem.localRotation = Quaternion.Euler(0, orbit * 1.6f, 0);
                    var rs = stageItem.GetComponentsInChildren<Renderer>(); var bb = rs.Length > 0 ? rs[0].bounds : new Bounds(stage.position, Vector3.one); foreach (var rr in rs) bb.Encapsulate(rr.bounds);
                    float r = Mathf.Max(1.4f, bb.extents.magnitude * 3.0f); var p = bb.center + new Vector3(0, r * 0.35f, -r);
                    pcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(bb.center - p));
                    break;
                }
                case PMode.Mirror:
                {
                    var me = Angler.I; if (me == null) break; var f = me.transform.forward; f.y = 0; f.Normalize();
                    float a = Mathf.Sin(orbit * 0.05f) * 35 * Mathf.Deg2Rad; var d = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0) * f;
                    var c = me.transform.position + Vector3.up * 1.2f; var p = c + d * 2.1f + Vector3.up * 0.05f;   // eye level, head and chest: the counter low in the frame, the shop sign above it
                    pcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(c - p));
                    // a mirror behind the counter: the counter, the wall and Mabel stand between, so the near plane starts
                    // where the clear air in front of me starts
                    float clear = Vector3.Distance(c, p);
                    foreach (var h in Physics.RaycastAll(c, p - c, clear)) if (!h.collider.transform.IsChildOf(me.transform) && h.distance < clear) clear = h.distance;
                    // Mabel (no collider) behind her counter stands on that line too: the near plane starts in front of her
                    if (Works.I != null) foreach (var k in Works.I.Keepers) if (k != null && k.Visible)
                    {
                        var kp = k.transform.position + Vector3.up * 1.1f - c; float along = Vector3.Dot(kp, d);
                        if (along > 0 && along < clear && (kp - d * along).magnitude < 0.7f) clear = Mathf.Max(0.3f, along - 0.45f);
                    }
                    pcam.nearClipPlane = Mathf.Max(0.05f, Vector3.Distance(c, p) - clear + 0.02f);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- open / close
        public static void Show(Works.Counter5 s) => I?.ShowI(s);
        void ShowI(Works.Counter5 s)
        {
            shop = s; cats.Clear(); foreach (var w in Catalog.Of(s.S)) if (!cats.Contains(w.Cat)) cats.Add(w.Cat);
            cat = cats.Count > 0 ? cats[0] : ""; Sel = null; key = null; scroll = 0; focus = 1; holdDone = true; barkT = 0;
            root.gameObject.SetActive(true); Ctl.Ui = 2; Snd.Play2D(Synth.Tik, 0.3f, 0.9f); Snd.Play2D(Synth.Chime, 0.18f, 1.3f);
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Color band = s.S == Store.Doyle ? Ponyo.Hex("F6B9A8") : s.S == Store.Plank ? Ponyo.Hex("CDE8C2") : Ponyo.Hex("E6D2F0");
            avatar.color = band; avatarIco.sprite = Glyphs.Get(s.S == Store.Doyle ? "anchor" : s.S == Store.Plank ? "house" : "hat");
            caption.text = UiKit.Caps($"{s.Owner} · {s.Place} · open {Hours(Catalog.Opens[(int)s.S])} – {Hours(Catalog.Closes[(int)s.S])}{(s.S != Store.Doyle ? " · shut Sundays" : "")}");
            title.text = s.Name; quoteWho.text = UiKit.Caps(s.Owner); quoteTxt.text = s.Line;
            Pick(FirstOf(cat));
            StartCoroutine(Bounce(root, 0.96f));
            Log("open " + s.Name);
        }
        static string Hours(double m) { int h = (int)(m / 60); return h == 12 ? "12 PM" : h > 12 ? $"{h - 12} PM" : $"{h} AM"; }
        public void Hide()
        {
            if (shop == null) return;
            Log("close " + shop.Name); shop = null; root.gameObject.SetActive(false); Ctl.Ui = 0; pcam.enabled = false; stage.gameObject.SetActive(false);
            Works.TryPaint = null; Wardrobe.TryOn = null; Wardrobe.TryDye = null;
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        Ware FirstOf(string c) { foreach (var w in Catalog.Of(shop.S)) if (w.Cat == c) return w; return null; }
        public void Tab(string c) { cat = c; scroll = 0; Pick(FirstOf(c)); }
        void NextTab(int d) { if (cats.Count == 0) return; Tab(cats[(cats.IndexOf(cat) + d + cats.Count) % cats.Count]); Snd.Play2D(Synth.Tik, 0.25f, 1.1f); }
        public void Pick(Ware w)
        {
            Sel = w; rush = false; slot = 0; pat = 0;
            if (w != null && w.Cat != cat) { cat = w.Cat; scroll = 0; }   // picking an item shows its own tab
            sw = -1;
            if (w != null && w.Fit == Fit.Paint) { var b = C.BoatOf(w.Boat); pat = b.Pattern; sw = -1; }
            if (w != null && w.Fit == Fit.Dye) slot = 1;
            key = null; SetPreview();
        }
        public void Swatch(int i) { sw = i; key = null; SetPreview(); Snd.Play2D(Synth.Pluck, 0.18f, 0.9f + (i % 8) * 0.05f); }
        public void PatternOf(int i) { pat = i; key = null; SetPreview(); }
        public void SlotOf(int i) { slot = i; key = null; SetPreview(); }

        int Arg => Sel == null ? 0 : Sel.Fit == Fit.Paint ? pat * 100 + Mathf.Max(0, sw) : Sel.Fit == Fit.Dye ? slot * 100 + Mathf.Max(0, sw) : 0;
        bool NeedsColour => Sel != null && (Sel.Fit == Fit.Paint || Sel.Fit == Fit.Dye) && sw < 0;

        // ---------------------------------------------------------------- every frame
        void Update()
        {
            if (shop == null) return;
            var kb = Keyboard.current; var pad = Gamepad.current; var me = Angler.I;
            if (Ctl.Escape || kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame || me == null || me.Swimming
                || (me.transform.position - shop.Front).magnitude > 3.5f || !shop.IsOpen(Sky.Minutes))
            { Ctl.UiEsc = Time.frameCount; if (!shop.IsOpen(Sky.Minutes)) Alerts.Push(shop.Name, "Closing up. Come back tomorrow.", Alerts.Kind.Info); Hide(); return; }
            if (barkT > 0) { barkT -= Time.unscaledDeltaTime; if (barkT <= 0) quoteTxt.text = shop.Line; }
            var bs = Sel != null && Sel.Boat != null ? C.BoatOf(Sel.Boat) : null;
            string k = $"{cat}|{Sel?.Id}|{sw}|{pat}|{slot}|{rush}|{C.Cash}|{C.Ledger.Count}|{C.Owned.Count}|{C.Orders.Count}|{C.Asks.Count}|{bs?.Job?.Start}|{(int)(Sky.Minutes / 10)}|{Works.OthersOnline}|{C.Spending}";
            if (k != key) { key = k; Draw(); }
            Nav(kb, pad);
            // the buy button's hold ring
            buyFill.color = Color.Lerp(BuyColour, Ponyo.Hex("FFE08A"), focus >= 0 && focus < btns.Count && btns[focus].r == buyBtn ? 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 5) : 0);
        }
        Color BuyColour = Ponyo.Sunflower;

        // ---------------------------------------------------------------- input (Shop.cs's way)
        void Nav(Keyboard kb, Gamepad pad)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float wh = mouse.scroll.ReadValue().y; if (wh != 0) Scroll(-Mathf.Sign(wh) * 90);
                var mp = mouse.position.ReadValue();
                if (mouse.delta.ReadValue().sqrMagnitude > 0) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { if (focus != i) { focus = i; Mark(); } break; }
                if (mouse.leftButton.wasPressedThisFrame) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { Press(i); return; }
            }
            if (kb != null && kb.qKey.wasPressedThisFrame) NextTab(kb.shiftKey.isPressed ? -1 : 1);
            if (pad != null) { if (pad.leftShoulder.wasPressedThisFrame) NextTab(-1); if (pad.rightShoulder.wasPressedThisFrame) NextTab(1); }
            int dy = 0, dx = 0;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dy = -1; if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy = 1; if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1; if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) dy = -1; if (pad.dpad.down.wasPressedThisFrame) dy = 1; if (pad.dpad.left.wasPressedThisFrame) dx = -1; if (pad.dpad.right.wasPressedThisFrame) dx = 1; }
            if ((dx != 0 || dy != 0) && btns.Count > 0)
            {
                if (focus < 0) focus = 0;
                else
                {
                    var at = Centre(btns[focus].r); var move = new Vector2(dx, -dy); int best = -1; float bd = 1e9f;
                    for (int i = 0; i < btns.Count; i++)
                    {
                        if (i == focus) continue; var d = Centre(btns[i].r) - at; if (Vector2.Dot(d.normalized, move) < 0.5f) continue;
                        float s = d.magnitude * (1 + 2 * Mathf.Abs(Vector2.Dot(d.normalized, new Vector2(move.y, -move.x)))); if (s < bd) { bd = s; best = i; }
                    }
                    if (best >= 0) focus = best;
                }
                Mark(); Reveal(focus);
                if (btns[focus].r.name.StartsWith("btn card ")) btns[focus].a();      // moving onto a card shows it on the right
            }
            bool down = kb != null && (kb.eKey.isPressed || kb.enterKey.isPressed) || pad != null && pad.buttonSouth.isPressed;
            if (!down) { holdDone = false; hold = 0; }
            else if (!holdDone && focus >= 0 && focus < btns.Count)
            {
                if (!btns[focus].hold) { holdDone = true; Press(focus); }
                else { hold += Time.unscaledDeltaTime; if (hold >= 0.6f) { holdDone = true; hold = 0; Press(focus); } }
            }
            // E with a card focused jumps to the Buy button (so E, E-hold buys what you're looking at)
            for (int i = 0; i < btns.Count; i++)
            {
                var ring = btns[i].r != null ? btns[i].r.Find("hold") : null; if (ring == null) continue;
                ring.GetComponent<Image>().fillAmount = i == focus && down && !holdDone ? hold / 0.6f : 0;
            }
        }
        void Scroll(float d) { scroll = Mathf.Clamp(scroll + d, 0, Mathf.Max(0, contentH - View)); grid.anchoredPosition = new Vector2(6, scroll); }
        void Reveal(int i)
        {
            if (i < 0 || i >= btns.Count || !btns[i].r.IsChildOf(grid)) return; var r = btns[i].r;
            float top = -grid.InverseTransformPoint(r.TransformPoint(new Vector3(0, r.rect.yMax))).y, bot = -grid.InverseTransformPoint(r.TransformPoint(new Vector3(0, r.rect.yMin))).y;
            if (top < scroll) Scroll(top - scroll - 10); else if (bot > scroll + View) Scroll(bot - scroll - View + 10);
        }
        bool Hit(RectTransform r, Vector2 sp) => r != null && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, sp, cam);
        static Vector2 Centre(RectTransform r) { var c = r.TransformPoint(r.rect.center); return new Vector2(c.x, c.y); }
        void Press(int i)
        {
            focus = i; var r = btns[i].r; var a = btns[i].a;
            if (r.name.StartsWith("btn card ") && r.name == "btn card " + Sel?.Id) { int b = btns.FindIndex(x => x.r == buyBtn); if (b >= 0) { focus = b; Mark(); } return; }
            Snd.Play2D(Synth.Tik, 0.25f, 1.2f); key = null; a();
        }
        void Mark()
        {
            for (int i = 0; i < btns.Count; i++) { var f = btns[i].r != null ? btns[i].r.Find("focus") : null; if (f != null) f.gameObject.SetActive(i == focus); }
        }
        public bool Tap(string label)
        {
            for (int i = 0; i < btns.Count; i++) if (btns[i].r != null && btns[i].r.name.StartsWith("btn " + label)) { focus = i; key = null; btns[i].a(); return true; }
            Log($"tap: no button \"{label}\""); return false;
        }

        // ---------------------------------------------------------------- drawing
        RectTransform Btn(RectTransform r, string label, Action a, bool holdToUse = false)
        {
            r.name = "btn " + label;
            var f = UiKit.Card(r, "focus", 20, new Color(0, 0, 0, 0), false, Ponyo.Sunflower); f.Stretch(-5); UiKit.Edge(f).color = Ponyo.Hex("E0A82E"); f.gameObject.SetActive(false);
            if (holdToUse)
            {
                var h = UiKit.Img(r, null, new Color(0.25f, 0.17f, 0.15f, 0.2f), "hold"); h.rectTransform.Stretch(); h.sprite = UiKit.Fill(r).sprite; h.type = Image.Type.Filled; h.fillMethod = Image.FillMethod.Horizontal; h.fillAmount = 0;
            }
            btns.Add((r, a, holdToUse)); return r;
        }
        static Text T(RectTransform p, string s, Font f, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft)
        { var t = UiKit.Txt(p, s, f, size, c, a); t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return t; }
        RectTransform Pill(RectTransform p, string label, float x, float y, float w, float h, Color fill, Color ink, Action a, string name = null, int size = 17, Sprite ico = null)
        {
            var c = UiKit.Card(p, "pill", h / 2, fill, true, Color.Lerp(fill, Ponyo.Line, 0.55f), 3); c.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1));
            var t = UiKit.Txt(c, label, UiKit.Bold, size, ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            if (ico != null) { t.rectTransform.offsetMin = new Vector2(26, 0); var i = UiKit.Img(c, ico, ink, "ico"); i.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(20, 20), new Vector2(0, 0.5f)); }
            return Btn(c, name ?? label, a);
        }

        void Draw()
        {
            // keep the buy button; rebuild the rest
            foreach (Transform t in grid) Destroy(t.gameObject);
            foreach (Transform t in opts) Destroy(t.gameObject);
            var tabs = root.Find("tabs"); if (tabs != null) Destroy(tabs.gameObject);
            btns.RemoveAll(b => b.r != buyBtn);
            DrawTabs(); DrawGrid(); DrawRight(); DrawFoot();
            if (focus >= btns.Count || focus < 0) focus = btns.Count > 0 ? 1 % btns.Count : -1;
            Mark();
        }
        void DrawTabs()
        {
            var tabs = UiKit.Rect("tabs", root).Place(new Vector2(0, 1), new Vector2(L, -166), new Vector2(GridW, 56), new Vector2(0, 1));   // under the sign, over the grid
            float x = 0;
            foreach (var c in cats)
            {
                var cc = c; float w = 34 + c.Length * 10.5f + 30; bool on = c == cat;
                Pill(tabs, c, x, 4, w, 46, on ? Ponyo.Paper : Ponyo.Paper2, on ? Ponyo.Ink : Ponyo.InkSoft, () => Tab(cc), "tab " + c, 17, Glyphs.Get(CatGlyph(c)));
                if (on) UiKit.Edge((RectTransform)tabs.GetChild(tabs.childCount - 1)).color = Ponyo.Ink;
                x += w + 10;
            }
            var q = UiKit.Key(tabs, "Q", 30); q.Place(new Vector2(1, 1), new Vector2(-96, -12), q.sizeDelta, new Vector2(1, 1));
            T(tabs, "switch tab", UiKit.Body, 15, Ponyo.InkSoft, GridW - 88, 16, 90, 24);
        }
        static string CatGlyph(string c) => c switch
        {
            "Boats" => "boat", "Repairs" => "wrench", "Upgrades" => "engine", "Paint" => "brush", "Piers & berths" => "plank", "Mooring amenities" => "lamp", "Storage" => "crate",
            "Living" => "bed", "Furniture" => "chair", "Office" => "house", "Hat" => "hat", "Top" => "shirt", "Over" => "coat", "Legs" => "legs", "Boots" => "boot", "Gloves" => "glove", "Dye" => "drop", "Story" => "star", _ => "plus"
        };
        string ItemGlyph(Ware w) => w.Fit == Fit.Wear ? CatGlyph(Catalog.SlotName[Math.Max(0, w.Slot)]) : w.Id.StartsWith("bed:") ? "bed" : w.Id.Contains("lamp") ? "lamp" : w.Id.StartsWith("furn:") ? "chair" : CatGlyph(w.Cat);

        // the cards: 3 across, 182 tall
        void DrawGrid()
        {
            const float cw = 377, ch = 182, gap = 20; int i = 0;
            var me = Firm.MyKey;
            foreach (var w in Catalog.Of(shop.S))
            {
                if (w.Cat != cat) continue;
                float x = (i % 3) * (cw + gap), y = 6 + (i / 3) * (ch + gap); i++;
                string why = C.WhyNotM5(me, w.Id, Default(w), Sky.Minutes);
                bool locked = w.Lock != null || why.StartsWith("Act ") || why.StartsWith("Buy the ") || why.StartsWith("Needs ") || why == "Not needed";
                bool owned = why == "Owned" || why == "Fitted" || why == "Done" || why == "You own it" || (w.Fit == Fit.Wear && C.OutfitOf(me).Owned.Contains(w.Id));
                bool sel = Sel == w;
                Color paper = sel ? Ponyo.Hex("FFF4D6") : locked ? Ponyo.Hex("F7EEDD") : Ponyo.Paper;
                var r = UiKit.Card(grid, "card", 22, paper, !locked, sel ? Ponyo.Hex("6E4E44") : locked ? Ponyo.Hex("DCCBB0") : Ponyo.Hex("E2D3BB"), sel ? 5 : 3);
                r.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(cw, ch), new Vector2(0, 1));
                // icon circle
                var ic = UiKit.Card(r, "icon", 32, locked ? Ponyo.Paper2 : IconBg(w), false, locked ? Ponyo.Hex("CDBFA8") : Ponyo.Line, 0); ic.Place(new Vector2(0, 1), new Vector2(18, -18), new Vector2(64, 64), new Vector2(0, 1));
                var g = UiKit.Img(ic, Glyphs.Get(ItemGlyph(w)), locked ? Ponyo.Hex("B5A99A") : Ponyo.Ink, "g"); g.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                if (w.Fit == Fit.Wear || w.Fit == Fit.Paint) { var dot = UiKit.Img(ic, UiKit.Circle, SwatchOf(w), "dot"); dot.rectTransform.Place(new Vector2(1, 0), new Vector2(4, -2), new Vector2(20, 20), new Vector2(1, 0)); UiKit.Img(dot.transform, UiKit.RingSprite, Ponyo.Line, "e").rectTransform.Stretch(-1); }
                string nm = w.Boat != null && w.Name.Contains(": ") ? w.Name.Substring(w.Name.IndexOf(": ") + 2) : w.Name;
                var nt = T(r, Cap(nm), UiKit.DisplayBold, 21, locked ? Ponyo.InkSoft : Ponyo.Ink, 96, 18, cw - 104 - 70, 52); nt.lineSpacing = 0.9f;
                T(r, w.Boat != null && w.Boat != "biscuit" || w.Id == "boat:kestrel" ? "Kestrel · " + w.Sub : w.Boat == "biscuit" ? "Biscuit · " + w.Sub : w.Sub, UiKit.Body, 14, Ponyo.InkSoft, 96, 72, cw - 112, 40);
                // chips: the first stat ("Speed 4.5 → 9")
                float cx = 18;
                foreach (var s in w.Stats)
                {
                    var b = s.Split('|'); if (b.Length < 3) continue; string lab = $"{b[0]} {b[1]}→{b[2]}{(b.Length > 3 ? b[3] : "")}";
                    float cwid = 22 + lab.Length * 7.4f; if (cx + cwid > cw - 18) break;
                    var chip = UiKit.Card(r, "chip", 13, locked ? Ponyo.Paper2 : Ponyo.Hex("F3ECDF"), false, Ponyo.Hex("D9C9AE"), 0); chip.Place(new Vector2(0, 1), new Vector2(cx, -112), new Vector2(cwid, 26), new Vector2(0, 1));
                    var ct = UiKit.Txt(chip, lab, UiKit.Bold, 12, Ponyo.InkSoft, TextAnchor.MiddleCenter); ct.rectTransform.Stretch();
                    cx += cwid + 6;
                }
                // price and badge
                int price = w.Fit == Fit.Paint ? w.Price : w.Price;
                if (locked)
                {
                    var lk = UiKit.Img(r, Glyphs.Get("lock"), Ponyo.InkSoft, "lock"); lk.rectTransform.Place(new Vector2(0, 0), new Vector2(18, 18), new Vector2(18, 18), new Vector2(0, 0));
                    T(r, why == "Not needed" ? "She came whole" : why, UiKit.Body, 14, Ponyo.Hex("B07A1E"), 42, ch - 38, cw - 60, 22);
                }
                else
                {
                    T(r, w.Fit == Fit.Dye ? Money(price) + " a colour" : w.Fit == Fit.Paint ? Money(price) + " a coat" : price == 0 ? "Free" : Money(price), UiKit.DisplayBold, 26, why.StartsWith("Need $") ? Ponyo.Poppy : Ponyo.Ink, 18, ch - 46, 200, 34);
                    string badge = owned ? (w.Fit == Fit.Wear ? "OWNED" : w.Id.StartsWith("up:") ? "FITTED" : w.Fit == Fit.Repair ? "DONE" : "OWNED")
                        : why.StartsWith("Built") || why.StartsWith("On the slip") ? "READY 07:00" : why.StartsWith("Need $") ? "NEED " + why.Substring(5).Replace(" more", "") : why.StartsWith("Doyle's crew") ? "PAINTING" : w.Fit == Fit.Overnight ? "BUILT OVERNIGHT" : w.Fit == Fit.Now && shop.S == Store.Plank ? "INSTALLS NOW" : "";
                    if (badge != "")
                    {
                        Color bc = owned ? Ponyo.Leaf : badge.StartsWith("NEED") ? Ponyo.Poppy : badge == "READY 07:00" || badge == "PAINTING" ? Ponyo.Night : Ponyo.Hex("D9C9AE");
                        Color bi = badge.StartsWith("BUILT") || badge.StartsWith("INSTALLS") ? Ponyo.InkSoft : Ponyo.Paper;
                        var bdg = UiKit.Card(r, "badge", 12, bc, false, Color.Lerp(bc, Ponyo.Line, 0.4f), 0); float bw = 20 + badge.Length * 8.2f;
                        bdg.Place(new Vector2(1, 0), new Vector2(-16, 22), new Vector2(bw, 24), new Vector2(1, 0));
                        var bt = UiKit.Txt(bdg, badge, UiKit.Bold, 11, bi, TextAnchor.MiddleCenter); bt.rectTransform.Stretch();
                    }
                }
                var ww = w; Btn(r, "card " + w.Id, () => Pick(ww));
            }
            contentH = 6 + ((i + 2) / 3) * (ch + gap); grid.sizeDelta = new Vector2(GridW, contentH);
            Scroll(0);
        }
        static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        static Color IconBg(Ware w) => w.At == Store.Doyle ? Ponyo.Hex("FBE1D8") : w.At == Store.Plank ? Ponyo.Hex("E4F1DC") : Ponyo.Hex("F0E4F6");
        Color SwatchOf(Ware w)
        {
            if (w.Fit == Fit.Paint) { var b = C.BoatOf(w.Boat); int s = b.Paint[w.Ch]; return s < 0 ? Ponyo.Hex(w.Ch == 3 ? "B88A5A" : w.Ch == 0 ? "F3EBDD" : w.Ch == 1 ? "3A86E0" : "9A7352") : Ponyo.Hex(w.Ch == 3 ? Catalog.Stain[s] : Catalog.Swatch[s]); }
            return Wardrobe.Colour(w.Id);
        }
        int Default(Ware w) => w.Fit == Fit.Paint ? (w == Sel ? Arg : C.BoatOf(w.Boat).Pattern * 100 + (C.BoatOf(w.Boat).Paint[w.Ch] + 1) % (w.Ch == 3 ? 8 : 24)) : w.Fit == Fit.Dye ? (w == Sel ? Arg : 100) : 0;

        // the right pane: the item, its stats, its options, the price, Buy
        void DrawRight()
        {
            var w = Sel;
            if (w == null) { nameT.text = ""; descT.text = "Nothing here yet."; totalT.text = ""; totalL.text = ""; bankT.text = ""; buyBtn.gameObject.SetActive(false); return; }
            nameT.text = Cap(w.Boat != null && w.Name.Contains(": ") ? w.Name.Substring(w.Name.IndexOf(": ") + 2) : w.Name);
            descT.text = (w.Boat != null ? (w.Boat == "kestrel" ? "For the Kestrel. " : "For Biscuit. ") : "") + w.Sub + (w.Line != null ? " " + w.Line : "") + Where(w);
            float y = 0;
            // stats before -> after
            foreach (var s in w.Stats)
            {
                var b = s.Split('|'); if (b.Length < 3) continue;
                var row = UiKit.Card(opts, "stat", 12, Ponyo.Paper, false, Ponyo.Hex("E2D3BB"), 0); row.Place(new Vector2(0, 1), new Vector2(0, -y), new Vector2(RW, 40), new Vector2(0, 1));
                T(row, b[0], UiKit.Body, 16, Ponyo.InkSoft, 14, 9, 160, 24);
                var v = T(row, $"{b[1]}  →  <b>{b[2]}</b> {(b.Length > 3 ? b[3] : "")}", UiKit.Body, 16, Ponyo.Ink, RW - 234, 9, 220, 24); v.alignment = TextAnchor.UpperRight;
                y += 48; if (y > 150) break;
            }
            // options
            if (w.Fit == Fit.Paint || w.Fit == Fit.Dye)
            {
                if (w.Fit == Fit.Paint && w.Ch == 1)
                {
                    T(opts, "PATTERN · " + UiKit.Caps(Catalog.Pattern[pat]), UiKit.Bold, 12, Ponyo.InkSoft, 0, y, RW, 18); y += 22;
                    for (int i = 0; i < Catalog.Patterns; i++) { int ii = i; var p = Pill(opts, (i + 1).ToString(), (i % 8) * 50, y, 44, 36, i == pat ? Ponyo.Night : Ponyo.Paper2, i == pat ? Ponyo.Paper : Ponyo.Ink, () => PatternOf(ii), "pattern " + i, 15); }
                    y += 46;
                }
                if (w.Fit == Fit.Dye)
                {
                    T(opts, "WHICH PIECE", UiKit.Bold, 12, Ponyo.InkSoft, 0, y, RW, 18); y += 22;
                    for (int i = 0; i < 6; i++) { int ii = i; Pill(opts, Catalog.SlotName[i], (i % 3) * 136, y + (i / 3) * 42, 128, 36, i == slot ? Ponyo.Night : Ponyo.Paper2, i == slot ? Ponyo.Paper : Ponyo.Ink, () => SlotOf(ii), "slot " + i, 15); }
                    y += 88;
                }
                bool stain = w.Fit == Fit.Paint && w.Ch == 3; int n = stain ? Catalog.Stain.Length : Catalog.Swatches;
                var bs = w.Fit == Fit.Paint ? C.BoatOf(w.Boat) : null;
                string nm = sw < 0 ? "pick one" : stain ? Catalog.StainName[sw] : Catalog.SwatchName[sw];
                T(opts, (stain ? "STAIN · " : "COLOUR · ") + UiKit.Caps(nm) + (bs != null && sw >= 0 && sw == bs.Last[w.Ch] ? " · FREE (THE ONE BEFORE)" : ""), UiKit.Bold, 12, Ponyo.InkSoft, 0, y, RW, 18); y += 24;
                int cols = 8; float d = 44, gap = (RW - cols * d) / (cols - 1);
                for (int i = 0; i < n; i++)
                {
                    int ii = i; float x = (i % cols) * (d + gap), yy = y + (i / cols) * (d + 8);
                    var c = UiKit.Card(opts, "sw", d / 2, Ponyo.Hex(stain ? Catalog.Stain[i] : Catalog.Swatch[i]), false, i == sw ? Ponyo.Ink : Ponyo.Hex("CDBFA8"), 0);
                    c.Place(new Vector2(0, 1), new Vector2(x, -yy), new Vector2(d, d), new Vector2(0, 1));
                    if (bs != null && bs.Paint[w.Ch] == i) { var ck = UiKit.Img(c, UiKit.Check, Ponyo.Paper, "now"); ck.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22)); }
                    if (i == sw) UiKit.Edge(c).color = Ponyo.Ink;
                    Btn(c, "swatch " + i, () => Swatch(ii));
                }
                y += ((n + cols - 1) / cols) * (d + 8);
                if (bs?.Job != null && Sky.Minutes < bs.Job.End)
                {
                    double m = Sky.Minutes; string st = m < bs.Job.Start + bs.Job.Walk ? "Kit and Arlo are walking over with the rollers…" : $"Painting: {(int)(bs.Job.Progress(m) * 100)}%";
                    T(opts, st, UiKit.Bold, 15, Ponyo.SeaDeep, 0, y + 4, RW, 22);
                }
            }
            else if (w.Fit == Fit.Repair && C.WhyNotM5(Firm.MyKey, w.Id, 0, Sky.Minutes) is var rw && (rw == "" || rw.StartsWith("Need") || rw.StartsWith("On the slip")))
            {
                T(opts, "WHEN", UiKit.Bold, 12, Ponyo.InkSoft, 0, y, RW, 18); y += 22;
                bool queued = C.Ordered(w.Id);
                if (!queued) Pill(opts, $"Ready 07:00 · {Money(w.Price)}", 0, y, 196, 40, !rush ? Ponyo.Night : Ponyo.Paper2, !rush ? Ponyo.Paper : Ponyo.Ink, () => { rush = false; key = null; }, "when overnight", 15);
                Pill(opts, $"Rush, now · {Money(queued ? w.Price : w.Price * 2)}", queued ? 0 : 206, y, 196, 40, rush || queued ? Ponyo.Night : Ponyo.Paper2, rush || queued ? Ponyo.Paper : Ponyo.Ink, () => { rush = true; key = null; }, "when rush", 15);
                if (queued) rush = true;
                y += 52;
            }
            // the price, the button
            int arg = Arg; bool instant = rush && w.Fit == Fit.Repair;
            string why = NeedsColour ? "Pick a colour" : C.WhyNotM5(Firm.MyKey, w.Id, arg, Sky.Minutes, instant);
            int price = C.PriceM5(Firm.MyKey, w.Id, arg) * (instant ? 2 : 1); if (instant && C.Ordered(w.Id)) price = w.Price;
            bool ask = why == "" && C.NeedsAsk(w.Id, arg, Firm.MyKey, Works.OthersOnline, instant);
            totalL.text = w.Fit == Fit.Paint ? (sw >= 0 && price == 0 ? "back to the one before" : "Doyle's crew paints her") : w.Fit == Fit.Overnight ? $"built tonight · ready 07:00{(w.Nights > 1 ? $" in {w.Nights} nights" : "")}" : w.Fit == Fit.Repair ? (instant ? "done now" : "on the slipway tonight") : w.Fit == Fit.Wear ? "yours to wear, any time" : w.Fit == Fit.Dye ? "dyed at once" : w.Fit == Fit.Boat ? "Doyle brings her round to the landing" : "installs itself at once";
            totalT.text = price == 0 ? "Free" : Money(price);
            buyBtn.gameObject.SetActive(true);
            BuyColour = why != "" ? Ponyo.Paper3 : ask ? Ponyo.Peach : Ponyo.Sunflower;
            buyFill.color = BuyColour;
            buyT.text = why != "" ? why : ask ? "Ask the crew" : w.Fit == Fit.Paint ? "Paint her" : w.Fit == Fit.Wear ? "Buy and wear" : w.Fit == Fit.Boat ? "Buy her" : "Buy";
            buyT.fontSize = buyT.text.Length > 18 ? 18 : 24;
            bankT.text = why != "" ? $"Company bank {Money(C.Cash)}" : ask ? $"Over {Money(Company.AskLimit(C.Cash))}: one crewmate's OK buys it" : $"Company bank {Money(C.Cash)} → {Money(C.Cash - price)}";
        }
        string Where(Ware w)
        {
            if (w.At != Store.Plank) return "";
            if (w.Id.StartsWith("dock:")) return " Goes on the landing.";
            if (w.Id.StartsWith("wh:")) return " The warehouse by the landing.";
            if (w.Id.StartsWith("bed:") || w.Id.StartsWith("furn:")) return " Goes in its place in the office.";
            return "";
        }
        void DrawFoot()
        {
            double m = Sky.Minutes;
            if (shop.S == Store.Doyle)
            {
                var b = Sel?.Boat != null ? Skiff.Of(Sel.Boat) : Skiff.I; var st = b != null ? C.BoatOf(b.Id) : null;
                footLC.text = UiKit.Caps(b != null ? b.Name + (b.Moored ? " · moored at the landing" : " · out on the water") : "Your boats");
                if (st?.Job != null && m < st.Job.End) footL.text = m < st.Job.Start + st.Job.Walk ? "Kit and Arlo are walking over with the rollers." : $"Being painted: {Catalog.Channel[st.Job.Ch].ToLower()}, {(int)(st.Job.Progress(m) * 100)}%. Watch her change.";
                else if (st != null) footL.text = $"Hull {PaintName(st, 0)} · stripe {PaintName(st, 1)} · trim {PaintName(st, 2)} · deck {PaintName(st, 3)}{(b.Id == "biscuit" ? (st.Engine ? " · engine rebuilt" : "") + (st.Hull ? " · hull patched" : "") : "")}";
            }
            else if (shop.S == Store.Plank)
            {
                footLC.text = "TONIGHT'S BUILDS · READY 07:00";
                var l = new List<string>(); foreach (var o in C.Orders) { var w = Catalog.Get(o.Item); if (w != null && w.At == Store.Plank) l.Add(w.Name); }
                footL.text = l.Count == 0 ? "Nothing on order. Furniture and beds go in at once; builds land at 7 in the morning." : string.Join(" · ", l);
            }
            else
            {
                var o = C.OutfitOf(Firm.MyKey); int p = o.Preset >= 0 ? o.Preset : Angler.Body;
                footLC.text = UiKit.Caps($"You · {Catalog.Preset[Mathf.Clamp(p, 0, 5)]}");
                int n = 0; foreach (var it in o.Owned) if (it.StartsWith("wear:")) n++;
                footL.text = $"{n} piece{(n == 1 ? "" : "s")} of Hartley's in your wardrobe. Clothes only: they never change how you fish. Change at any bed.";
            }
            string pol = C.Spending == Policy.Anyone ? "anyone can spend" : C.Spending == Policy.HostOnly ? "the host OKs every buy" : $"over {Money(Company.AskLimit(C.Cash))} the crew OKs it";
            int open = C.Asks.FindAll(a => a.St == AskSt.Open).Count;
            footR.text = $"Bank <b>{Money(C.Cash)}</b> · {(Works.OthersOnline ? pol : "solo: no asks")}{(open > 0 ? $"\n{open} ask{(open == 1 ? "" : "s")} waiting on the phone" : "")}";
        }
        static string PaintName(BoatSt st, int ch) => st.Paint[ch] < 0 ? "as built" : (ch == 3 ? Catalog.StainName[st.Paint[ch]] : Catalog.SwatchName[st.Paint[ch]]).ToLower();

        // ---------------------------------------------------------------- buying
        void Buy()
        {
            var w = Sel; if (w == null || shop == null) return;   // the shop shut (closing time) under the click
            int arg = Arg; bool instant = rush && w.Fit == Fit.Repair;
            string why = NeedsColour ? "Pick a colour first." : C.WhyNotM5(Firm.MyKey, w.Id, arg, Sky.Minutes, instant);
            if (why != "") { Snd.Play2D(Synth.Miss, 0.35f, 1); StartCoroutine(Shake(buyBtn)); Bark(Refuse(why)); return; }
            Works.Buy(w.Id, arg, instant);
        }
        public bool BuyNow(string item, int arg = 0, bool instant = false) { var w = Catalog.Get(item); if (w == null) return false; Pick(w); if (w.Fit == Fit.Paint || w.Fit == Fit.Dye) { sw = arg % 100; if (w.Fit == Fit.Paint) pat = arg / 100; else slot = arg / 100; } rush = instant; Buy(); return true; }
        void OnDone(string item, bool ok, string why)
        {
            if (shop == null) return;
            var w = Catalog.Get(item);
            if (!ok) { if (why != "") { Snd.Play2D(Synth.Miss, 0.35f, 1); StartCoroutine(Shake(buyBtn)); Bark(Refuse(why)); } else Bark("Asked the crew. One of them says OK and it's yours."); return; }
            Snd.Play2D(M1Assets.I.Kaching != null ? M1Assets.I.Kaching : Synth.Ting, 0.7f, 1);
            if (w != null && (w.Price >= 1000 || w.Fit == Fit.Boat)) Snd.Play2D(Synth.Fanfare, 0.35f, 1);
            StartCoroutine(Confetti()); StartCoroutine(Bounce(buyBtn, 0.86f));
            Bark(Thanks(w)); key = null;
            if (w != null && w.Fit == Fit.Paint) Alerts.Push("Doyle Boatworks", $"Kit and Arlo are on their way to {Skiff.Of(w.Boat)?.Name ?? "her"} with the rollers.", Alerts.Kind.Info);
        }
        void Bark(string s) { quoteTxt.text = s; barkT = 6; StartCoroutine(Bounce(quote, 0.94f)); }
        string Refuse(string why) => shop.S switch
        {
            Store.Doyle => why.StartsWith("Need $") ? $"{why}, I'm afraid. I take fish money, not promises." : why.StartsWith("Moor") ? why : why,
            Store.Plank => why.StartsWith("Need $") ? $"{why}. Timber isn't free, love." : why,
            _ => why.StartsWith("Need $") ? $"{why}, dear. It'll still be here." : why,
        };
        string Thanks(Ware w)
        {
            if (w == null) return "Pleasure.";
            if (w.Fit == Fit.Paint) return "Lovely choice. Kit and Arlo will have her done before your tea's gone cold.";
            if (w.Fit == Fit.Boat) return "She's yours. I'll bring her round to the landing myself. Treat her kindly.";
            if (w.Fit == Fit.Repair) return C.Ordered(w.Id) ? "On the slipway tonight. Ready by seven." : "Done. Listen to her purr.";
            if (w.Id.StartsWith("up:")) return "Fitted. Try not to hit anything for at least a day.";
            if (w.Fit == Fit.Overnight) return "My lads will be there at first light. Ready by seven.";
            if (w.Fit == Fit.Now && w.At == Store.Plank) return "It's in. Go and have a look, it's in its place.";
            if (w.Fit == Fit.Dye) return "There. Like it came that way.";
            return "Suits you. Wear it out, it's a crime to fold it.";
        }

        // ---- feedback: confetti-lite, a bounce, a shake
        IEnumerator Confetti()
        {
            var parts = new List<(RectTransform r, Vector2 v, float spin)>();
            var at = new Vector2(RX + RW / 2, -(826 + 31));
            Color[] cols = { Ponyo.Sunflower, Ponyo.Poppy, Ponyo.SeaC, Ponyo.Sky, Ponyo.Leaf, Ponyo.Peach };
            for (int i = 0; i < 34; i++)
            {
                var im = UiKit.Img(root, i % 3 == 0 ? UiKit.Circle : null, cols[i % cols.Length], "confetti"); var r = im.rectTransform;
                r.Place(new Vector2(0, 1), at + new Vector2(UnityEngine.Random.Range(-150f, 150f), 0), i % 3 == 0 ? new Vector2(12, 12) : new Vector2(8, 16), new Vector2(0.5f, 0.5f));
                parts.Add((r, new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(420f, 820f)), UnityEngine.Random.Range(-540f, 540f)));
            }
            for (float t = 0; t < 1.3f; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < parts.Count; i++)
                {
                    var (r, v, s) = parts[i]; if (r == null) continue; v.y -= 1500 * dt; v *= 1 - 0.8f * dt; parts[i] = (r, v, s);
                    r.anchoredPosition += v * dt; r.localRotation *= Quaternion.Euler(0, 0, s * dt);
                    r.GetComponent<Image>().color = new Color(r.GetComponent<Image>().color.r, r.GetComponent<Image>().color.g, r.GetComponent<Image>().color.b, Mathf.Clamp01((1.3f - t) / 0.4f));
                }
                yield return null;
            }
            foreach (var p in parts) if (p.r != null) Destroy(p.r.gameObject);
        }
        static IEnumerator Bounce(RectTransform r, float from)
        {
            if (r == null) yield break;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime) { float k = t / 0.35f; r.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1, Util.EaseOutBack(k)); yield return null; }
            r.localScale = Vector3.one;
        }
        static IEnumerator Shake(RectTransform r)
        {
            if (r == null) yield break; var p0 = r.anchoredPosition;
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime) { r.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 70) * 8 * (1 - t / 0.3f), 0); yield return null; }
            r.anchoredPosition = p0;
        }
    }
}
