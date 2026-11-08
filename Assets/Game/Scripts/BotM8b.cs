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
            JumpTo(today + 1, 6.6); yield return Until(() => Kai() >= 4, 4);
            Ok(Kai() == 4 && C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.Contains("foam board")) && C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.Contains("in the pocket")), $"Kai's hello the morning after Act 2, with his challenges ({Kai()} texts)");
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
            Ok(C.Seen("kai:surf:" + (today + 2)) && (good ? Kai() == 5 && C.Msgs.FindLast(m => m.From == Company.KaiName).Text.Contains(ft + " ft") : Kai() == 4), $"Kai's dawn report: {ft} ft {word} ({(good ? "a text" : "no surf, no text")}, {Kai()} texts)");
            Phone.I.Show(Phone.Scr.Thread, Company.KaiName); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("kai-texts"); }
            Phone.I.Hide();
            Ok(C.Check() == "", $"the company's invariants ({C.Check()})");
            Debug.Log($"[PB] RESULT surfshoptest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-wavetest <dir> (M8 step 5): the surf at Breaker Point, a 1.5 m face (forced: SurfWaves.TestHeight). A set rolls
        // through: the sea at the break line rises to ~the face height as each wave passes, waves break (white water on the
        // CPU as on the GPU), the broken stretch peels along, nothing lifts the sand. Frames every 1.5 s from the beach and
        // from the side (day), a few at dusk, and the Widowmaker at Gullrock. Logs the height at the break line.
        IEnumerator WaveTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] wavetest: {what}: {(ok ? "ok" : "FAIL")}"); }
            int breaks = 0; SurfWaves.OnLog = sx => { breaks++; Debug.Log("[PB] wavetest: " + sx); };
            yield return new WaitForSeconds(1f);
            SurfWaves.TestHeight = 1.5f; JumpTo(DayTime.Day(Sky.Minutes), 11);
            var bp = SurfWaves.Breaker; var O = new Vector3(bp.O.x, 0, bp.O.y); var f = new Vector3(bp.Dir.x, 0, bp.Dir.y); var r = new Vector3(f.z, 0, -f.x);
            me.Teleport(O + f * (bp.Shore + 6) + Vector3.up * 2f, Mathf.Atan2(-f.x, -f.z) * Mathf.Rad2Deg); yield return new WaitForSeconds(1f);
            Ok(SurfWaves.Live == bp, $"Breaker Point's surf is up near me ({SurfWaves.Live?.Name})");
            // watch the break line for a minute and a half: the highest the sea gets there, the white water, the sand stays dry
            float top = 0, foamMax = 0, sand = 0; int seen = 0, shotN = 0;
            var beachCam = O + f * (bp.Shore + 4) + r * 22f + Vector3.up * 3.2f; var sideCam = O + f * -2f + r * 34f + Vector3.up * 4f;
            for (float t = 0; t < 90; t += Time.deltaTime)
            {
                for (float u = -40; u <= 40; u += 10)
                {
                    var q = O + r * u; float h = Sea.Height(q) ; SurfWaves.Bump(q.x, q.z, out float fm);
                    top = Mathf.Max(top, h); foamMax = Mathf.Max(foamMax, fm);
                }
                var land = O + f * (bp.Shore + 4); sand = Mathf.Max(sand, SurfWaves.Bump(land.x, land.z));
                seen = Mathf.Max(seen, SurfWaves.N);
                if (shots != null && t > 6 && shotN < 16 && t > 6 + shotN * 1.5f)
                {
                    bool side = shotN >= 8; FlyCam(side ? sideCam : beachCam, side ? O + f * -8f + Vector3.up * 0.5f : O + f * -25f - r * 10f + Vector3.up * 0.5f);
                    yield return Shot($"surf-{(side ? "side" : "beach")}-{shotN:00}"); shotN++; CameraRig.I.enabled = true; continue;
                }
                yield return null;
            }
            Ok(seen >= 2 && seen <= SurfWaves.Max, $"a set rolls in ({seen} waves up at once at most)");
            Ok(top > 1.0f && top < 2.4f, $"the sea at the break line rose to {top:F2} m (a 1.5 m face, 0.8-1.2x a wave)");
            Ok(foamMax > 0.6f, $"white water ({foamMax:F2})");
            Ok(breaks >= 2, $"waves broke ({breaks})");
            Ok(sand < 0.01f, $"nothing on the sand ({sand:F3} m)");
            // dusk
            JumpTo(DayTime.Day(Sky.Minutes), 19.4); yield return new WaitForSeconds(2f);
            if (shots != null) for (int i = 0; i < 4; i++) { FlyCam(beachCam, O + f * -25f - r * 10f + Vector3.up * 0.5f); yield return Shot($"surf-dusk-{i}"); CameraRig.I.enabled = true; yield return new WaitForSeconds(2.5f); }
            // the Widowmaker (forced: 1.8x)
            JumpTo(DayTime.Day(Sky.Minutes) + 1, 11);
            var wm = SurfWaves.Widowmaker; var WO = new Vector3(wm.O.x, 0, wm.O.y); var wf = new Vector3(wm.Dir.x, 0, wm.Dir.y); var wr = new Vector3(wf.z, 0, -wf.x);
            me.Teleport(WO + wf * (wm.Shore + 8) + Vector3.up * 3f, 0); yield return new WaitForSeconds(1f);
            float wtop = 0; for (float t = 0; t < 40; t += Time.deltaTime) { for (float u = -30; u <= 30; u += 10) { var q = WO + wr * u; wtop = Mathf.Max(wtop, Sea.Height(q)); } yield return null; }
            Ok(SurfWaves.Live == wm && wtop > 2.0f, $"the Widowmaker at Gullrock: {wtop:F2} m");
            if (shots != null)
            {
                bool OnLedge() { for (int k = 0; k < SurfWaves.N; k++) if (SurfWaves.W[k].S > -4 && SurfWaves.W[k].S < 18) return true; return false; }
                for (float t = 0; t < 80 && !OnLedge(); t += Time.deltaTime) yield return null;
                for (int i = 0; i < 3; i++) { FlyCam(WO + wf * 26f + wr * 38f + Vector3.up * 7f, WO + wf * 4f + Vector3.up * 1.5f); yield return Shot($"widowmaker-{i}"); CameraRig.I.enabled = true; yield return new WaitForSeconds(1.4f); }
            }
            SurfWaves.TestHeight = -1;
            Debug.Log($"[PB] RESULT wavetest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-surftest <dir> (M8 step 6): surfing at Breaker Point on a forced 1.4 m day. In the water with Kai's foamie: E,
        // lying on it. Paddle (W); a duck-dive (Ctrl) sinks me and the board. Lined up in front of an incoming wave on its
        // shoulder, paddling in: the pop-up bar; Space in the green: up and riding, carving along the face away from the
        // peak (the bot holds ~55 degrees); a few seconds, the pocket maybe, the ride ends (over or a wipeout). The next wave:
        // turned too far round: a wipeout, the leash, paddling again. The bodyboard: no pop-up, ridden lying down. E: off.
        // Chase-cam frames of each.
        IEnumerator SurfTest(string dir)
        {
            shots = string.IsNullOrEmpty(dir) ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] surftest: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.deltaTime) yield return null; }
            IEnumerator E() { Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null; }
            yield return new WaitForSeconds(1f);
            var C = Firm.C; var k = Firm.I.MyKit; var sf = me.Surf;
            C.SetAct(2, Sky.Minutes); C.Earn(Firm.I.NewTx("test"), Firm.MyName, "surftest", 5000, Sky.Minutes);
            foreach (var it in new[] { "board:1", "board:2", "board:4" }) { if (it == "board:4") C.Event("kai:ch1", Sky.Minutes); Firm.I.Buy(it, 1); }
            Firm.I.UseBoard(1); SurfWaves.TestHeight = 1.4f; JumpTo(DayTime.Day(Sky.Minutes) + 1, 10);
            var bp = SurfWaves.Breaker; var O = new Vector3(bp.O.x, 0, bp.O.y); var f = new Vector3(bp.Dir.x, 0, bp.Dir.y); var r = new Vector3(f.z, 0, -f.x);
            float inYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            me.Teleport(O + f * -30f + Vector3.up * -0.5f, inYaw); yield return Until(() => me.Swimming, 4);
            Ok(me.Swimming && SurfWaves.Live == bp, "in the water at Breaker Point");
            yield return null; Ok(me.Prompt == "Lie on your foam board", $"the prompt: \"{me.Prompt}\"");
            yield return E();
            Ok(sf.State == Surfer.St.Paddle && sf.Board == 1, $"lying on the foamie ({sf.State})");
            // paddle
            var p0 = sf.Pos; Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(2.5f); Ctl.BotMove = Vector2.zero;
            Ok((sf.Pos - p0).magnitude > 2.5f && sf.Speed > 1f, $"paddled {(sf.Pos - p0).magnitude:F1} m ({sf.Speed:F1} m/s)");
            if (shots != null) { Chase(sf, -7f, 2.6f, 3f); yield return Shot("paddle"); CameraRig.I.enabled = true; }
            // a duck-dive
            Surfer.BotDuck = true; yield return new WaitForSeconds(0.5f);
            float under = Sea.Height(sf.Pos) - me.transform.position.y; Surfer.BotDuck = false; yield return new WaitForSeconds(0.8f);
            Ok(under > 0.4f, $"the duck-dive: {under:F2} m under");
            // 1) line up on an incoming wave's shoulder and catch it
            int ridden = 0, wipes = 0; float best = 0; bool pocket = false;
            for (int attempt = 0; attempt < 4 && ridden < 1; attempt++)
            {
                yield return Until(() => { for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22 && SurfWaves.W[q].H > 0.6f) return true; return false; }, 90);
                int wk = -1; for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) wk = q;
                if (wk < 0) continue;
                var w = SurfWaves.W[wk]; float side = attempt % 2 == 0 ? 1 : -1, u = w.U0 + side * 14f;
                sf.Pos = O + f * (w.S + 7f) + r * u; sf.Yaw = inYaw; sf.Speed = 1.2f; Ctl.BotMove = Vector2.up;
                yield return Until(() => sf.State == Surfer.St.Pop || sf.State == Surfer.St.Ride, 12);
                Ok(sf.State == Surfer.St.Pop, $"wave {w.Id}: the pop-up bar ({sf.State})");
                if (sf.State != Surfer.St.Pop) { Ctl.BotMove = Vector2.zero; continue; }
                if (shots != null && attempt == 0) { Chase(sf, -6f, 2.2f, 4f); yield return Shot("popup"); CameraRig.I.enabled = true; }
                yield return Until(() => sf.PopNeedle >= 0.62f, 2); Surfer.Bot.PopNow = true; yield return null; Surfer.Bot.PopNow = false; Ctl.BotMove = Vector2.zero;
                Ok(sf.State == Surfer.St.Ride, $"Space in the green: riding ({sf.State})");
                if (sf.State != Surfer.St.Ride) continue;
                float target = sf.Theta > 0 ? 55 : -55, t0 = Time.time; int frame = 0;
                while (sf.State == Surfer.St.Ride && Time.time - t0 < 20)
                {
                    Ctl.BotSteer = Mathf.Clamp((target - sf.Theta) / 25f, -1, 1);
                    best = Mathf.Max(best, sf.Speed); pocket |= sf.InPocket;
                    if (shots != null && frame < 6 && Time.time - t0 > 0.8f + frame * 1.1f) { Chase(sf, -8f, 3.2f, sf.Theta > 0 ? -5f : 5f); yield return Shot($"ride-{frame}"); CameraRig.I.enabled = true; frame++; continue; }
                    yield return null;
                }
                Ctl.BotSteer = 0; ridden++;
                Ok(sf.LastRide > 2.5f, $"the ride: {sf.LastRide:F1} s, {(sf.LastOutcome == "wipe" ? "a wipeout" : "over")}, top {best:F1} m/s, the pocket {(pocket ? "found" : "not")}");
            }
            Ok(ridden >= 1, $"rode a wave ({ridden})");
            Ok(sf.LastScore > 50 && k.SurfBest == sf.LastScore && k.Rides >= 1, $"the ride's score {sf.LastScore} pts, my best {k.SurfBest}, {k.Rides} ride(s)");
            yield return Until(() => C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.StartsWith("8 seconds")), 4);
            Ok(sf.LastRide < 8 || C.Seen("kai:ch2") && C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.StartsWith("8 seconds")), $"Kai's challenge 2 (8 s): {(C.Seen("kai:ch2") ? "done, his sunburst" : "not yet")}");
            // the pocket: hang back until the curl's close, then run with it (Kai's challenge 3: 3 s in the pocket)
            float bestPocket = 0;
            for (int attempt = 0; attempt < 4 && !C.Seen("kai:ch3"); attempt++)
            {
                yield return Until(() => sf.State == Surfer.St.Paddle, 6);
                yield return Until(() => { for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22 && SurfWaves.W[q].H > 0.6f) return true; return false; }, 90);
                int wk = -1; for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) wk = q;
                if (wk < 0) continue;
                var w = SurfWaves.W[wk]; bool fastRight = ((w.Id / 10 + w.Id % 10) & 1) == 0; float side = fastRight ? 1 : -1;   // (SurfWaves.Waves: which way it peels fast)
                sf.Pos = O + f * (w.S + 7f) + r * (w.U0 + side * 4f); sf.Yaw = inYaw; sf.Speed = 1.2f; Ctl.BotMove = Vector2.up;
                yield return Until(() => sf.State == Surfer.St.Pop, 12); yield return Until(() => sf.PopNeedle >= 0.62f, 2); Surfer.Bot.PopNow = true; yield return null; Surfer.Bot.PopNow = false; Ctl.BotMove = Vector2.zero;
                float inPocket = 0, logT = 0;
                while (sf.State == Surfer.St.Ride)
                {
                    int q = SurfWaves.Find(sf.WaveId); if (q < 0) break; var ww = SurfWaves.W[q];
                    float sgn = sf.Theta > 0 ? 1 : -1, curlU = sgn > 0 ? ww.U0 + ww.BR : ww.U0 - ww.BL, gap = (sf.U - curlU) * sgn;
                    // match the curl's peel (5 m/s on the fast side): a bit slower to let it close, a bit faster once it's there
                    float lat = !ww.Broke ? 1.2f : SurfWaves.PeelFast + (gap > 3f ? -2f : gap < 1f ? 1.5f : 0.3f);       // (wait by the peak till it breaks)
                    float target = sgn * Mathf.Clamp(Mathf.Asin(Mathf.Clamp(lat / Mathf.Max(sf.Speed, 1f), 0, 0.97f)) * Mathf.Rad2Deg, 10, 75);
                    Ctl.BotSteer = Mathf.Clamp((target - sf.Theta) / 20f, -1, 1);
                    if (sf.InPocket) inPocket += Time.deltaTime;
                    logT += Time.deltaTime; if (logT > 0.5f) { logT = 0; SurfWaves.Local(q, sf.U, sf.X, out float bs, out _, out float wf); Debug.Log($"[PB] surftest:   S {ww.S:F1} broke {ww.Broke} gap {gap:F1} X {sf.X:F2} face {wf:F2} bs {bs:F2} theta {sf.Theta:F0} v {sf.Speed:F1} pocket {sf.InPocket}"); }
                    yield return null;
                }
                Ctl.BotSteer = 0; bestPocket = Mathf.Max(bestPocket, inPocket);
                Debug.Log($"[PB] surftest: pocket try {attempt}: {inPocket:F1} s in the pocket, {sf.LastOutcome} after {sf.LastRide:F1} s, {sf.LastScore} pts");
            }
            yield return Until(() => C.Msgs.Exists(m => m.From == Company.KaiName && m.Text.StartsWith("three seconds")), 4);
            Ok(C.Seen("kai:ch3"), $"Kai's challenge 3: 3 s in the pocket (best {bestPocket:F1} s)");

            yield return Until(() => sf.State == Surfer.St.Paddle, 4);
            // 2) the next one, turned too far round: a wipeout
            yield return Until(() => { for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22 && SurfWaves.W[q].H > 0.6f) return true; return false; }, 90);
            { int wk = -1; for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) wk = q;
              if (wk >= 0) { var w = SurfWaves.W[wk]; sf.Pos = O + f * (w.S + 7f) + r * (w.U0 + 14f); sf.Yaw = inYaw; sf.Speed = 1.2f; Ctl.BotMove = Vector2.up; } }
            yield return Until(() => sf.State == Surfer.St.Pop, 12); yield return Until(() => sf.PopNeedle >= 0.62f, 2); Surfer.Bot.PopNow = true; yield return null; Surfer.Bot.PopNow = false; Ctl.BotMove = Vector2.zero;
            if (sf.State == Surfer.St.Ride) { Ctl.BotSteer = sf.Theta > 0 ? 1 : -1; yield return Until(() => sf.State != Surfer.St.Ride, 6); Ctl.BotSteer = 0; }
            Ok(sf.State == Surfer.St.Wipe && sf.LastOutcome == "wipe", $"turned too far round: a wipeout ({sf.State})");
            if (shots != null && sf.State == Surfer.St.Wipe) { Chase(sf, -6f, 2.4f, 4f); yield return Shot("wipeout"); CameraRig.I.enabled = true; }
            yield return Until(() => sf.State == Surfer.St.Paddle, 4);
            Ok(sf.State == Surfer.St.Paddle, "the leash: back on the board, paddling");
            // 3) the bodyboard: no pop-up
            yield return E(); Ok(sf.State == Surfer.St.Off && me.Cc.enabled, "E: off the board");
            Firm.I.UseBoard(2); yield return Until(() => me.Swimming && me.Prompt == "Lie on your bodyboard", 4); yield return E();
            Ok(sf.State == Surfer.St.Paddle && sf.Board == 2, "on the bodyboard");
            yield return Until(() => { for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22 && SurfWaves.W[q].H > 0.6f) return true; return false; }, 90);
            { int wk = -1; for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) wk = q;
              if (wk >= 0) { var w = SurfWaves.W[wk]; sf.Pos = O + f * (w.S + 7f) + r * (w.U0 - 14f); sf.Yaw = inYaw; sf.Speed = 1.2f; Ctl.BotMove = Vector2.up; } }
            yield return Until(() => sf.State == Surfer.St.Ride || sf.State == Surfer.St.Pop, 12); Ctl.BotMove = Vector2.zero;
            Ok(sf.State == Surfer.St.Ride, $"the bodyboard catches it straight away, lying down ({sf.State})");
            if (shots != null && sf.State == Surfer.St.Ride) { yield return new WaitForSeconds(1f); Chase(sf, -6f, 2.4f, sf.Theta > 0 ? -4f : 4f); yield return Shot("bodyboard"); CameraRig.I.enabled = true; }
            yield return Until(() => sf.State == Surfer.St.Paddle, 25);
            yield return E();
            me.Teleport(O + f * (bp.Shore + 8) + Vector3.up * 2f, 0); yield return new WaitForSeconds(1f);
            Phone.I.Show(Phone.Scr.Journal); yield return null; yield return null;
            if (shots != null) { yield return new WaitForSeconds(0.4f); yield return Shot("journal-surf"); }
            Phone.I.Hide();
            SurfWaves.TestHeight = -1;
            Debug.Log($"[PB] RESULT surftest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
        // -pb-surfcoop host|rider (M8 step 7): two on one wave. Both on foamies at Breaker Point (a forced 1.4 m day; the waves
        // come from the shared sea clock, so both machines have the same ones). The same wave, 6 m apart on its shoulder:
        // both pop up and ride it: a party wave on both (each sees the other on it in their pose). The next wave, 1 m apart:
        // a drop-in: both go down.
        IEnumerator SurfCoop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] surfcoop {role}: {what}: {(ok ? "ok" : "FAIL")}"); }
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            Ok(Coop.Joined && Coop.I.Roster.Count >= 2, "the crew is together");
            if (!Coop.Joined) { Debug.Log($"[PB] RESULT surfcoop {role}: FAIL (never connected)"); Application.Quit(); yield break; }
            var C = Firm.C; var sf = me.Surf; bool host = role == "host";
            shots = Arg("-pb-surfshots"); if (shots != null) Directory.CreateDirectory(shots);
            if (host) { C.SetAct(2, Sky.Minutes); C.Earn(Firm.I.NewTx("test"), Firm.MyName, "surfcoop", 5000, Sky.Minutes); Firm.I.Dirty = true; }
            yield return Until(() => C.Act >= 2 && C.Cash >= 5000, 20);
            Firm.I.Buy("board:1", 1); yield return Until(() => SurfGear.Owns(Firm.I.MyKit, 1), 10);
            if (!host) { yield return new WaitForSeconds(1f); Firm.I.Buy("board:1", 1); yield return Until(() => SurfGear.Owns(Firm.I.MyKit, 1), 10); }
            Ok(SurfGear.Owns(Firm.I.MyKit, 1), "a foamie");
            SurfWaves.TestHeight = 1.4f;
            var bp = SurfWaves.Breaker; var O = new Vector3(bp.O.x, 0, bp.O.y); var f = new Vector3(bp.Dir.x, 0, bp.Dir.y); var r = new Vector3(f.z, 0, -f.x);
            float inYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            me.Teleport(O + f * -35f + r * (host ? 0 : 8) + Vector3.up * -0.5f, inYaw); yield return Until(() => me.Swimming, 5);
            yield return Until(() => me.Prompt != null && me.Prompt.StartsWith("Lie on"), 3);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return null;
            Ok(sf.State == Surfer.St.Paddle, "on the board");
            yield return Until(() => { foreach (var m in Coop.I.Mates.Values) if (m != null && m.Surf != null && m.Surf.Shown) return true; return false; }, 40);
            Ok(true, "my crewmate's on a board too");
            // both take the first wave of the next set (the sea clock is shared, so it's the same wave on both machines)
            int n1 = Mathf.FloorToInt(Sea.T / SurfWaves.SetGrid) + 1;
            IEnumerator Catch(float gap, int id)
            {
                yield return Until(() => { for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].Id == id && SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) return true; return false; }, 200);
                int wk = -1; for (int q = 0; q < SurfWaves.N; q++) if (SurfWaves.W[q].Id == id && SurfWaves.W[q].S > -32 && SurfWaves.W[q].S < -22) wk = q;
                if (wk < 0) { Debug.Log($"[PB] surfcoop {role}: wave {id} never came"); yield break; }
                var w = SurfWaves.W[wk]; Debug.Log($"[PB] surfcoop {role}: lining up on wave {w.Id}");
                sf.Pos = O + f * (w.S + 7f) + r * (w.U0 + 10f + (host ? 0 : gap)); sf.Yaw = inYaw; sf.Speed = 1.2f; Ctl.BotMove = Vector2.up;
                yield return Until(() => sf.State == Surfer.St.Pop, 12); yield return Until(() => sf.PopNeedle >= 0.62f, 2);
                Surfer.Bot.PopNow = true; yield return null; Surfer.Bot.PopNow = false; Ctl.BotMove = Vector2.zero;
                float rt = 0; bool shot = false;
                while (sf.State == Surfer.St.Ride)
                {
                    Ctl.BotSteer = Mathf.Clamp((50 - sf.Theta) / 25f, -1, 1); rt += Time.deltaTime;
                    if (!host && !shot && rt > 1.5f && shots != null)
                    {
                        shot = true; Angler mate = null; foreach (var m in Coop.I.Mates.Values) if (m != null) mate = m;
                        if (mate != null) { var mp = mate.transform.position; FlyCam(mp + f * 6f + r * 4f + Vector3.up * 2.2f, mp + Vector3.up * 0.8f); yield return Shot("crewmate-surfing"); CameraRig.I.enabled = true; continue; }
                    }
                    yield return null;
                }
                Ctl.BotSteer = 0;
            }
            // 1) the same wave, 6 m apart: a party wave
            yield return Catch(6f, n1 * 10);
            Ok(sf.LastRide > 2, $"rode it ({sf.LastRide:F1} s, {sf.LastScore} pts)");
            Ok(sf.LastParty, "a party wave (two on one wave)");
            yield return Until(() => Firm.C.Seen("surf:party"), 8);                 // (Firm.C: a rider's company is replaced at each sync)
            Ok(Firm.C.Seen("surf:party"), "the crew's company has the party wave");
            yield return Until(() => sf.State == Surfer.St.Paddle, 6);
            // 2) the next one, 1 m apart: a drop-in, both off
            yield return Catch(1f, (n1 + 1) * 10);
            Ok(sf.Dropped && sf.LastOutcome == "wipe", $"a drop-in: both down ({sf.LastOutcome}, dropped {sf.Dropped})");
            yield return new WaitForSeconds(4f);
            SurfWaves.TestHeight = -1;
            Ok(C.Check() == "", "the company's invariants");
            Debug.Log($"[PB] RESULT surfcoop {role}: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // a chase camera: behind the board (back m), up, and to the side
        static void Chase(Surfer s, float back, float up, float side)
        {
            var h = Quaternion.Euler(0, s.Yaw, 0); var at = s.Pos;
            FlyCam(at + h * new Vector3(side, 0, back) + Vector3.up * up, at + h * Vector3.forward * 3f + Vector3.up * 0.6f);
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
