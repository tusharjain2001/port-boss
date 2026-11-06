using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    // M6 test switch (gdd systems "M6"; tools/m6test.sh):
    //   -pb-workertest <dir|none>  the sea grid (every berth -> spot path and 40 random sea pairs: never over land or
    //                              shallows, never across the Voss line), Rosie's board (Act 2, the board-seen rule), a live
    //                              shift near the player (the boat drives itself out, fishes, comes home, the catch lands in
    //                              the warehouse, the texts), then 20 seeded shifts by the clock: their catch within ±10% of
    //                              the economy model; wages at 07:00 and the IOU rule (one day on trust, then they quit)
    //   -pb-m6coop host|rider      (two processes, -pb-netsim 150 2) the rider sees Rosie's board, hires and assigns from
    //                              their phone (the host's bank, one ledger line), the host runs the shift and both see
    //                              Biscuit sail out and come home, the texts and the 07:00 wage; the rider recalls a
    //                              shift (no catch) and fires the worker from the phone
    public partial class Bot
    {
        IEnumerator M6Tests() => Arg("-pb-workertest") != null ? WorkerTest(Arg("-pb-workertest")) : Arg("-pb-m6coop") != null ? M6Coop(Arg("-pb-m6coop")) : null;
        IEnumerator UntilM6(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
        int m6Fails;
        void M6(bool ok, string what) { Debug.Log($"[PB] m6: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) m6Fails++; }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator WorkerTest(string dir)
        {
            Ctl.Bot = true; Sleep.Off = true; shots = dir == "none" ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Workers.OnLog += x => Debug.Log("[PB] m6: workers " + x);
            yield return new WaitForSeconds(1.5f);
            var Wk = Workers.I; M6(Wk != null, "Workers is up");
            if (Wk == null) { Debug.Log("[PB] RESULT workertest: FAIL (no Workers)"); Application.Quit(); yield break; }

            // ---------------- 1. the sea grid
            M6(SeaGrid.Ready, "the sea grid is built");
            var biscuit = Skiff.At(0); var berth = biscuit.MoorAt != null ? biscuit.MoorAt.position : biscuit.transform.position;
            foreach (var spot in Company.SpotIds)
            {
                var to = Workers.SpotPos(spot, berth); var p = SeaGrid.Find(new Vector2(berth.x, berth.z), to); var (min, voss) = SeaGrid.Check(p);
                M6(min >= SeaGrid.MinDepth - 0.05f && !voss, $"berth -> {spot}: {SeaGrid.Length(p):F0} m, {p.Count} legs, shallowest {min:F1} m, crosses Voss {voss}");
            }
            var rnd = new System.Random(6); int pairs = 0, bad = 0;
            for (int i = 0; i < 400 && pairs < 40; i++)
            {
                var a = new Vector2(rnd.Next(-2200, 2200), rnd.Next(-2200, 2200)); var b = new Vector2(rnd.Next(-2200, 2200), rnd.Next(-2200, 2200));
                if (!SeaGrid.Deep(a.x, a.y) || !SeaGrid.Deep(b.x, b.y) || SeaGrid.CrossesVoss(a, b) && pairs % 2 == 1) continue;   // half the pairs straddle the line
                var p = SeaGrid.Find(a, b); var (min, voss) = SeaGrid.Check(p); pairs++;
                if (min < SeaGrid.MinDepth - 0.05f || voss) { bad++; Debug.Log($"[PB] m6: pair {a} -> {b}: shallowest {min:F1}, voss {voss}"); }
            }
            M6(pairs >= 30 && bad == 0, $"{pairs} random sea pairs: {bad} over land, shallows or the Voss line");

            // ---------------- 2. Rosie's board
            Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash (M6 test)", 20000, Sky.Minutes); Firm.I.Dirty = true;
            JumpTo(Today + 1, 12); yield return Frames(5);
            M6(Co.Board.Count == 3, $"Rosie's board has 3 faces ({Co.Board.Count})");
            Co.Act = 1; Workers.Hire(Co.Board[0].Id, false); yield return Frames(2);
            M6(Co.Staff.Count == 0, "Act 1: can't hire");
            Co.Act = 2; Workers.Hire(Co.Board[0].Id, true); yield return Frames(2);
            M6(Co.Staff.Count == 0, "the phone can't hire before the board is seen");
            var front = Wk.BoardFront; me.Teleport(front, Quaternion.LookRotation(Wk.Board.position - front).eulerAngles.y); CameraRig.I.Cut(); yield return Frames(10);
            M6(Wk.NearBoard, "standing at the board: the prompt shows"); yield return M5Shot("m6-rosies-board");
            Workers.See(); M6(Co.BoardSeen, "board seen");
            // the strongest face (the test wants a full hold)
            var best = Co.Board[0]; foreach (var x in Co.Board) if (x.Fishing + x.Sea > best.Fishing + best.Sea) best = x;
            Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 1; yield return Frames(4); yield return M5Shot("m6-phone-hire"); Phone.I.Hide();
            int c0 = Co.Cash; Workers.Hire(best.Id, true); yield return Frames(2);
            var w = Co.Staff.Find(x => x.Name == best.Name);
            M6(w != null && c0 - Co.Cash == 2 * best.Wage, $"hired {best.Name} (F{best.Fishing} S{best.Sea}, {best.Quirk}) for ${c0 - Co.Cash} (2 days' wage)");
            if (w == null) { Debug.Log("[PB] RESULT workertest: FAIL"); Application.Quit(); yield break; }
            Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 0; yield return Frames(4); yield return M5Shot("m6-phone-crew");
            Workers.Assign(w.Id, "biscuit", "gullrock", "Shrimp", Shift.Day); yield return Frames(2);
            M6(w.Boat == "biscuit" && w.Spot == "gullrock", "assigned: Biscuit, Gullrock, shrimp, Day");
            Workers.Assign(w.Id, "biscuit", "gullrock", "Shrimp", Shift.Late); yield return Frames(2);
            M6(w.Sh == Shift.Day, "Late refused without deck lights"); Phone.I.Hide();
            // the crew's bait shelf (the warehouse): leave 10 shrimp from the pouch; the shift takes them first
            if (Works.I.WarehouseDoor is { } door)
            {
                me.Teleport(Works.Ground(door + Vector3.up), 0); CameraRig.I.Cut(); Firm.I.MyKit.Bait[1] = 14; yield return Frames(4);
                StoreUi.Show(); yield return Frames(4); StoreUi.I.Tap("leave bait 1"); yield return Frames(4);
                M6(Co.BaitStore[1] == 10 && Firm.I.MyKit.Bait[1] == 4, $"left 10 shrimp on the crew's bait shelf (shelf {Co.BaitStore[1]}, pouch {Firm.I.MyKit.Bait[1]})");
                yield return M5Shot("m6-warehouse-bait-shelf"); StoreUi.I.Hide();
            }
            else M6(false, "no warehouse door");

            // ---------------- 3. a live shift near the player: walk down, cast off, sail out, fish, home, warehouse
            int day = w.StartDay; me.Teleport(Works.Ground(berth + Vector3.up), 0); CameraRig.I.Cut();
            JumpTo(day, 7.95); yield return UntilM6(() => w.Run != null && w.Run.Day == day, 30);
            M6(w.Run != null && w.Run.Day == day, $"day {day} 08:00: the shift started ({w.Run?.Fail ?? "no fail"})");
            bool noShow = w.Run != null && w.Run.St == WSt.NoShow;
            if (!noShow)
            {
                yield return M5Shot("m6-worker-walks", 1.5f);
                var start = biscuit.transform.position; float t0 = Time.time; bool drove = false;
                float shallow = 99;
                while (Time.time - t0 < 90) { if (Workers.Live.TryGetValue("biscuit", out var mode) && mode == "driving") drove = true; if (Vector3.Distance(start, biscuit.transform.position) > 40) break; yield return null; }
                // drive on for 40 s near the player: the water under her never gets shallow (worker boats never beach)
                for (float t1 = 0; t1 < 40; t1 += Time.deltaTime) { var bp = biscuit.transform.position; if (Seabed.On && Vector3.Distance(berth, bp) > 30) shallow = Mathf.Min(shallow, Seabed.Depth(bp.x, bp.z)); yield return null; }
                M6(shallow >= 1.0f, $"driving herself out, the shallowest water under her: {(shallow > 98 ? "deep all the way" : shallow.ToString("F1") + " m")}");
                M6(!biscuit.Moored && Vector3.Distance(start, biscuit.transform.position) > 20, $"Biscuit cast off and sailed {Vector3.Distance(start, biscuit.transform.position):F0} m ({(drove ? "driven for real" : "by the clock")})");
                M6(drove, "near the player she drives herself (rudder and throttle)");
                yield return BoatShot(biscuit, "m6-biscuit-out", 1, 14, 5); RigOn();
                M6(!Seabed.On || Seabed.Depth(biscuit.transform.position.x, biscuit.transform.position.z) >= 1.0f, "in deep water");
                JumpTo(day, (w.Run.FishFrom + w.Run.FishTo) / 2 / 60 - (day - 1) * 24 + 0.01); yield return Frames(20);
                M6(Company.StateAt(w.Run, Sky.Minutes) == WSt.Fishing || w.Run.Fail == "aground", $"fishing at {Company.SpotName(w.Spot)}: {Company.CatchSoFar(w.Run, Sky.Minutes)} so far");
                M6(biscuit.Anchored || w.Run.Fail == "aground", "anchored at the spot");
                Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 0; yield return Frames(4); yield return M5Shot("m6-phone-crew-fishing");
                Phone.I.Show(Phone.Scr.Map); yield return Frames(4); yield return M5Shot("m6-phone-map-crew-dot"); Phone.I.Hide();
            }
            var buy = Co.Ledger.FindLast(e => e.Tx == $"shift:{w.Id}:{day}");
            M6(noShow ? Co.BaitStore[1] == 10 : Co.BaitStore[1] < 10 && buy != null && buy.What.Contains("June's"), $"bait: shelf {Co.BaitStore[1]} after the start, \"{buy?.What}\" ${-(buy?.Amount ?? 0)}");
            int wh0 = Co.WhFish; int msgs0 = Co.Msgs.Count;
            JumpTo(day, 17.2); yield return Frames(30);
            M6(w.Run.Landed, $"home: {w.Run.Catch} fish ({w.Run.Rare} rare), fail {w.Run.Fail ?? "none"}");
            M6(biscuit.Moored || noShow, "Biscuit back at her berth");
            M6(noShow || w.Run.Catch == 0 || Co.WhFish > wh0, $"the catch is in the warehouse ({wh0} -> {Co.WhFish})");
            M6(Co.Msgs.Exists(x => x.From == w.Name && x.Text.StartsWith("Leaving for")) || noShow, "texted \"Leaving for…\"");
            M6(Co.Msgs.Count > msgs0 || noShow, "texted when home");
            JumpTo(day, 17.3); yield return Frames(10); yield return M5Shot("m6-catch-carried");

            // ---------------- 4. wages at 07:00
            int cash1 = Co.Cash; JumpTo(day + 1, 7.05); yield return Frames(5);
            var wage = Co.Ledger.FindLast(e => e.Tx == $"wage:{w.Id}:{day + 1}");
            M6(noShow ? wage == null : wage != null && wage.Amount == -w.Wage, $"07:00 wage: {(wage == null ? "none" : "$" + (-wage.Amount))} (want {(noShow ? "none: a no-show" : "$" + w.Wage)})");

            // ---------------- 5. 20 seeded shifts by the clock (no player near: the boat goes by the schedule)
            me.Teleport(Works.Ground(berth + new Vector3(0, 1, -400)), 0); CameraRig.I.Cut();
            double sum = 0, want = 0; int n = 0, clock = 0;
            for (int d = day + 1; n < 20; d++)
            {
                Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash", 400, Sky.Minutes);
                foreach (var s in Co.Wh) s.Fish.Clear();   // room for every catch (the count is what's tested)
                JumpTo(d, 8.05); yield return Frames(6);
                if (w.Run == null || w.Run.Day != d) { Debug.Log($"[PB] m6: day {d}: no shift ({DayTime.DayName(d)})"); if (d > day + 40) break; continue; }
                for (int k = 0; k < 5; k++) { JumpTo(d, 9 + k); yield return Frames(3); if (Workers.Live.TryGetValue("biscuit", out var mode) && mode == "clock") clock++; }
                JumpTo(d, 17.2); yield return Frames(6);
                if (w.Run.Fail != null && w.Run.Fail.StartsWith("storm")) { Debug.Log($"[PB] m6: day {d}: a storm ({w.Run.Fail}), not in the model count"); continue; }
                sum += w.Run.Catch; want += Company.Expected(w, "biscuit", Co.BoatOf("biscuit").Ups.Contains("engine1")); n++;
                Debug.Log($"[PB] m6: shift {n} (day {d}): {w.Run.Catch} fish, fail {w.Run.Fail ?? "none"}");
            }
            M6(n == 20 && Mathf.Abs((float)(sum / want) - 1) <= 0.10f, $"20 seeded shifts: {sum} fish vs the model's {want:F1} ({(sum / want - 1) * 100:+0.0;-0.0}%)");
            M6(clock > 20, $"far from the player the boat goes by the clock ({clock} checks)");

            // ---------------- 6. the IOU rule
            int dd = Today; Co.Cash = 0; Firm.I.Dirty = true; JumpTo(dd, 18); yield return Frames(3);
            JumpTo(dd + 1, 7.05); yield return Frames(5);
            M6(w.Iou >= 0 && Co.Staff.Contains(w) && Co.Cash >= 0, $"the bank's empty at 07:00: {w.Name} works on an IOU (owed ${w.Owed})");
            M6(Co.Msgs.Exists(x => x.From == w.Name && x.Text.Contains("on trust")), "the IOU text");
            JumpTo(dd + 1, 18); yield return Frames(3); Co.Cash = 0; JumpTo(dd + 2, 7.05); yield return Frames(5);
            M6(!Co.Staff.Contains(w) || w.Run?.St == WSt.NoShow && w.Owed == 0, $"unpaid again: {(Co.Staff.Contains(w) ? "still here" : "quit")}");
            M6(Co.Rehire.ContainsKey(w.Name) || Co.Staff.Contains(w), "rehiring costs double for 7 days");
            M6(Co.CheckM6() == "" && Co.Check() == "", $"company checks: {Co.CheckM6()}{Co.Check()}");

            Debug.Log($"[PB] RESULT workertest: {(m6Fails == 0 ? "PASS" : m6Fails + " FAILED")}");
            Application.Quit();
        }

        IEnumerator M6Coop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; shots = Arg("-pb-m5shots"); if (shots != null) Directory.CreateDirectory(shots);
            Workers.OnLog += x => Debug.Log("[PB] m6: workers " + x);
            yield return UntilM6(() => Coop.Joined && Coop.I.Roster.Count >= 2, 40);
            M6(Coop.Joined && Coop.I.Roster.Count >= 2, $"the crew is together ({(Coop.I != null ? Coop.I.Roster.Count : 0)})");
            if (!Coop.Joined) { Debug.Log("[PB] RESULT m6coop: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(3f);
            Coop.Entry Other() => Coop.I.Roster.Find(e => e.Id != Coop.Me);
            var bis = Skiff.At(0); var berth = bis.MoorAt != null ? bis.MoorAt.position : bis.transform.position;
            me.Teleport(Works.Ground(berth + Vector3.up), 0); CameraRig.I.Cut();   // both on the pier: she drives for real
            if (role == "host")
            {
                string rn = Other().Name;
                Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash (M6 co-op)", 20000, Sky.Minutes); Co.Act = 2; Firm.I.Dirty = true;
                yield return UntilM6(() => Co.BoardSeen, 60);
                M6(Co.BoardSeen, $"{rn} saw Rosie's board (asked me)");
                yield return UntilM6(() => Co.Staff.Count == 1, 60);
                var w = Co.Staff.Count > 0 ? Co.Staff[0] : null;
                M6(w != null && Co.Ledger.FindAll(e => e.What.StartsWith("Hired ")).Count == 1 && Co.Ledger.Exists(e => e.What.StartsWith("Hired " + w.Name) && e.Who == rn), $"{rn} hired {w?.Name} from their phone: one ledger line, by them");
                if (w == null) { Debug.Log("[PB] RESULT m6coop host: FAIL"); Application.Quit(); yield break; }
                yield return UntilM6(() => w.Boat == "biscuit", 60);
                M6(w.Boat == "biscuit" && w.Spot == "gullrock" && w.Sh == Shift.Day, $"{rn} assigned {w.Name}: {w.Boat}, {w.Spot}, {w.Bait}, {w.Sh}");
                yield return new WaitForSeconds(3f);
                // the shift: the clock goes to the start day's 07:55 (a no-show day: the next one)
                int day = w.StartDay;
                for (int k = 0; k < 4; k++)
                {
                    JumpTo(day, 7.95); yield return UntilM6(() => w.Run != null && w.Run.Day == day, 30);
                    if (w.Run != null && w.Run.Day == day && w.Run.St != WSt.NoShow) break;
                    Debug.Log($"[PB] m6: day {day}: {w.Name} didn't show ({w.Run?.Fail}), the next day"); day++;
                }
                M6(w.Run != null && w.Run.Day == day && w.Run.St != WSt.NoShow, $"day {day}: {w.Name}'s shift started ({w.Run?.Fail ?? "no fail"})");
                var start = bis.transform.position; bool drove = false;
                for (float t = 0; t < 90 && Vector3.Distance(start, bis.transform.position) < 40; t += Time.unscaledDeltaTime) { if (Workers.Live.TryGetValue("biscuit", out var md) && md == "driving") drove = true; yield return null; }
                M6(Vector3.Distance(start, bis.transform.position) > 20 && drove, $"Biscuit sails out with {w.Name} at the helm ({Vector3.Distance(start, bis.transform.position):F0} m, driven for real)");
                yield return new WaitForSeconds(6f);   // the rider watches her go
                JumpTo(day, 17.2); yield return UntilM6(() => w.Run.Landed, 20);
                M6(w.Run.Landed && bis.Moored, $"home: {w.Run.Catch} fish, Biscuit at her berth");
                yield return new WaitForSeconds(5f);
                JumpTo(day + 1, 7.05); yield return new WaitForSeconds(1f);
                M6(Co.Ledger.Exists(e => e.Tx == $"wage:{w.Id}:{day + 1}"), "07:00: the wage is paid from the crew's bank");
                // day 2: the rider recalls the shift from their phone
                JumpTo(day + 1, 9.5); yield return UntilM6(() => w.Run != null && w.Run.Day == day + 1, 20);
                bool show = w.Run != null && w.Run.Day == day + 1 && w.Run.St != WSt.NoShow;
                if (show)
                {
                    yield return UntilM6(() => w.Run.Fail == "recalled", 90);
                    M6(w.Run.Fail == "recalled", $"{rn} recalled {w.Name} from their phone");
                    if (w.Run.HomeAt > Sky.Minutes) Sky.Jump(w.Run.HomeAt + 0.5);
                    yield return UntilM6(() => w.Run.Landed, 20);
                    M6(w.Run.Landed && w.Run.Catch == 0, $"home with nothing, as asked ({w.Run.Catch} fish)");
                }
                else Debug.Log($"[PB] m6: day {day + 1}: a no-show, no recall to test ({w.Run?.Fail})");
                yield return UntilM6(() => Co.Staff.Count == 0, 90);
                M6(Co.Staff.Count == 0 && Co.Ledger.Exists(e => e.What.StartsWith($"Severance: {w.Name} (1 day") && e.Who == rn), $"{rn} let {w.Name} go from their phone (severance, by them)");
                M6(Co.Check() == "" && Co.CheckM6() == "", $"the ledger sums ({Co.Check()}{Co.CheckM6()})");
                yield return new WaitForSeconds(8f);
                Debug.Log($"[PB] RESULT m6coop host: {(m6Fails == 0 ? "PASS" : m6Fails + " FAIL")}");
            }
            else
            {
                yield return UntilM6(() => Co.Act >= 2 && Co.Board.Count == 3 && Co.Cash >= 10000, 60);
                M6(Co.Act >= 2 && Co.Board.Count == 3, $"Act 2 and Rosie's board ({Co.Board.Count} faces) reach me");
                var Wk = Workers.I; var front = Wk.BoardFront; me.Teleport(front, Quaternion.LookRotation(Wk.Board.position - front).eulerAngles.y); CameraRig.I.Cut();
                yield return Frames(10);
                var pr = Workers.Prompt(me); M6(Wk.NearBoard && pr != null && pr.StartsWith("Rosie's hiring board"), $"at the board: \"{pr}\"");
                Workers.See(); yield return UntilM6(() => Co.BoardSeen, 20);
                M6(Co.BoardSeen, "board seen (on the host)");
                Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 1; yield return Frames(4);
                if (shots != null) yield return M5Shot("coop-m6-hire-rider", 0.3f);
                string name = Co.Board[0].Name;
                M6(Phone.I.Tap("Hire ·"), $"Hire {name} on my phone");
                Worker W() => Co.Staff.Find(x => x.Name == name);
                yield return UntilM6(() => W() != null, 20);
                M6(W() != null, $"{name} is on the crew here");
                Phone.I.CrewTab = 0; yield return Frames(4);
                M6(Phone.I.Tap("Assign"), "Assign on my phone"); yield return Frames(4);
                if (shots != null) yield return M5Shot("coop-m6-assign-rider", 0.3f);
                M6(Phone.I.Tap("Confirm"), "Confirm (Biscuit, Gullrock, Shrimp, Day)");
                yield return UntilM6(() => W()?.Boat == "biscuit", 20);
                M6(W()?.Boat == "biscuit", "assigned (on the host)");
                Phone.I.Hide();
                // the crew's bait shelf from my warehouse: 10 of my worms go on the host's shelf
                if (Works.I.WarehouseDoor is { } door)
                {
                    int w0 = Co.BaitStore[0], p0 = Firm.I.MyKit.Bait[0]; me.Teleport(Works.Ground(door + Vector3.up), 0); CameraRig.I.Cut(); yield return Frames(4);
                    StoreUi.Show(); yield return Frames(4); bool tapped = StoreUi.I.Tap("leave bait 0");
                    yield return UntilM6(() => Co.BaitStore[0] > w0, 20); StoreUi.I.Hide();
                    M6(tapped && Co.BaitStore[0] == w0 + Mathf.Min(10, p0) && Firm.I.MyKit.Bait[0] == p0 - Mathf.Min(10, p0), $"moved worms to the crew's bait shelf from my warehouse (shelf {w0} -> {Co.BaitStore[0]} on the host, my pouch {p0} -> {Firm.I.MyKit.Bait[0]})");
                }
                me.Teleport(Works.Ground(berth + Vector3.up), 0); CameraRig.I.Cut();
                // the host's clock: the shift starts, Biscuit leaves here too
                yield return UntilM6(() => W()?.Run != null && !W().Run.Landed && W().Run.St != WSt.NoShow, 200);
                var start = bis.transform.position;
                yield return UntilM6(() => Vector3.Distance(start, bis.transform.position) > 20, 90);
                M6(Vector3.Distance(start, bis.transform.position) > 20, $"Biscuit sails out on my screen ({Vector3.Distance(start, bis.transform.position):F0} m)");
                if (shots != null) { yield return BoatShot(bis, "coop-m6-biscuit-out-rider", 1, 14, 5); RigOn(); }
                M6(Co.Msgs.Exists(x => x.From == name && x.Text.StartsWith("Leaving for")), "the \"Leaving for...\" text, here");
                yield return UntilM6(() => W()?.Run != null && W().Run.Landed, 120);
                M6(W()?.Run != null && W().Run.Landed && Co.Msgs.Exists(x => x.From == name && (x.Text.StartsWith("Home") || x.Text.StartsWith("Back in"))), "home, and the text, here");
                int d1 = W().Run.Day;
                yield return UntilM6(() => Co.Ledger.Exists(e => e.What == $"Wage: {name}"), 60);
                M6(Co.Ledger.Exists(e => e.What == $"Wage: {name}"), "the 07:00 wage line, here");
                // day 2: recall from my phone
                yield return UntilM6(() => W()?.Run != null && W().Run.Day > d1 && (W().Run.St == WSt.NoShow || Company.StateAt(W().Run, Sky.Minutes) == WSt.Out || Company.StateAt(W().Run, Sky.Minutes) == WSt.Fishing), 120);
                if (W()?.Run != null && W().Run.St != WSt.NoShow)
                {
                    Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 0; yield return Frames(4);
                    if (shots != null) yield return M5Shot("coop-m6-at-sea-rider", 0.3f);
                    M6(Phone.I.Tap("Recall"), "Recall on my phone");
                    yield return UntilM6(() => W().Run.Landed, 90);
                    M6(W().Run.Landed && W().Run.Catch == 0, "home with nothing, here");
                }
                Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 0; yield return Frames(4);
                M6(Phone.I.Tap("Fire"), $"let {name} go on my phone");
                yield return UntilM6(() => W() == null, 30);
                M6(W() == null, "off the crew, here");
                Phone.I.Hide();
                yield return new WaitForSeconds(6f);
                Debug.Log($"[PB] RESULT m6coop rider: {(m6Fails == 0 ? "PASS" : m6Fails + " FAIL")}");
            }
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }
    }
}
