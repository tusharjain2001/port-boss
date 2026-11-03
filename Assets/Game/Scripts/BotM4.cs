using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Saves = PortBoss.M1.Save;   // (Bot has a Save(path) of its own: the screenshot)

namespace PortBoss.M1
{
    // M4 test switches (gdd systems §5.2):
    //   -pb-firstday [dir]       the practised player's first three days (economy §2.3) through the real shops, phone and
    //                            nights; the bank checked against the table at every row (±10%). With a dir: a shot of
    //                            every phone screen, the shops, the toasts and the night card. -pb-hold s: a pause on
    //                            each (for -pb-film). -pb-grades game: the game's own grade odds instead of the model's
    //   -pb-savetest write|read  (tools/m4test.sh, with -pb-savedir) a lived-in world saved, then loaded in a second
    //                            process: the world's hash must match; the backups; a CRC failure falls back; a fixture
    //   -pb-banktest host|rider  (tools/m4test.sh) the crew's shared bank: the rider sells, buys, accepts Mari; the host's
    //                            morning pays rent and sends the rider its save copy
    public partial class Bot
    {
        static bool M4Run => Arg("-pb-firstday") != null || Arg("-pb-savetest") != null || Arg("-pb-banktest") != null || Arg("-pb-realbait") != null;
        IEnumerator M4Tests()
        {
            if (Arg("-pb-firstday") != null) return FirstDay(Arg("-pb-firstday"));
            if (Arg("-pb-savetest") != null) return SaveTest(Arg("-pb-savetest"));
            if (Arg("-pb-banktest") != null) return BankTest(Arg("-pb-banktest"));
            return null;
        }

        int m4Fails;
        void M4(bool ok, string what) { Debug.Log($"[PB] m4: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) m4Fails++; }
        float hold;
        IEnumerator Hold(float k = 1) { if (hold > 0) yield return new WaitForSeconds(hold * k); else { yield return null; yield return null; } }
        IEnumerator M4Shot(string n) { if (shots == null) yield break; yield return null; yield return null; yield return Shot(n); }

        // walk up to a counter (teleport) and open it
        IEnumerator AtCounter(Firm.Kind k)
        {
            var s = Firm.I.Stalls.Find(x => x.K == k);
            var back = s.At.forward; back.y = 0;
            me.Teleport(s.Front + back.normalized * 0.4f, Quaternion.LookRotation(-back).eulerAngles.y);
            CameraRig.I.Yaw = Quaternion.LookRotation(-back).eulerAngles.y + 20; CameraRig.I.Pitch = 14;
            yield return null; yield return null;
            Shop.Show(s); yield return null; yield return null;
        }
        IEnumerator SellAll(Firm.Kind k, string shot = null)
        {
            yield return AtCounter(k);
            if (shot != null) yield return M4Shot(shot);
            yield return Hold();
            for (int guard = 0; guard < 8 && Shop.Open; guard++)
            {
                var b = Shop.I.Buttons();
                if (b.Contains("Sell all")) Shop.I.Tap("Sell all");
                else { int i = b.IndexOf("Sell "); if (i < 0) break; var lab = b.Substring(i).Split(',')[0]; Shop.I.Tap(lab); }
                yield return null; yield return null;
            }
            if (shot != null) yield return M4Shot(shot + "-sold");
            yield return Hold();
            Shop.I.Hide(); yield return null;
        }
        IEnumerator Buy(string id, int packs)
        {
            yield return AtCounter(Firm.Kind.June);
            int i = System.Array.FindIndex(FishDb.Baits, b => b.Id == id);
            for (int p = packs; p > 0;)
            {
                int q = p >= 5 ? 5 : p >= 2 ? 2 : 1;
                Shop.I.Tap($"x{q} {id}"); yield return null; yield return null;
                if (!Shop.I.Tap("Buy " + id)) break; yield return null; yield return null;
                p -= q;
            }
            yield return Hold(0.5f);
            Shop.I.Hide(); yield return null;
        }

        // the catch of one trip, from..to: each fish rolled by the game (rod, the bait on the hook), the grade Rare with
        // probability pRare(bait), 1 bait spent (the bait on the hook; when it runs out, worms, then shrimp)
        int Catch(System.Random r, int n, double from, double to, System.Func<string, float> pRare)
        {
            var k = Firm.I.MyKit; int got = 0;
            for (int i = 0; i < n; i++)
            {
                double at = from + (to - from) * (i + 0.5) / n;
                int bi = k.Bait[k.BaitI] > 0 ? k.BaitI : k.Bait[0] > 0 ? 0 : k.Bait[1] > 0 ? 1 : -1;
                string bait = bi >= 0 ? FishDb.Baits[bi].Id : "None";
                var sp = FishDb.Roll(r, k.Rod, bait);
                FishDb.RollSize(r, sp, out var cm, out var kg);
                var f = new Fish { Species = sp, Grade = r.NextDouble() < pRare(bait) ? 1 : 0, Cm = cm, Kg = kg, CaughtAt = (float)at };
                if (me.Bar.Add(f) < 0) { Debug.Log("[PB] m4: the hotbar is full: a fish let go"); continue; }
                if (bi >= 0) k.Bait[bi]--;
                if (!k.Best.TryGetValue(sp.Id, out var b) || cm > b) k.Best[sp.Id] = cm;
                got++;
            }
            return got;
        }
        static float MeanPrice(int rod, string bait)
        {
            float w = 0, v = 0; foreach (var s in FishDb.All) { float x = FishDb.Weight(s, rod, bait); w += x; v += x * s.Price; }
            return v / Mathf.Max(1e-6f, w);
        }

        // ---------------------------------------------------------------- the first three days (economy §2.3, practised)
        IEnumerator FirstDay(string dir)
        {
            Ctl.Bot = true; Sleep.Off = true; hold = ArgF("-pb-hold", 0);
            if (!string.IsNullOrEmpty(dir) && dir != "-") { shots = dir; Directory.CreateDirectory(dir); }
            yield return new WaitForSeconds(0.5f);
            var c = Firm.C; var r = new System.Random(4);
            sigma = 0.07f; float pGame = ExpectedMix()[1], mean = MeanPrice(1, "Worm"), pModel = Mathf.Clamp01(16f / mean - 1);
            bool game = Arg("-pb-grades") == "game";
            float p(string bait) => game ? pGame : Mathf.Clamp01(16f / MeanPrice(1, bait == "None" ? "Worm" : bait) - 1); // the table: $16 a fish
            Debug.Log($"[PB] m4: a fish on Walt's bamboo + worms is worth ${mean:F2} Basic; the game's Rare odds for a practised player (sigma 70 ms): {pGame:P0} -> ${mean * (1 + pGame):F2} a fish. The table assumes $16 a fish (Rare {pModel:P0}). This run: {(game ? "the game's odds" : "the table's $16")}");
            var rows = new List<(string what, int want, int got)>();
            void Row(string what, int want) { rows.Add((what, want, c.Cash)); Debug.Log($"[PB] m4: firstday {DayTime.DayName(DayTime.Day(Sky.Minutes))} D{DayTime.Day(Sky.Minutes)} {DayTime.Clock(Sky.Minutes)} {what}: bank ${c.Cash} (table ${want}), earned ${c.Earned}"); }
            void Jump(int day, double h) { if (DayTime.At(day, h * 60) > Sky.Minutes) Sky.Jump(DayTime.At(day, h * 60)); }
            void StandIn(string what, int amount) { var tx = Firm.I.NewTx("standin"); if (amount < 0) c.Spend(tx, Firm.MyName, what + " (stand-in until M5)", -amount, Sky.Minutes); else c.Earn(tx, Firm.MyName, what + " (stand-in until M5)", amount, Sky.Minutes); }
            IEnumerator Sleep_(int toDay, string shot)
            {
                Jump(toDay - 1, 23);
                yield return null;
                StartCoroutine(Sleep.I.Night(null, Sleep.Kind.Bed, DayTime.At(toDay, DayTime.Wake), Firm.MyName));
                for (float t = 0; t < 8 && !Sleep.I.CardUp; t += Time.unscaledDeltaTime) yield return null;
                if (shot != null) yield return M4Shot(shot);
                for (float t = 0; t < 12 && Sleep.I.Skipping; t += Time.unscaledDeltaTime) yield return null;
                yield return null; yield return null;
            }

            // Day 1 07:00: Walt's office, $40, June's bag of worms
            M4(c.Cash == 40 && Firm.I.MyKit.Bait[0] == 10, $"a new game: ${c.Cash}, {Firm.I.MyKit.Bait[0]} worms");
            Phone.I.Show(); yield return M4Shot("phone-home-day1"); yield return Hold(); Phone.I.Hide();
            Row("start", 40);
            // trip 1, the Shallows 10:00-14:00: 9 fish
            Jump(1, 10); Catch(r, 9, Sky.Minutes, DayTime.At(1, 14 * 60), p); Jump(1, 15);
            yield return SellAll(Firm.Kind.Pratt, "pratt-day1"); Row("first sale at Pratt's (60%)", 126);
            // 16:00: worms x3 at June's, the engine at Doyle's
            Jump(1, 16); yield return AtCounter(Firm.Kind.June); yield return M4Shot("june-bait"); Shop.I.Tab("Rods"); yield return M4Shot("june-rods"); Shop.I.Tab("Tackle"); yield return M4Shot("june-tackle"); Shop.I.Tab("Bait"); Shop.I.Hide(); yield return null;
            yield return Buy("Worm", 3); StandIn("Biscuit engine rebuild", -90); Row("worms + the engine", 21);
            // trip 2 at dusk, 17:00-23:00: 14 fish kept overnight (Pratt shut at 16:00)
            Jump(1, 17); Catch(r, 14, Sky.Minutes, DayTime.At(1, 23 * 60), p); StandIn("Fuel, trip 2", -8); Jump(1, 22.9);
            Row("after trip 2", 13);
            yield return Sleep_(2, "night-card-day1"); Row("wake Day 2", 13);
            // Day 2 07:30: sell last night's fish (Good, 85%)
            Jump(2, 7.5); yield return SellAll(Firm.Kind.Pratt, "pratt-day2-morning"); Row("last night's fish", 128);
            Jump(2, 8); yield return Buy("Worm", 1); yield return Buy("Shrimp", 3); Row("worms + shrimp", 88);
            // the bait pouch: hold R, point at the shrimp, let go
            me.Select(0); yield return new WaitForSeconds(0.8f);
            Pouch.I.BotShow(1); yield return M4Shot("pouch-radial"); yield return Hold(); Pouch.I.BotHide();
            M4(Pouch.I.Pick(1) && fish.BaitId == "Shrimp", $"the pouch radial puts shrimp on the hook ({fish.BaitId})");
            M4(!Pouch.Usable(3) && !Pouch.I.Pick(3), "the squid pocket waits for June's tackle pouch");
            yield return M4Shot("bait-chip-shrimp");
            // trip 3, 09:00-15:00: 15 fish; Mari texts once TOTAL EARNED passes $250
            Jump(2, 9); Catch(r, 15, Sky.Minutes, DayTime.At(2, 15 * 60), p); StandIn("Fuel, trip 3", -8); Jump(2, 15);
            yield return SellAll(Firm.Kind.Pratt, "pratt-day2"); Row("sell", 224);
            yield return null; yield return null;
            M4(c.Mari == DealSt.Offered, $"Mari texts when total earned passes $250 (earned ${c.Earned}, Mari {c.Mari})");
            yield return M4Shot("toast-mari-text");
            M4(Alerts.Queue.Exists(a => a.Sender == Company.MariName && a.Hint == "Open phone"), "her text pops up on the LEFT with \"Tab · Open phone\"");
            Phone.I.Show(); yield return M4Shot("phone-home-request"); yield return Hold();
            Phone.I.Tap("Messages"); yield return M4Shot("phone-messages"); yield return Hold();
            Phone.I.Tap(Company.MariName); yield return M4Shot("phone-mari-offer"); yield return Hold();
            M4(Phone.I.Tap("Accept"), "Accept on her mooring request (the phone)");
            yield return null; yield return null; yield return M4Shot("phone-mari-accepted"); yield return Hold();
            M4(c.Mari == DealSt.Active && c.MariFirstPay == 3, $"the deal is on: rent from Day {c.MariFirstPay} 7 AM");
            Phone.I.Go(Phone.Scr.Contracts); yield return M4Shot("phone-contracts"); Phone.I.Tap("Track"); yield return M4Shot("phone-contracts-tracked");
            Phone.I.Go(Phone.Scr.Bank); yield return M4Shot("phone-bank");
            Phone.I.Go(Phone.Scr.Forecast); yield return M4Shot("phone-forecast");
            Phone.I.Go(Phone.Scr.Map); yield return M4Shot("phone-map");
            Phone.I.Go(Phone.Scr.Journal); yield return M4Shot("phone-journal");
            Phone.I.Go(Phone.Scr.Settings); yield return M4Shot("phone-settings");
            Phone.I.Go(Phone.Scr.Crew); yield return M4Shot("phone-crew-locked");
            Phone.I.Go(Phone.Scr.Thread, "Notifications"); yield return M4Shot("phone-notifications");
            Phone.I.Hide();
            // she ties up within the in-game hour
            Jump(2, 16.1); yield return null; yield return null;
            M4(c.MariHere(Sky.Minutes), "Mari's taco boat is at her berth within the hour");
            StandIn("Ingrid's three planks + bench", -100); Row("planks", 124);
            // trip 4, 17:00-23:00: 14 fish, kept
            Jump(2, 17); Catch(r, 14, Sky.Minutes, DayTime.At(2, 23 * 60), p); StandIn("Fuel, trip 4", -8); Jump(2, 22.9);
            Row("after trip 4", 116);
            int before = c.Cash;
            yield return Sleep_(3, "night-card-day2");
            var rent = c.Ledger.Find(e => e.Tx == "rent:mari:3");
            M4(rent != null && rent.Amount == 40 && c.Cash == before + 40, $"Day 3 07:00: Mari's $40 is in the bank (${before} -> ${c.Cash})");
            M4(Alerts.Queue.Exists(a => a.Text.StartsWith("Rent's in, landlord. +$40")), "the rent toast: \"Rent's in, landlord. +$40\"");
            yield return M4Shot("toast-rent");
            Row("wake Day 3 (+ rent)", 156);
            // Day 3 09:00: last night's fish, 10 to Mari at 100%, the rest to Pratt
            Jump(3, 9.05); yield return SellAll(Firm.Kind.Mari, "mari-sell"); yield return SellAll(Firm.Kind.Pratt, "pratt-day3-morning"); Row("last night's fish (Mari + Pratt)", 325);
            M4(c.MariToday == Market.MariDaily, $"Mari took her {c.MariToday} fish");
            Jump(3, 9.5); yield return Buy("Worm", 1); yield return Buy("Shrimp", 3); StandIn("Biscuit hull patch", -120); Row("bait + the hull patch", 165);
            // trip 5, 10:00-15:00: 13 fish
            Jump(3, 10); Catch(r, 13, Sky.Minutes, DayTime.At(3, 15 * 60), p); StandIn("Fuel, trip 5", -7); Jump(3, 15);
            yield return SellAll(Firm.Kind.Pratt, null); Row("sell", 283);
            Jump(3, 18); StandIn("Amara's order, 3 mackerel", 90); Row("Amara's order", 373);
            Phone.I.Show(Phone.Scr.Bank); yield return M4Shot("phone-bank-day3"); Phone.I.Hide();
            // shrimp and the new fish (R11: the rod is a hard gate). Bamboo: shrimp swaps sprats for mullet and mackerel;
            // the Fibreglass rod (June's, $450) + shrimp brings garfish and bream
            string Mix(int rod, string bait) { float t = 0; foreach (var s in FishDb.All) t += FishDb.Weight(s, rod, bait); var o = new List<string>(); foreach (var s in FishDb.All) { float w = FishDb.Weight(s, rod, bait); if (w > 0) o.Add($"{s.Id} {w / t:P0}"); } return string.Join(", ", o); }
            float Share(int rod, string bait, params string[] ids) { float t = 0, x = 0; foreach (var s in FishDb.All) { float w = FishDb.Weight(s, rod, bait); t += w; if (System.Array.IndexOf(ids, s.Id) >= 0) x += w; } return x / t; }
            foreach (var (rd, bt) in new[] { (1, "Worm"), (1, "Shrimp"), (2, "Worm"), (2, "Shrimp") }) Debug.Log($"[PB] m4: bites on rod {rd} + {bt}: {Mix(rd, bt)}");
            M4(Share(1, "Shrimp", "sprat") < Share(1, "Worm", "sprat") / 3 && Share(1, "Shrimp", "garfish", "bream") == 0, "bamboo + shrimp: far fewer sprats, no garfish/bream (the rod gate)");
            M4(Share(2, "Shrimp", "garfish", "bream") > 0.5f && Share(2, "Shrimp", "garfish", "bream") > 3 * Share(2, "Worm", "garfish", "bream"), $"Fibreglass + shrimp: garfish and bream are {Share(2, "Shrimp", "garfish", "bream"):P0} of bites (worms: {Share(2, "Worm", "garfish", "bream"):P0})");
            { var r2 = new System.Random(11); int n2 = 0; for (int i = 0; i < 200; i++) { var sp = FishDb.Roll(r2, 2, "Shrimp"); if (sp.Id is "garfish" or "bream") n2++; } M4(n2 > 100, $"200 real rolls on Fibreglass + shrimp: {n2} garfish/bream"); }
            // the table
            int within = 0; Debug.Log("[PB] m4: firstday | row | table | game | off");
            foreach (var x in rows)
            {
                float off = x.want == 0 ? 0 : (x.got - x.want) / (float)x.want; bool ok = Mathf.Abs(x.got - x.want) <= Mathf.Max(0.1f * x.want, 10);
                if (ok) within++;
                Debug.Log($"[PB] m4: firstday | {x.what} | ${x.want} | ${x.got} | {off:+0%;-0%;0%} {(ok ? "" : "OUT")}");
            }
            M4(within == rows.Count, $"the bank within ±10% (or $10 on the small rows) of economy §2.3 at {within}/{rows.Count} rows");
            M4(c.Check() == "", "the Ledger sums to the bank, never below $0: " + (c.Check() == "" ? "yes" : c.Check()));
            Debug.Log($"[PB] RESULT firstday: {(m4Fails == 0 ? "PASS" : m4Fails + " FAIL")} (bank ${c.Cash}, earned ${c.Earned}, {c.Ledger.Count} ledger lines, grades {(game ? "game" : "table")})");
            yield return Hold(2);
            Application.Quit();
        }

        // ---------------------------------------------------------------- save, quit, load: the same world
        IEnumerator SaveTest(string mode)
        {
            Ctl.Bot = true; Sleep.Off = true;
            var dir = Arg("-pb-savedir"); if (dir == null) { Debug.Log("[PB] RESULT savetest: FAIL (needs -pb-savedir)"); Application.Quit(); yield break; }
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(0.5f);
            var c = Firm.C; var s = Skiff.I;
            if (mode == "write")
            {
                // a lived-in world: fish sold, shrimp bought, Mari moored, the clock on to Day 2 15:20, Biscuit anchored off
                // the pier with water in her and a fish on her deck, fresh and day-old fish in the hotbar
                var r = new System.Random(9);
                Sky.Jump(DayTime.At(1, 600));
                Catch(r, 8, DayTime.At(1, 480), Sky.Minutes, _ => 0.3f);
                var sell = new List<Fish>(); foreach (var sl in me.Bar.S) if (sl.Fish.Count > 0 && sell.Count < 3) sell.Add(sl.Fish[0]);
                Firm.I.Sell(Buyer.Pratt, sell);
                Firm.I.Buy("bait:1", 2);
                c.Earn(Firm.I.NewTx("test"), Firm.MyName, "A test windfall", 260, Sky.Minutes); c.Tick(Sky.Minutes);
                Firm.I.AnswerMari(true); Firm.I.Track(true);
                Sky.Jump(DayTime.At(2, 920)); yield return null; yield return null;
                Catch(r, 4, DayTime.At(2, 800), Sky.Minutes, _ => 0.3f);
                s.Moored = false; s.transform.position += s.transform.forward * -14 + s.transform.right * 9; s.TakeOver(); s.AnchorNow(); s.Water = 0.32f; s.Leaking = true;
                yield return new WaitForSeconds(0.5f);
                var deckFish = new Fish { Species = FishDb.Get("mackerel"), Grade = 1, Cm = 33, Kg = 0.5f, CaughtAt = (float)Sky.Minutes - 30 };
                FishPickup.Drop(deckFish, s.transform.TransformPoint(new Vector3(0.2f, 0.75f, -0.6f)), Quaternion.identity);
                var spawn = Landing.I.Spawn.position; me.Teleport(spawn + new Vector3(0.6f, 0.05f, -1.2f), 140);
                yield return new WaitForSeconds(0.3f);
                Saves.CurrentName = "Save test harbour";
                M4(Saves.Write("test"), "saved");
                string id = Saves.Current, h1 = Saves.LastHash, w = Path.Combine(dir, id);
                File.WriteAllText(Path.Combine(dir, "id.txt"), id); File.WriteAllText(Path.Combine(dir, "hash1.txt"), h1); File.WriteAllText(Path.Combine(dir, "world1.json"), Saves.Capture(true));
                Debug.Log($"[PB] m4: save: world {id} hash {h1}: bank ${c.Cash}, Mari {c.Mari}, {me.Bar.FishCount} fish in the hotbar, {FishPickup.All.Count} on deck, Biscuit {(s.Anchored ? "anchored" : "?")} water {s.Water:F2}");
                Saves.Write("test 2"); Saves.Write("test 3"); Saves.Write("test 4");
                foreach (var f in new[] { "world.json.gz", "world.json.gz.bak1", "world.json.gz.bak2", "world.json.gz.bak3", "meta.json", "thumb.png" }) M4(File.Exists(Path.Combine(w, f)), "file " + f);
                M4(!File.Exists(Path.Combine(w, "world.json.gz.tmp")), "no .tmp left over (the swap happened)");
                M4(Saves.LastHash == h1, "saving again changes nothing");
                // a damaged file falls back to the newest backup
                var bad = Path.Combine(dir, id + "-damaged"); Directory.CreateDirectory(bad);
                foreach (var f in Directory.GetFiles(w)) File.Copy(f, Path.Combine(bad, Path.GetFileName(f)), true);
                var bytes = File.ReadAllBytes(Path.Combine(bad, "world.json.gz")); bytes[bytes.Length / 2] ^= 0x5A; File.WriteAllBytes(Path.Combine(bad, "world.json.gz"), bytes);
                var o = Saves.ReadWorld(id + "-damaged", out var from);
                M4(o != null && from == "world.json.gz.bak1", $"a damaged world.json.gz fails its CRC and the newest backup loads (read {from})");
                Directory.Delete(bad, true);
                M4(Saves.Worlds().Exists(x => x.Id == id && x.Day == 2 && x.Cash == c.Cash), "the title's world list shows it (Day 2, the bank)");
                Debug.Log($"[PB] RESULT savetest write: {(m4Fails == 0 ? "PASS" : m4Fails + " FAIL")}");
            }
            else
            {
                string id = File.ReadAllText(Path.Combine(dir, "id.txt")).Trim(), h1 = File.ReadAllText(Path.Combine(dir, "hash1.txt")).Trim();
                M4(Saves.Load(id), "loaded " + id);
                string h2 = Saves.LastHash; File.WriteAllText(Path.Combine(dir, "world2.json"), Saves.Capture(true));
                M4(h2 == h1, $"the world after the reload is the world that was saved: hash {h2} vs {h1}");
                M4(c != Firm.C, "a fresh company object was read");
                c = Firm.C;
                M4(c.Mari == DealSt.Active && c.MariTracked && c.Ledger.Exists(e => e.Tx == "rent:mari:2"), $"Mari's deal (active, tracked, Day 2 rent paid): {c.Mari}");
                M4(Firm.I.MyKit.Bait[1] == 20 && me.Fish.Kit == Firm.I.MyKit, $"the bait pouch (20 shrimp: {Firm.I.MyKit.Bait[1]}), and the rod reads it");
                M4(s.Anchored && !s.Moored && Mathf.Abs(s.Water - 0.32f) < 0.01f && s.Leaking, $"Biscuit anchored off the pier, water {s.Water:F2}, still leaking");
                M4(FishPickup.All.Count == 1, $"the fish on her deck ({FishPickup.All.Count})");
                M4(c.Check() == "", "the Ledger sums");
                yield return new WaitForSeconds(3f);                         // and the world runs on from there
                M4(Sky.Minutes > DayTime.At(2, 920), $"the clock runs on ({DayTime.Clock(Sky.Minutes)})");
                M4(Saves.Write("quit"), "Save & quit again");
                // a schema 1 file (tests/fixtures/save-v1.json) migrates and loads
                var fx = Arg("-pb-fixture");
                if (fx != null)
                {
                    Saves.Apply(Saves.Parse(File.ReadAllBytes(fx)));
                    M4(Firm.C.Cash == 132 && Firm.C.Check() == "" && me.Bar.FishCount == 2 && Firm.I.MyKit.Bait[0] == 4, $"the schema 1 fixture loads: ${Firm.C.Cash}, {me.Bar.FishCount} fish, {Firm.I.MyKit.Bait[0]} worms");
                }
                Debug.Log($"[PB] RESULT savetest read: {(m4Fails == 0 ? "PASS" : m4Fails + " FAIL")}");
            }
            Application.Quit();
        }

        // ---------------------------------------------------------------- the crew's bank (LAN, two processes)
        IEnumerator BankTest(string role)
        {
            Ctl.Bot = true; Sleep.Off = true;
            for (float t = 0; t < 30 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            M4(Coop.Joined && Coop.I.Roster.Count >= 2, $"the crew is together ({(Coop.I != null ? Coop.I.Roster.Count : 0)})");
            if (!Coop.Joined) { Debug.Log("[PB] RESULT banktest: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(2f);
            Firm.OnLog += x => Debug.Log("[PB] m4: firm " + x);
            if (role == "host")
            {
                var rider = Coop.I.Roster.Find(e => e.Id != Coop.Me); string rn = rider.Name;
                Coop.I.Grant(4);
                Company C() => Firm.C;
                for (float t = 0; t < 40 && !C().Ledger.Exists(e => e.Who == rn && e.Amount > 0); t += Time.unscaledDeltaTime) yield return null;
                var sale = C().Ledger.Find(e => e.Who == rn && e.Amount > 0);
                M4(sale != null, $"{rn}'s sale is in the crew's bank: {sale?.What} +${sale?.Amount}");
                M4(Coop.I.Book.Bar(rider.Id).FishCount == 0, "and the fish left their hotbar (the host's book)");
                for (float t = 0; t < 30 && !C().Ledger.Exists(e => e.Who == rn && e.Amount < 0); t += Time.unscaledDeltaTime) yield return null;
                M4(C().Ledger.Exists(e => e.Who == rn && e.What.StartsWith("Shrimp")), $"{rn} bought shrimp from the crew's bank");
                for (float t = 0; t < 10 && C().KitOf(rider.Key).Bait[1] < 10; t += Time.unscaledDeltaTime) yield return null;
                M4(C().KitOf(rider.Key).Bait[1] >= 10, $"{rn}'s kit report: {C().KitOf(rider.Key).Bait[1]} shrimp");
                Sky.Jump(DayTime.At(3, DayTime.Wake + 1)); yield return null;
                M4(C().Mari == DealSt.Offered, "Day 3: Mari texts the crew");
                for (float t = 0; t < 30 && C().Mari != DealSt.Active; t += Time.unscaledDeltaTime) yield return null;
                M4(C().Mari == DealSt.Active && C().MariBy == rn, $"{rn} accepted on the phone: {C().Mari} by {C().MariBy}");
                yield return new WaitForSeconds(2f);
                int before = C().Cash;
                Sky.Jump(DayTime.At(4, DayTime.Wake)); yield return null;
                M4(C().Cash == before + 40, $"Day 4 07:00: the rent (${before} -> ${C().Cash})");
                M4(Saves.Write("morning"), "the morning save (and the crew's copy)");
                M4(C().Check() == "", "the Ledger sums");
                yield return new WaitForSeconds(8f);
                Debug.Log($"[PB] m4: bank host final ${C().Cash} ledger {C().Ledger.Count}");
                Debug.Log($"[PB] RESULT banktest host: {(m4Fails == 0 ? "PASS" : m4Fails + " FAIL")}");
            }
            else
            {
                for (float t = 0; t < 20 && me.Bar.FishCount == 0; t += Time.unscaledDeltaTime) yield return null;
                M4(me.Bar.FishCount > 0, $"fish in my hotbar ({me.Bar.FishCount})");
                int start = Firm.C.Cash;
                yield return SellAll(Firm.Kind.Pratt);
                for (float t = 0; t < 10 && !Firm.C.Ledger.Exists(e => e.Who == Firm.MyName && e.Amount > 0); t += Time.unscaledDeltaTime) yield return null;
                var sale = Firm.C.Ledger.Find(e => e.Who == Firm.MyName && e.Amount > 0);
                M4(sale != null && me.Bar.FishCount == 0, $"my sale, by the host: +${sale?.Amount}, the hotbar empty ({me.Bar.FishCount})");
                M4(Alerts.Queue.Exists(a => a.Sender == "Sold"), "the Sold toast");
                yield return Buy("Shrimp", 1);
                for (float t = 0; t < 10 && Firm.I.MyKit.Bait[1] < 10; t += Time.unscaledDeltaTime) yield return null;
                M4(Firm.I.MyKit.Bait[1] == 10, $"my shrimp: {Firm.I.MyKit.Bait[1]}");
                for (float t = 0; t < 40 && Firm.C.Mari != DealSt.Offered; t += Time.unscaledDeltaTime) yield return null;
                M4(Firm.C.Mari == DealSt.Offered && Alerts.Queue.Exists(a => a.Sender == Company.MariName), "Mari's text reaches me too");
                Phone.I.Show(Phone.Scr.Thread, Company.MariName); yield return null; yield return null;
                M4(Phone.I.Tap("Accept"), "Accept on my phone");
                for (float t = 0; t < 10 && Firm.C.Mari != DealSt.Active; t += Time.unscaledDeltaTime) yield return null;
                M4(Firm.C.Mari == DealSt.Active, "the deal is on, for the whole crew");
                Phone.I.Hide();
                for (float t = 0; t < 30 && !Firm.C.Ledger.Exists(e => e.Tx.StartsWith("rent:")); t += Time.unscaledDeltaTime) yield return null;
                M4(Alerts.Queue.Exists(a => a.Text.StartsWith("Rent's in")), "the rent toast on my screen");
                for (float t = 0; t < 20 && (Saves.Current == null || !Saves.Current.EndsWith("-copy")); t += Time.unscaledDeltaTime) yield return null;
                M4(Saves.Current != null && Saves.Current.EndsWith("-copy") && Saves.Worlds().Exists(w => w.Copy), $"the host's morning save, kept here as {Saves.Current}");
                Debug.Log($"[PB] m4: bank rider final ${Firm.C.Cash} ledger {Firm.C.Ledger.Count}");
                Debug.Log($"[PB] RESULT banktest rider: {(m4Fails == 0 ? "PASS" : m4Fails + " FAIL")}");
            }
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }
    }
}
