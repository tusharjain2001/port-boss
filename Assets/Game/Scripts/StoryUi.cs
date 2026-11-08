using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M7 step 1: the story on screen. Story.cs holds it, the host runs it, every machine shows it.
    // - Walt's journal pages: a small paper card at the side (right, above the middle) for 16 s with the page's keys. Never
    //   a full-screen tutorial (world §4.1). The Journal app keeps every page.
    // - Calls: the phone rings. A LEFT toast ("Tab · Answer") and a ring every 2.2 s, for 25 s. Tab opens the call screen.
    //   Answer plays the call as captions along the bottom. Decline, or no answer, is a missed call, and its voicemail sits
    //   in the caller's thread. Each player answers their own.
    // - Letters: a toast, then the letter on a paper card in Fraunces, opened from the phone (the caller's thread or the
    //   Journal). M7 step 2 adds the mailbox.
    // - M7 step 2: a new game opens in Walt's cot (Sleep.WakeOpening), then his letter on the desk. The post comes to the
    //   mailbox at the pier's head (a code-built box on a post, its red flag up while a letter waits; E reads it).
    // - M7 step 4, moments in the world, played where you are (each raises its event once; the story texts after):
    //   the conger's head under Walt's pier after dark from Day 2 (on foot, near the pier: June's text), a swordfish
    //   leaping ahead of you at sea on Day 3 from 3 PM (Dot's), Mari's line to catch when she ties up (E at her bow).
    // - The ring, the captions and the page card run on game time (a pause holds them; tests at -pb-fps see them right).
    // - Event(): what the world tells the story. The host books it; a crewmate's machine sends it (CoopFirm "pb.ev").
    public class StoryUi : MonoBehaviour
    {
        public static StoryUi I;
        public static System.Action<string> OnLog;
        int seenLetter, seenCall, seenPage;
        readonly HashSet<string> asked = new();                     // (a crewmate) events already sent to the host
        public Company.Call Ringing; float ringLeft, ringNext;
        public readonly HashSet<int> Answered = new(), Missed = new(), ReadLetters = new();
        public const float RingSecs = 25;

        RectTransform hud;
        // captions (calls now; barks and the rest with M7 step 6's subtitles)
        RectTransform capRoot; Text capWho, capLine; CanvasGroup capCg; Company.Call playing; int capI; float capLeft;
        public bool Talking => playing != null;
        public string Caption => playing != null && capI < playing.Lines.Length ? playing.Lines[capI] : null;   // (tests)
        // the journal page card
        RectTransform pageRoot; CanvasGroup pageCg; float pageLeft; public Company.Page Showing;
        // the letter card
        RectTransform letRoot; float letAt; public Company.Letter Reading;
        public static bool LetterOpen => I != null && I.Reading != null;
        // M7 step 5: the demo's end card
        RectTransform endRoot; bool endShown, endReady; public bool EndUp => endRoot != null && endReady;   // (up: fully shown, taking input)
        // the opening (a new game) and the mailbox
        public static bool OpeningNext;                             // (Save.NewWorld; -pb-opening in a test)
        public bool InOpening;
        Transform mailbox, flag; public Vector3 MailboxFront; public float MailboxYaw => mailbox != null ? mailbox.eulerAngles.y + 180 : 0;   // (facing it)

        static void Log(string s) { Debug.Log("[PB] story: " + s); OnLog?.Invoke(s); }

        public static void Create() { var g = new GameObject("StoryUi"); I = g.AddComponent<StoryUi>(); I.Build(); }

        // who's who on the phone (the avatar colours)
        public static Color Who(string name) => name switch
        {
            Company.MariName => Ponyo.Hex("2FB5A3"),
            Company.WaltName => Ponyo.Hex("C9743A"),
            Company.JuneName => Ponyo.Hex("46B3A6"),
            Company.PrattName => Ponyo.Hex("8D96A3"),
            Company.FergusName => Ponyo.Hex("D9822B"),
            Company.AmaraName => Ponyo.Hex("B5446E"),
            Company.HugoName => Ponyo.Hex("7E6BB8"),
            Company.IngridName => Ponyo.Hex("6E8B3D"),
            Company.DotName => Ponyo.Hex("2E6FA8"),
            Company.UnknownName => Ponyo.Hex("3F3A48"),
            "Notifications" => Ponyo.Hex("8C7468"),
            _ => Ponyo.Hex("4C5BB8"),
        };

        // the world tells the story something happened (the first time counts; the host books it)
        public static void Event(string ev)
        {
            var c = Firm.C; if (c == null || c.Seen(ev)) return;
            if (Sky.Authority) { if (c.Event(ev, Sky.Minutes)) { Firm.I.Dirty = true; Log("event " + ev); } }
            else if (I != null && I.asked.Add(ev)) { CoopFirm.Event(ev); Log("event " + ev + " (sent to the host)"); }
        }

        // a world loaded, or the crew's company the first time: what's in it already isn't news
        public void Mark(Company c)
        {
            seenLetter = seenCall = seenPage = 0;
            foreach (var l in c.Letters) { seenLetter = Mathf.Max(seenLetter, l.Id); ReadLetters.Add(l.Id); }
            foreach (var k in c.Calls) seenCall = Mathf.Max(seenCall, k.Id);
            foreach (var p in c.Pages) seenPage = Mathf.Max(seenPage, p.Id);
            Ringing = null; playing = null; Showing = null;
        }

        public int UnreadLetters(string from = null)
        {
            var c = Firm.C; if (c == null) return 0; int n = 0;
            foreach (var l in c.Letters) if (!ReadLetters.Contains(l.Id) && (from == null || l.From == from)) n++;
            return n;
        }
        public string CallState(Company.Call k) => Ringing == k ? "Ringing" : Answered.Contains(k.Id) ? "Answered" : Missed.Contains(k.Id) ? "Missed" : "Call";

        // ---------------------------------------------------------------- building
        void Build()
        {
            hud = (RectTransform)Hud.I.transform;
            // captions: a paper strip above the prompts
            capRoot = UiKit.Card(hud, "captions", 18, Ponyo.Paper, true, null, 3); capRoot.Place(new Vector2(0.5f, 0), new Vector2(0, 268), new Vector2(760, 64), new Vector2(0.5f, 0));
            capCg = capRoot.gameObject.AddComponent<CanvasGroup>(); capCg.alpha = 0; capCg.blocksRaycasts = false;
            capWho = UiKit.Txt(capRoot, "", UiKit.Bold, 15, Ponyo.Ink, TextAnchor.UpperLeft); capWho.rectTransform.Place(new Vector2(0, 1), new Vector2(22, -10), new Vector2(700, 20), new Vector2(0, 1));
            capLine = UiKit.Txt(capRoot, "", UiKit.Body, 21, Ponyo.Ink, TextAnchor.UpperLeft); capLine.rectTransform.Place(new Vector2(0, 1), new Vector2(22, -32), new Vector2(716, 30), new Vector2(0, 1));
            capLine.horizontalOverflow = HorizontalWrapMode.Wrap;
            capRoot.SetAsFirstSibling(); capRoot.gameObject.SetActive(false);   // (under the phone and the fades)
            BuildMailbox();
        }

        // ---- moments in the world (M7 step 4): played on my screen when I'm where they happen
        GameObject show; float showT; System.Action<float> showTick;
        public GameObject ShowObj => show;                          // (tests: the moment playing now)
        void Moments(Company c)
        {
            if (show != null) { showT += Time.deltaTime; showTick?.Invoke(showT); return; }
            var me = Angler.I; if (me == null || me.Remote || Sky.Minutes < 1) return;
            double m = Sky.Minutes; int day = DayTime.Day(m); double h = DayTime.OfDay(m);
            var l = Landing.I;
            // the conger: after dark (20:00-01:00) from Day 2, on foot within 30 m of the pier's ladder
            if (!c.Seen("conger") && day >= 2 && (h >= 20 * 60 || h < 60) && l != null && l.LadderBottom != null && !me.OnBoat && !me.Swimming
                && Vector3.Distance(me.transform.position, l.LadderBottom.position) < 30f)
            {
                var o = l.LadderBottom.position - l.LadderTop.position; o.y = 0; o = o.sqrMagnitude > 1e-4f ? o.normalized : l.transform.right;   // (out from the pier: the ladder leans out)
                PlayConger(l.LadderBottom.position + o * 2.2f);            // in open water beside the pier, not under its planks
            }
            // the swordfish: Day 3 on, 15:00-18:00, at sea more than 250 m from the landing
            else if (!c.Seen("swordfish") && day >= 3 && h >= 15 * 60 && h < 18 * 60 && me.OnBoat && l != null && Vector3.Distance(me.transform.position, l.transform.position) > 250f) PlaySwordfish(me);
        }
        GameObject FishShow(string id, float cm)
        {
            var sp = FishDb.Get(id); if (sp == null) return null;
            var v = FishView.Make(new Fish { Species = sp, Cm = cm }, null); return v != null ? v.gameObject : null;
        }
        void PlayConger(Vector3 near)
        {
            var g = FishShow("conger", 150); if (g == null) { Event("conger"); return; }
            show = g; showT = 0; var at = near + Vector3.down * 0.3f; at.y = Sea.Height(at);
            g.transform.position = at + Vector3.down * 0.9f; g.transform.rotation = Quaternion.Euler(-70, Random.Range(0f, 360f), 0);
            Snd.Play(M1Assets.I.Splash, at, 0.35f, 0.7f); PortBoss.M1.Fx.Ring(at, 0.8f, 1.6f);
            Log("the conger's head under Walt's pier");
            showTick = t =>
            {
                float up = t < 1.2f ? Mathf.SmoothStep(0, 1, t / 1.2f) : t < 3.2f ? 1 : Mathf.SmoothStep(1, 0, (t - 3.2f) / 0.8f);
                g.transform.position = at + Vector3.down * Mathf.Lerp(0.9f, 0.15f, up) + Vector3.right * Mathf.Sin(t * 3) * 0.04f;
                if (t > 4.1f) { Snd.Play(M1Assets.I.Splash, at, 0.3f, 0.6f); End("conger"); }
            };
        }
        void PlaySwordfish(Angler me)
        {
            var g = FishShow("swordfish", 260); if (g == null) { Event("swordfish"); return; }
            show = g; showT = 0;
            var cam = CameraRig.I != null ? CameraRig.I.Cam.transform : null;   // (where I'm looking: it's for being seen)
            var fwd = cam != null ? cam.forward : me.Boat != null ? me.Boat.transform.forward : me.transform.forward; fwd.y = 0; fwd.Normalize();
            var a = me.transform.position + fwd * 38 + Vector3.Cross(Vector3.up, fwd) * 6; a.y = Sea.Height(a);
            var b = a + fwd * 14 + Vector3.Cross(Vector3.up, fwd) * 4; b.y = Sea.Height(b);
            Snd.Play(M1Assets.I.Splash, a, 0.6f, 0.8f); PortBoss.M1.Fx.Splash(a, 0.5f, 18);
            Log("a swordfish leaps");
            showTick = t =>
            {
                float k = Mathf.Clamp01(t / 1.6f); var p = Vector3.Lerp(a, b, k) + Vector3.up * (6f * 4 * k * (1 - k) - 0.6f);
                var v = (b - a) / 1.6f + Vector3.up * 6f * 4 * (1 - 2 * k) / 1.6f;
                g.transform.SetPositionAndRotation(p, Quaternion.LookRotation(v.normalized) * Quaternion.Euler(0, 90, 0));
                if (k >= 1) { Snd.Play(M1Assets.I.Splash, b, 0.6f, 0.7f); PortBoss.M1.Fx.Splash(b, 0.5f, 18); End("swordfish"); }
            };
        }
        void End(string ev) { if (show != null) Destroy(show); show = null; showTick = null; Event(ev); }

        // Mari's line (M7 step 4): for 12 minutes after she ties up, E at her bow catches it (Story: "mari:here" / "mari:tied")
        public static string LinePrompt(Angler me)
        {
            var c = Firm.C; if (c == null || I == null || me.Remote || me.OnBoat || me.Swimming || !c.Fired("mari:here") || c.Seen("mari:tied") || c.Fired("mari:self")) return null;
            var st = Firm.I?.Stalls.Find(s => s.K == Firm.Kind.Mari); if (st == null) return null;
            var d = st.Front - me.transform.position; d.y *= 0.5f; if (d.magnitude > 3f) return null;
            if (Ctl.Interact) { Event("mari:tied"); Snd.Play2D(Synth.Ratchet, 0.4f, 1.2f); Log("caught Mari's line"); }
            return "Catch Mari's line";
        }

        // ---- M7 step 5: the demo's end. The phone buzzes with the unknown number; then the screen dims to a paper card: to be
        //      continued, what comes next, the days and the money, Keep fishing / Quit. (The demo build only: Company.Demo.)
        System.Collections.IEnumerator EndCard(Company c)
        {
            Log("the demo ends");
            yield return new WaitForSeconds(1.5f);
            if (Phone.IsOpen) Phone.I.Hide(); Close();
            var dim = UiKit.Img(hud, null, new Color(0.11f, 0.12f, 0.24f, 0), "demo end"); dim.rectTransform.Stretch(); endRoot = dim.rectTransform; endRoot.SetAsLastSibling();
            Ctl.Ui = 2; endReady = false;                                    // (no walking off while it dims)
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { if (dim == null) yield break; dim.color = new Color(0.11f, 0.12f, 0.24f, Mathf.SmoothStep(0, 0.78f, t / 1.6f)); yield return null; }
            if (dim == null) yield break;
            const float w = 720; float y = 40;
            var card = UiKit.Card(endRoot, "card", 16, Ponyo.Hex("FFFBF2"), true, Ponyo.Line, 4);
            Text T(string s, Font f, int size, Color col, TextAnchor a = TextAnchor.UpperCenter, float pad = 60)
            {
                var t = UiKit.Txt(card, s, f, size, col, a); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.lineSpacing = 1.1f;
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(pad, -y), new Vector2(w - 2 * pad, 20), new Vector2(0, 1));
                float h = t.preferredHeight; t.rectTransform.sizeDelta = new Vector2(w - 2 * pad, h); y += h; return t;
            }
            T(UiKit.Caps("Port Boss · the demo"), UiKit.Bold, 13, Phone.SubInk); y += 10;
            T("To be continued", UiKit.DisplayBold, 52, Ponyo.Ink); y += 18;
            T("Somebody has noticed the Landing is waking up. Pratt's been on the phone to Voss Harbour, and the number that texted you isn't done.", UiKit.Display, 21, Ponyo.Ink); y += 14;
            T("In the full game: hire a crew and send them fishing, a second boat, the office restored, piers and ship orders, Voss and his men, and the vote for the Harbour Charter.", UiKit.Body, 17, Phone.SubInk); y += 22;
            int days = DayTime.Day(Sky.Minutes); int fish = 0; foreach (var e in c.Ledger) if (e.Tx.StartsWith("sell") || e.What.StartsWith("Sold")) fish++;
            T($"{days} days at Walt's Landing · ${c.Earned:N0} earned · {(c.Mari == DealSt.Active ? "Mari moored" : "no moorers yet")}{(c.Seen("amara:1") ? " · Amara's first order" : "")}", UiKit.Bold, 16, Ponyo.Ink); y += 18;
            T("Wishlist Port Boss on Steam to hear when it's out.", UiKit.Body, 16, Phone.SubInk); y += 26;
            float bw = 220;
            var keep = UiKit.Card(card, "btn Keep fishing", 26, Ponyo.Paper2, true, Ponyo.Line, 3); keep.Place(new Vector2(0.5f, 1), new Vector2(-bw / 2 - 12, -y), new Vector2(bw, 52), new Vector2(0.5f, 1));
            var kt = UiKit.Txt(keep, "Keep fishing", UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleCenter); kt.rectTransform.Stretch();
            var quit = UiKit.Card(card, "btn Quit", 26, Ponyo.Sunflower, true, Ponyo.Line, 3); quit.Place(new Vector2(0.5f, 1), new Vector2(bw / 2 + 12, -y), new Vector2(bw, 52), new Vector2(0.5f, 1));
            var qt = UiKit.Txt(quit, "Quit", UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleCenter); qt.rectTransform.Stretch();
            keepBtn = keep; quitBtn = quit; y += 52 + 40;
            card.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, y));
            endReady = true; if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Snd.Play2D(Synth.Horn, 0.35f, 0.75f);
            if (Sky.Authority && Save.Enabled) Save.Write("demo end");
        }
        RectTransform keepBtn, quitBtn;
        void EndInput()
        {
            if (keepBtn == null) return;
            var kb = Keyboard.current; var pad = Gamepad.current; var ms = Mouse.current; var cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            bool Hit(RectTransform r) => ms != null && ms.leftButton.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(r, ms.position.ReadValue(), cam);
            if (Hit(keepBtn) || kb != null && (kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame) KeepFishing();
            else if (Hit(quitBtn) || kb != null && kb.qKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame) { Log("quit from the demo's end"); Application.Quit(); }
        }
        public void KeepFishing()
        {
            if (endRoot == null) return;
            Destroy(endRoot.gameObject); endRoot = null; endReady = false; keepBtn = quitBtn = null; Ctl.Ui = 0; Ctl.UiEsc = Time.frameCount;
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            Log("keep fishing after the demo's end");
        }

        // ---- the opening: wake in the cot, then Walt's letter on the desk
        public System.Collections.IEnumerator Opening()
        {
            OpeningNext = false; InOpening = true; Log("the opening");
            Hud.I?.Quiet(true);                                          // (a quiet wake: Walt's texts wait till the letter's down)
            if (Sleep.I != null) yield return Sleep.I.WakeOpening();
            Company.Letter Desk() => Firm.C?.Letters.Find(x => x.Where == "desk");
            for (float t = 0; t < 4 && Desk() == null; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.5f);
            if (Desk() != null) Open(Desk());
            InOpening = false;
            while (LetterOpen) yield return null;
            Hud.I?.Quiet(false);
        }

        // ---- the mailbox at the pier's head, beside the way-to-town board (landing metres (5.7, 10.8), facing down the
        //      pier): a blue box with a round top on a post, HALE on its door, a red flag that's up while a letter waits.
        //      ponytail: code-built like the counters and the board, until the props pass gives it a Blender model.
        void BuildMailbox()
        {
            var l = Landing.I; if (l == null) return;
            var at = l.transform.TransformPoint(new Vector3(-5.7f, 0, -10.8f));
            if (Physics.Raycast(at + Vector3.up * 30, Vector3.down, out var hit, 60, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y; else at.y = 1.3f;
            var face = l.transform.TransformDirection(Vector3.forward); face.y = 0; face.Normalize();
            mailbox = new GameObject("Mailbox").transform; mailbox.SetParent(transform, false);
            mailbox.SetPositionAndRotation(at, Quaternion.LookRotation(face));
            Color wood = Ponyo.Hex("6E5038"), blue = Ponyo.Hex("4A7FB5"), blueDk = Ponyo.Hex("36608C"), red = Ponyo.Hex("E2553F");
            Firm.Box(mailbox, "post", new Vector3(0, 0.55f, 0), new Vector3(0.11f, 1.1f, 0.11f), wood);
            Firm.Box(mailbox, "shelf", new Vector3(0, 1.1f, 0), new Vector3(0.3f, 0.04f, 0.5f), wood);
            Firm.Box(mailbox, "box", new Vector3(0, 1.25f, 0), new Vector3(0.32f, 0.26f, 0.48f), blue);
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder); top.name = "round top"; Object.Destroy(top.GetComponent<Collider>());
            top.transform.SetParent(mailbox, false); top.transform.localPosition = new Vector3(0, 1.38f, 0); top.transform.localRotation = Quaternion.Euler(90, 0, 0); top.transform.localScale = new Vector3(0.32f, 0.24f, 0.32f);
            var tm = new Material(Game.ToonMat); tm.SetColor("_BaseColor", blue); top.GetComponent<MeshRenderer>().sharedMaterial = tm;
            Firm.Box(mailbox, "door", new Vector3(0, 1.3f, 0.241f), new Vector3(0.27f, 0.3f, 0.02f), blueDk);
            Firm.Box(mailbox, "knob", new Vector3(0, 1.24f, 0.26f), new Vector3(0.05f, 0.03f, 0.03f), Ponyo.Hex("E8C66A"));
            Firm.Sign(mailbox, "HALE", new Vector3(0, 1.36f, 0.256f), Ponyo.Paper, 0.0065f);
            flag = new GameObject("flag").transform; flag.SetParent(mailbox, false); flag.localPosition = new Vector3(0.17f, 1.2f, 0.05f);
            Firm.Box(flag, "arm", new Vector3(0, 0.13f, 0), new Vector3(0.02f, 0.26f, 0.03f), red);
            Firm.Box(flag, "flag", new Vector3(0, 0.23f, 0.06f), new Vector3(0.02f, 0.09f, 0.12f), red);
            foreach (var r in mailbox.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var col = mailbox.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.75f, 0); col.size = new Vector3(0.36f, 1.5f, 0.52f);
            MailboxFront = mailbox.position + mailbox.forward * 0.75f;
            Log($"the mailbox at {at.x:F1} {at.y:F2} {at.z:F1}");
        }
        public Company.Letter Post() { var c = Firm.C; return c?.Letters.Find(x => x.Where == "mail" && !ReadLetters.Contains(x.Id)); }
        // the E prompt at the mailbox (Angler.Interact)
        public static string Prompt(Angler me)
        {
            if (I == null || I.mailbox == null || me.Remote || me.Swimming || me.OnBoat || LetterOpen) return null;
            var d = I.MailboxFront - me.transform.position; d.y *= 0.5f; if (d.magnitude > 1.4f) return null;
            var post = I.Post();
            if (post == null) return "The mailbox · nothing today";
            if (Ctl.Interact) I.Open(post);
            return $"Read the post · {post.Title}";
        }


        // ---------------------------------------------------------------- the loop
        void Update()
        {
            var c = Firm.C; if (c == null || Hud.I == null) return;
            // new letters, calls, pages (in the company: the host's, or the crew's copy of it)
            foreach (var l in c.Letters) if (l.Id > seenLetter) { seenLetter = l.Id; NewLetter(l); }
            foreach (var k in c.Calls) if (k.Id > seenCall) { seenCall = k.Id; if (Sky.Minutes - k.At <= 20) Ring(k); else Missed.Add(k.Id); }
            foreach (var p in c.Pages) if (p.Id > seenPage) { seenPage = p.Id; ShowPage(p); }
            Rings(); Captions(); PageCard(); LetterCard(); Moments(c);
            if (!endShown && c.Seen("demo:end")) { endShown = true; StartCoroutine(EndCard(c)); }
            if (endRoot != null) EndInput();
            if (flag != null) flag.localRotation = Quaternion.Slerp(flag.localRotation, Quaternion.Euler(Post() != null ? 0 : -90, 0, 0), Time.deltaTime * 6);
        }

        // ---- calls
        void Ring(Company.Call k)
        {
            if (Ringing != null) Miss();                              // (a second call: the first is missed)
            Ringing = k; ringLeft = RingSecs; ringNext = 0;
            Alerts.Push(k.From, "is calling you", Alerts.Kind.Story, "call:" + k.Id, null, "Answer");
            Log($"{k.From} is calling (call {k.Id})");
        }
        void Rings()
        {
            if (Ringing == null) return;
            ringLeft -= Time.deltaTime; ringNext -= Time.deltaTime;
            if (ringNext <= 0) { ringNext = 2.2f; Snd.Play2D(Synth.Ding[2], 0.22f, 1.25f); StartCoroutine(Twice()); }
            if (ringLeft <= 0 || Sky.Minutes - Ringing.At > RingSecs) Miss();   // (and by the clock: a sleep or a skip past it)
        }
        System.Collections.IEnumerator Twice() { yield return new WaitForSeconds(0.22f); if (Ringing != null) Snd.Play2D(Synth.Ding[2], 0.22f, 1.25f); }
        public void Answer()
        {
            var k = Ringing; if (k == null) return;
            Ringing = null; Answered.Add(k.Id); playing = k; capI = 0; capLeft = Dur(k.Lines[0]);
            Alerts.Push(k.From, "On the phone", Alerts.Kind.Story, "talk", "call:" + k.Id);
            capRoot.gameObject.SetActive(true); ShowLine();
            Log($"answered {k.From} (call {k.Id})");
        }
        public void Decline() => Miss(true);
        void Miss(bool declined = false)
        {
            var k = Ringing; if (k == null) return;
            Ringing = null; Missed.Add(k.Id);
            Alerts.Push(k.From, $"{(declined ? "Declined" : "Missed call")} · the voicemail's in Messages", Alerts.Kind.Story, "missed", "call:" + k.Id, "Open phone");
            Log($"{(declined ? "declined" : "missed")} {k.From} (call {k.Id})");
        }
        static float Dur(string line) => line == "..." ? 1.6f : Mathf.Clamp(1.4f + 0.06f * line.Length, 2.6f, 7.5f);
        void ShowLine()
        {
            capWho.text = UiKit.Caps(playing.From); capWho.color = Color.Lerp(Who(playing.From), Ponyo.Ink, 0.35f);
            capLine.text = playing.Lines[capI];
            float h = Mathf.Max(30, capLine.preferredHeight); capLine.rectTransform.sizeDelta = new Vector2(716, h); capRoot.sizeDelta = new Vector2(760, 44 + h);
        }
        void Captions()
        {
            if (playing == null) { capCg.alpha = Mathf.MoveTowards(capCg.alpha, 0, Time.deltaTime * 4); if (capCg.alpha <= 0 && capRoot.gameObject.activeSelf) capRoot.gameObject.SetActive(false); return; }
            capCg.alpha = Mathf.MoveTowards(capCg.alpha, Phone.IsOpen ? 0.35f : 1, Time.deltaTime * 4);
            capLeft -= Time.deltaTime;
            if (capLeft > 0) return;
            if (++capI >= playing.Lines.Length) { Log($"call {playing.Id} ended"); Alerts.Push(playing.From, "Call ended", Alerts.Kind.Story, "ended", "talk"); playing = null; return; }
            capLeft = Dur(playing.Lines[capI]); ShowLine();
        }

        // ---- the journal page card
        void ShowPage(Company.Page p)
        {
            if (pageRoot != null) Destroy(pageRoot.gameObject);
            Showing = p; pageLeft = 16;
            pageRoot = UiKit.Card(hud, "journal page", 20, Ponyo.Paper, true, null, 3);
            pageCg = pageRoot.gameObject.AddComponent<CanvasGroup>(); pageCg.alpha = 0; pageCg.blocksRaycasts = false;
            const float w = 400; float y = 18;
            var tag = UiKit.Img(pageRoot, Glyphs.Get("journal"), Ponyo.Hex("A0704E"), "icon"); tag.rectTransform.Place(new Vector2(0, 1), new Vector2(22, -y), new Vector2(22, 22), new Vector2(0, 1));
            var cap = UiKit.Txt(pageRoot, UiKit.Caps("Walt's journal"), UiKit.Bold, 12, Phone.SubInk, TextAnchor.UpperLeft); cap.rectTransform.Place(new Vector2(0, 1), new Vector2(52, -y - 4), new Vector2(w - 70, 18), new Vector2(0, 1));
            y += 30;
            var title = UiKit.Txt(pageRoot, p.Title, UiKit.DisplayBold, 25, Ponyo.Ink, TextAnchor.UpperLeft); title.rectTransform.Place(new Vector2(0, 1), new Vector2(22, -y), new Vector2(w - 44, 32), new Vector2(0, 1));
            y += 38;
            var body = UiKit.Txt(pageRoot, p.Text, UiKit.Display, 19, Ponyo.Ink, TextAnchor.UpperLeft); body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.rectTransform.Place(new Vector2(0, 1), new Vector2(22, -y), new Vector2(w - 44, 20), new Vector2(0, 1));
            float bh = body.preferredHeight; body.rectTransform.sizeDelta = new Vector2(w - 44, bh); y += bh + 14;
            if (!string.IsNullOrEmpty(p.Keys))
                foreach (var pair in p.Keys.Split(';'))
                {
                    var kv = pair.Split('|'); if (kv.Length < 2) continue;
                    var key = UiKit.Key(pageRoot, kv[0], 30); key.Place(new Vector2(0, 1), new Vector2(22, -y), key.sizeDelta, new Vector2(0, 1));
                    var lt = UiKit.Txt(pageRoot, kv[1], UiKit.Body, 16, Ponyo.Ink, TextAnchor.MiddleLeft); lt.rectTransform.Place(new Vector2(0, 1), new Vector2(30 + key.sizeDelta.x, -y), new Vector2(w - 60 - key.sizeDelta.x, 30), new Vector2(0, 1));
                    y += 38;
                }
            pageRoot.Place(new Vector2(1, 0.5f), new Vector2(-44, 150), new Vector2(w, y + 8), new Vector2(1, 0.5f));
            pageRoot.SetAsFirstSibling();                                // (under the phone, which slides up over it)
            Snd.Play2D(Synth.Pluck, 0.25f, 1.1f);
            Log($"journal page {p.Key}: {p.Title}");
        }
        void PageCard()
        {
            if (pageRoot == null) return;
            bool hide = Phone.IsOpen || Shop.Open || LetterOpen;
            if (!hide) pageLeft -= Time.deltaTime;
            pageCg.alpha = Mathf.MoveTowards(pageCg.alpha, hide || pageLeft <= 0 ? 0 : 1, Time.deltaTime * 3);
            if (pageLeft <= 0 && pageCg.alpha <= 0) { Destroy(pageRoot.gameObject); pageRoot = null; Showing = null; }
        }

        // ---- letters
        void NewLetter(Company.Letter l)
        {
            if (l.Where == "desk" && (InOpening || OpeningNext)) { Log($"letter {l.Id} on the desk (the opening opens it)"); return; }
            if (l.Where == "mail") Alerts.Push(l.From, "A letter on the morning ferry. It's in the mailbox at the pier's head.", Alerts.Kind.Story, "letter:" + l.Id, null, "Read");
            else Alerts.Push(l.From, "On the desk: " + l.Title, Alerts.Kind.Story, "letter:" + l.Id, null, "Read");
            Log($"letter {l.Id} from {l.From}: {l.Title}");
        }
        public void Open(Company.Letter l)
        {
            if (l == null) return;
            if (Phone.IsOpen) Phone.I.Hide();
            Close(); Reading = l; ReadLetters.Add(l.Id); letAt = Time.unscaledTime;
            Ctl.Ui = 2; if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            const float w = 640; float y = 34;
            letRoot = UiKit.Card(hud, "letter", 10, Ponyo.Hex("FFFBF2"), true, Ponyo.Line, 4);
            var date = UiKit.Txt(letRoot, UiKit.Caps($"{DayTime.DayName(DayTime.Day(l.At))} · Day {DayTime.Day(l.At)}"), UiKit.Bold, 12, Phone.SubInk, TextAnchor.UpperRight);
            date.rectTransform.Place(new Vector2(0, 1), new Vector2(40, -22), new Vector2(w - 80, 18), new Vector2(0, 1));
            for (int i = 0; i < l.Lines.Length; i++)
            {
                string s = l.Lines[i]; bool sign = s.StartsWith("—"), ps = s.StartsWith("P.S.");
                var t = UiKit.Txt(letRoot, s, sign ? UiKit.DisplayBold : UiKit.Display, i == 0 ? 26 : 22, Ponyo.Ink, sign ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.lineSpacing = 1.12f;
                if (ps) t.color = Color.Lerp(Ponyo.Ink, Phone.SubInk, 0.5f);
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(44, -y), new Vector2(w - 88, 20), new Vector2(0, 1));
                float h = t.preferredHeight; t.rectTransform.sizeDelta = new Vector2(w - 88, h); y += h + (i == 0 ? 14 : 12);
            }
            y += 10;
            var hint = UiKit.Rect("hint", letRoot).Place(new Vector2(0, 1), new Vector2(0, -y), new Vector2(w, 34), new Vector2(0, 1));
            var key = UiKit.Key(hint, "E", 30); key.Place(new Vector2(0.5f, 0.5f), new Vector2(-62, 0), key.sizeDelta);
            var ht = UiKit.Txt(hint, "Put it down", UiKit.Body, 16, Phone.SubInk, TextAnchor.MiddleLeft); ht.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-38, 0), new Vector2(140, 30), new Vector2(0, 0.5f));
            y += 50;
            letRoot.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(w, y));
            Snd.Play2D(Synth.Whoosh, 0.2f, 1.6f);
            Log($"reading letter {l.Id}: {l.Title}");
        }
        public void Close()
        {
            if (Reading == null) return;
            Reading = null; if (letRoot != null) Destroy(letRoot.gameObject); letRoot = null;
            Ctl.Ui = 0; Ctl.UiEsc = Time.frameCount;
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            Snd.Play2D(Synth.Tik, 0.25f, 0.7f);
        }
        void LetterCard()
        {
            if (Reading == null || Time.unscaledTime - letAt < 0.3f) return;
            var kb = Keyboard.current; var pad = Gamepad.current;
            bool shut = kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                     || pad != null && (pad.buttonEast.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)
                     || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (shut) Close();
        }
    }
}
