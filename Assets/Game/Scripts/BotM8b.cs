using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-holdertest <dir> (M8 step 2, fishing F11): rod holders and trolling, solo. Doyle's holder on Biscuit (a steel
        // tube, not boxes); the Fibreglass bought (a second rod). At her mooring: cast, E puts the line in the holder (the
        // bamboo comes to hand); the bell; E takes it struck: the fight, landed on the Fibreglass. Again: the bell missed:
        // it swims off and re-baits; E takes it back (no bite). With only the bamboo: "needs a second rod". Out in open
        // water: sardine on the holder rod, at the tiller ⅔ throttle (~3 m/s): it trolls 14 m astern; its bell: E leaves the
        // tiller, E takes it, landed. Worms at the same speed: the line drags in, the rod rests; E takes it. Stills.
        IEnumerator HolderTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] holdertest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.deltaTime) yield return null; }
            IEnumerator E() { Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null; }
            yield return new WaitForSeconds(1f);
            var C = Firm.C; var k = Firm.I.MyKit; var s = Skiff.Of("biscuit");
            C.Earn(Firm.I.NewTx("test"), Firm.MyName, "holdertest", 5000, Sky.Minutes); Firm.I.Dirty = true;
            Firm.I.Buy("rod:2", 1); Firm.I.Buy("bait:2", 2); yield return null;
            Ok(fish.RodTier == 2 && (k.Rods & 3) == 3 && k.Bait[2] > 0, $"the Fibreglass in my hands, the bamboo still mine, sardines ({k.Bait[2]}) (rod {fish.RodTier}, rods {k.Rods})");
            bool got = Works.HostBuy(Firm.I.NewTx("m8"), Works.MyKey, Works.MyName, "up:biscuit:holders", 0, false, out var why);
            yield return null; yield return null;
            Transform sock0 = null; foreach (var t in s.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("socket_rodholder")) sock0 = t;
            Debug.Log($"[PB] holdertest: bought the holders {got} ({why}), has {C.Has("up:biscuit:holders")}, socket {(sock0 != null ? sock0.name : "none")}");
            yield return Until(() => RodHolders.I.All.Exists(x => x.Boat == s), 3);
            var h = RodHolders.I.All.Find(x => x.Boat == s);
            { var sh = Works.I.ShownOf("up:biscuit:holders"); var names = new System.Text.StringBuilder(); foreach (Transform c in s.transform) names.Append(c.name).Append(", ");
              Debug.Log($"[PB] holdertest: shown {(sh != null ? sh.name + " under " + (sh.transform.parent != null ? sh.transform.parent.name : "-") + " kids " + sh.transform.childCount : "null")}; Biscuit's children: {names}; holders {RodHolders.I.All.Count}"); }
            Ok(h != null, $"Doyle's holder on Biscuit ({why})");
            if (h == null) { Debug.Log("[PB] RESULT holdertest: FAIL (no holder)"); Application.Quit(); yield break; }
            var sl = s.transform.InverseTransformPoint(h.Socket.position);
            void Stand() { me.Teleport(s.transform.TransformPoint(new Vector3(sl.x + 0.42f, 1.0f, sl.z + 0.35f)), s.transform.eulerAngles.y - 90); CameraRig.I.Yaw = s.transform.eulerAngles.y - 100; }
            Stand(); yield return new WaitForSeconds(0.8f);
            Ok(me.OnBoat && me.Boat == s, "aboard Biscuit by her holder");
            if (shots != null) { yield return BoatShot(s, "holder-fitted", -1, 4.5f, 1.6f); RigOn(); }

            // 1) a line out -> E -> into the holder; the bamboo comes to hand
            me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
            yield return CastLine(0.55f);
            Ok(fish.State == Fishing.St.Waiting, $"a line out from the deck ({fish.State})");
            yield return null; Ok(me.Prompt == "Put the rod in the holder", $"the prompt: \"{me.Prompt}\"");
            yield return E();
            Ok(h.St == RodHolders.HS.Out && h.Tier == 2 && fish.RodTier == 1 && fish.State == Fishing.St.Ready, $"the Fibreglass in the holder ({h.St}, tier {h.Tier}), the bamboo in my hands ({fish.RodTier}, {fish.State})");
            if (shots != null) { yield return new WaitForSeconds(0.6f); yield return BoatShot(s, "holder-rod-out", -1, 6f, 2.4f); RigOn(); }
            // 2) the bell; E takes it struck; the fight; landed on the Fibreglass
            yield return Until(() => h.St == RodHolders.HS.Bite, 14);
            Ok(h.St == RodHolders.HS.Bite, "the bell: a bite on the holder rod");
            if (shots != null) { yield return BoatShot(s, "holder-bite", -1, 5f, 2f); RigOn(); }
            yield return null; Ok(me.Prompt == "Take the rod! A bite", $"the prompt: \"{me.Prompt}\"");
            yield return E();
            Ok(h.St == RodHolders.HS.Empty && fish.RodTier == 2 && fish.State is Fishing.St.Strike or Fishing.St.Minigame or Fishing.St.Heavy, $"taken struck: the Fibreglass in my hands, {fish.State}");
            Fishing.Bot.FightHeavy = true;
            yield return FightOnce(new[] { true }, "holder", false, true);
            Ok(lastFight.landed && fish.RodTier == 2, $"the holder's fish landed ({lastFight.landed}, {(lastFight.grade == 1 ? "Rare" : "Basic")}) on the Fibreglass");
            // 3) the bell missed: it swims off, re-baits; then E takes it back with no bite
            Stand(); yield return new WaitForSeconds(0.4f); me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
            yield return CastLine(0.55f); yield return null; yield return E();
            Ok(h.St == RodHolders.HS.Out && fish.RodTier == 1, "the Fibreglass back in the holder");
            yield return Until(() => h.St == RodHolders.HS.Bite, 14); float rang = Time.time;
            yield return Until(() => h.St == RodHolders.HS.Out, 4);
            Ok(h.St == RodHolders.HS.Out && Time.time - rang > 1.9f && h.BiteAt > Time.time + 3.5f, $"the bell missed: off after {Time.time - rang:F1} s, re-baited (next bite in {h.BiteAt - Time.time:F1} s)");
            yield return null; Ok(me.Prompt == "Take the rod back", $"the prompt: \"{me.Prompt}\"");
            yield return E();
            Ok(h.St == RodHolders.HS.Empty && fish.RodTier == 2 && fish.State == Fishing.St.Ready, $"taken back: the Fibreglass in my hands, reeled in ({fish.State})");
            // 4) only the bamboo: no second rod for my hands
            int rods = k.Rods; k.Rods = 1; fish.SetRod(1, true); yield return null;
            yield return CastLine(0.55f); yield return null;
            Ok(me.Prompt == "Rod holder: needs a second rod", $"only the bamboo: \"{me.Prompt}\"");
            fish.Abort(); yield return new WaitForSeconds(0.8f); k.Rods = rods; fish.SetRod(2, true); yield return null;

            // 5) trolling: open water, sardine on the holder rod, at the tiller at ⅔ (~3 m/s)
            var home = s.transform.position; Vector3 start = home, head = Vector3.forward; bool found = false;
            for (int r = 60; r <= 160 && !found; r += 20)
                for (int a = 0; a < 360 && !found; a += 20)
                {
                    var p = home + Quaternion.Euler(0, a, 0) * Vector3.forward * r;
                    for (int b = 0; b < 360 && !found; b += 30)
                    {
                        var d = Quaternion.Euler(0, b, 0) * Vector3.forward; bool clear = true;
                        for (float m = -10; m <= 110 && clear; m += 5) { var q = p + d * m; clear = Seabed.Depth(q.x, q.z) > 4 && Seabed.Depth(q.x + d.z * 8, q.z - d.x * 8) > 3 && Seabed.Depth(q.x - d.z * 8, q.z + d.x * 8) > 3; }
                        if (clear) { start = p; head = d; found = true; }
                    }
                }
            Ok(found, $"open water for a run: {start.x:F0} {start.z:F0} heading {Mathf.Atan2(head.x, head.z) * Mathf.Rad2Deg:F0}");
            s.CastOff(true); s.transform.SetPositionAndRotation(new Vector3(start.x, s.transform.position.y, start.z), Quaternion.LookRotation(head)); s.TakeOver(); s.AnchorNow();
            yield return new WaitForSeconds(0.5f); Stand(); yield return new WaitForSeconds(0.8f);
            fish.BaitI = 2; me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
            yield return CastLine(0.55f); yield return null; yield return E();
            Ok(h.St == RodHolders.HS.Out && h.Bait == 2, $"sardine on the holder rod ({h.Bait})");
            h.BiteAt = Time.time + 9;                                          // (time to get going first)
            // to the tiller: E, then ⅔ throttle
            var helm = s.Helm; me.Teleport(helm.position + s.transform.forward * 0.35f + Vector3.up * 0.2f, s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            if (s.Anchored) s.ToggleAnchor(); yield return Until(() => !s.Anchored, 3);
            yield return E(); Ok(me.AtHelm, $"at the tiller ({me.Prompt})");
            s.Notch = 2; yield return Until(() => s.Velocity.magnitude > 2.4f, 10);
            yield return new WaitForSeconds(2f);
            var tip = h.Rod.Tip.position; float behind = Vector3.Dot(new Vector3(h.Float.x - tip.x, 0, h.Float.z - tip.z), s.transform.forward);
            Ok(h.Troll && h.St == RodHolders.HS.Out && behind < -8, $"trolling at {s.Velocity.magnitude:F1} m/s: the line {-behind:F0} m astern");
            if (shots != null) { yield return BoatShot(s, "trolling", -1, 9f, 3.2f); RigOn(); }
            yield return Until(() => h.St == RodHolders.HS.Bite, 14);
            Ok(h.St == RodHolders.HS.Bite && h.Troll, $"a bite while trolling ({h.St})");
            yield return null; Ok(me.Prompt == "The bell!   E  Take the rod", $"at the tiller: \"{me.Prompt}\"");
            if (shots != null) yield return Shot("tiller-bell");
            yield return E();
            Ok(!me.AtHelm && h.St == RodHolders.HS.Empty && fish.State is Fishing.St.Strike or Fishing.St.Minigame or Fishing.St.Heavy, $"one E: off the tiller, the trolled rod in my hands, struck ({fish.State}) inside the bell's 2 s");
            yield return FightOnce(new[] { true }, "trolled", false, true);
            Ok(lastFight.landed, $"the trolled fish landed ({lastFight.landed})");
            // 6) worms at speed: the line drags in, the rod rests in the holder
            yield return Until(() => s.Velocity.magnitude < 0.5f, 12);
            Stand(); yield return new WaitForSeconds(0.5f); fish.BaitI = 0; me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
            yield return CastLine(0.55f); yield return null; yield return E();
            Ok(h.St == RodHolders.HS.Out && h.Bait == 0, "worms on the holder rod");
            h.BiteAt = Time.time + 30;
            me.Teleport(helm.position + s.transform.forward * 0.35f + Vector3.up * 0.2f, s.transform.eulerAngles.y); yield return new WaitForSeconds(0.4f);
            yield return E(); s.Notch = 2;
            yield return Until(() => h.St == RodHolders.HS.Rest, 12);
            Ok(h.St == RodHolders.HS.Rest, $"worms at {s.Velocity.magnitude:F1} m/s: the line dragged in, the rod rests ({h.St})");
            yield return E(); yield return Until(() => s.Velocity.magnitude < 0.5f, 12);
            Stand(); yield return new WaitForSeconds(0.4f); yield return null;
            Ok(me.Prompt == "Take the rod", $"the prompt: \"{me.Prompt}\"");
            yield return E();
            Ok(h.St == RodHolders.HS.Empty && fish.RodTier == 2 && !RodHolders.I.AnyMine, $"the resting rod taken: the Fibreglass in my hands ({fish.RodTier})");
            Ok(C.Check() == "", $"the company's invariants ({C.Check()})");
            Debug.Log($"[PB] RESULT holdertest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        IEnumerator CastLine(float power)
        {
            float t0 = Time.time; while (fish.State != Fishing.St.Ready && Time.time - t0 < 5) yield return null;
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
            while (fish.State == Fishing.St.Charging && fish.Power < power && Time.time - t0 < 8) yield return null;
            Ctl.BotUse = false;
            t0 = Time.time; while (fish.State != Fishing.St.Waiting && Time.time - t0 < 6) yield return null;
        }

        // -pb-holdercoop host|rider (M8 step 2): the rider's holder in co-op. The host fits Biscuit's holder; the rider buys
        // the Fibreglass, puts its line in the holder, casts the bamboo too (two lines out: two rolls on the host), then
        // takes the holder's bite and lands it: the host shows the rider's rod in the holder (tier 2, the bell) and checks
        // the landed fish against the holder's roll (none flagged); after, no roll is left aside.
        IEnumerator HolderCoop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] holdercoop {role}: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            Ok(Coop.Joined && Coop.I.Roster.Count >= 2, "the crew is together");
            if (!Coop.Joined) { Debug.Log($"[PB] RESULT holdercoop {role}: FAIL (never connected)"); Application.Quit(); yield break; }
            var s = Skiff.Of("biscuit");
            if (role == "host")
            {
                Firm.C.Earn(Firm.I.NewTx("test"), Firm.MyName, "holdercoop", 5000, Sky.Minutes); Firm.I.Dirty = true;
                Works.HostBuy(Firm.I.NewTx("m8"), Works.MyKey, Works.MyName, "up:biscuit:holders", 0, false, out _);
                var rider = Coop.I.Roster.Find(e => e.Id != Coop.Me);
                RodHolders.Holder H() => RodHolders.I.All.Find(x => x.Boat == s);
                yield return Until(() => H() != null && H().St == RodHolders.HS.Out && H().Owner == rider.Id, 60);
                Ok(H() != null && H().St == RodHolders.HS.Out && H().Tier == 2 && H().Rod != null, $"the rider's Fibreglass in Biscuit's holder, here ({H()?.St}, tier {H()?.Tier})");
                yield return Until(() => Coop.I.HeldRolls >= 1, 10);
                Ok(Coop.I.HeldRolls == 1, $"its roll kept aside ({Coop.I.HeldRolls})");
                yield return Until(() => H().St == RodHolders.HS.Bite || H().St == RodHolders.HS.Empty, 40);
                Ok(H().St == RodHolders.HS.Bite || H().St == RodHolders.HS.Empty, $"the rider's bell ({H().St})");
                int was = Coop.I.Checked;
                yield return Until(() => Coop.I.Checked > was, 60);
                Ok(Coop.I.Checked > was && Coop.I.Flagged == 0, $"the rider's holder fish checked out ({Coop.I.Checked - was} checked, {Coop.I.Flagged} flagged)");
                Ok(Coop.I.HeldRolls == 0 && H().St == RodHolders.HS.Empty, $"nothing left aside, the holder empty ({Coop.I.HeldRolls}, {H().St})");
                yield return new WaitForSeconds(3f);
            }
            else
            {
                yield return Until(() => Firm.C.Cash >= 5000 && RodHolders.I.All.Exists(x => x.Boat == s), 30);
                Firm.I.Buy("rod:2", 1); yield return Until(() => fish.RodTier == 2, 10);
                Ok(fish.RodTier == 2, "the Fibreglass bought through the host, in my hands");
                var h = RodHolders.I.All.Find(x => x.Boat == s); var sl = s.transform.InverseTransformPoint(h.Socket.position);
                me.Teleport(s.transform.TransformPoint(new Vector3(sl.x + 0.42f, 1.0f, sl.z + 0.35f)), s.transform.eulerAngles.y - 90); CameraRig.I.Yaw = s.transform.eulerAngles.y - 100;
                yield return new WaitForSeconds(0.8f); me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
                yield return CastLine(0.55f); yield return null;
                Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null;
                Ok(h.St == RodHolders.HS.Out && fish.RodTier == 1, $"my Fibreglass in the holder, the bamboo in my hands ({h.St})");
                h.BiteAt = Time.time + 12;                                    // (time for a second line)
                CameraRig.I.Yaw = s.transform.eulerAngles.y - 70; yield return CastLine(0.4f);
                Ok(fish.State == Fishing.St.Waiting, $"the bamboo's line out too: two lines ({fish.State})");
                yield return Until(() => h.St == RodHolders.HS.Bite, 20);
                Ok(h.St == RodHolders.HS.Bite, "the holder's bell");
                Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null;
                Fishing.Bot.FightHeavy = true;
                yield return FightOnce(new[] { true }, "rider-holder", false, true);
                Ok(lastFight.landed && fish.RodTier == 2, $"the holder's fish landed on the Fibreglass ({lastFight.landed})");
                yield return new WaitForSeconds(5f);
            }
            Ok(Firm.C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT holdercoop {role}: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
