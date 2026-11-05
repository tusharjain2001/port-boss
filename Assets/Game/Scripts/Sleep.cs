using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M3 sleep (gdd systems §1.4, MUST): one sleeper skips the night to 07:00 for everyone and the weather moves on.
    // - Walt's old cot stands by the shack. Hold E 1 s on it from 20:00 to 05:00, or any time you're tired; before that
    //   "Too early. Back after 8 PM." You lie down (1 s), then the skip.
    // - Tired from 23:00 (a toast); from 01:30 the edges darken and you walk 10% slower; at 02:00 you pass out where you
    //   stand, which counts as one player sleeping.
    // - Co-op: the host runs the skip. Everyone else gets "Pip is going to sleep · night skips in 5 s"; a crewmate in a
    //   fight holds it up to 10 s more ("Waiting for Rani's fish…"); two sleepers make one skip. Then every machine shows
    //   the night card and wakes its own player per §1.4.2: in the bed, on the boat (left loose: it anchors itself), at
    //   the tiller, on a ladder if swimming, in the free bed within 30 m, else where they stood.
    // - The skip: Sky.Jump(the next 07:00): everything that reads the clock moves with it (light, weather; rot later).
    public class Sleep : MonoBehaviour
    {
        public static Sleep I;
        public static bool Frozen;                  // the night card is up: nobody walks
        public static bool Off;                     // tests that drive the clock through the night (-pb-daytest)
        public static float WalkMul = 1;            // 0.9 from 01:30 (tired)
        public static Transform Cot; public static float CotH = 0.5f;
        public static System.Action<string> OnLog; // test hooks
        public const float HeadsUp = 5, FightWait = 10, BedHold = 1, NearBed = 30;

        // the local player's own night
        float holdT; bool lying, passedOut, slumped; int tiredDay = -1, heavyDay = -1, passDay = -1;
        public string LastWake;                     // how I woke last time (tests)
        public bool Skipping;                       // a skip is under way (heads-up, waiting, the card)
        // the host's: who asked, when
        bool hostRun;
        Image vignette, fade; RectTransform card, cardRows; Text cardTitle, cardBody;
        // the night card's rows (built fresh each night): labels left, amounts right, like a page of the Ledger
        const float CardW = 540, CardTop = 140; float cy; static readonly Color Money = Ponyo.Hex("3E9A4E");
        void CardClear() { for (int i = cardRows.childCount - 1; i >= 0; i--) Destroy(cardRows.GetChild(i).gameObject); cy = 0; }
        Text CT(string s, Font f, int size, Color c, TextAnchor a, float x, float w, float h)
        { var t = UiKit.Txt(cardRows, s, f, size, c, a); t.supportRichText = true; t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -cy), new Vector2(w, h), new Vector2(0, 1)); return t; }
        void CardRow(string l, string r, Color rc, Font rf = null, int size = 20, float h = 32)
        { CT(l, UiKit.Body, Mathf.Min(size, 20), Ponyo.Ink, TextAnchor.MiddleLeft, 0, CardW * 0.7f, h); CT(r, rf ?? UiKit.Bold, size, rc, TextAnchor.MiddleRight, 0, CardW, h); cy += h + 2; }
        void CardCaps(string s, string right = null)
        {
            CT(UiKit.Caps(s), UiKit.Bold, 13, Ponyo.InkSoft, TextAnchor.MiddleLeft, 0, CardW, 20);
            if (right != null) CT(right, UiKit.Body, 14, Ponyo.InkSoft, TextAnchor.MiddleRight, 0, CardW, 20);
            cy += 26;
        }
        void CardRule() { var i = UiKit.Img(cardRows, null, Ponyo.Paper3, "rule"); i.rectTransform.Place(new Vector2(0, 1), new Vector2(0, -cy - 5), new Vector2(CardW, 2), new Vector2(0, 1)); cy += 12; }

        public static void Create()
        {
            var g = new GameObject("Sleep"); I = g.AddComponent<Sleep>();
            I.BuildCot(); I.BuildUi();
        }

        static void Log(string s) { Debug.Log("[PB] sleep: " + s); OnLog?.Invoke(s); }

        // ---- Walt's cot (a bed you can't buy yet: no "Rested" buff, economy §2.3). Built in code beside the shack's door.
        void BuildCot()
        {
            var l = Landing.I; if (l == null || l.Spawn == null || l.LadderTop == null) return;
            Vector3 B(float x, float y) => Sky.FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(x, y, 0));
            // along the shack's front wall, under the window, on the ground in front of it
            Vector3 a = B(-7.6f, 11.35f), b = B(-5.9f, 11.35f), c = (a + b) / 2;
            float gy = 0;
            if (Physics.Raycast(new Vector3(c.x, 60, c.z), Vector3.down, out var hit, 120, ~0, QueryTriggerInteraction.Ignore)) gy = hit.point.y;
            var root = new GameObject("WaltsCot").transform; root.SetParent(l.transform, true);
            var along = b - a; along.y = 0;
            root.position = new Vector3(c.x, gy, c.z); root.rotation = Quaternion.LookRotation(along.normalized); // local z = the cot's length
            var mat = Game.ToonMat;
            Color wood = Ponyo.Hex("8A6A4E"), canvas = Ponyo.Hex("D9CBA8"), stripe = Ponyo.Hex("3A86E0"), pillow = Ponyo.Hex("F3EBDD"), blanket = Ponyo.Hex("C9584A");
            void Box(string n, Vector3 p, Vector3 s, Color col, float rx = 0)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Cube); q.name = n; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root, false); q.transform.localPosition = p; q.transform.localRotation = Quaternion.Euler(rx, 0, 0); q.transform.localScale = s;
                var m = new Material(mat); m.SetColor("_BaseColor", col); q.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            CotH = 0.46f;
            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) Box("leg", new Vector3(i == 0 ? -0.36f : 0.36f, 0.2f, j == 0 ? -0.86f : 0.86f), new Vector3(0.06f, 0.42f, 0.06f), wood, (i ^ j) == 0 ? 6 : -6);
            Box("rail L", new Vector3(-0.38f, 0.38f, 0), new Vector3(0.06f, 0.06f, 1.95f), wood); Box("rail R", new Vector3(0.38f, 0.38f, 0), new Vector3(0.06f, 0.06f, 1.95f), wood);
            Box("canvas", new Vector3(0, 0.39f, 0), new Vector3(0.74f, 0.04f, 1.86f), canvas);
            Box("stripe", new Vector3(0, 0.413f, 0), new Vector3(0.745f, 0.01f, 0.16f), stripe);
            Box("pillow", new Vector3(0, 0.46f, -0.72f), new Vector3(0.52f, 0.1f, 0.3f), pillow, -6);
            Box("blanket", new Vector3(0, 0.425f, 0.45f), new Vector3(0.76f, 0.05f, 0.85f), blanket);
            var col = root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.25f, 0); col.size = new Vector3(0.8f, 0.5f, 1.95f);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Cot = root;
            Log($"cot at {root.position.x:F1} {root.position.y:F2} {root.position.z:F1}");
        }

        // where you stand to get in or out: beside the cot, on the sea side, facing it
        public static Vector3 BedSide => Cot.position + Cot.right * 0.75f * Side + Vector3.up * 0.05f;
        public static Vector3 FarSide => Cot.position - Cot.right * 0.75f * Side + Vector3.up * 0.05f;
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        static float Side => Landing.I != null && Vector3.Dot(Landing.I.Spawn.position - Cot.position, Cot.right) < 0 ? -1 : 1;

        // Angler.Interact asks: the prompt on the cot (and hold E 1 s to lie down)
        public static string BedPrompt(Angler me)
        {
            if (I == null || Cot == null || me.Remote || me.OnBoat || me.Swimming || I.Skipping && !I.lying) return null;
            if (I.lying) return "Asleep…";
            var d = Cot.position - me.transform.position; d.y = 0;
            if (d.magnitude > 1.7f) { I.holdT = 0; return null; }
            if (!DayTime.BedOk(Sky.Minutes)) { I.holdT = 0; return "Too early. Back after 8 PM."; }
            if (Ctl.InteractHeld) { I.holdT += Time.deltaTime; if (I.holdT >= BedHold) { I.holdT = 0; I.LieDown(me); } }
            else I.holdT = 0;
            return I.holdT > 0 ? $"Sleeping… {Mathf.CeilToInt((BedHold - I.holdT) * 10) / 10f:0.0}" : "Hold to sleep (skips to 7:00 AM)";
        }

        public bool Lying => lying;
        public bool Still => lying || slumped || passedOut && Skipping; // in the cot, or out cold waiting for the skip: no walking
        public bool CardUp => card != null && card.gameObject.activeSelf;
        public bool CotTaken;                       // someone is in the cot (co-op: the host says who)

        void LieDown(Angler me)
        {
            lying = true; Log($"lies down at {DayTime.Clock(Sky.Minutes)}");
            me.Fish.Stow();
            // the foot end, facing away from the pillow, so lying back puts the head on it
            var foot = Cot.position + Cot.forward * 1.25f; foot.y = Cot.position.y; // (heels just past the end: clear of the cot's collider)
            me.Teleport(foot, Quaternion.LookRotation(Cot.forward).eulerAngles.y);
            me.LieH = CotH + 0.2f;
            StartCoroutine(Ease(me, 0, 1, 1f));
            Request(Kind.Bed);
        }

        public void TestLie() => LieDown(Angler.I); // (tests: the one-cot latency race's outcome, lying down after a crewmate did)

        IEnumerator Ease(Angler me, float a, float b, float s) { for (float t = 0; t < s; t += Time.deltaTime) { me.Lie = Mathf.SmoothStep(a, b, t / s); yield return null; } me.Lie = b; }

        // ---- tired, heavy, pass-out (the local player, every machine for itself)
        void Update()
        {
            var me = Angler.I; if (me == null || Title.Shown || Off) return;
            double m = Sky.Minutes; int day = DayTime.Day(m - DayTime.Wake); // (the "awake day" runs 07:00 -> 07:00)
            if (DayTime.Tired(m) && tiredDay != day && !Skipping) { tiredDay = day; Alerts.Push("Getting tired", "It's late. Walt's cot is by the shack. Hold E on it to sleep.", Alerts.Kind.Info); }
            bool heavy = DayTime.Heavy(m) && !Skipping;
            if (heavy && heavyDay != day) { heavyDay = day; Log("heavy eyes (01:30): vignette, walk -10%"); }
            WalkMul = heavy ? 0.9f : 1;
            float va = heavy ? Mathf.Clamp01((float)(DayTime.OfDay(m) - DayTime.HeavyAt) / 20f) * 0.55f : 0;
            if (vignette != null) { vignette.color = new Color(0.07f, 0.09f, 0.2f, Mathf.MoveTowards(vignette.color.a, va, Time.deltaTime * 0.5f)); vignette.enabled = vignette.color.a > 0.005f; }
            if (DayTime.PassOut(m) && passDay != day && !Frozen && !lying) // (a crewmate's heads-up doesn't keep you up: you still drop, and it stays one skip)
            {
                passDay = day; passedOut = true;
                Log($"passed out at {DayTime.Clock(m)}{(me.Swimming ? " (swimming)" : me.AtHelm ? " (at the tiller)" : "")}");
                me.Fish.Abort();
                StartCoroutine(Faint(me));
                Request(Kind.PassOut);
            }
        }

        IEnumerator Faint(Angler me)
        {
            yield return null; yield return null;          // (a line out reels in first: no knock-down with a line out)
            if (me.OnBoat || me.AtHelm)
            {
                // on a boat: no ragdoll (the knock carries the boat's speed and threw the driver overboard); throttle off,
                // slump onto the deck where you are
                var s = Skiff.I; if (me.AtHelm) me.LeaveHelm(); if (s != null && !s.Remote) s.Notch = 0;
                slumped = true; me.LieH = 0.25f; StartCoroutine(Ease(me, 0, 1, 0.8f));
            }
            else if (!me.Swimming) me.Knock(me.transform.forward * 0.6f + Vector3.up * 0.2f);
        }

        // ---- asking for the skip: solo runs it here; co-op asks the host
        public enum Kind : byte { Bed, PassOut }

        void Request(Kind k)
        {
            if (!Coop.On) { if (!Skipping) StartCoroutine(Night(null, k, DayTime.NextWake(Sky.Minutes), Angler.I.NetName ?? "You")); return; }
            if (Coop.IsHost) HostAsk(Coop.Me, k);
            else Coop.AskSleep(k);
        }

        // host: one skip at a time (two sleepers = one skip), heads-up, then wait for fights, then go
        public void HostAsk(ulong who, Kind k)
        {
            var e = Coop.I.Find(who); string name = e != null ? e.Name : "Someone";
            if (k == Kind.Bed) CotTaken = true;
            if (hostRun) { Log($"{name} {(k == Kind.Bed ? "goes to bed" : "passes out")} too: one skip"); return; }
            hostRun = true;
            StartCoroutine(HostRun(who, k, name));
        }

        IEnumerator HostRun(ulong who, Kind k, string name)
        {
            Log($"host: {name} {(k == Kind.Bed ? "is going to sleep" : "passed out")} at {DayTime.Clock(Sky.Minutes)}: heads-up {HeadsUp} s");
            Coop.Night(0, who, k, name, 0);
            HeadsUpLocal(who, k, name);
            yield return new WaitForSeconds(HeadsUp);
            // a crewmate mid-fight: wait up to 10 s for it to resolve
            string fighter = null; float w = 0;
            while (w < FightWait && (fighter = Fighter()) != null)
            {
                if (w == 0) { Log($"host: waiting for {fighter}'s fish"); Coop.Night(1, who, k, fighter, 0); Alerts.Push("Night", $"Waiting for {fighter}'s fish…", Alerts.Kind.Info); }
                w += Time.deltaTime; yield return null;
            }
            if (w > 0) Log($"host: {(fighter == null ? "the fight ended" : "still fighting")} after {w:F1} s: the skip goes");
            double to = DayTime.NextWake(Sky.Minutes);
            Coop.Night(2, who, k, name, to);
            yield return Night(who, k, to, name);
            hostRun = false; CotTaken = false;
        }

        // who is fighting a fish right now (the host's own player, then each crewmate as their poses show)
        static string Fighter()
        {
            var me = Angler.I; var c = Coop.I;
            if (c == null) return null;                    // (the host left its crew mid heads-up: no one to wait for)
            if (me != null && me.Fish.Fighting) return c.Find(Coop.Me)?.Name ?? "Host";
            foreach (var kv in c.Mates) if (kv.Value != null && kv.Value.Fish.Fighting) return Coop.I.Find(kv.Key)?.Name ?? kv.Value.NetName;
            return null;
        }

        // a client whose host left: a skip still in its heads-up never comes. In the cot or out cold: sleep alone; awake:
        // carry on (the night card already up finishes on its own)
        public void HostGone()
        {
            if (!Skipping || Frozen || hostRun) return; // (a host leaving its own skip just carries on)
            if (lying || passedOut) { Log("the host left mid heads-up: sleeping alone"); StartCoroutine(Night(null, lying ? Kind.Bed : Kind.PassOut, DayTime.NextWake(Sky.Minutes), Angler.I.NetName ?? "You")); }
            else { Log("the host left mid heads-up: no skip"); Skipping = false; CotTaken = false; }
        }

        // every machine: the heads-up toast for the others
        public void HeadsUpLocal(ulong who, Kind k, string name)
        {
            Skipping = true;
            if (Coop.On && who == Coop.Me) return;
            Alerts.Push("Night", k == Kind.Bed ? $"{name} is going to sleep · night skips in {HeadsUp:0} s" : $"{name} passed out · night skips in {HeadsUp:0} s", Alerts.Kind.Info, secs: HeadsUp);
            if (k == Kind.Bed) CotTaken = true;
        }

        // ---- the skip itself, on every machine: fade, wake where §1.4.2 says, jump the clock, the night card, fade in
        public IEnumerator Night(ulong? who, Kind k, double to, string name)
        {
            Skipping = true; Frozen = true;
            var me = Angler.I; double from = Sky.Minutes; int fromDay = DayTime.Day(from - DayTime.Wake);
            bool mine = who == null || Coop.On && who == Coop.Me; // (a host that left its crew mid heads-up still runs the rider's skip: not mine)
            Log($"night: {name} {(k == Kind.Bed ? "slept" : "passed out")}, {DayTime.Clock(from)} Day {DayTime.Day(from)} -> {DayTime.Clock(to)} Day {DayTime.Day(to)}");
            Hud.I?.Quiet(true);                            // the toasts fade with the day; new ones wait for the morning
            yield return Fade(0, 1, 1.2f);
            // where I wake
            string how = Wake(me, mine && k == Kind.Bed);
            LastWake = how;
            var p = me.transform.position;
            Log($"wake {how} at {p.x:F2} {p.y:F2} {p.z:F2}{(Skiff.I != null ? $" · Biscuit {(Skiff.I.Moored ? "moored" : Skiff.I.Anchored ? "anchored" : "loose")}" : "")}");
            Sky.Jump(to); // (a client sets it too: the host's next "pb.day" agrees)
            // the night card: the day that ended, tomorrow's forecast with how sure Dot is
            int newDay = DayTime.Day(to);
            cardTitle.text = $"Day {fromDay} · {DayTime.DayName(fromDay)} is over";
            cardBody.text = k == Kind.PassOut ? $"{name} passed out at {DayTime.Clock(from)}." : $"{name} slept through the night.";
            CardClear();
            // M4: the day's money as a little ledger (each of the crew), Mari's rent this morning, the bank, fish about to
            // turn; then tomorrow's forecast with how sure Dot is; then the save
            var co = Firm.C; bool saved = false;
            if (co != null)
            {
                if (Sky.Authority) { co.Tick(Sky.Minutes); saved = Save.Enabled && Save.Write("morning"); }
                var (inS, outS, by) = co.DaySummary(DayTime.At(fromDay, DayTime.Wake), to);
                by.RemoveAll(x => x.who == Company.MariName);
                int sales = 0; foreach (var x in by) sales += x.amount;
                CardCaps("The day");
                CardRow("Sales", $"+${sales:N0}", Ponyo.Ink);
                if (Coop.On && by.Count > 1) foreach (var x in by) CardRow("    " + x.who, $"+${x.amount:N0}", Ponyo.InkSoft, UiKit.Body, 18, 28);
                if (outS > 0) CardRow("Spent", $"−${outS:N0}", Ponyo.Ink);
                var rent = co.Ledger.Find(e => e.Tx == $"rent:mari:{DayTime.Day(to - DayTime.Wake)}");
                if (rent != null) CardRow("Mari's rent at 7 AM", $"+${rent.Amount}", Money);
                CardRule();
                CardRow("Company bank", $"${co.Cash:N0}", Ponyo.Ink, UiKit.DisplayBold, 26, 40);
                int turning = 0; if (me != null) foreach (var s in me.Bar.S) foreach (var f in s.Fish) if (f.Fresh >= 2) turning++;
                if (turning > 0) { CT($"{turning} fish {(turning == 1 ? "is" : "are")} going off: sell today", UiKit.Bold, 17, Ponyo.Poppy, TextAnchor.MiddleLeft, 0, CardW, 28); cy += 30; }
                cy += 12;
            }
            CardCaps($"Tomorrow · {DayTime.DayName(newDay)}, Day {newDay}", "how sure Dot is");
            for (int b = 0; b < 3; b++)
            {
                var wx = Sky.W.Shown(newDay, b, 0); int dots = Weather.Dots(b, 0); var ink = wx == Wx.Storm ? Ponyo.Poppy : Ponyo.Ink;
                var ic = UiKit.Img(cardRows, Glyphs.Get(wx.ToString()), ink, "icon"); ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, -cy - 3), new Vector2(28, 28), new Vector2(0, 1));
                CT(Weather.BlockName[b], UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleLeft, 44, 160, 34);
                CT(wx.ToString(), UiKit.Body, 19, ink, TextAnchor.MiddleLeft, 190, 200, 34);
                CT($"<color=#3F2C27>{new string('●', dots)}</color>{new string('●', 4 - dots)}", UiKit.Body, 16, Ponyo.Paper3, TextAnchor.MiddleRight, 0, CardW, 34);
                cy += 36;
            }
            if (saved) { cy += 10; CT("Saved ✓", UiKit.Body, 15, Ponyo.InkSoft, TextAnchor.MiddleCenter, 0, CardW, 22); cy += 22; }
            card.sizeDelta = new Vector2(660, CardTop + cy + 36);
            yield return CardFade(true);
            yield return new WaitForSeconds(Application.isBatchMode ? 0.5f : 4f);
            yield return CardFade(false);
            Hud.I?.Quiet(false);
            if (slumped) { me.Lie = 0; slumped = false; }
            if (lying) { me.Lie = 0; lying = false; var at = mine ? BedSide : FarSide; me.Teleport(at, Quaternion.LookRotation(Flat(Cot.position - at)).eulerAngles.y + 180); } // (two in the one cot, a latency race: the second steps out the other side)
            Frozen = false;
            Snd.Play2D(Synth.Horn, 0.4f); Log("the ferry horn: 07:00");
            yield return Fade(1, 0, 1f);
            Skipping = false; CotTaken = false;
            Alerts.Push($"Day {newDay} · {DayTime.DayName(newDay)}", $"7:00 AM. {Sky.Word(Sky.W.Block(newDay, 0))}", Alerts.Kind.Weather);
            if (passedOut && mine) Alerts.Push("Grandpa Walt", "Bed's for sleeping, kid.", Alerts.Kind.Message);
            passedOut = false;
        }

        // §1.4.2: the local player's wake spot. Returns which case it was.
        string Wake(Angler me, bool inBed)
        {
            var s = Skiff.I;
            bool ownBoat = s != null && !s.Remote && (!Coop.On || Coop.I.BoatOwner == Coop.Me);
            if (ownBoat && !s.Moored && !s.Anchored) { s.AnchorNow(); Log("Biscuit was loose: anchored where she is"); }
            me.Fish.Abort();
            if (inBed || lying) return "in bed";
            if (me.AtHelm) return "at the tiller";
            if (me.Swimming)
            {
                // the nearest ladder: the boat's or the pier's
                Transform best = null; float bd = 1e9f;
                foreach (var t in new[] { s != null ? s.LadderTop : null, Landing.I != null ? Landing.I.LadderTop : null })
                    if (t != null) { float d = (t.position - me.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = t; } }
                if (best != null)
                {
                    var at = best.position + Vector3.up * 0.1f;
                    if (s != null && best == s.LadderTop) at = s.transform.TransformPoint(s.transform.InverseTransformPoint(best.position) + new Vector3(0, 0.05f, -0.5f));
                    me.Teleport(at, best.eulerAngles.y);
                    return best == (s != null ? s.LadderTop : null) ? "on the boat's ladder (damp)" : "on the pier ladder (damp)";
                }
            }
            if (passedOut) return "where I fell";
            if (me.OnBoat) return "on the boat";
            if (Cot != null && !CotTaken && Vector3.Distance(me.transform.position, Cot.position) < NearBed)
            {
                me.Teleport(BedSide, Quaternion.LookRotation(Cot.position - BedSide).eulerAngles.y + 180);
                Alerts.Push("Night", "You went to bed too.", Alerts.Kind.Info);
                return "in the free bed";
            }
            return "where I stood";
        }

        // the night card fades and rises in (0.35 s), and fades out (0.3 s): never a hard cut
        IEnumerator CardFade(bool show)
        {
            var g = card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>();
            var home = card.anchoredPosition; card.gameObject.SetActive(true);
            float s = show ? 0.35f : 0.3f;
            for (float t = 0; t < s; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / s), v = show ? k : 1 - k;
                g.alpha = v; card.anchoredPosition = home + new Vector2(0, show ? -14 * (1 - k) : 0); yield return null;
            }
            g.alpha = show ? 1 : 0; card.anchoredPosition = home;
            if (!show) { card.gameObject.SetActive(false); g.alpha = 1; }
        }

        IEnumerator Fade(float a, float b, float s)
        {
            fade.enabled = true;
            for (float t = 0; t < s; t += Time.unscaledDeltaTime) { fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, Mathf.SmoothStep(a, b, t / s)); yield return null; }
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, b); fade.enabled = b > 0.001f;
        }

        // ---- the UI: a soft night vignette, the fade, the night card (paper, Fraunces title)
        void BuildUi()
        {
            var hud = Hud.I.transform;
            var vt = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 64f, dy = (y - 63.5f) / 64f, r = Mathf.Sqrt(dx * dx * 0.75f + dy * dy);
                    vt.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1.15f, r))));
                }
            vt.Apply();
            vignette = UiKit.Img(hud, Sprite.Create(vt, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f)), new Color(0.07f, 0.09f, 0.2f, 0), "tired");
            vignette.rectTransform.Stretch(); vignette.rectTransform.SetAsFirstSibling(); vignette.enabled = false;
            fade = UiKit.Img(hud, null, new Color(Ponyo.Night.r, Ponyo.Night.g, Ponyo.Night.b, 0), "night fade"); fade.rectTransform.Stretch(); fade.enabled = false;
            card = UiKit.Card(hud, "night card", 26, Ponyo.Paper, true);
            card.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 600));
            cardTitle = UiKit.Txt(card, "", UiKit.Display, 40, Ponyo.Ink, TextAnchor.UpperCenter); cardTitle.rectTransform.Stretch(30);
            cardBody = UiKit.Txt(card, "", UiKit.Body, 20, Ponyo.InkSoft, TextAnchor.UpperCenter); cardBody.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(600, 28), new Vector2(0.5f, 1));
            cardRows = UiKit.Rect("rows", card).Place(new Vector2(0.5f, 1), new Vector2(0, -CardTop), new Vector2(CardW, 10), new Vector2(0.5f, 1));
            card.gameObject.SetActive(false);
        }
    }

    // the sleep messages (co-op): a client asks the host; the host tells everyone (0 heads-up, 1 waiting for a fish,
    // 2 go to `to`)
    public partial class Coop
    {
        void RegisterSleep(CustomMessagingManager m) { Reg(m, "pb.sleep", OnSleep); Reg(m, "pb.night", OnNight); }

        public static void AskSleep(Sleep.Kind k) { if (On && !IsHost) I.Send("pb.sleep", w => w.WriteValueSafe((byte)k), NetworkDelivery.ReliableSequenced); }
        void OnSleep(ulong from, FastBufferReader r) { r.ReadValueSafe(out byte k); Deliver(() => { if (IsHost) Sleep.I?.HostAsk(from, (Sleep.Kind)k); }, true); }

        public static void Night(byte phase, ulong who, Sleep.Kind k, string name, double to)
        {
            if (!IsHost) return;
            foreach (var id in I.nm.ConnectedClientsIds) if (id != I.nm.LocalClientId)
                I.SendTo(id, "pb.night", w => { w.WriteValueSafe(phase); w.WriteValueSafe(who); w.WriteValueSafe((byte)k); w.WriteValueSafe(name); w.WriteValueSafe(to); }, NetworkDelivery.ReliableSequenced);
        }

        void OnNight(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte phase); r.ReadValueSafe(out ulong who); r.ReadValueSafe(out byte k); r.ReadValueSafe(out string name); r.ReadValueSafe(out double to);
            Deliver(() =>
            {
                var s = Sleep.I; if (s == null) return;
                if (phase == 0) s.HeadsUpLocal(who, (Sleep.Kind)k, name);
                else if (phase == 1) Alerts.Push("Night", $"Waiting for {name}'s fish…", Alerts.Kind.Info);
                else s.StartCoroutine(s.Night(who, (Sleep.Kind)k, to, name));
            }, true);
        }
    }
}
