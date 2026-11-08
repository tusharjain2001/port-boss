using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-storytest <dir> (M7 step 1): the story driver in the game, solo. Walt's three texts at 07:02; the rod up ->
        // journal page 1 as a side card; a real cast from the quay -> Walt's "wave" text 2 minutes later; the first sale ->
        // his two texts; 9 PM -> lights out; Day 2: Pratt's price list at 7, Walt's call at 8 (it rings, Tab opens the
        // call screen, Answer plays the captions to the end); a call declined = missed, its voicemail in the thread;
        // Day 4: Walt's letter (read on its card); the Journal (pages + letters); Day 5 6 PM: Act 2, Rosie's text, the Crew
        // app unlocked. Pictures of every card (-pb-fps 60).
        IEnumerator StoryTestRun(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] storytest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            var C = Firm.C; var S = StoryUi.I;
            int Walt() => Firm.C.Msgs.FindAll(m => m.From == Company.WaltName && !m.Me).Count;
            yield return new WaitForSeconds(1f);
            Ok(S != null && C != null, "the story UI is up");
            // 1) 07:02: Walt's three texts, each a LEFT toast
            yield return Until(() => Walt() >= 3, 8);
            Ok(Walt() == 3, $"Walt's 3 morning texts by 07:02 ({Walt()})");
            Ok(Alerts.Queue.Exists(a => a.Sender == Company.WaltName && a.Text.StartsWith("Morning")), "his first text was a toast");
            if (shots != null) yield return Shot("walt-texts-toasts");
            // 2) the rod up on the quay's edge: page 1, the side card
            var port = Landing.I != null ? Landing.I.transform.Find("Port") : null;
            Vector3 P(float x, float y) => port.TransformPoint(new Vector3(-x, 1.32f, -y));
            if (port != null) me.Teleport(P(-8, -0.9f), 180);
            yield return new WaitForSeconds(0.5f);
            if (!me.Bar.Sel.Rod) me.Select(0);
            yield return Until(() => fish.State == Fishing.St.Ready, 4);
            yield return Until(() => S.Showing != null, 3);
            Ok(C.Pages.Count == 1 && S.Showing != null && S.Showing.Key == "cast", $"the rod up: Walt's journal page 1 on the side ({C.Pages.Count} pages, showing {S.Showing?.Title})");
            if (shots != null) { yield return new WaitForSeconds(1.2f); yield return Shot("journal-page-1"); }
            // 3) a real cast: Walt's "wave" text 2 minutes (2 s) later
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
            yield return new WaitForSeconds(0.8f); Ctl.BotUse = false;
            yield return Until(() => C.Seen("cast"), 3);
            Ok(C.Seen("cast"), $"the cast is a story event (the rod is {fish.State})");
            yield return Until(() => Walt() >= 4, 6);
            Ok(Walt() == 4 && Firm.C.Msgs.FindLast(m => m.From == Company.WaltName).Text.StartsWith("Dot says"), "Walt: Dot can see you, wave");
            fish.Abort(); yield return new WaitForSeconds(0.5f); me.Select(1); yield return new WaitForSeconds(0.5f);
            // 4) the first sale (at Pratt's, through the company): Walt's two texts a minute later
            var mk = new Company.Lot { Species = "mackerel", Everyday = true, Value = 10, Fresh = 0 };
            Ok(C.Sell(Firm.I.NewTx("sell"), Firm.MyName, Buyer.Pratt, new[] { mk }, Sky.Minutes, out _) != null, "sold a mackerel at Pratt's");
            yield return Until(() => Walt() >= 6, 5);
            Ok(Walt() == 6 && Firm.C.Msgs.FindLast(m => m.From == Company.WaltName).Text.Contains("THE PELICAN"), "Walt after the first sale: Pratt, then THE PELICAN");
            // 5) 9 PM: lights out
            JumpTo(1, 21); yield return Until(() => Walt() >= 7, 3);
            Ok(Firm.C.Msgs.FindLast(m => m.From == Company.WaltName).Text.StartsWith("lights out"), "9 PM: lights out here");
            // 6) Day 2: Pratt's price list at 7; Walt's call at 8
            JumpTo(2, 7); yield return Until(() => Firm.C.Msgs.Exists(m => m.From == Company.PrattName), 3);
            var pr = Firm.C.Msgs.Find(m => m.From == Company.PrattName);
            Ok(pr != null && pr.Text.Contains("Mackerel $"), $"Pratt's price list at 7 ({pr?.Text})");
            JumpTo(2, 8); yield return Until(() => S.Ringing != null, 3);
            Ok(S.Ringing != null && S.Ringing.From == Company.WaltName, "8 AM: Walt is calling");
            if (shots != null) yield return Shot("call-ringing-toast");
            Phone.I.Show(); yield return null; yield return null;
            Ok(Phone.I.Screen == Phone.Scr.Call, $"Tab while it rings: the call screen ({Phone.I.Screen})");
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("call-screen"); }
            Ok(Phone.I.Tap("Answer"), "Answer");
            yield return null;
            Ok(S.Talking && !Phone.IsOpen, $"on the phone: the captions run ({S.Caption})");
            if (shots != null) { yield return new WaitForSeconds(1.2f); yield return Shot("call-captions"); }
            yield return Until(() => !S.Talking, 40);
            Ok(!S.Talking && S.Answered.Count == 1, "the call played to its end");
            // a second call, declined: missed, its voicemail in the thread
            var k2 = C.Ring(Company.WaltName, Sky.Minutes, "It's me again. Never mind. Go fishing."); Firm.I.Dirty = true;
            yield return Until(() => S.Ringing == k2, 3);
            Phone.I.Show(); yield return null; yield return null;
            Ok(Phone.I.Tap("Decline"), "Decline the second call");
            yield return null;
            Ok(S.Missed.Contains(k2.Id) && S.CallState(k2) == "Missed", "a declined call is missed");
            Phone.I.Show(Phone.Scr.Thread, Company.WaltName); yield return null; yield return null;
            Ok(Phone.I.Screen == Phone.Scr.Thread, "Walt's thread: texts, the call and the voicemail");
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("walt-thread"); }
            Phone.I.Hide(); yield return new WaitForSeconds(0.3f);
            // 7) Day 4, 07:00: Walt's letter; read it on its card
            var desk = C.Letters.Find(x => x.Where == "desk");
            Ok(desk != null && desk.Lines.Length == 6, "Walt's letter on the desk (Day 1, 07:00, six lines)");
            if (desk != null) { S.Open(desk); yield return null; S.Close(); yield return null; }
            JumpTo(4, 7); yield return Until(() => S.Post() != null, 3); yield return null;
            var mail = S.Post();
            Ok(mail != null && C.Letters.Count == 2 && S.UnreadLetters() == 1, $"Day 4: Walt's letter in the mailbox ({C.Letters.Count} letters, unread {S.UnreadLetters()})");
            Ok(Phone.I.Unread(Company.WaltName) >= 1, "the unread letter shows on the phone");
            // read it at the mailbox: walk up to it, E
            me.Teleport(S.MailboxFront, S.MailboxYaw); yield return new WaitForSeconds(0.5f);
            Ok(me.Prompt != null && me.Prompt.StartsWith("Read the post"), $"at the mailbox: \"{me.Prompt}\"");
            if (shots != null) yield return Shot("mailbox-flag-up");
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null;
            Ok(StoryUi.LetterOpen && S.Reading == mail, "E at the mailbox: Walt's letter");
            if (!StoryUi.LetterOpen && mail != null) S.Open(mail);
            yield return null;
            Ok(StoryUi.LetterOpen && Ctl.Ui == 2, "the letter's paper card (input held)");
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("letter-day4"); }
            S.Close(); yield return null;
            Ok(!StoryUi.LetterOpen && Ctl.Ui == 0 && S.UnreadLetters() == 0 && S.Post() == null, "put it down: read (the mailbox's flag goes down)");
            Phone.I.Show(Phone.Scr.Journal); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("journal-app"); }
            Phone.I.Hide(); yield return new WaitForSeconds(0.3f);
            // 8) Day 5, 6 PM: Act 2 (Act 1's goal not met: the latest it comes), Rosie's text, the Crew app open
            JumpTo(5, 17.9); yield return null; Ok(C.Act == 1, "still Act 1 at 5:54 PM on Day 5");
            JumpTo(5, 18); yield return Until(() => Firm.C.Act == 2, 3);
            Ok(Firm.C.Act == 2 && Firm.C.Msgs.Exists(m => m.From == Company.WaltName && m.Text.StartsWith("Rosie")), $"Day 5, 6 PM: Act 2 and Walt's text about Rosie (act {Firm.C.Act})");
            Ok(Firm.C.CanBuy(Firm.MyKey, "rod:3", 1) != "Act 2", $"the Carbon rod isn't act-locked any more ({Firm.C.CanBuy(Firm.MyKey, "rod:3", 1)})");
            Phone.I.Show(); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("phone-home-act2"); }
            Phone.I.Hide();
            Ok(Firm.C.Check() == "", $"the company's invariants ({Firm.C.Check()})");
            Debug.Log($"[PB] RESULT storytest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-openingtest <dir> (M7 step 2, with -pb-opening -pb-coldstart): a new game's first morning. Black, then Walt's cot
        // (lying, the gulls), up, Walt's letter on the desk; Biscuit half full of rainwater and cold; at the tiller W pulls
        // the cord: flooded, she coughs; bail her (hold E on deck) below a quarter; three pulls and she catches (Walt's
        // "Is that my Biscuit" text); W/S drive her out. Stills of every beat (-pb-fps 60).
        IEnumerator OpeningTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] openingtest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.deltaTime) yield return null; }
            var S = StoryUi.I; var s = Skiff.I;
            yield return Until(() => S.InOpening, 3);
            Ok(S.InOpening, "a new game opens with the opening");
            yield return new WaitForSeconds(1.4f);
            yield return new WaitForSeconds(2.8f);
            Ok(Works.I.InOffice && Vector3.Distance(me.transform.position, Sleep.BedSide) < 0.8f, "in Walt's office, by his cot (the camera inside)");
            if (shots != null) yield return Shot("opening-in-the-office");
            yield return Until(() => StoryUi.LetterOpen && !S.InOpening, 20);
            Ok(StoryUi.LetterOpen && S.Reading.Where == "desk", "Walt's letter from the desk on its card");
            if (shots != null) { yield return new WaitForSeconds(0.5f); yield return Shot("opening-desk-letter"); }
            S.Close(); yield return null;
            Ok(s != null && s.Cold && Mathf.Abs(s.Water - 0.5f) < 0.05f, $"Biscuit: cold, half full of rainwater ({s?.Water:F2})");
            // the tiller: a pull while she's flooded
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y); yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.6f);
            Ok(me.Prompt != null && me.Prompt.StartsWith("W  Pull the cord"), $"at the tiller: \"{me.Prompt}\"");
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.7f);
            Ok(s.Cold && s.Pulls == 0 && Alerts.Queue.Exists(a => a.Topic == "flooded"), "flooded: she coughs (Bail her out first)");
            if (shots != null) yield return Shot("pull-flooded");
            // bail: hold E on deck until she's under a quarter
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return new WaitForSeconds(0.4f);   // (E: off the tiller)
            me.Teleport(s.transform.TransformPoint(new Vector3(0, 1.0f, 0.4f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            Ctl.BotInteractHeld = true; float w0 = s.Water;
            if (shots != null) { yield return new WaitForSeconds(1.5f); yield return Shot("bailing"); }
            yield return Until(() => s.Water < Skiff.FloodAt - 0.03f, 20);
            Ctl.BotInteractHeld = false;
            Ok(s.Water < Skiff.FloodAt, $"bailed her out: {w0:F2} -> {s.Water:F2}");
            // three pulls: the third catches
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y); yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.6f);
            for (int i = 0; i < 2; i++) { Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.75f); }
            Ok(s.Cold && s.Pulls == 2, $"two pulls: still cold ({s.Pulls})");
            if (shots != null) yield return Shot("pull-two");
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.75f);
            Ok(!s.Cold && Firm.C.Seen("engine"), "the third pull: she caught");
            yield return Until(() => Firm.C.Msgs.Exists(m => m.Text.StartsWith("Is that my Biscuit")), 3);
            Ok(Firm.C.Msgs.Exists(m => m.From == Company.WaltName && m.Text.StartsWith("Is that my Biscuit")), "Walt: Is that my Biscuit I can hear");
            if (shots != null) yield return Shot("she-caught");
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(3f);
            Ok(s.Notch >= 1 && s.Speed > 0.5f, $"W/S drive her now (notch {s.Notch}, {s.Speed:F1} m/s)");
            Ctl.BotThrottle = -1; yield return new WaitForSeconds(0.3f); Ctl.BotThrottle = -1;
            Ok(Firm.C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT openingtest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-pelicantest <dir> (M7 step 3): Amara's order end to end. Day 2 11:30 her order comes (a text with the order
        // card); Deal on the phone; three Rare mackerel in the hotbar; Biscuit moors at The Pelican's jetty (F: a visiting
        // berth); walk to her counter: E delivers, $90 to the bank, Act 1's goal (amara:1); at 7:30 PM Walt: AMARA CALLED ME.
        // Stills: the restaurant from the sea, the boat at the jetty, the counter with Amara, the phone's order card.
        IEnumerator PelicanTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] pelicantest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.deltaTime) yield return null; }
            var C = Firm.C; var s = Skiff.I;
            yield return new WaitForSeconds(1f);
            Ok(Pelican.Root != null && Pelican.Berth != null && Pelican.Counter != null, "The Pelican is on Lantern Key (model, berth, counter)");
            if (Pelican.Root == null) { Debug.Log("[PB] RESULT pelicantest: FAIL (no model)"); Application.Quit(); yield break; }
            // 1) Day 2, 11:30: her order; Deal on the phone
            JumpTo(2, 11.5); yield return Until(() => C.FishOrders.Count > 0, 3);
            var o = C.FishOrders.Count > 0 ? C.FishOrders[0] : null;
            Ok(o != null && o.St == OrdSt.Offered && o.Count == 3 && o.Grade == 1 && o.Each == 30, "Day 2 11:30: Amara orders 3 Rare mackerel at $30");
            Phone.I.Show(Phone.Scr.Thread, Company.AmaraName); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("phone-amara-order"); }
            Ok(Phone.I.Tap("Deal"), "Deal on the phone"); yield return null;
            Ok(o != null && o.St == OrdSt.Active, "the order is on");
            Phone.I.Hide(); yield return new WaitForSeconds(0.3f);
            // 2) three Rare mackerel (and one Basic, which she doesn't take)
            var mk = FishDb.All.Find(x => x.Id == "mackerel");
            for (int i = 0; i < 3; i++) me.Bar.Add(new Fish { Species = mk, Grade = 1, Cm = 34 + i, Kg = 0.5f, CaughtAt = (float)Sky.Minutes });
            me.Bar.Add(new Fish { Species = mk, Grade = 0, Cm = 30, Kg = 0.4f, CaughtAt = (float)Sky.Minutes });
            // 3) Biscuit to The Pelican's jetty: F moors her at the visiting berth
            var bp = Pelican.Berth.position - Pelican.Berth.forward * 3f + Pelican.Berth.right * 1.5f;
            s.CastOff(true); s.transform.SetPositionAndRotation(new Vector3(bp.x, s.transform.position.y, bp.z), Pelican.Berth.rotation); s.TakeOver();
            yield return new WaitForSeconds(0.5f);
            Ok(s.CanMoor, "by The Pelican's jetty: she can moor");
            s.Moor(); yield return new WaitForSeconds(2.5f);
            Ok(s.MooredAway && s.AwayName == "The Pelican" && Vector3.Distance(s.transform.position, Pelican.Berth.position) < 1.5f, $"moored at The Pelican ({Vector3.Distance(s.transform.position, Pelican.Berth.position):F1} m off the berth)");
            if (shots != null) { FlyCam(Pelican.Root.position + Pelican.Root.forward * 34 + Pelican.Root.right * -14 + Vector3.up * 9, Pelican.Root.position + Vector3.up * 2.5f); yield return new WaitForSeconds(0.5f); yield return Shot("pelican-from-the-sea"); CameraRig.I.enabled = true; }
            // 4) walk from the jetty to her counter; E delivers
            var stall = Firm.I.Stalls.Find(x => x.K == Firm.Kind.Pelican);
            me.Teleport(Pelican.Root.TransformPoint(new Vector3(0, 1.45f, 18f)), Pelican.Root.eulerAngles.y + 180); yield return new WaitForSeconds(0.6f);   // (on the jetty: the model's -Y is the game's +Z)
            Ok(!me.Swimming, $"on the jetty ({me.transform.position.y:F2} m up)");
            var to = stall.Front;
            for (float t = 0; t < 30; t += Time.deltaTime)
            {
                var d = to - me.transform.position; d.y = 0; if (d.magnitude < 0.6f) break;
                CameraRig.I.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up; yield return null;
            }
            Ctl.BotMove = Vector2.zero; CameraRig.I.Yaw = Quaternion.LookRotation(-stall.At.forward).eulerAngles.y; yield return new WaitForSeconds(0.6f);
            Debug.Log($"[PB] pelicantest: walked to {me.transform.position.x:F1} {me.transform.position.y:F2} {me.transform.position.z:F1}, the counter's front {to.x:F1} {to.y:F2} {to.z:F1}");
            Ok(Firm.I.Near == stall && me.Prompt != null && me.Prompt.StartsWith("Deliver Amara's order"), $"at her counter: \"{me.Prompt}\"");
            if (shots != null) yield return Shot("pelican-counter");
            if (shots != null) { var side = stall.At.position + stall.At.forward * 2.6f + stall.At.right * 3.2f + Vector3.up * 1.9f; FlyCam(side, stall.At.position + Vector3.up * 1.1f); yield return new WaitForSeconds(0.4f); yield return Shot("pelican-amara"); CameraRig.I.enabled = true; }
            int cash = C.Cash;
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return new WaitForSeconds(0.5f);
            Ok(o.St == OrdSt.Done && C.Cash == cash + 90 && C.Seen("amara:1"), $"delivered: +${C.Cash - cash}, amara:1 {C.Seen("amara:1")}");
            int left = 0; foreach (var sl in me.Bar.S) foreach (var f in sl.Fish) if (f.Species == mk) left++;
            Ok(left == 1, $"her three Rare mackerel left my hotbar (the Basic one stayed: {left})");
            Ok(C.Msgs.Exists(m => m.From == Company.AmaraName && m.Text.StartsWith("These are good")), "Amara: These are good. Not great. Good.");
            // 5) that evening: Walt
            JumpTo(2, 19.6); yield return Until(() => C.Msgs.Exists(m => m.Text.StartsWith("AMARA CALLED ME")), 3);
            Ok(C.Msgs.Exists(m => m.From == Company.WaltName && m.Text.StartsWith("AMARA CALLED ME")), "7:30 PM: Walt: AMARA CALLED ME");
            Ok(C.Check() == "", $"the company's invariants ({C.Check()})");
            Debug.Log($"[PB] RESULT pelicantest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-probe (M7 step 3, once): the ground round Lantern Key, to place The Pelican. Heights on a 20 m grid within 320 m of
        // the lantern_key anchor (land > 0.3 m), the islet's middle, and the shore point on the line from the middle to
        // Walt's Landing (where the jetty should point).
        IEnumerator Probe()
        {
            yield return new WaitForSeconds(3f);
            var a = Firm.Anchor("lantern_key"); var at = a != null ? a.position : new Vector3(-1520, 0, -110);
            Debug.Log($"[PB] probe: lantern_key anchor {at.x:F0} {at.z:F0} ({(a != null ? "found" : "default")})");
            float H(float x, float z) => Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : -99;
            Vector3 sum = Vector3.zero; int n = 0; float top = -99;
            for (int i = -16; i <= 16; i++)
            {
                var row = new System.Text.StringBuilder();
                for (int j = -16; j <= 16; j++)
                {
                    float x = at.x + j * 20, z = at.z + i * 20, h = H(x, z);
                    row.Append(h > 20 ? '#' : h > 8 ? '+' : h > 0.3f ? '.' : h > -98 ? '~' : '?');
                    if (h > 0.3f) { sum += new Vector3(x, 0, z); n++; top = Mathf.Max(top, h); }
                }
                Debug.Log($"[PB] probe: z {at.z + i * 20,6:F0} {row}");
            }
            if (n == 0) { Debug.Log("[PB] probe: no land near the anchor"); Application.Quit(); yield break; }
            var mid = sum / n; var home = Landing.I != null ? Landing.I.transform.position : Vector3.zero;
            var dir = home - mid; dir.y = 0; dir.Normalize();
            Vector3 shore = mid; for (float d = 0; d < 600; d += 1) { var p = mid + dir * d; if (H(p.x, p.z) < 0.05f) { shore = p; break; } }
            float depth20 = -H((shore + dir * 20).x, (shore + dir * 20).z);
            Debug.Log($"[PB] probe: {n} land cells, top {top:F1} m, middle {mid.x:F1} {mid.z:F1}; toward the landing {dir.x:F2} {dir.z:F2}; shore at {shore.x:F1} {shore.z:F1}; 20 m out the sea floor is {depth20:F1} m down");
            for (int k = -4; k <= 4; k++) { var side = Quaternion.Euler(0, k * 10, 0) * dir; Vector3 sp = mid; for (float d = 0; d < 600; d += 1) { var p = mid + side * d; if (H(p.x, p.z) < 0.05f) { sp = p; break; } } Debug.Log($"[PB] probe: bearing {k * 10,4} shore {sp.x:F1} {sp.z:F1}, 2 m in {H((sp - side * 2).x, (sp - side * 2).z):F2} m, 6 m in {H((sp - side * 6).x, (sp - side * 6).z):F2} m, 10 m out {H((sp + side * 10).x, (sp + side * 10).z):F2}"); }
            Application.Quit();
        }

        // -pb-storycoop host|rider (M7 step 1, tools/m7test.sh with -pb-netsim): the story is the crew's. Walt welcomes the
        // rider by name; the rider's rod raises the event on the host and page 1 shows on the rider's screen; Walt's
        // Day 2 call rings on both (the rider answers, the host declines); the Day 4 letter reaches the rider.
        IEnumerator StoryCoop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] storycoop {role}: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            Ok(Coop.Joined && Coop.I.Roster.Count >= 2, "the crew is together");
            if (!Coop.Joined) { Debug.Log($"[PB] RESULT storycoop {role}: FAIL (never connected)"); Application.Quit(); yield break; }
            var S = StoryUi.I; string rider = Coop.I.Roster.Find(e => e.Id != Coop.Me && role == "host")?.Name ?? Coop.MyName;
            bool Welcome() => Firm.C.Msgs.Exists(m => m.From == Company.WaltName && m.Text.Contains(rider + "?"));
            yield return Until(Welcome, 15);
            Ok(Welcome(), $"Walt welcomed {rider} by name");
            if (role == "rider")
            {
                yield return new WaitForSeconds(2f);
                me.Select(0); yield return Until(() => S.Showing != null, 12);
                Ok(S.Showing != null && S.Showing.Key == "cast", "my rod up: the host booked it and page 1 is on my screen");
                me.Select(1);
                yield return Until(() => S.Ringing != null, 60);
                Ok(S.Ringing != null && S.Ringing.From == Company.WaltName, "Walt's call rings here too");
                S.Answer(); yield return null; Ok(S.Talking, "I answered: the captions");
                yield return Until(() => S.Post() != null, 60);
                Ok(S.Post() != null && S.UnreadLetters() == 1, "the Day 4 letter reached me (in the mailbox)");
                S.Open(S.Post()); yield return null; Ok(StoryUi.LetterOpen, "I read it"); S.Close();
                yield return new WaitForSeconds(4f);
            }
            else
            {
                yield return Until(() => Firm.C.Pages.Count >= 1, 30);
                Ok(Firm.C.Pages.Count == 1 && Firm.C.Seen("rod"), "the rider's rod: page 1 in the crew's company");
                yield return new WaitForSeconds(2f);
                JumpTo(2, 8); yield return Until(() => S.Ringing != null, 5);
                Ok(S.Ringing != null, "Walt's call rings on the host"); S.Decline(); yield return null;
                Ok(S.Missed.Count == 1, "the host declined: missed here (the rider's own call is theirs)");
                yield return new WaitForSeconds(6f);
                JumpTo(4, 7); yield return Until(() => Firm.C.Letters.Exists(x => x.Where == "mail"), 5);
                Ok(Firm.C.Letters.Count == 2, "Day 4: the letter (and Walt's desk letter from Day 1)");
                yield return new WaitForSeconds(12f);
            }
            Ok(Firm.C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT storycoop {role}: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
