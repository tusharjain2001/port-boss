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

        // -pb-netcoop host|rider (M8 step 3): the landing net. The rider buys a net through the host and stands beside the
        // host on the quay. The host casts; while the host fights, the rider sees "Ready the net for Host" and presses E:
        // the host gets 2 stops (one more go) and a 10% wider green. The host misses the first stop and hits the second:
        // a Rare, so the rider scoops it (both machines: the net out, the scoop).
        IEnumerator NetCoop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] netcoop {role}: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            Ok(Coop.Joined && Coop.I.Roster.Count >= 2, "the crew is together");
            if (!Coop.Joined) { Debug.Log($"[PB] RESULT netcoop {role}: FAIL (never connected)"); Application.Quit(); yield break; }
            var port = Landing.I != null ? Landing.I.transform.Find("Port") : null;
            Vector3 P(float x, float y) => port.TransformPoint(new Vector3(-x, 1.32f, -y));
            if (role == "host")
            {
                Firm.C.Earn(Firm.I.NewTx("test"), Firm.MyName, "netcoop", 5000, Sky.Minutes); Firm.I.Dirty = true;
                me.Teleport(P(-8, -0.9f), 180); yield return new WaitForSeconds(0.5f);
                var rider = Coop.I.Roster.Find(e => e.Id != Coop.Me);
                yield return Until(() => Firm.C.Kits.TryGetValue(rider.Key ?? Coop.MyKey(rider.Name), out var rk) && rk.Net && Coop.I.Mates.TryGetValue(rider.Id, out var ra) && ra != null && (ra.transform.position - me.transform.position).magnitude < 2.5f, 40);
                Ok(Coop.I.Mates.TryGetValue(rider.Id, out var rb) && (rb.transform.position - me.transform.position).magnitude < 2.5f, "the rider is beside me with a net");
                Fishing.Bot.FightHeavy = true;
                me.Select(0); yield return Until(() => fish.State == Fishing.St.Ready, 4);
                Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
                yield return Until(() => fish.Power > 0.55f, 3); Ctl.BotUse = false;
                yield return Until(() => fish.State == Fishing.St.Bite, 20);
                yield return new WaitForSeconds(0.3f); Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false;
                yield return Until(() => fish.State == Fishing.St.Minigame, 4);
                yield return Until(() => fish.Netted, 3);
                float w0 = Mathf.Min(0.6f, FishDb.Green[(int)fish.Hooked.Class] * fish.Rod.GreenMul * Fishing.ZoneScale); int tries0 = 1 + fish.Retries + (fish.HeavyFight ? 1 : 0);
                Ok(fish.Netted && fish.Tries == tries0 + 1 && Mathf.Abs(fish.ZoneW - Mathf.Min(0.6f, w0 * 1.1f)) < 1e-4f, $"the rider's net: {fish.Tries} stops (without it {tries0}), green {w0:F3} -> {fish.ZoneW:F3}");
                yield return FightOnce(new[] { false, true }, "netted", false, true);
                Ok(lastFight.landed && lastFight.grade == 1 && lastFight.stops == 2, $"missed, then hit: a Rare ({lastFight.stops} stops)");
                yield return Until(() => LandingNet.I.Scoops > 0, 5);
                Ok(LandingNet.I.Scoops == 1, "the rider scooped it (here)");
                yield return new WaitForSeconds(4f);
            }
            else
            {
                yield return Until(() => Firm.C.Cash >= 5000, 30);
                Firm.I.Buy("net", 1); yield return Until(() => Firm.I.MyKit.Net, 10);
                Ok(Firm.I.MyKit.Net, "a landing net, bought through the host");
                me.Teleport(P(-6.9f, -0.9f), 180); yield return new WaitForSeconds(0.5f);
                var host = Coop.I.Mates.Count > 0 ? new System.Collections.Generic.List<Angler>(Coop.I.Mates.Values).Find(a => a != null) : null;
                yield return Until(() => host != null && host.Fish.State is Fishing.St.Minigame or Fishing.St.Strike, 60);
                yield return Until(() => me.Prompt != null && me.Prompt.StartsWith("Ready the net"), 2);
                Ok(me.Prompt != null && me.Prompt.StartsWith("Ready the net for"), $"beside the host's fight: \"{me.Prompt}\"");
                Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null;
                Ok(LandingNet.I.On.Count == 1 && LandingNet.I.On[0].Net != null, "the net out in my hands");
                shots = Arg("-pb-netshots"); if (shots != null) { Directory.CreateDirectory(shots); CameraRig.I.Yaw = me.transform.eulerAngles.y + 120; yield return new WaitForSeconds(0.5f); yield return Shot("net-ready"); }
                yield return Until(() => LandingNet.I.Scoops > 0, 30);
                Ok(LandingNet.I.Scoops == 1, "the Rare came up: I scooped it");
                if (shots != null) { yield return new WaitForSeconds(0.45f); yield return Shot("net-scoop"); }
                yield return new WaitForSeconds(4f);
            }
            Ok(Firm.C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT netcoop {role}: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-surfshoptest <dir> (M8 step 4): Swell Surf Co. Act 2 (as F7): Kai's hello the next morning at 6:30 (three texts);
        // at his counter at 10 AM: the shelf (the free foamie taken, the longboard and the wetsuit bought, the shortboard
        // locked till his challenge, Carry); a storm shuts the shop ("Gnarly"); the Forecast app's surf line; the wetsuit's
        // +50% stamina in the water; his dawn report the next morning (or no text when there's no surf). Also a look round:
        // the shop from the sea and the ground and water around it (where a jetty could go). Stills.
        IEnumerator SurfShopTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] surfshoptest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.deltaTime) yield return null; }
            yield return new WaitForSeconds(1f);
            var C = Firm.C; var k = Firm.I.MyKit; var st = SurfShop.Stall;
            Ok(st != null && st.At != null, $"Swell Surf Co.'s counter at the Cut ({st?.At.position.x:F0} {st?.At.position.z:F0})");
            if (st == null) { Debug.Log("[PB] RESULT surfshoptest: FAIL (no shop)"); Application.Quit(); yield break; }
            C.Earn(Firm.I.NewTx("test"), Firm.MyName, "surfshoptest", 5000, Sky.Minutes);
            int Kai() => C.Msgs.FindAll(m => m.From == Company.KaiName).Count;
            // the look round: heights and depths out from the door, every 30 degrees
            var at = st.At.position; var sb = new System.Text.StringBuilder();
            for (int a = 0; a < 360; a += 30)
            {
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward; sb.Append($"\n  {a,3} deg:");
                for (int r = 5; r <= 60; r += 5) { var q = at + d * r; sb.Append($" {r}m {-Seabed.Depth(q.x, q.z):F1}"); }
            }
            Debug.Log($"[PB] surfshoptest: ground round the door at {at.x:F1} {at.y:F2} {at.z:F1}, facing {Mathf.Atan2(st.At.forward.x, st.At.forward.z) * Mathf.Rad2Deg:F0} deg (+Z = 0, height m, - = under water):{sb}");
            // 1) Act 2; Kai the next morning
            C.SetAct(2, Sky.Minutes); Firm.I.Dirty = true; int today = DayTime.Day(Sky.Minutes);
            JumpTo(today + 1, 6.6); yield return Until(() => Kai() >= 3, 4);
            Ok(Kai() == 3 && C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.Contains("foam board")), $"Kai's hello the morning after Act 2 ({Kai()} texts)");
            // 2) at his counter at 10
            JumpTo(today + 1, 10); yield return null;
            walk = false; yield return AtCounter(Firm.Kind.Surf);
            Ok(Shop.Open, "Swell Surf Co.'s shelf open");
            if (shots != null) yield return Shot("surf-shelf-new");
            string b0 = Shop.I.Buttons();
            Ok(b0.Contains("Buy board:1") && !b0.Contains("Buy board:4") && b0.Contains("Buy board:3") && b0.Contains("Buy wetsuit"), $"the shelf: the free foamie, the shortboard locked ({b0})");
            int cash = C.Cash;
            Ok(Shop.I.Tap("Buy board:1"), "take the foamie"); yield return null; yield return null;
            Ok(SurfGear.Owns(k, 1) && C.Cash == cash && k.Board == 1, $"Kai's foamie: free, carried (${cash - C.Cash})");
            Ok(Shop.I.Tap("Buy board:3") && Shop.I.Tap("Buy wetsuit"), "buy the longboard and the wetsuit"); yield return null; yield return null;
            Ok(SurfGear.Owns(k, 3) && k.Wetsuit && C.Cash == cash - 1600, $"the longboard and the wetsuit (${cash - C.Cash})");
            Ok(Shop.I.Tap("Carry board:3"), "carry the longboard"); yield return null; yield return null;
            Ok(k.Board == 3, $"carrying the longboard ({k.Board})");
            if (shots != null) yield return Shot("surf-shelf-bought");
            Shop.I.Hide(); yield return null;
            // a storm shuts him
            st.Shut = m => true; yield return null; yield return null;
            Ok(me.Prompt != null && me.Prompt.Contains("Gnarly"), $"a storm: \"{me.Prompt}\"");
            st.Shut = SurfShop.Storm; yield return null;
            // the shop and Kai, from the front and from the sea (me out of the way)
            me.Teleport(st.Front + Vector3.Cross(Vector3.up, st.At.forward) * 4f + Vector3.up * 0.1f, 0); yield return new WaitForSeconds(0.3f);
            if (shots != null)
            {
                var f = st.At.forward; f.y = 0; f.Normalize(); var side = Vector3.Cross(Vector3.up, f);
                FlyCam(at + f * 7f + side * 2.5f + Vector3.up * 2.2f, at + Vector3.up * 1.4f); yield return new WaitForSeconds(0.5f); yield return Shot("surf-shop-front");
                FlyCam(at + f * 40f + Vector3.up * 9f, at + Vector3.up * 3f); yield return new WaitForSeconds(0.3f); yield return Shot("surf-shop-out-front");
                FlyCam(at - f * 30f + side * 25f + Vector3.up * 30f, at + f * 20f); yield return new WaitForSeconds(0.3f); yield return Shot("surf-shop-above");
                CameraRig.I.enabled = true;
            }
            // Kai's jetty: placed off the beach, its berth in water a boat floats in; Biscuit moors there (F)
            Ok(SurfShop.JettyRoot != null && SurfShop.Berth != null, "Kai's jetty and its visiting berth");
            if (SurfShop.Berth != null)
            {
                var bp = SurfShop.Berth.position; float dep = Seabed.Depth(bp.x, bp.z);
                Ok(dep > 0.9f, $"the berth in {dep:F2} m of water");
                var s = Skiff.Of("biscuit"); var near = bp - SurfShop.Berth.forward * 3f + SurfShop.Berth.right * 1.5f;
                s.CastOff(true); s.transform.SetPositionAndRotation(new Vector3(near.x, s.transform.position.y, near.z), SurfShop.Berth.rotation); s.TakeOver();
                yield return new WaitForSeconds(0.5f);
                Ok(s.CanMoor, "by Kai's jetty: she can moor");
                s.Moor(); yield return new WaitForSeconds(2.5f);
                Ok(s.MooredAway && s.AwayName == "Swell Surf Co.", $"moored at Swell Surf Co. ({Vector3.Distance(s.transform.position, bp):F1} m off the berth)");
                if (shots != null)
                {
                    var jr = SurfShop.JettyRoot; var jf = jr.forward;
                    FlyCam(jr.position + jf * 48f + jr.right * 18f + Vector3.up * 10f, jr.position + jf * 14f + Vector3.up * 1.5f); yield return new WaitForSeconds(0.5f); yield return Shot("surf-jetty-from-sea");
                    FlyCam(jr.position - jf * 6f + jr.right * 3f + Vector3.up * 2.4f, jr.position + jf * 20f + Vector3.up * 1.6f); yield return new WaitForSeconds(0.3f); yield return Shot("surf-jetty-land-end");
                    FlyCam(jr.position + jf * 4f + jr.right * -6f + Vector3.up * 4.5f, jr.position - jf * 2f + Vector3.up * 3.6f); yield return new WaitForSeconds(0.3f); yield return Shot("surf-jetty-sign");
                    CameraRig.I.enabled = true;
                }
                // walk the jetty: from the berth to the beach end
                me.Teleport(SurfShop.JettyRoot.TransformPoint(new Vector3(0, 1.35f, 26f)), SurfShop.JettyRoot.eulerAngles.y + 180); yield return new WaitForSeconds(0.6f);
                Ok(!me.Swimming && me.transform.position.y > 1.0f, $"on the jetty ({me.transform.position.y:F2} m up)");
            }
            // 3) the Forecast app's surf line
            Phone.I.Show(Phone.Scr.Forecast); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("forecast-surf"); }
            Phone.I.Hide();
            // 4) the wetsuit in the water: 3 s of treading costs a third less
            var water = at; bool wet = false;
            for (int r = 5; r <= 120 && !wet; r += 5) for (int a = 0; a < 360 && !wet; a += 15) { var q = at + Quaternion.Euler(0, a, 0) * Vector3.forward * r; if (Seabed.Depth(q.x, q.z) > 2.5f) { water = q; wet = true; } }
            me.Teleport(new Vector3(water.x, Sea.Level - 0.5f, water.z), 0); yield return Until(() => me.Swimming, 3);
            me.Stamina = 1; yield return new WaitForSeconds(3f); float wetUse = 1 - me.Stamina;
            k.Wetsuit = false; me.Stamina = 1; yield return new WaitForSeconds(3f); float dryUse = 1 - me.Stamina; k.Wetsuit = true;
            Ok(me.Swimming && dryUse > 0 && Mathf.Abs(wetUse * 1.5f - dryUse) < dryUse * 0.15f, $"the wetsuit: 3 s treading used {wetUse:P1} (without it {dryUse:P1}), water {water.x:F0} {water.z:F0}");
            me.Teleport(st.Front + Vector3.up * 0.1f, 0); yield return new WaitForSeconds(0.4f);   // (ashore: the phone's not for swimming)
            // 5) his dawn report the next morning
            JumpTo(today + 2, 6.05); yield return Until(() => C.Seen("kai:surf:" + (today + 2)), 4);
            var (ft, word, good) = SurfShop.Report(today + 2, 0);
            Ok(C.Seen("kai:surf:" + (today + 2)) && (good ? Kai() == 4 && C.Msgs.FindLast(m => m.From == Company.KaiName).Text.Contains(ft + " ft") : Kai() == 3), $"Kai's dawn report: {ft} ft {word} ({(good ? "a text" : "no surf, no text")}, {Kai()} texts)");
            Phone.I.Show(Phone.Scr.Thread, Company.KaiName); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("kai-texts"); }
            Phone.I.Hide();
            Ok(C.Check() == "", $"the company's invariants ({C.Check()})");
            Debug.Log($"[PB] RESULT surfshoptest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
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
