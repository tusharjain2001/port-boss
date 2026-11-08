using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-tackletest <dir> (M8 step 1, fishing F10): June's Tackle tab. Buy the Smooth reel, Mono 15, the Barbed hook and
        // the Sight float (a worse reel and the same line again aren't offered); the Circle hook goes on when bought, Use puts
        // the Barbed back; the strike window and the label follow. Then fish from the quay: with the Barbed hook the first
        // stop missed on purpose, the second hit = a Rare (2 stops); a forced heavy fish fought and missed: it holds for 3
        // stops (the hook's retry + the heavy fish's own), then snaps. The float on the water is 1.5x. Stills (-pb-fps 60).
        IEnumerator TackleTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] tackletest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            yield return new WaitForSeconds(1f);
            var C = Firm.C; var k = Firm.I.MyKit;
            C.Earn(Firm.I.NewTx("test"), Firm.MyName, "tackletest", 5000, Sky.Minutes); Firm.I.Dirty = true;
            Ok(Tackle.Label(k, fish.RodTier) == "Rusty reel · Frayed mono 8 kg · Bent hook" && Mathf.Abs(fish.StrikeWin - 1.2f) < 1e-4f, $"a new game's tackle ({Tackle.Label(k, fish.RodTier)}, strike {fish.StrikeWin:F2} s)");
            // 1) June's Tackle tab
            walk = false; yield return AtCounter(Firm.Kind.June);
            Ok(Shop.Open, "June's counter open"); Shop.I.Tab("Tackle"); yield return null; yield return null;
            if (shots != null) yield return Shot("june-tackle-new");
            foreach (var item in new[] { "reel:2", "line:1", "hook:2", "float" }) { Ok(Shop.I.Tap("Buy " + item), "Buy " + item); yield return null; yield return null; }
            Ok(k.Reel == 2 && k.Line == 1 && k.Hook == 2 && k.Sight, $"the kit: reel {k.Reel}, line {k.Line}, hook {k.Hook}, float {k.Sight}");
            string b = Shop.I.Buttons();
            Ok(!b.Contains("Buy reel:1") && !b.Contains("Buy reel:2") && !b.Contains("Buy line:1") && b.Contains("Buy reel:3") && b.Contains("Buy hook:1"), $"no worse reel, nothing twice; better ones still for sale ({b})");
            Ok(Mathf.Abs(fish.StrikeWin - 1.35f) < 1e-4f && fish.Retries == 1 && Mathf.Abs(fish.LineKg - 15) < 1e-4f, $"Smooth + Barbed: strike {fish.StrikeWin:F2} s, {fish.Retries} retry, line {fish.LineKg} kg");
            Ok(Shop.I.Tap("Buy hook:1"), "Buy the Circle hook"); yield return null; yield return null;
            Ok(k.Hook == 1 && Mathf.Abs(fish.StrikeWin - 1.5f) < 1e-4f && fish.Retries == 0, $"the Circle hook goes on: strike {fish.StrikeWin:F2} s");
            if (shots != null) { Shop.I.Tab("Tackle"); yield return null; yield return null; yield return Shot("june-tackle-bought"); }
            Ok(Shop.I.Tap("Use hook:2"), "Use the Barbed hook"); yield return null; yield return null;
            Ok(k.Hook == 2 && fish.Retries == 1, "the Barbed hook back on");
            Ok(Tackle.Label(k, fish.RodTier) == "Smooth reel · Mono 15 kg · Barbed", "the label: " + Tackle.Label(k, fish.RodTier));
            Shop.I.Tab("Rods"); yield return null; yield return null; if (shots != null) yield return Shot("june-rods");
            Shop.I.Hide(); yield return null;
            // 2) fish from the quay with the Barbed hook: miss, then hit = Rare in 2 stops
            var port = Landing.I != null ? Landing.I.transform.Find("Port") : null;
            Vector3 P(float x, float y) => port.TransformPoint(new Vector3(-x, 1.32f, -y));
            if (port != null) me.Teleport(P(-8, -0.9f), 180);
            yield return new WaitForSeconds(0.5f);
            me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
            if (shots != null) yield return Shot("rod-label");
            yield return FightOnce(new[] { false, true }, "barbed");
            Ok(lastFight.landed && lastFight.grade == 1 && lastFight.stops == 2 && fish.Tries == 2, $"Barbed: missed, then hit: landed {lastFight.landed}, {(lastFight.grade == 1 ? "Rare" : "Basic")}, {lastFight.stops} stops of {fish.Tries}");
            yield return FightOnce(new[] { false, false }, "barbed-2miss");
            Ok(lastFight.landed && lastFight.grade == 0 && lastFight.stops == 2, $"Barbed: two misses: landed Basic ({lastFight.stops} stops)");
            // 3) a heavy fish (forced), fought and never hit: the Barbed's retry + its own, then snap
            Fishing.Bot.ForceHeavy = true; Fishing.Bot.FightHeavy = true;
            yield return FightOnce(new[] { false, false, false }, "heavy", true);
            Ok(lastFight.snapped && lastFight.stops == 3 && fish.Tries == 3, $"heavy + Barbed: 3 stops, then the snap ({lastFight.stops} stops, snapped {lastFight.snapped})");
            Fishing.Bot.ForceHeavy = false; Fishing.Bot.FightHeavy = false;
            // 4) the Sight float, out on the water
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
            yield return Until(() => fish.Power > 0.85f, 2); Ctl.BotUse = false;
            yield return Until(() => fish.State == Fishing.St.Waiting, 6);
            var bobs = FindObjectsByType<BobberView>(FindObjectsSortMode.None);
            Ok(System.Array.Exists(bobs, x => x.Floating && Mathf.Abs(x.Big - 1.5f) < 1e-4f), "the Sight float: 1.5x on the water");
            if (shots != null) { CameraRig.I.Pitch = 22; yield return new WaitForSeconds(0.6f); yield return Shot("sight-float"); }
            fish.Abort(); yield return new WaitForSeconds(0.8f);
            Ok(Firm.C.Check() == "", $"the company's invariants ({Firm.C.Check()})");
            Debug.Log($"[PB] RESULT tackletest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // one cast to the end: each stop hit or missed as the pattern says (a hit aims at the zone's centre; a miss lands
        // 0.45 of a pass away). What happened goes in lastFight.
        (bool landed, bool snapped, int grade, int stops) lastFight;
        IEnumerator FightOnce(bool[] pattern, string tag, bool heavy = false, bool struck = false)
        {
            lastFight = default; float t0 = Time.time;
            if (!struck)        // (struck: the fight's already on, a holder rod taken on its bell)
            {
                while (fish.State != Fishing.St.Ready && Time.time - t0 < 6) yield return null;
                Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
                while (fish.State == Fishing.St.Charging && fish.Power < 0.55f && Time.time - t0 < 8) yield return null;
                Ctl.BotUse = false;
                t0 = Time.time; while (fish.State != Fishing.St.Bite && Time.time - t0 < 25) yield return null;
                if (fish.State != Fishing.St.Bite) { Debug.Log($"[PB] tackletest {tag}: no bite ({fish.State})"); yield break; }
                yield return new WaitForSeconds(0.3f); Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false;
            }
            int i = 0; t0 = Time.time; bool seenFight = false;
            while (Time.time - t0 < 40)
            {
                var st = fish.State;
                if (st == Fishing.St.Minigame) seenFight = true;
                if (st == Fishing.St.Minigame && !fish.Frozen)
                {
                    double now = Time.timeAsDouble, hitT = now + 0.2;
                    for (double t = now + 0.15; t < now + 3.0 / fish.PtrSpeed; t += 0.002) if (Mathf.Abs(fish.PointerAt(t) - fish.ZoneAt(t)) < 0.004f) { hitT = t; break; }
                    bool hit = i < pattern.Length ? pattern[i] : true;
                    double at = hit ? hitT : hitT + 0.45 / fish.PtrSpeed;
                    while (Time.timeAsDouble < at && fish.State == Fishing.St.Minigame) yield return null;
                    Bot.PressAt = at; Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; i++;
                    if (shots != null && i == 1) yield return Shot($"fight-{tag}");
                    while (fish.State == Fishing.St.Minigame && fish.Frozen) yield return null;
                    continue;
                }
                if (st == Fishing.St.Reveal && fish.Catch != null) { lastFight.landed = true; lastFight.grade = fish.Catch.Grade; if (fish.StateT > 0.8f) { Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; } }
                if (seenFight && (st == Fishing.St.Ready || st == Fishing.St.Recover || st == Fishing.St.Stowed))
                {
                    lastFight.snapped = fish.Snapped; lastFight.stops = fish.Stop;
                    Debug.Log($"[PB] tackletest {tag}: {(lastFight.snapped ? "snapped" : lastFight.landed ? "landed " + (lastFight.grade == 1 ? "Rare" : "Basic") : fish.LastOutcome)} after {fish.Stop} stop(s) of {fish.Tries}, misses {fish.Misses}{(heavy ? ", heavy" : "")}");
                    fish.LastOutcome = null; yield return new WaitForSeconds(0.4f); yield break;
                }
                yield return null;
            }
            Debug.Log($"[PB] tackletest {tag}: stuck in {fish.State}");
        }

        // -pb-tacklecoop host|rider (M8 step 1): the rider buys a reel and two hooks through the host (the answer fits them
        // on the rider's own rods), picks the Circle hook; the host's record of the rider's kit follows; the host's own kit
        // is untouched. Then the rider fights a fish with the Barbed hook back on: the host sees the 2-stop fight check out.
        IEnumerator TackleCoop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] tacklecoop {role}: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            Ok(Coop.Joined && Coop.I.Roster.Count >= 2, "the crew is together");
            if (!Coop.Joined) { Debug.Log($"[PB] RESULT tacklecoop {role}: FAIL (never connected)"); Application.Quit(); yield break; }
            if (role == "host")
            {
                Firm.C.Earn(Firm.I.NewTx("test"), Firm.MyName, "tacklecoop", 5000, Sky.Minutes); Firm.I.Dirty = true;
                var rider = Coop.I.Roster.Find(e => e.Id != Coop.Me);
                Kit Theirs() => Firm.C.Kits.TryGetValue(rider.Key ?? Coop.MyKey(rider.Name), out var x) ? x : null;
                yield return Until(() => Theirs() != null && Theirs().Reel == 3 && (Theirs().Hooks & 6) == 6 && Theirs().Hook == 1, 60);
                var th = Theirs();
                Ok(th != null && th.Reel == 3 && (th.Hooks & 6) == 6, $"the rider's kit here: the Twin-drag and two hooks ({th?.Reel}, hooks {th?.Hooks})");
                Ok(th != null && th.Hook == 1, $"the rider's hook choice reached me (the Circle: {th?.Hook})");
                Ok(Firm.I.MyKit.Reel == 0 && Firm.I.MyKit.Hook == 0, "my own kit untouched");
                int was = Coop.I.Checked;
                yield return Until(() => Coop.I.Checked > was, 90);
                Ok(Coop.I.Checked > was && Coop.I.Flagged == 0, $"the rider's 2-stop fight checked out ({Coop.I.Checked - was} checked, {Coop.I.Flagged} flagged)");
                yield return new WaitForSeconds(3f);
            }
            else
            {
                yield return Until(() => Firm.C.Cash >= 5000, 20);
                foreach (var item in new[] { "reel:3", "hook:1", "hook:2" }) { Firm.I.Buy(item, 1); yield return new WaitForSeconds(1.5f); }
                var k = Firm.I.MyKit;
                Ok(k.Reel == 3 && (k.Hooks & 6) == 6 && k.Hook == 2, $"bought through the host: on my rods (reel {k.Reel}, hooks {k.Hooks}, on {k.Hook})");
                Firm.I.UseHook(1); yield return new WaitForSeconds(3f);
                Ok(k.Hook == 1 && Mathf.Abs(fish.StrikeWin - 1.55f) < 1e-4f, $"the Circle on: strike {fish.StrikeWin:F2} s (Twin-drag + Circle)");
                yield return new WaitForSeconds(4f);
                Firm.I.UseHook(2); yield return new WaitForSeconds(1.5f);
                var port = Landing.I != null ? Landing.I.transform.Find("Port") : null;
                if (port != null) me.Teleport(port.TransformPoint(new Vector3(6, 1.32f, 0.9f)), 180);
                yield return new WaitForSeconds(0.5f); me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
                yield return FightOnce(new[] { false, true }, "rider-barbed");
                Ok(lastFight.landed && lastFight.grade == 1 && lastFight.stops == 2, $"my Barbed fight: missed, hit, Rare ({lastFight.stops} stops)");
                yield return new WaitForSeconds(4f);
            }
            Ok(Firm.C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT tacklecoop {role}: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
