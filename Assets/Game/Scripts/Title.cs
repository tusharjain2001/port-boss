using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // The title menu (M2 step 6, gdd systems §5.2): play solo, host a crew (Steam or this network), or join one (a friend
    // on Steam, or an address on this network). A paper card over the world, which waits (frozen) behind it. Mouse, keys
    // (arrows / W S, Enter, the number keys, Esc back) or a pad (d-pad, A, B). Not shown in the tests' batch mode unless
    // -pb-menutest asks for it; -pb-host / -pb-join skip it.
    public class Title : MonoBehaviour
    {
        public static Title I;
        public static bool Shown => I != null && I.root.gameObject.activeSelf;
        public static event Action Closed;                      // (Game holds its welcome tips until play starts)
        enum Page { Main, Friends, Address, Waiting, Worlds }
        class Row { public string Label, Sub; public Action Go; public bool On = true; public RectTransform R; public Image Fill; }

        RectTransform root, card, list; Text status, foot;
        Page page; readonly List<Row> rows = new(); int sel;
        string address; float waitT; string waitWhat;
        Camera cam;

        public static void Open()
        {
            if (I == null) { var g = new GameObject("Title"); I = g.AddComponent<Title>(); I.Build(); }
            I.root.gameObject.SetActive(true); I.Show(Page.Main);
            Time.timeScale = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public static void Close()
        {
            if (I == null || !Shown) return;
            I.root.gameObject.SetActive(false); Time.timeScale = 1;
            var c = Closed; Closed = null; c?.Invoke();
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I.UiCam;
            root = UiKit.Rect("title", hud); root.Stretch(); root.SetAsLastSibling();
            var dim = UiKit.Img(root, null, new Color(0.11f, 0.15f, 0.28f, 0.45f), "dim"); dim.rectTransform.Stretch();
            card = UiKit.Card(root, "card", 28, Ponyo.Paper, true); card.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(720, 660));
            var t = UiKit.Txt(card, "Port Boss", UiKit.DisplayBold, 66, Ponyo.Ink, TextAnchor.UpperCenter); t.rectTransform.Stretch(); t.rectTransform.offsetMax = new Vector2(0, -26);
            var sub = UiKit.Txt(card, "Fishing on Biscuit, alone or with up to three friends", UiKit.Body, 20, Ponyo.InkSoft, TextAnchor.UpperCenter); sub.rectTransform.Stretch(); sub.rectTransform.offsetMax = new Vector2(0, -110);
            list = UiKit.Rect("rows", card).Place(new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(620, 400), new Vector2(0.5f, 1));
            status = UiKit.Txt(card, "", UiKit.Bold, 18, Ponyo.Hex("C2452F"), TextAnchor.LowerCenter); status.rectTransform.Stretch(); status.rectTransform.offsetMin = new Vector2(30, 56);
            foot = UiKit.Txt(card, "", UiKit.Body, 17, Ponyo.InkSoft, TextAnchor.LowerCenter); foot.rectTransform.Stretch(); foot.rectTransform.offsetMin = new Vector2(30, 24);
            address = PlayerPrefs.GetString("m1.lastip", "127.0.0.1");
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnChar;
        }
        void OnDestroy() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnChar; }

        static string LocalAddress()
        {
            try { foreach (var a in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())) if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a)) return a.ToString(); }
            catch { }
            return "this PC's address";
        }

        void Show(Page p)
        {
            page = p; rows.Clear(); sel = 0;
            bool steam = Coop.SteamOk;
            switch (p)
            {
                case Page.Main:
                    // M4: Continue the last world, or a new one; hosting a crew hosts the world you're in (Continue first)
                    var worlds = Save.Worlds(); var last = worlds.Count > 0 ? worlds[0] : null;
                    if (last != null) rows.Add(new Row { Label = "Continue", Sub = $"{last.Name} · Day {last.Day} · ${last.Cash:N0} · saved {Save.Ago(last.Saved)}", Go = () => { if (Save.Load(last.Id)) Close(); } });
                    rows.Add(new Row { Label = last != null ? "New game" : "Play solo", Sub = "Walt's dock, Biscuit, $40 and a bag of worms", Go = () => { Save.NewWorld(); Close(); } });
                    if (worlds.Count > 1) rows.Add(new Row { Label = "Your worlds", Sub = $"{worlds.Count} saved, crew copies too", Go = () => Show(Page.Worlds) });
                    rows.Add(new Row { Label = "Host a crew on Steam", Sub = steam ? "Then invite friends from the pause menu (Esc, I)" : "Steam isn't running", On = steam, Go = () => { Coop.HostSteam(); Wait("Making a crew on Steam…"); } });
                    rows.Add(new Row { Label = "Join a friend on Steam", Sub = steam ? "Friends hosting right now, or accept their invite" : "Steam isn't running", On = steam, Go = () => Show(Page.Friends) });
                    rows.Add(new Row { Label = "Host on this network", Sub = $"Friends on the same network join {LocalAddress()}", Go = () => { Coop.HostLan(); Wait("Hosting on this network…"); } });
                    rows.Add(new Row { Label = "Join on this network", Sub = "Type the host's address", Go = () => Show(Page.Address) });
                    break;
                case Page.Friends:
                    foreach (var (name, lobby) in Coop.FriendsHosting()) { var l = lobby; rows.Add(new Row { Label = name + "'s crew", Sub = "Join them", Go = () => { _ = Coop.JoinLobby(l); Wait($"Joining {name}…"); } }); }
                    if (rows.Count == 0) rows.Add(new Row { Label = "Nobody's hosting right now", Sub = "Ask a friend to host and invite you", On = false });
                    rows.Add(new Row { Label = "Look again", Sub = "", Go = () => Show(Page.Friends) });
                    rows.Add(new Row { Label = "Back", Sub = "", Go = () => Show(Page.Main) });
                    break;
                case Page.Address:
                    rows.Add(new Row { Label = "Address: " + address, Sub = "Type it (a port after a colon if not 7777); Backspace deletes", Go = JoinAddress });
                    rows.Add(new Row { Label = "Join", Sub = "", Go = JoinAddress });
                    rows.Add(new Row { Label = "Back", Sub = "", Go = () => Show(Page.Main) });
                    break;
                case Page.Worlds:
                    var ws = Save.Worlds(); if (ws.Count > 6) ws.RemoveRange(6, ws.Count - 6);
                    foreach (var w in ws) { var id = w.Id; rows.Add(new Row { Label = w.Name, Sub = $"Day {w.Day} · ${w.Cash:N0} · saved {Save.Ago(w.Saved)}{(w.Copy ? " · a crew copy" : "")}", Go = () => { if (Save.Load(id)) Close(); } }); }
                    rows.Add(new Row { Label = "Back", Sub = "", Go = () => Show(Page.Main) });
                    break;
                case Page.Waiting:
                    rows.Add(new Row { Label = waitWhat, Sub = "", On = false });
                    rows.Add(new Row { Label = "Cancel", Sub = "", Go = () => { Coop.Leave(); Show(Page.Main); } });
                    sel = 1; break;
            }
            Draw();
        }

        void JoinAddress()
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            PlayerPrefs.SetString("m1.lastip", address); Coop.JoinLan(address.Trim()); Wait($"Joining {address}…");
        }

        void Wait(string what) { Coop.Status = ""; waitWhat = what; waitT = 0; Show(Page.Waiting); }

        void Draw()
        {
            foreach (Transform c in list) Destroy(c.gameObject);
            card.sizeDelta = new Vector2(720, Mathf.Max(380, 160 + Mathf.Min(rows.Count, 8) * 72 + 96)); // the card fits its rows (no dead strip under them)
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                r.R = UiKit.Card(list, "row" + i, 18, Ponyo.Paper2, false); r.R.Place(new Vector2(0.5f, 1), new Vector2(0, -i * 72), new Vector2(620, 62), new Vector2(0.5f, 1));
                r.Fill = UiKit.Fill(r.R);
                var l = UiKit.Txt(r.R, (page == Page.Main ? $"{i + 1}  " : "") + r.Label, UiKit.Bold, 25, r.On ? Ponyo.Ink : Ponyo.InkSoft, TextAnchor.MiddleLeft); l.rectTransform.Stretch(); l.rectTransform.offsetMin = new Vector2(24, r.Sub != "" ? 18 : 0);
                if (r.Sub != "") { var s = UiKit.Txt(r.R, r.Sub, UiKit.Body, 16, Ponyo.InkSoft, TextAnchor.MiddleLeft); s.rectTransform.Stretch(); s.rectTransform.offsetMin = new Vector2(24 + (page == Page.Main ? 30 : 0), 0); s.rectTransform.offsetMax = new Vector2(0, -26); }
                r.R.GetComponent<RectTransform>().localScale = Vector3.one;
            }
            Mark();
        }
        void Mark()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i]; bool on = i == sel && r.On;
                r.Fill.color = on ? Ponyo.Sunflower : r.On ? Ponyo.Paper2 : Ponyo.Paper3;
                r.R.localScale = Vector3.one * (on ? 1.02f : 1f);
            }
        }

        void OnChar(char c)
        {
            if (!Shown || page != Page.Address) return;
            if (c == '\b') { if (address.Length > 0) address = address.Substring(0, address.Length - 1); }
            else if (char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '-') address += c;
            else return;
            rows[0].Label = "Address: " + address; Draw();
        }
        // (tests) as if typed
        public void Type(string s) { address = ""; foreach (var c in s) OnChar(c); }
        public void Choose(string label) => Choose(rows.FindIndex(r => r.Label == label));
        public void Choose(int i) { if (i >= 0 && i < rows.Count && rows[i].On) { sel = i; Mark(); rows[i].Go(); } }
        public void GoTo(string p) => Show(p == "address" ? Page.Address : p == "friends" ? Page.Friends : Page.Main);

        void Update()
        {
            if (!Shown) return;
            if (root.GetSiblingIndex() != root.parent.childCount - 1) root.SetAsLastSibling(); // (over anything the HUD adds)
            foot.text = page == Page.Main ? $"Playing as {Coop.MyName}" + (Coop.SteamOk ? " on Steam" : "") : page == Page.Address ? "Enter joins · Esc back" : "Esc back";
            status.text = Coop.Status;
            if (page == Page.Waiting)
            {
                waitT += Time.unscaledDeltaTime;
                if (Coop.Joined && (!Coop.IsHost || !Coop.I.ViaSteam || Coop.Lobby != null || waitT > 6)) { Close(); return; }
                if (Coop.I == null && waitT > 0.5f || waitT > 12) { if (Coop.I != null) Coop.Leave(); if (Coop.Status == "") Coop.Status = "Couldn't reach them. Check the address, or that they're hosting."; Show(Page.Main); }
                // (a client that can't connect: NGO gives up after a few seconds and Coop goes away)
            }
            var kb = Keyboard.current; var pad = Gamepad.current; var mouse = Mouse.current;
            int move = 0;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || page != Page.Address && kb.wKey.wasPressedThisFrame) move = -1; if (kb.downArrowKey.wasPressedThisFrame || page != Page.Address && kb.sKey.wasPressedThisFrame) move = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) move = -1; if (pad.dpad.down.wasPressedThisFrame) move = 1; }
            if (move != 0 && rows.Count > 0) { do sel = (sel + move + rows.Count) % rows.Count; while (!rows[sel].On && rows.Exists(r => r.On)); Mark(); }
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0)
                for (int i = 0; i < rows.Count; i++) if (rows[i].On && RectTransformUtility.RectangleContainsScreenPoint(rows[i].R, mouse.position.ReadValue(), cam)) { if (sel != i) { sel = i; Mark(); } }
            bool go = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || page != Page.Address && kb.spaceKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && sel < rows.Count && RectTransformUtility.RectangleContainsScreenPoint(rows[sel].R, mouse.position.ReadValue(), cam)) go = true;
            if (page == Page.Main && kb != null) for (int i = 0; i < rows.Count; i++) if (kb[Key.Digit1 + i].wasPressedThisFrame) { Choose(i); return; }
            if (go && sel < rows.Count && rows[sel].On) { rows[sel].Go(); return; }
            bool back = kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame;
            if (back && page != Page.Main) { if (page == Page.Waiting) Coop.Leave(); Show(Page.Main); }
        }
    }
}
