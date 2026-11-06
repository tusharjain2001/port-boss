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
        public static bool Shown => Up && I.page != Page.Invite;  // the title menu itself (the world waits behind it)
        public static bool Up => I != null && I.root.gameObject.activeSelf; // ... or the invite list (M3.1: the world runs on)
        public static event Action Closed;                      // (Game holds its welcome tips until play starts)
        public static int ClosedFrame = -1;                     // (Game: the Esc that closed the invite list doesn't also pause)
        enum Page { Main, Friends, Address, Code, Waiting, Invite, Worlds, HostWorld }
        class Row { public string Label, Sub; public Action Go; public bool On = true; public RectTransform R; public Image Fill; }

        RectTransform root, card, list; Text status, foot, head, sub; Image dim;
        Page page; readonly List<Row> rows = new(); int sel;
        string address, code = ""; float waitT; string waitWhat;
        Action hostGo; string hostWhat;   // the crew being hosted, once its world is picked (Page.HostWorld)
        Camera cam;

        public static void Open()
        {
            if (I == null) { var g = new GameObject("Title"); I = g.AddComponent<Title>(); I.Build(); }
            I.root.gameObject.SetActive(true); I.Show(Page.Main);
            Time.timeScale = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        // the invite list (Coop.Invite, I in a crew): the same card, but the world, the clock and the crew carry on
        public static void InviteList()
        {
            if (I == null) { var g = new GameObject("Title"); I = g.AddComponent<Title>(); I.Build(); }
            I.root.gameObject.SetActive(true); I.Show(Page.Invite);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public static void CloseInvite()
        {
            if (!Up || I.page != Page.Invite) return;
            I.root.gameObject.SetActive(false); ClosedFrame = Time.frameCount;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
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
            dim = UiKit.Img(root, null, new Color(0.11f, 0.15f, 0.28f, 0.45f), "dim"); dim.rectTransform.Stretch();
            card = UiKit.Card(root, "card", 28, Ponyo.Paper, true); card.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(720, 660));
            head = UiKit.Txt(card, "Port Boss", UiKit.DisplayBold, 66, Ponyo.Ink, TextAnchor.UpperCenter); head.rectTransform.Stretch(); head.rectTransform.offsetMax = new Vector2(0, -26);
            sub = UiKit.Txt(card, "Fishing on Biscuit, alone or with up to three friends", UiKit.Body, 20, Ponyo.InkSoft, TextAnchor.UpperCenter); sub.rectTransform.Stretch(); sub.rectTransform.offsetMax = new Vector2(0, -110);
            list = UiKit.Rect("rows", card).Place(new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(620, 400), new Vector2(0.5f, 1));
            status = UiKit.Txt(card, "", UiKit.Bold, 18, Ponyo.Hex("C2452F"), TextAnchor.LowerCenter); status.rectTransform.Stretch(); status.rectTransform.offsetMin = new Vector2(30, 56);
            foot = UiKit.Txt(card, "", UiKit.Body, 17, Ponyo.InkSoft, TextAnchor.LowerCenter); foot.rectTransform.Stretch(); foot.rectTransform.offsetMin = new Vector2(30, 24);
            address = PlayerPrefs.GetString("m1.lastip", "127.0.0.1");
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnChar;
        }
        void OnDestroy() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnChar; }

        public static string LocalAddress()
        {
            // the home network's address first (192.168.x, 10.x, 172.16-31.x), not a VPN's (Tailscale's 100.x: M3.1)
            string any = null;
            try
            {
                foreach (var a in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
                {
                    if (a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || System.Net.IPAddress.IsLoopback(a)) continue;
                    var b = a.GetAddressBytes();
                    if (b[0] == 192 && b[1] == 168 || b[0] == 10 || b[0] == 172 && b[1] >= 16 && b[1] < 32) return a.ToString();
                    any ??= a.ToString();
                }
            }
            catch { }
            return any ?? "this PC's address";
        }

        void Show(Page p)
        {
            page = p; rows.Clear(); sel = 0;
            bool steam = Coop.SteamOk;
            bool inv = p == Page.Invite;
            head.text = inv ? "Invite friends" : "Port Boss";
            sub.text = inv ? (Coop.Code != null ? "Crew code  " + Coop.Code : "This crew is on your home network") : "Fishing on Biscuit, alone or with up to three friends";
            dim.color = new Color(0.11f, 0.15f, 0.28f, inv ? 0.25f : 0.45f);
            switch (p)
            {
                case Page.Main:
                    // M4: Continue the last world, or a new one; hosting asks which world the crew plays (a save, a crew copy or a
                    // new one: systems §1 "every player keeps a world copy, so anyone can host", §3.5, §4.5)
                    var worlds = Save.Worlds(); var last = worlds.Count > 0 ? worlds[0] : null;
                    if (last != null) rows.Add(new Row { Label = "Continue", Sub = $"{last.Name} · Day {last.Day} · ${last.Cash:N0} · saved {Save.Ago(last.Saved)}", Go = () => { if (Save.Load(last.Id)) Close(); } });
                    rows.Add(new Row { Label = last != null ? "New game" : "Play solo", Sub = "Walt's dock, Biscuit, $40 and a bag of worms", Go = () => { Save.NewWorld(); Close(); } });
                    if (worlds.Count > 1) rows.Add(new Row { Label = "Your worlds", Sub = $"{worlds.Count} saved, crew copies too", Go = () => Show(Page.Worlds) });
                    rows.Add(new Row { Label = "Host a crew on Steam", Sub = steam ? "Then press I in the game to invite friends" : "Steam isn't running", On = steam, Go = () => Host(Coop.HostSteam, "Making a crew on Steam…") });
                    rows.Add(new Row { Label = "Join a friend on Steam", Sub = steam ? "Friends hosting right now, or accept their invite" : "Steam isn't running", On = steam, Go = () => Show(Page.Friends) });
                    rows.Add(new Row { Label = "Host on this network", Sub = $"Friends on the same network join {LocalAddress()}", Go = () => Host(() => Coop.HostLan(), "Hosting on this network…") });
                    rows.Add(new Row { Label = "Join on this network", Sub = "Type the host's address", Go = () => Show(Page.Address) });
                    break;
                case Page.Friends:
                    foreach (var (name, lobby) in Coop.FriendsHosting()) { var l = lobby; rows.Add(new Row { Label = name + "'s crew", Sub = "Join them", Go = () => { _ = Coop.JoinLobby(l); Wait($"Joining {name}…"); } }); }
                    if (rows.Count == 0) rows.Add(new Row { Label = "Nobody's hosting right now", Sub = "Ask a friend to host and invite you", On = false });
                    rows.Add(new Row { Label = "Type a crew code", Sub = "The code your friend sees in their pause menu (Esc)", Go = () => Show(Page.Code) });
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
                case Page.HostWorld:
                    head.text = "Which world?"; sub.text = "Your crew plays the world you host";
                    var hw = Save.Worlds(); if (hw.Count > 5) hw.RemoveRange(5, hw.Count - 5);
                    foreach (var w in hw) { var id = w.Id; rows.Add(new Row { Label = w.Name, Sub = $"Day {w.Day} · ${w.Cash:N0} · saved {Save.Ago(w.Saved)}{(w.Copy ? " · a crew copy" : "")}", Go = () => { if (Save.Load(id)) StartHost(); } }); }
                    rows.Add(new Row { Label = "A new world", Sub = "Walt's dock, Biscuit, $40 and a bag of worms", Go = () => { Save.NewWorld(); StartHost(); } });
                    rows.Add(new Row { Label = "Back", Sub = "", Go = () => Show(Page.Main) });
                    break;
                case Page.Code:
                    rows.Add(new Row { Label = "Crew code: " + code, Sub = "Type it (like K7Q-4MXA); Backspace deletes", Go = JoinCode });
                    rows.Add(new Row { Label = "Join", Sub = "", Go = JoinCode });
                    rows.Add(new Row { Label = "Back", Sub = "", Go = () => Show(Page.Friends) });
                    break;
                case Page.Invite:
                    if (Coop.Code != null)
                    {
                        bool ov = Coop.OverlayOn;
                        rows.Add(new Row { Label = "Copy the crew code", Sub = "Friends type it in Join a friend on Steam", Go = () => { GUIUtility.systemCopyBuffer = Coop.Code; Alerts.Push("Crew", $"Crew code {Coop.Code} copied. Paste it to a friend.", Alerts.Kind.Info); } });
                        rows.Add(new Row { Label = "Steam's invite window", Sub = ov ? "Opens over the game" : "Not here: Steam's overlay isn't running in this game", On = ov, Go = () => { if (Coop.Lobby is Steamworks.Data.Lobby l) Steamworks.SteamFriends.OpenGameInviteOverlay(l.Id); } });
                        var fr = Coop.FriendsToInvite();
                        for (int i = 0; i < fr.Count && i < 5; i++) { var f = fr[i]; rows.Add(new Row { Label = "Invite " + f.name, Sub = f.sub, Go = () => Coop.InviteFriend(f.name, f.id) }); }
                        if (fr.Count == 0) rows.Add(new Row { Label = "No friends online right now", Sub = "Share the crew code instead", On = false });
                    }
                    else rows.Add(new Row { Label = "Friends join " + Coop.LanAddress, Sub = "They pick Join on this network and type it", On = false });
                    rows.Add(new Row { Label = "Back to the game", Sub = "", Go = CloseInvite });
                    sel = rows.FindIndex(r => r.On); if (sel < 0) sel = 0;
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

        void JoinCode()
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            Coop.JoinCode(code.Trim()); Wait($"Joining crew {code}…");
        }

        // host a crew: straight away with no saved world, else ask which one first
        void Host(Action go, string what) { hostGo = go; hostWhat = what; if (Save.Worlds().Count == 0) StartHost(); else Show(Page.HostWorld); }
        void StartHost() { hostGo(); Wait(hostWhat); }
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
            if (!Shown || page != Page.Address && page != Page.Code) return;
            bool cd = page == Page.Code; string buf = cd ? code : address;
            if (c == '\b') { if (buf.Length > 0) buf = buf.Substring(0, buf.Length - 1); }
            else if (char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '-') buf += cd ? char.ToUpperInvariant(c) : c;
            else return;
            if (cd) code = buf; else address = buf;
            rows[0].Label = (cd ? "Crew code: " : "Address: ") + buf; Draw();
        }
        // (tests) as if typed
        public void Type(string s) { address = ""; code = ""; foreach (var c in s) OnChar(c); }
        public int Rows => rows.Count;
        public string RowText(int i) => i < rows.Count ? rows[i].Label + " | " + rows[i].Sub + (rows[i].On ? "" : " (off)") : "";
        public void Choose(string label) => Choose(rows.FindIndex(r => r.Label == label));
        public void Choose(int i) { if (i >= 0 && i < rows.Count && rows[i].On) { sel = i; Mark(); rows[i].Go(); } }
        public void GoTo(string p) => Show(p == "address" ? Page.Address : p == "code" ? Page.Code : p == "friends" ? Page.Friends : Page.Main);

        void Update()
        {
            if (!Up) return;
            if (root.GetSiblingIndex() != root.parent.childCount - 1) root.SetAsLastSibling(); // (over anything the HUD adds)
            foot.text = page == Page.Main ? $"Playing as {Coop.MyName}" + (Coop.SteamOk ? " on Steam" : "") : page == Page.Address || page == Page.Code ? "Enter joins · Esc back" : page == Page.Invite ? "Arrows or the mouse · Enter · Esc back to the game" : "Esc back";
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
            bool keys = page != Page.Address && page != Page.Code && page != Page.Invite; // (typing, or the world going on: W S Space are theirs)
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || keys && kb.wKey.wasPressedThisFrame) move = -1; if (kb.downArrowKey.wasPressedThisFrame || keys && kb.sKey.wasPressedThisFrame) move = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) move = -1; if (pad.dpad.down.wasPressedThisFrame) move = 1; }
            if (move != 0 && rows.Count > 0) { do sel = (sel + move + rows.Count) % rows.Count; while (!rows[sel].On && rows.Exists(r => r.On)); Mark(); }
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0)
                for (int i = 0; i < rows.Count; i++) if (rows[i].On && RectTransformUtility.RectangleContainsScreenPoint(rows[i].R, mouse.position.ReadValue(), cam)) { if (sel != i) { sel = i; Mark(); } }
            bool go = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || keys && kb.spaceKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && sel < rows.Count && RectTransformUtility.RectangleContainsScreenPoint(rows[sel].R, mouse.position.ReadValue(), cam)) go = true;
            if (page == Page.Main && kb != null) for (int i = 0; i < rows.Count; i++) if (kb[Key.Digit1 + i].wasPressedThisFrame) { Choose(i); return; }
            if (go && sel < rows.Count && rows[sel].On) { rows[sel].Go(); return; }
            bool back = kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame;
            if (back && page == Page.Invite) { CloseInvite(); return; }
            if (back && page != Page.Main) { if (page == Page.Waiting) Coop.Leave(); Show(page == Page.Code ? Page.Friends : Page.Main); }
        }
    }
}
