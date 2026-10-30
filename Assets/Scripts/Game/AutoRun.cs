using System.Collections;
using System.Collections.Generic;
using System.IO;
using PortBoss.Net;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Command-line test harness (no effect without -pb-* args):
    //   -pb-solo | -pb-host-lan | -pb-host-steam | -pb-join-lan <ip> | -pb-join-lobby
    //   -pb-bot  -pb-shots <dir> <seconds>  -pb-quit <seconds>  -pb-timescale <x>  -pb-night <seconds>
    public class AutoRun : MonoBehaviour
    {
        static string Arg(string name, int k = 1)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - k; i++) if (a[i] == name) return a[i + k];
            return null;
        }
        static bool Has(string name) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;

        public static void Begin(GameUI ui)
        {
            // Look / transport switches alone don't make a test run (test mode auto-sells, skips the intro, frees the cursor).
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), x => x.StartsWith("-pb-") && System.Array.IndexOf(LookSwitches, x) < 0)) return;
            Active = true;
            new GameObject("AutoRun").AddComponent<AutoRun>().ui = ui;
        }

        public static bool Active;
        static readonly string[] LookSwitches = { "-pb-ownart", "-pb-bevel", "-pb-oldwater", "-pb-oldfog", "-pb-noclouds", "-pb-force-steam" };
        // Numbers on the command line use '.' whatever the PC's locale ("0.5" is 5 on a de-DE machine otherwise).
        static bool ArgF(string name, out float v, int k = 1) => float.TryParse(Arg(name, k), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
        GameUI ui;
        float quitAt = -1, nextLog;
        Bot bot;

        IEnumerator Start()
        {
            if (ArgF("-pb-quit", out var q)) quitAt = q;
            if (ArgF("-pb-timescale", out var ts)) Time.timeScale = ts;
            yield return null;
            if (Has("-pb-solo")) ui.Play();
            else if (Has("-pb-host-lan")) ui.Host(true);
            else if (Has("-pb-host-steam")) ui.Host(false);
            else if (Arg("-pb-join-lan") != null) ui.JoinIp(Arg("-pb-join-lan"));
            else if (Has("-pb-join-lobby"))
            {
                for (int tries = 0; tries < 20 && !NetworkManager.Singleton.IsListening; tries++)
                {
                    var t = Session.FindLobbies();
                    while (!t.IsCompleted) yield return null;
                    Debug.Log($"[PB] lobby search: {t.Result.Length} found");
                    if (t.Result.Length > 0) { var j = Session.JoinLobby(t.Result[0]); while (!j.IsCompleted) yield return null; break; }
                    yield return new WaitForSecondsRealtime(2);
                }
            }
            if (Has("-pb-bot")) { bot = new Bot(); Player.BotInput = bot.Tick; }
            var dir = Arg("-pb-shots");
            if (dir != null) StartCoroutine(Shots(dir, ArgF("-pb-shots", out var iv, 2) ? iv : 5f));
            if (ArgF("-pb-night", out var n)) StartCoroutine(ForceNight(n));
            if (Has("-pb-trolleytest")) StartCoroutine(TrolleyTest());
            if (Has("-pb-wobbletest")) StartCoroutine(WobbleTest());
            if (Arg("-pb-charshots") != null) StartCoroutine(CharShots(Arg("-pb-charshots")));
            if (Arg("-pb-crateshots") != null) StartCoroutine(CrateShots(Arg("-pb-crateshots")));
            if (Has("-pb-swimtest")) StartCoroutine(SwimTest());
            if (Has("-pb-piertest")) StartCoroutine(PierTest());
            if (Has("-pb-knocktest")) StartCoroutine(KnockTest());
            if (Has("-pb-shovehost")) StartCoroutine(ShoveHost());
            if (Has("-pb-shovetest")) StartCoroutine(ShoveTest());
            if (Arg("-pb-stagetest") != null) StartCoroutine(StageTest(Arg("-pb-stagetest")));
            if (Has("-pb-contracttest")) StartCoroutine(ContractTest());
            if (int.TryParse(Arg("-pb-day"), out var day)) StartCoroutine(StartOnDay(day));
            if (int.TryParse(Arg("-pb-settings"), out var st)) ui.DebugOpenSettings(st);
            if (Has("-pb-settingstest")) StartCoroutine(SettingsTest());
            if (Has("-pb-storm")) StartCoroutine(StormTest());
            if (Arg("-pb-carrytest") != null) StartCoroutine(CarryTest(Arg("-pb-carrytest")));
            if (Arg("-pb-mapshots") != null) StartCoroutine(MapShots(Arg("-pb-mapshots")));
            if (Arg("-pb-uiscreens") != null) StartCoroutine(UiScreens(Arg("-pb-uiscreens")));
            if (Arg("-pb-fxshots") != null) StartCoroutine(FxShots(Arg("-pb-fxshots")));
            if (Arg("-pb-areashots") != null) StartCoroutine(AreaShots(Arg("-pb-areashots")));
            if (Arg("-pb-uishots") != null) StartCoroutine(UiShots(Arg("-pb-uishots")));
            if (Arg("-pb-lookshots") != null) StartCoroutine(LookShots(Arg("-pb-lookshots")));
            if (Has("-pb-randomlook")) StartCoroutine(RandomLook());
            if (Arg("-pb-laddershot") != null) StartCoroutine(LadderShot(Arg("-pb-laddershot")));
            if (Arg("-pb-film") != null) StartCoroutine(Film(Arg("-pb-film")));
            if (float.TryParse(Arg("-pb-weather"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var wsev)) StartCoroutine(ForceWeatherSoon(wsev));
            if (Has("-pb-forklifttest")) StartCoroutine(ForkliftTest());
            if (Arg("-pb-handletest") != null) StartCoroutine(HandleTest(Arg("-pb-handletest")));
            if (Has("-pb-staminatest")) StartCoroutine(StaminaTest());
            if (Has("-pb-trucktest")) StartCoroutine(TruckTest());
            if (Has("-pb-boattest")) StartCoroutine(BoatTest());
            if (Has("-pb-boardtest")) StartCoroutine(BoardTest());
            if (Has("-pb-evicttest")) StartCoroutine(EvictTest());
            if (Arg("-pb-storeshots") != null) StartCoroutine(StoreShots(Arg("-pb-storeshots")));
            if (Arg("-pb-pushtest") != null) StartCoroutine(PushTest(Arg("-pb-pushtest")));
            if (Arg("-pb-intro") != null) StartCoroutine(IntroShots(Arg("-pb-intro")));
            if (Arg("-pb-rivalshots") != null) StartCoroutine(RivalShots(Arg("-pb-rivalshots")));
        }

        // One input for one frame (a key press), then hands off.
        IEnumerator Press(Player.In i, float after = 0.45f)
        {
            Player.BotInput = () => i; yield return null;
            Player.BotInput = () => new Player.In();
            yield return new WaitForSeconds(after);
        }

        // Held for a while (hold-to-repeat), then let go.
        IEnumerator Hold(Player.In i, float secs)
        {
            bool first = true;
            Player.BotInput = () => { var x = i; if (!first) { x.grab = x.drop = false; } first = false; return x; };
            yield return new WaitForSeconds(secs);
            Player.BotInput = () => new Player.In();
            yield return new WaitForSeconds(0.4f);
        }

        // Crate handling: store on the rack (Q, hold Q), take back (E, hold E), into the trolley (Q), set one down
        // on open ground (Q: upright, at ground level). Shots of each.
        IEnumerator HandleTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var pl = Player.Local; var g = PortGame.I;
            g.DebugGrantKit();
            yield return new WaitForSeconds(1.5f);
            var cam = Camera.main;
            pl.DebugTeleport(Storage.Center + new Vector3(0.5f, 0.2f, 1.7f), 180);
            yield return new WaitForSeconds(0.4f);
            pl.DebugGiveCrates(5);
            yield return new WaitForSeconds(0.6f);
            yield return Press(new Player.In { drop = true });
            int rack1 = Storage.Count;
            yield return Hold(new Player.In { drop = true, dropHeld = true }, 1.6f);
            Debug.Log($"[PB] handle test: rack after Q={rack1}, after hold Q={Storage.Count} carrying={pl.Carrying}");
            Player.CamOverride = true;
            cam.transform.position = Storage.Center + new Vector3(3.5f, 2.6f, 5f); cam.transform.LookAt(Storage.Center + Vector3.up * 1.3f);
            yield return Snap(dir, "handle-rack");
            Player.CamOverride = false;
            bool rackOk = rack1 == 1 && Storage.Count == 5 && pl.Carrying == 0;
            yield return Press(new Player.In { grab = true });
            int got1 = pl.Carrying;
            yield return Hold(new Player.In { grab = true, grabHeld = true }, 1.2f);
            Debug.Log($"[PB] handle test: took from rack E={got1}, after hold E={pl.Carrying}, rack now {Storage.Count}");
            bool takeOk = got1 == 1 && pl.Carrying >= 3 && Storage.Count == 5 - pl.Carrying;
            var t = Trolley.I;
            bool trolleyOk = false;
            if (t != null)
            {
                pl.DebugTeleport(t.transform.position + new Vector3(0, 0.3f, 1.6f), 180);
                yield return new WaitForSeconds(0.5f);
                yield return Press(new Player.In { drop = true });
                yield return Press(new Player.In { drop = true });
                Debug.Log($"[PB] handle test: trolley load={t.Load} carrying={pl.Carrying}");
                trolleyOk = t.Load == 2;
                Player.CamOverride = true;
                cam.transform.position = t.transform.position + new Vector3(2.5f, 2.2f, 2.5f); cam.transform.LookAt(t.transform.position + Vector3.up * 0.6f);
                yield return Snap(dir, "handle-trolley");
                Player.CamOverride = false;
            }
            pl.DebugTeleport(new Vector3(30, 0.2f, -50), 0);
            yield return new WaitForSeconds(0.6f);
            int before = pl.Carrying;
            yield return Press(new Player.In { drop = true }, 1.2f);
            Crate down = null; float bd = 9f;
            foreach (var c in Crate.All) { float d = Vector3.Distance(c.transform.position, pl.transform.position); if (!c.Attached.Value && d < bd) { bd = d; down = c; } }
            bool groundOk = down != null && pl.Carrying == before - 1 && Vector3.Angle(down.transform.up, Vector3.up) < 5f && Mathf.Abs(down.transform.position.y - Crate.Size / 2) < 0.1f;
            Debug.Log($"[PB] handle test: set down at {(down != null ? down.transform.position.ToString("F2") : "-")} tilt={(down != null ? Vector3.Angle(down.transform.up, Vector3.up) : -1):F1}");
            Player.CamOverride = true;
            cam.transform.position = pl.transform.position + new Vector3(2.5f, 2.4f, -1.5f); cam.transform.LookAt(pl.transform.position + pl.transform.forward * 0.9f + Vector3.up * 0.5f);
            yield return Snap(dir, "handle-ground");
            Player.CamOverride = false;
            Debug.Log($"[PB] handle test: {(rackOk && takeOk && trolleyOk && groundOk ? "PASS" : "FAIL")} (rack {rackOk}, take {takeOk}, trolley {trolleyOk}, ground {groundOk})");
        }

        // Swim (sprinting) until out of stamina: the harbour master should fish us out onto a ladder.
        IEnumerator StaminaTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(30, -2.4f, 30), 0);
            float t0 = Time.time, next = 0;
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = new Vector3(Mathf.Sin(Time.time * 0.3f), 0, Mathf.Cos(Time.time * 0.3f)), sprint = true };
            while (Time.time - t0 < 40 && (pl.Swimming || Time.time - t0 < 2))
            {
                if (Time.time > next) { next = Time.time + 3; Debug.Log($"[PB] stamina test: t={Time.time - t0:F0} swimming={pl.Swimming} stamina={pl.Stamina:F2}"); }
                yield return null;
            }
            Player.BotInput = () => new Player.In();
            yield return new WaitForSeconds(2f);
            bool onDeck = pl.transform.position.y > -0.5f && !pl.Swimming;
            Debug.Log($"[PB] stamina test: {(onDeck ? "PASS" : "FAIL")} out of the water after {Time.time - t0:F1}s at {pl.transform.position:F1}, stamina {pl.Stamina:F2}");
        }

        // Stand in a lorry's lane ahead of it: it stops short and honks (never runs us over), at most MaxQueue line up
        // behind us however long we stand there, and once we step aside they drive on.
        IEnumerator TruckTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            Vector3? spot = null;
            while (spot == null) { spot = RoadTraffic.DebugAhead(60f); yield return null; }
            pl.DebugTeleport(spot.Value, 90);
            Debug.Log($"[PB] truck test: standing in the road at {spot.Value:F1}");
            float t0 = Time.time, minGap = float.MaxValue; int maxStopped = 0; bool knocked = false;
            Player.BotInput = () => new Player.In();
            while (Time.time - t0 < 150f)
            {
                var (stopped, held, nearest) = RoadTraffic.DebugLane(pl.transform.position);
                if (nearest < minGap) minGap = nearest;
                maxStopped = Mathf.Max(maxStopped, stopped);
                knocked |= pl.Stunned;
                if (Mathf.Repeat(Time.time - t0, 10f) < Time.deltaTime) Debug.Log($"[PB] truck test: t={Time.time - t0:F0} queued={stopped} held={held} nearestGap={nearest:F1}");
                yield return null;
            }
            var dir = Arg("-pb-shots");
            if (dir != null)
            {
                Player.CamOverride = true;
                Camera.main.transform.position = pl.transform.position + new Vector3(-14f, 9f, 10f); Camera.main.transform.LookAt(pl.transform.position + new Vector3(-20f, 0, 0));
                yield return Snap(dir, "truck-queue");
                Player.CamOverride = false;
            }
            pl.DebugTeleport(new Vector3(spot.Value.x, 0.3f, World.RoadZ + 8f), 0); // step off the road
            yield return new WaitForSeconds(20f);
            var after = RoadTraffic.DebugLane(spot.Value);
            bool ok = !knocked && minGap > 0.5f && maxStopped >= 1 && maxStopped <= RoadTraffic.MaxQueue && after.stopped == 0;
            Debug.Log($"[PB] truck test: {(ok ? "PASS" : "FAIL")} knocked={knocked} closest={minGap:F1}m maxQueue={maxStopped} queuedAfterStepAside={after.stopped}");
        }

        // Boats are solid: an arriving boat shoves a swimmer in its way aside (never sails through them), and on deck
        // you can't walk into the bridge.
        IEnumerator BoatTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            Boat b = null;
            while (b == null) { b = Boat.All.Find(x => x != null && x.State.Value == Boat.St.Arriving); yield return null; }
            yield return new WaitForSeconds(2f);
            var ahead = b.transform.position + b.Velocity.normalized * 14f; ahead.y = Water.Level - 1.3f;
            pl.DebugTeleport(ahead, 0);
            Player.BotInput = () => new Player.In();
            float t0 = Time.time, inside = 0;
            while (Time.time - t0 < 6f)
            {
                var lp = b.transform.InverseTransformPoint(pl.transform.position + Vector3.up);
                if (Mathf.Abs(lp.x) < Boat.HalfW - 0.3f && Mathf.Abs(lp.z) < Boat.HalfL - 0.3f && lp.y < Boat.DeckY) inside += Time.deltaTime;
                yield return null;
            }
            Debug.Log($"[PB] boat test: swimmer inside the hull for {inside:F2}s (0 = shoved aside)");
            while (b.State.Value != Boat.St.Docked) yield return null;
            yield return new WaitForSeconds(1f);
            pl.DebugTeleport(b.transform.TransformPoint(new Vector3(0, Boat.DeckY + 0.3f, 1.5f)), b.transform.eulerAngles.y + 180);
            var aft = b.transform.TransformDirection(Vector3.back);
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = aft };
            yield return new WaitForSeconds(3f);
            Player.BotInput = () => new Player.In();
            float z = b.transform.InverseTransformPoint(pl.transform.position).z;
            Debug.Log($"[PB] boat test: {(inside < 0.3f && z > -2.9f ? "PASS" : "FAIL")} walked aft to local z={z:F2} (the bridge starts about -2.4)");
        }

        // Walk from the jetty over the gangplank onto the docked boat (and back): never held up on the way.
        IEnumerator BoardTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            Boat b = null;
            while (b == null) { b = Boat.DockedAt[0]; yield return null; }
            yield return new WaitForSeconds(1.5f);
            var deckY = b.transform.TransformPoint(new Vector3(0, Boat.DeckY, 0)).y;
            Debug.Log($"[PB] board test: deck at y={deckY:F2}, pier top {World.PierTop:F2}, boat at {b.transform.position:F2}");
            bool ok = true;
            foreach (var (from, dir, name) in new[] { (new Vector3(1.2f, World.PierTop + 0.1f, World.Gangway), Vector3.right, "on"), (Vector3.zero, Vector3.left, "off") })
            {
                if (name == "on") pl.DebugTeleport(from, 90);
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = dir };
                float t0 = Time.time, lastX = pl.transform.position.x, stuck = 0;
                while (Time.time - t0 < 3.5f)
                {
                    yield return new WaitForSeconds(0.25f);
                    float x = pl.transform.position.x;
                    if (Mathf.Abs(x - lastX) < 0.3f) stuck += 0.25f;
                    lastX = x;
                    Debug.Log($"[PB] board test {name}: t={Time.time - t0:F2} pos={pl.transform.position:F2}");
                    if (name == "on" && x > b.transform.position.x - 0.5f) break;
                    if (name == "off" && x < 1.5f) break;
                }
                foreach (var h in Physics.SphereCastAll(pl.transform.position + Vector3.up * 0.9f - dir * 0.2f, 0.45f, dir, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.collider.transform.IsChildOf(pl.transform)) Debug.Log($"[PB] board test {name}: ahead {h.collider.name} ({h.collider.GetType().Name}) parent={h.collider.transform.parent?.name} at {h.point:F2} d={h.distance:F2}");
                float xEnd = pl.transform.position.x;
                bool done = name == "on" ? xEnd > b.transform.position.x - 0.5f && pl.transform.position.y > deckY - 0.3f : xEnd < 1.5f;
                Debug.Log($"[PB] board test {name}: {(done ? "made it" : "STUCK")} in {Time.time - t0:F1}s, stuck for {stuck:F2}s");
                ok &= done && stuck <= 0.25f;
            }
            Player.BotInput = () => new Player.In();
            Debug.Log($"[PB] board test: {(ok ? "PASS" : "FAIL")}");
        }

        // Stay aboard as the boat casts off: the captain throws us back onto the jetty (and we land on it, dry).
        IEnumerator EvictTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            Boat b = null;
            while (b == null) { b = Boat.DockedAt[0]; yield return null; }
            yield return new WaitForSeconds(1.5f);
            pl.DebugGiveCrates(2);
            pl.DebugTeleport(b.transform.TransformPoint(new Vector3(0.5f, Boat.DeckY + 0.1f, 1f)), 0);
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"[PB] evict test: aboard at {pl.transform.position:F1}");
            b.DebugLeaveNow();
            float t0 = Time.time, maxY = 0;
            while (Time.time - t0 < 4f) { maxY = Mathf.Max(maxY, pl.transform.position.y); yield return null; }
            var p = pl.transform.position;
            bool onJetty = Mathf.Abs(p.x) < 3f && p.y > World.PierTop - 0.2f && !pl.Swimming;
            Debug.Log($"[PB] evict test: landed at {p:F1} (peak y {maxY:F1}), swimming={pl.Swimming}, carrying={pl.Carrying}, boat at {b.transform.position:F1}: {(onJetty ? "PASS" : "FAIL")}");
        }

        // The crate store at each level (shop buys, any time of day), with crates on the racks across the upgrade.
        IEnumerator StoreShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I; var pl = Player.Local;
            PortGame.I.ForceWeather(0);
            g.Money.Value = 2000;
            for (int i = 0; i < 4; i++) { var c = g.SpawnCrate(Storage.Center + new Vector3(0, 1, 3), 1f); yield return null; Storage.Put(c, Storage.SlotPos(i)); }
            pl.DebugTeleport(Storage.Center + new Vector3(4f, 0.2f, 6f), 200);
            Player.CamOverride = true;
            var cam = Camera.main;
            for (int lvl = 1; lvl <= 3; lvl++)
            {
                if (lvl > 1) { g.BuyStoreRpc(); yield return new WaitForSeconds(1f); }
                var ctr = Storage.Center;
                cam.transform.position = ctr + new Vector3(3f, 3.2f, 9.5f); cam.transform.LookAt(ctr + Vector3.up * 1.6f);
                yield return new WaitForSeconds(0.5f);
                Debug.Log($"[PB] store test: level {Storage.Level} racks={Storage.RackCount} slots={Storage.Slots} stored={Storage.Count} cold={Storage.Cold} money={g.Money.Value} near(-15)={Storage.Near(new Vector3(-15, 0, -6))}");
                yield return Snap(dir, $"store-{lvl}");
            }
            // Fill a slot on the new west rack.
            var c2 = g.SpawnCrate(Storage.Center + new Vector3(0, 1, 3), 1f); yield return null;
            Debug.Log($"[PB] store test: put on rack 4: {Storage.Put(c2, Storage.SlotPos(20))} at {c2.transform.position:F1}");
            g.BuyTrolleyRpc(); yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] store test: trolley bought by day: {Trolley.I != null} money={g.Money.Value}");
            Player.CamOverride = false;
        }

        // The trolley: shots of the new cart, then push it into a wall (stops), to the jetty edge (stops, stays dry),
        // turn it beside a wall (doesn't swing through), and push it along freely; held pose stays in front of the hands.
        IEnumerator PushTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I; var pl = Player.Local;
            g.ForceWeather(0);
            g.DebugGrantKit();
            yield return new WaitForSeconds(2f);
            var t = Trolley.I;
            if (t == null) { Debug.Log("[PB] push test: FAIL no trolley"); yield break; }
            for (int k = 0; k < 4; k++) g.SpawnCrate(t.transform.position + new Vector3(0, 1.4f + k * 0.7f, 0), 1f);
            yield return new WaitForSeconds(2.5f);
            Player.CamOverride = true;
            var cam = Camera.main;
            foreach (var (nm, off) in new[] { ("trolley-front", new Vector3(2.2f, 1.6f, 2.4f)), ("trolley-back", new Vector3(-2.4f, 1.9f, -2.2f)) })
            {
                var tp = t.transform.position;
                cam.transform.position = tp + t.transform.rotation * off; cam.transform.LookAt(tp + Vector3.up * 0.6f);
                yield return Snap(dir, nm);
            }
            Player.CamOverride = false;
            Debug.Log($"[PB] push test: load={t.Load} at {t.transform.position:F2}");
            bool ok = true;
            // Take hold: we step in behind the handle.
            pl.DebugTeleport(t.Handle - t.transform.forward * 0.6f + Vector3.down * t.Handle.y + Vector3.up * 0.1f, t.transform.eulerAngles.y);
            yield return new WaitForSeconds(0.3f);
            t.SetPusher(pl);
            yield return new WaitForSeconds(0.4f);
            float gap = Vector3.Distance(Vector3.ProjectOnPlane(t.transform.position - pl.transform.position, Vector3.up), Vector3.zero);
            Debug.Log($"[PB] push test: holding, trolley {gap:F2} m ahead (want {Trolley.HoldDist})");
            ok &= Mathf.Abs(gap - Trolley.HoldDist) < 0.15f;
            // Drive it, in turn: east into the shelter wall (x 6), north to the quay edge (z -6 by the slipway, x -20), and a free run.
            IEnumerator Drive(string name, Vector3 at, Vector3 way, float secs)
            {
                if (at != Vector3.zero) { t.SetPusher(null); yield return null; var rb = t.GetComponent<Rigidbody>(); rb.position = at - way * 0.0f; rb.rotation = Quaternion.LookRotation(way); rb.linearVelocity = Vector3.zero; t.transform.SetPositionAndRotation(at, Quaternion.LookRotation(way)); yield return new WaitForSeconds(0.3f); pl.DebugTeleport(at - way * Trolley.HoldDist + Vector3.up * 0.1f, Quaternion.LookRotation(way).eulerAngles.y); yield return new WaitForSeconds(0.2f); t.SetPusher(pl); yield return new WaitForSeconds(0.3f); }
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = way };
                float t0 = Time.time; var start = t.transform.position;
                while (Time.time - t0 < secs) yield return null;
                Player.BotInput = () => new Player.In();
                yield return new WaitForSeconds(0.3f);
                var tp = t.transform.position; var pp = pl.transform.position;
                Debug.Log($"[PB] push test {name}: trolley {start:F2} -> {tp:F2} (moved {Vector3.Distance(start, tp):F1}), player {pp:F2}, tilt={Vector3.Angle(t.transform.up, Vector3.up):0}, load={t.Load}, pushing={pl.Pushing}, lost={(g.HasTrolley.Value ? "no" : "YES")}");
                cam.transform.position = tp + new Vector3(-3.5f, 3f, -3.5f); Player.CamOverride = true; cam.transform.LookAt(tp);
                yield return Snap(dir, "push-" + name);
                Player.CamOverride = false;
            }
            yield return Drive("free", Vector3.zero, Vector3.left, 2.5f);
            var free = t.transform.position;
            yield return Drive("wall", new Vector3(2.5f, 0, -9f), Vector3.right, 3f);
            float wallX = t.transform.position.x;
            ok &= wallX < 6.1f - 0.6f + 0.15f; // the bed's front edge stops at the shelter wall
            yield return Drive("edge", new Vector3(-24f, 0, -9f), Vector3.forward, 3f);
            float edgeZ = t.transform.position.z;
            ok &= g.HasTrolley.Value && edgeZ + 0.72f < 0.3f; // stopped at the quay edge (z 0 here), not in the sea
            // Turn on the spot beside the wall: it may only swing where it fits.
            yield return Drive("turn", new Vector3(3.6f, 0, -9f), Vector3.forward, 0.01f);
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right * 0.2f };
            yield return new WaitForSeconds(1.5f);
            Player.BotInput = () => new Player.In();
            bool inWall = false;
            foreach (var c in Physics.OverlapBox(t.transform.position + t.transform.rotation * new Vector3(0, 0.68f, 0.02f), new Vector3(0.55f, 0.22f, 0.62f), t.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                inWall |= !c.transform.IsChildOf(t.transform) && c.GetComponentInParent<Player>() == null;
            Debug.Log($"[PB] push test turn: yaw {t.transform.eulerAngles.y:0} at {t.transform.position:F2}, overlapping={inWall}");
            ok &= !inWall;
            Debug.Log($"[PB] push test: free run moved to {free:F1}, wall stop x={wallX:F2}, edge stop z={edgeZ:F2}: {(ok ? "PASS" : "FAIL")}");
        }

        // The intro flythrough at a few moments (captions are IMGUI: not in batchmode shots), and the clock held meanwhile.
        IEnumerator IntroShots(string dir)
        {
            Directory.CreateDirectory(dir);
            while (!Intro.Playing) yield return null;
            foreach (var at in new[] { 2f, 9f, 14f, 19f, 24f, 28f, 32f, 37f, 41f })
            {
                while (Intro.Playing && Intro.I.T < at) yield return null;
                if (!Intro.Playing) break;
                Debug.Log($"[PB] intro shot at {at}s: dayT={PortGame.I?.DayT.Value:F1} cam={Camera.main.transform.position:F0}");
                yield return Snap(dir, $"intro-{at:00}");
            }
            while (Intro.Playing) yield return null;
            yield return new WaitForSeconds(2f);
            Debug.Log($"[PB] intro over: dayT={PortGame.I?.DayT.Value:F1} camOverride={Player.CamOverride}");
        }

        // Marlowe's drive-by (stopped off the jetty end, gloating), the banner plane and the fireworks.
        IEnumerator RivalShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(0.5f, World.PierTop + 0.1f, 30f), 0);
            Player.BotInput = () => new Player.In();
            // Speedboat: wait for the gloat (cycle second 14), shot from the jetty end, then a side view.
            float Cyc() => Mathf.Repeat(Water.T - Speedboat.Offset, Speedboat.Period);
            while (Water.T < Speedboat.Offset || Cyc() > 30f || Cyc() < 11.8f) yield return null;
            Player.CamOverride = true;
            var cam = Camera.main;
            foreach (var (at, nm) in new[] { (12.6f, "rival-splash"), (14.5f, "rival-gloat"), (16f, "rival-gloat-side") })
            {
                while (Cyc() < at) yield return null;
                var boat = Speedboat.I.transform.position;
                if (nm == "rival-gloat-side") { cam.transform.position = boat + new Vector3(9, 3, -3); cam.transform.LookAt(boat + Vector3.up * 1.5f); }
                else { cam.transform.position = new Vector3(0.5f, 2.6f, 27.5f); cam.transform.LookAt(boat + Vector3.up * 1.2f); }
                Debug.Log($"[PB] rival shot {nm}: bubble=\"{Speedboat.Bubble}\" boat={boat:F1}");
                yield return Snap(dir, nm);
            }
            Player.CamOverride = false;
            // Fireworks: set some off and look at Marlowe Port.
            Player.CamOverride = true;
            Fireworks.Show(Fireworks.Over);
            cam.transform.position = new Vector3(0, 12, 20); cam.transform.LookAt(Fireworks.Over + Vector3.up * 60);
            yield return new WaitForSeconds(1.2f); yield return Snap(dir, "rival-fireworks");
            yield return new WaitForSeconds(2.5f); yield return Snap(dir, "rival-fireworks-2");
            Player.CamOverride = false;
            // The banner plane's first pass (from second 150): from the yard, looking up as it goes over.
            while (Water.T < 150f + 19f) yield return null;
            Player.CamOverride = true;
            cam.transform.position = new Vector3(-10, 3, -10); cam.transform.LookAt(new Vector3(20, 40, -40));
            yield return Snap(dir, "rival-plane");
            yield return new WaitForSeconds(2.5f);
            cam.transform.LookAt(new Vector3(-30, 40, -40));
            yield return Snap(dir, "rival-plane-2");
            Player.CamOverride = false;
            Debug.Log("[PB] rival shots done");
        }

        IEnumerator WaitGame() { while (PortGame.I == null || Player.Local == null) yield return null; yield return new WaitForSeconds(1); }

        // Fall through the pier hole, swim to a floating crate, tow it, then climb the nearest ladder.
        IEnumerator SwimTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            var crate = PortGame.I.SpawnCrate(new Vector3(-5.2f, -1f, 9f), 1f);
            pl.DebugTeleport(new Vector3(-1.5f, 0.6f, 10.3f), -90);
            for (int i = 0; i < 40 && !pl.Swimming; i++) yield return new WaitForSeconds(0.1f);
            Debug.Log($"[PB] swim test: swimming={pl.Swimming} y={pl.transform.position.y:F2} surface={Water.Height(pl.transform.position):F2}");
            float t0 = Time.time;
            while (Time.time - t0 < 6 && !pl.Towing)
            {
                var to = crate.transform.position - pl.transform.position; to.y = 0;
                bool near = pl.FloatingCrateNear() != null;
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = near ? Vector3.zero : to.normalized, grab = near };
                yield return null;
            }
            Debug.Log($"[PB] swim test: towing={pl.Towing} after {Time.time - t0:F1}s");
            var shotDir = Arg("-pb-shots");
            if (shotDir != null)
            {
                // Tow towards open water for a moment so there is a clean swimming frame.
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = new Vector3(-0.6f, 0, 0.8f) };
                yield return new WaitForSeconds(2.5f);
                yield return Snap(shotDir, "swim-towing-crate");
            }
            var ladder = World.Ladders[0];
            t0 = Time.time;
            while (Time.time - t0 < 10 && pl.Swimming)
            {
                var to = ladder.Bottom - pl.transform.position; to.y = 0;
                bool near = pl.NearLadder() != null;
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = near ? Vector3.zero : to.normalized, grab = near && Time.frameCount % 20 == 0 };
                yield return null;
            }
            Player.BotInput = () => new Player.In();
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] swim test: out={!pl.Swimming} at {pl.transform.position:F1} crate at {crate.transform.position:F1} inSea={crate.InSea}");
            if (shotDir != null)
            {
                // Jump back in off the side and swim along the pier.
                pl.DebugTeleport(new Vector3(-6.5f, 0.6f, 12f), 0);
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.forward };
                yield return new WaitForSeconds(3f);
                yield return Snap(shotDir, "swim-open-water");
                // Side-on close-ups: swimming along (stroke), then still (treading water).
                Player.CamOverride = true;
                var cam = Camera.main;
                for (int k = 0; k < 2; k++)
                {
                    if (k == 1) { Player.BotInput = () => new Player.In(); yield return new WaitForSeconds(1.5f); }
                    else yield return new WaitForSeconds(0.5f);
                    var p = pl.transform.position + Vector3.up * SwimCamY;
                    cam.transform.position = p + pl.transform.right * 3.2f + Vector3.up * 0.6f; cam.transform.LookAt(p);
                    var an = pl.GetComponentInChildren<Animator>();
                    if (an != null && an.isHuman)
                    {
                        float s = Water.Height(pl.transform.position);
                        Debug.Log($"[PB] swim pose {(k == 0 ? "stroke" : "tread")}: head {an.GetBoneTransform(HumanBodyBones.Head).position.y - s:F2} chest {an.GetBoneTransform(HumanBodyBones.Chest).position.y - s:F2} hips {an.GetBoneTransform(HumanBodyBones.Hips).position.y - s:F2} feet(transform) {pl.transform.position.y - s:F2} (m above the surface)");
                    }
                    yield return Snap(shotDir, k == 0 ? "swim-side-stroke" : "swim-side-tread");
                }
                Player.CamOverride = false;
                Player.BotInput = () => new Player.In();
            }
        }
        const float SwimCamY = 1.2f;

        // Pile crates on the old pier end and watch it sag.
        IEnumerator PierTest()
        {
            yield return WaitGame();
            for (int i = 0; i < 16; i++) PortGame.I.SpawnCrate(new Vector3(-2f + (i % 4) * 1.2f, 0.7f + (i / 8) * 0.6f, 22.5f + ((i / 4) % 2) * 2.5f), 1f);
            for (int s = 0; s < 6; s++) { yield return new WaitForSeconds(1); Debug.Log($"[PB] pier test: sag={PortGame.I.PierSag.Value:F1} endRot={World.PierEnd.eulerAngles.x:F1}"); }
            Player.CamOverride = true;
            Camera.main.transform.position = new Vector3(14, 3, 24); Camera.main.transform.LookAt(new Vector3(0, 0, 24));
        }

        // Stage 2: earn it, end the day, build the quay, check the shed is up and tomorrow's boat is bigger.
        IEnumerator StageTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I;
            g.Money.Value = 1600;
            g.EndDayNow();
            yield return new WaitForSeconds(1f);
            g.BuyStageRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] stage test: stage={g.Stage.Value} money={g.Money.Value} next={g.NextStageName} ${g.NextStageCost} shed={GameObject.Find("Stage2") != null}");
            g.NextDayRpc();
            yield return new WaitForSeconds(8f);
            foreach (var b in Boat.All) Debug.Log($"[PB] stage test: day {g.DayNum.Value} boat cargo={b.CargoCount} (stage 1 would be {Mathf.RoundToInt((8 + (g.DayNum.Value - 1) * 2) * g.Today.Cargo)})");
            Player.CamOverride = true;
            Camera.main.transform.position = new Vector3(14, 9, -12); Camera.main.transform.LookAt(new Vector3(28, 0, -34));
            yield return new WaitForSeconds(1f);
            yield return Snap(dir, "stage2");
            Player.CamOverride = false;
        }

        // Co-op shove (LAN): the host holds 2 crates; the joiner walks up behind and shoves.
        IEnumerator ShoveHost()
        {
            yield return WaitGame();
            Player.Local.DebugGiveCrates(2);
            for (int i = 0; i < 40; i++) { yield return new WaitForSeconds(1f); Debug.Log($"[PB] shove host: carrying={Player.Local.Carrying} pos={Player.Local.transform.position:F1}"); }
        }

        IEnumerator ShoveTest()
        {
            yield return WaitGame();
            while (Player.All.Count < 2) yield return null;
            yield return new WaitForSeconds(4f);
            var me = Player.Local; Player host = null;
            foreach (var p in Player.All) if (p != me) host = p;
            var hp = host.transform.position;
            var from = hp - host.transform.forward * 1.2f;
            me.DebugTeleport(from, Quaternion.LookRotation(hp - from).eulerAngles.y);
            yield return new WaitForSeconds(0.6f);
            Debug.Log($"[PB] shove test: target found={me.DebugShove()} host carrying before={host.Carrying}");
            yield return new WaitForSeconds(2f);
            Debug.Log($"[PB] shove test: host carrying after={host.Carrying}");
        }

        IEnumerator KnockTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            pl.DebugGiveCrates(2);
            yield return new WaitForSeconds(0.5f);
            var p = pl.transform.position + pl.transform.forward * 4f + Vector3.up * 1.2f;
            var c = PortGame.I.SpawnCrate(p, 1f);
            c.GetComponent<Rigidbody>().linearVelocity = (pl.transform.position + Vector3.up - p).normalized * 11f;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] knock test: carrying after hit={pl.Carrying} (was 2)");
        }

        // Captains take turns: day 3 is Bjorn's (big catch, short stay). Start day 3, check the captain and the countdown.
        IEnumerator ContractTest()
        {
            yield return WaitGame();
            var g = PortGame.I;
            yield return new WaitForSeconds(1);
            g.DebugStartDay(3);
            yield return new WaitForSeconds(1);
            Debug.Log($"[PB] contract test: day={g.DayNum.Value} contract={g.Today.Captain} dock={g.Today.DockSeconds}");
            Boat b = null;
            for (int i = 0; i < 80 && (b = Boat.DockedAt[0]) == null; i++) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1);
            Debug.Log($"[PB] contract test: boat docked={b != null} secondsLeft={(b ? b.SecondsLeft.Value : -1)}");
            bool ok = g.Today.Captain == "BJORN" && b != null && b.SecondsLeft.Value > 0 && b.SecondsLeft.Value <= g.Today.DockSeconds;
            Debug.Log($"[PB] contract test: {(ok ? "PASS" : "FAIL")} (day 3 captain {g.Today.Captain}, dock {g.Today.DockSeconds} s)");
        }

        // Carried stacks must be visible from the real gameplay camera: carry 1, 3, 5 and count the crates the
        // camera can see (line of sight to any corner of each crate, the carrier's body counts as blocking).
        IEnumerator CarryTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            PortGame.I.ForceWeather(0);
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-12, 0.2f, -6), 30);
            bool allOk = true;
            foreach (var n in new[] { 1, 3, 5 })
            {
                while (pl.Carrying < n) { pl.DebugGiveCrates(n - pl.Carrying); yield return null; }
                yield return new WaitForSeconds(2f);
                var cam = Camera.main;
                int seen = 0;
                foreach (var c in Crate.All)
                {
                    if (!c.Attached.Value || c.transform.parent != pl.transform) continue;
                    // Visible = at least 3 of 7 points on the crate (centre, corners) in clear view.
                    int clear = 0; float h = Crate.Size * 0.45f;
                    foreach (var o in new[] { Vector3.zero, new Vector3(h, h, h), new Vector3(-h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h), new Vector3(h, -h, -h), new Vector3(-h, -h, -h) })
                    {
                        var p = c.transform.TransformPoint(o);
                        if (!Physics.Linecast(cam.transform.position, p, out var hit, ~0, QueryTriggerInteraction.Ignore) && BodyClear(cam.transform.position, p, pl)) clear++;
                    }
                    bool vis = clear >= 3;
                    // Rendered = the crate's renderers were actually drawn by a camera last frame (not just "would be in view").
                    var rs = c.GetComponentsInChildren<Renderer>();
                    // (isVisible is useless in batchmode; what broke in Manan's build was the visual flung away from the crate)
                    bool drawn = System.Array.TrueForAll(rs, r => !r.enabled || (r.gameObject.activeInHierarchy && (r.bounds.center - c.transform.position).sqrMagnitude < 1f));
                    var vis0 = c.transform.Find("vis");
                    Debug.Log($"[PB] carry test:   crate {c.NetworkObjectId} slot={c.Slot.Value} local={c.transform.localPosition:F2} world={c.transform.position:F2} scale={c.transform.lossyScale:F2} vis={(vis0 ? vis0.gameObject.activeInHierarchy + " " + vis0.localPosition.ToString("F2") : "none")} r0={(rs.Length > 0 ? rs[0].name + " en=" + rs[0].enabled + " b=" + rs[0].bounds.center.ToString("F2") + " sz=" + rs[0].bounds.size.ToString("F2") : "-")} clear={clear}");
                    if (vis && drawn) seen++;
                }
                Debug.Log($"[PB] carry test: carrying={pl.Carrying} visible={seen} {(seen == n ? "OK" : "FAIL")}");
                allOk &= seen == n;
                yield return Snap(dir, $"carry-{n}");
                if (n == 5)
                {
                    Player.CamOverride = true;
                    var side = pl.transform.position + pl.transform.right * 4.5f + Vector3.up * 2.4f;
                    Camera.main.transform.position = side; Camera.main.transform.LookAt(pl.transform.position + Vector3.up * 2.6f + pl.transform.forward * 0.7f);
                    yield return Snap(dir, "carry-5-side");
                    Player.CamOverride = false;
                }
            }
            Debug.Log($"[PB] carry test: {(allOk ? "PASS" : "FAIL")}");
        }

        // The worker model has no colliders: treat the body as a capsule (feet to 1.85 m, radius 0.3) between camera and point.
        static bool BodyClear(Vector3 a, Vector3 b, Player pl)
        {
            var p0 = pl.transform.position + Vector3.up * 0.3f; var p1 = pl.transform.position + Vector3.up * 1.5f;
            var headC = pl.transform.position + Vector3.up * 1.72f;
            for (int i = 0; i <= 40; i++)
            {
                var q = Vector3.Lerp(a, b, i / 40f);
                var axis = p1 - p0; float t = Mathf.Clamp01(Vector3.Dot(q - p0, axis) / axis.sqrMagnitude);
                if ((q - (p0 + axis * t)).sqrMagnitude < 0.32f * 0.32f) return false; // torso + shoulders
                if ((q - headC).sqrMagnitude < 0.19f * 0.19f) return false;            // head + hard hat
            }
            return true;
        }

        // Trailer frames (windowed, so the menu and HUD are in): fixed 30 fps game time, every frame saved as a JPEG.
        // Title flyover (berth, then the whole harbour), then a bot shift; the weather turns rough near the end.
        IEnumerator Film(string dir)
        {
            Directory.CreateDirectory(dir);
            int n = 0;
            IEnumerator Frame()
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"f_{n++:00000}.jpg"), tex.EncodeToJPG(90));
                Destroy(tex);
            }
            yield return new WaitForSecondsRealtime(3f);
            Time.captureFramerate = 30;
            foreach (var (from, secs) in new[] { (1f, 7f), (43f, 6f) })
                for (int f = 0; f < secs * 30; f++) { GameUI.TitleTime = from + f / 30f; yield return Frame(); }
            GameUI.TitleTime = -1;
            ui.Play();
            while (PortGame.I == null || Player.Local == null) yield return Frame();
            bot = new Bot(); Player.BotInput = bot.Tick;
            // 62 s of footage; standing around waiting for a boat is cut (time runs on, frames aren't kept).
            var last = Player.Local.transform.position;
            for (int kept = 0, f = 0; kept < 62 * 30 && f < 400 * 30; f++)
            {
                if (kept == 44 * 30) PortGame.I.ForceWeather(0.85f);
                var p = Player.Local.transform.position;
                bool idle = (p - last).sqrMagnitude < 0.0004f && Player.Local.Carrying == 0 && !Boat.All.Exists(b => b.State.Value == Boat.St.Docked);
                last = p;
                if (idle) { yield return null; continue; }
                kept++;
                yield return Frame();
            }
            Time.captureFramerate = 0;
            Debug.Log($"[PB] film done: {n} frames");
            Application.Quit();
        }

        // The front door: each title shot (camera only in batchmode; with the menu drawn when windowed), the logo texture,
        // then Join, Settings and (in a solo session) Pause.
        IEnumerator UiShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(3f);
            for (int i = 0; i < 4; i++)
            {
                GameUI.TitleTime = i * 14f + 5f;
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Snap(dir, $"title-{i}");
            }
            var rt = ui.Logo();
            var lc = GameObject.Find("LogoCam"); if (lc != null) lc.GetComponent<Camera>().Render();
            if (rt != null)
            {
                yield return new WaitForEndOfFrame();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, "logo.png"), tex.EncodeToPNG());
            }
            GameUI.TitleTime = 5f;
            ui.DebugScreen(1); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "join");
            ui.DebugOpenSettings(0); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "settings");
            ui.DebugScreen(0);
            GameUI.TitleTime = -1;
            ui.Play();
            yield return WaitGame();
            yield return new WaitForSecondsRealtime(3f);
            ui.DebugPause(); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "pause");
            Debug.Log("[PB] ui shots done");
        }

        // Fixed cameras over the harbour map (same every build, for A/B), with the frame rate at each.
        IEnumerator MapShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            PortGame.I.ForceWeather(0);
            yield return new WaitForSeconds(3f);
            Player.CamOverride = true;
            var cam = Camera.main;
            foreach (var (name, pos, look) in new[] {
                ("map-aerial", new Vector3(-40, 70, -90), new Vector3(30, 0, 90)),
                ("map-yard-top", new Vector3(15, 160, -44), new Vector3(15, 0, -43.9f)),
                ("map-berth", new Vector3(-14, 9, -24), new Vector3(10, 0, 20)),
                ("map-rival", new Vector3(0, 4, -6), new Vector3(0, 25, 600)),
                ("map-rival-zoom", new Vector3(0, 30, 250), new Vector3(0, 20, 620)),
                ("map-rival-sign", new Vector3(30, 100, 700), new Vector3(30, 100, 850)),
                ("map-mouth", new Vector3(30, 18, 120), new Vector3(190, 0, 300)),
                ("map-demo-angle", new Vector3(40, 17, 40), new Vector3(5, 0, 5)),
                ("map-back", new Vector3(-10, 10, -35), new Vector3(-10, 2, -75)),
                ("map-quay", new Vector3(-12, 3, 14), new Vector3(10, -0.5f, -2)),
                ("map-kit", new Vector3(2, 5, 6), new Vector3(14, 0, -8)),
                ("map-forklift", new Vector3(14.5f, 2.6f, -7.5f), new Vector3(19, 0.8f, -12)),
                ("map-stall", new Vector3(-12f, 3.2f, -106f), new Vector3(-6, 1.5f, -116)),
                ("map-market", new Vector3(10f, 7f, -98f), new Vector3(-6, 2f, -117)),
                ("map-to-stall", new Vector3(1, 2.6f, 6), new Vector3(-6, 1, -90)),
                ("map-gate", new Vector3(-6f, 3f, -70f), new Vector3(-6, 3, -100)),
                ("map-road", new Vector3(-30f, 6f, -84f), new Vector3(10, 0, -105)),
                ("map-store", new Vector3(-2f, 3f, 1f), new Vector3(-8.5f, 1.5f, -7.6f)),
                ("map-wall", new Vector3(80f, 12f, -10f), new Vector3(20, 0, -80)),
                ("map-shelter", new Vector3(5, 2.2f, 1.5f), new Vector3(9, 1.6f, -9)) })
            {
                cam.transform.position = pos; cam.transform.LookAt(look);
                float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount;
                yield return new WaitForSeconds(1.5f);
                Debug.Log($"[PB] map shot {name}: fps={(Time.frameCount - f0) / (Time.realtimeSinceStartup - t0):0}");
                yield return Snap(dir, name);
            }
            if (Trolley.I != null) Debug.Log($"[PB] trolley at {Trolley.I.transform.position:F1} (spawn {World.TrolleySpawn:F1})");
            Player.CamOverride = false;
        }

        // Same cameras on every area in day, dusk, rain, storm and night (before/after look checks). The first two
        // views also get a 6-frame burst 0.1 s apart with the camera drifting 1 cm a frame, to measure shimmer (aliased
        // or sparkling pixels flip on and off; a clean picture just slides).
        static readonly (string name, Vector3 pos, Vector3 look)[] Areas =
        {
            ("jetty", new Vector3(-12, 3, 14), new Vector3(10, -0.5f, -2)),
            ("berth", new Vector3(-14, 9, -24), new Vector3(10, 0, 20)),
            ("boat", new Vector3(1.5f, 4.5f, 9f), new Vector3(7.6f, 1.6f, 16f)),
            ("store", new Vector3(-2f, 3f, 1f), new Vector3(-8.5f, 1.5f, -7.6f)),
            ("yard", new Vector3(-40, 70, -90), new Vector3(30, 0, 90)),
            ("gate", new Vector3(-6f, 3f, -70f), new Vector3(-6, 3, -100)),
            ("road", new Vector3(-30f, 6f, -84f), new Vector3(10, 0, -105)),
            ("market", new Vector3(10f, 7f, -98f), new Vector3(-6, 2f, -117)),
            ("sea", new Vector3(30, 18, 120), new Vector3(190, 0, 300)),
            ("marlowe", new Vector3(0, 30, 250), new Vector3(0, 20, 620)),
            ("lamps", new Vector3(80f, 12f, -10f), new Vector3(20, 0, -80)),
            ("deck", new Vector3(3.4f, 2.5f, 15.5f), new Vector3(7.6f, 1.8f, 15.5f)),
            ("decktop", new Vector3(7.6f, 9f, 11f), new Vector3(7.6f, 1.6f, 16f)),
        };

        // Splashes where Kufu saw a soft white blob in the dusk jetty shot (pixel 358,817): a crate's, then a person's.
        IEnumerator FxShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I;
            g.ForceWeather(0);
            yield return new WaitForSeconds(2f);
            Player.CamOverride = true;
            var cam = Camera.main;
            cam.transform.position = Areas[0].pos; cam.transform.LookAt(Areas[0].look);
            cam.aspect = 16f / 9f;
            var ray = cam.ViewportPointToRay(new Vector3(358f / 1600, 1 - 817f / 900, 0));
            var at = ray.GetPoint((Water.Level - ray.origin.y) / ray.direction.y);
            Debug.Log($"[PB] fx shots: splash point {at:F1}");
            foreach (var (size, nm) in new[] { (0.7f, "crate"), (1.2f, "person") })
            {
                g.DayT.Value = 445f;
                yield return new WaitForSeconds(1.5f);
                Fx.Splash(at, size);
                foreach (var t in new[] { 0.15f, 0.45f, 0.8f }) { yield return new WaitForSeconds(t == 0.15f ? t : 0.3f); yield return Snap(dir, $"fx-{nm}-splash-{t:0.00}"); }
            }
            Player.CamOverride = false;
            Debug.Log("[PB] fx shots done");
            Application.Quit();
        }

        // Every live particle drawn in this frame, by emitter, with where it sits on screen (stray sprites in a shot).
        static void ParticlesInView(Camera cam, string shot)
        {
            foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps.particleCount == 0) continue;
                var parts = new ParticleSystem.Particle[ps.particleCount];
                int n = ps.GetParticles(parts), shown = 0;
                float nearest = float.MaxValue;
                var sb = new System.Text.StringBuilder();
                bool world = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;
                for (int i = 0; i < n; i++)
                {
                    var wp = world ? parts[i].position : ps.transform.TransformPoint(parts[i].position);
                    var sp = cam.WorldToViewportPoint(wp);
                    if (sp.z <= 0 || sp.x < 0 || sp.x > 1 || sp.y < 0 || sp.y > 1) continue;
                    nearest = Mathf.Min(nearest, Vector3.Distance(cam.transform.position, wp));
                    if (shown++ < 4) sb.Append($" ({sp.x * 1600:0},{(1 - sp.y) * 900:0}) at {wp:F1} size {parts[i].GetCurrentSize(ps):F2}");
                }
                if (shown > 0) Debug.Log($"[PB] particles in {shot}: {ps.name} (parent {ps.transform.parent?.name}) x{shown}, nearest {nearest:F1} m:{sb}");
            }
        }

        // Crates on a boat that sink into its hull (the pack meshes) by more than 3 cm.
        static void DeckCheck(string when)
        {
            foreach (var b in Boat.All)
            {
                var hull = b.GetComponentsInChildren<MeshCollider>();
                int aboard = 0, sunk = 0;
                foreach (var c in Crate.All)
                {
                    var l = b.transform.InverseTransformPoint(c.transform.position);
                    if (Mathf.Abs(l.x) > Boat.HalfW + 0.5f || Mathf.Abs(l.z) > Boat.HalfL + 1f || l.y < Boat.DeckY - 1f || l.y > Boat.DeckY + 4f) continue;
                    aboard++;
                    var cc = c.GetComponent<Collider>();
                    foreach (var h in hull)
                        if (Physics.ComputePenetration(cc, c.transform.position, c.transform.rotation, h, h.transform.position, h.transform.rotation, out _, out float d) && d > 0.03f) { sunk++; break; }
                }
                Debug.Log($"[PB] deck check ({when}): boat {b.State.Value} crates aboard={aboard} sunk into the hull={sunk}");
            }
        }

        IEnumerator AreaShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I;
            g.ForceWeather(0);
            DeckCheck("arriving");
            while (!Boat.All.Exists(b => b.State.Value == Boat.St.Docked)) yield return null;
            yield return new WaitForSeconds(2f);
            DeckCheck("docked");
            Player.CamOverride = true;
            var cam = Camera.main;
            foreach (var (cond, sev, dayT) in new[] { ("day", 0f, 72f), ("rain", 0.65f, 72f), ("storm", 1f, 72f), ("dusk", 0f, 445f), ("night", 0f, -1f) })
            {
                g.ForceWeather(sev);
                if (dayT < 0) g.EndDayNow(); else g.DayT.Value = dayT;
                while (Mathf.Abs(Storm.Level - sev) > 0.02f) { if (dayT >= 0) g.DayT.Value = dayT; yield return null; }
                yield return new WaitForSeconds(1f);
                foreach (var (name, pos, look) in Areas)
                {
                    if (dayT >= 0) g.DayT.Value = dayT;
                    cam.transform.position = pos; cam.transform.LookAt(look);
                    yield return new WaitForSeconds(1.2f);
                    yield return Snap(dir, $"{name}-{cond}");
                    ParticlesInView(cam, $"{name}-{cond}");
                    if (cond == "day" && (name == "jetty" || name == "berth" || name == "lamps"))
                        for (int k = 0; k < 6; k++) { cam.transform.position += cam.transform.right * 0.01f; yield return new WaitForSeconds(0.1f); yield return Snap(dir, $"{name}-burst{k}"); }
                }
            }
            Player.CamOverride = false;
            Debug.Log("[PB] area shots done");
            Application.Quit();
        }

        // The IMGUI screens (windowed runs only: batchmode draws no GUI): HUD with freshness rings, Tab shop, Stats, the
        // end-of-day card as it counts up, and Stats over it.
        IEnumerator UiScreens(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(1.2f, World.PierTop + 0.1f, 12f), 0);
            for (int i = 0; i < 4; i++) PortGame.I.SpawnCrate(pl.transform.position + new Vector3(-0.8f + i * 0.6f, 0.5f, 1.6f), 1f - i * 0.25f);
            yield return new WaitForSeconds(3f);
            yield return Snap(dir, "ui-hud");
            ui.DebugPanels(true, false); yield return new WaitForSecondsRealtime(0.8f); yield return Snap(dir, "ui-shop");
            ui.DebugPanels(false, true); yield return new WaitForSecondsRealtime(0.8f); yield return Snap(dir, "ui-stats");
            ui.DebugPanels(false, false);
            PortGame.I.EndDayNow();
            float shown = 0;
            foreach (var t in new[] { 0.4f, 1.5f, 6f }) { yield return new WaitForSecondsRealtime(t - shown); shown = t; yield return Snap(dir, $"ui-card-{t:0.0}"); }
            ui.DebugPanels(false, true); yield return new WaitForSecondsRealtime(0.8f); yield return Snap(dir, "ui-card-stats");
            Debug.Log("[PB] ui screens done");
            Application.Quit();
        }

        IEnumerator ForceWeatherSoon(float s) { yield return WaitGame(); PortGame.I.ForceWeather(s); }

        IEnumerator StormTest()
        {
            yield return WaitGame();
            for (int i = 0; i < 6; i++) PortGame.I.SpawnCrate(new Vector3(-1.5f + i * 0.7f, 0.6f, 14f), 1f);
            // Same camera at severity 0, 0.5 and 1 (boat docked, crates loose on the pier).
            while (!Boat.All.Exists(b => b.State.Value == Boat.St.Docked)) yield return null;
            var dir = Arg("-pb-shots");
            foreach (var sev in new[] { 0f, 0.5f, 1f })
            {
                PortGame.I.ForceWeather(sev);
                while (Mathf.Abs(Storm.Level - sev) > 0.02f) yield return null;
                yield return new WaitForSeconds(4f);
                Player.CamOverride = true;
                Camera.main.transform.position = new Vector3(-9f, 4.5f, 6f); Camera.main.transform.LookAt(new Vector3(4f, 0.5f, 17f));
                Debug.Log($"[PB] weather test: severity={PortGame.I.Severity.Value:F2} level={Storm.Level:F2} waves={Water.Storm:F2} rain={Storm.Rain:F2} gust={Storm.Gust:F2} inSea={PortGame.I.InSea.Value}");
                if (dir != null) yield return Snap(dir, $"weather-{sev:0.0}");
                Player.CamOverride = false;
            }
            PortGame.I.ForceWeather(-1);
        }

        // Forklift: get in, lift a loaded pallet, drive, corner hard.
        IEnumerator ForkliftTest()
        {
            yield return WaitGame();
            var fl = Instantiate(ui.ForkliftPrefab, new Vector3(20f, 0.4f, -10f), Quaternion.Euler(0, -90, 0)).GetComponent<Forklift>();
            fl.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(1.5f);
            var front = fl.transform.TransformPoint(new Vector3(0, 0, 1.45f));
            var pal = Instantiate(ui.PalletPrefab, new Vector3(front.x, 0.05f, front.z), fl.transform.rotation).GetComponent<Pallet>();
            pal.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 6; i++) PortGame.I.SpawnCrate(pal.transform.TransformPoint(new Vector3(-0.6f + (i % 3) * 0.6f, 0.6f, i < 3 ? -0.3f : 0.3f)), 1f);
            yield return new WaitForSeconds(1.5f);
            var pl = Player.Local;
            pl.DebugTeleport(fl.Seat + fl.transform.right * -1.2f + Vector3.down * 0.9f, 0);
            yield return new WaitForSeconds(0.3f);
            fl.EnterRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] forklift test: driver={(pl.Driving != null)} owner={fl.IsOwner} palletLoad={pal.transform.childCount}");
            Forklift.BotDrive = () => new Vector3(0, 0, 1);          // raise forks
            yield return new WaitForSeconds(0.6f);
            Forklift.BotDrive = () => Vector3.zero;
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"[PB] forklift test: lifted hasPallet={fl.HasPallet.Value} mast={fl.Mast.Value:F2}");
            Forklift.BotDrive = () => new Vector3(0, 1, 0);          // drive forward
            yield return new WaitForSeconds(2.5f);
            Debug.Log($"[PB] forklift test: drove to {fl.transform.position:F1} hasPallet={fl.HasPallet.Value}");
            Forklift.BotDrive = () => new Vector3(1, 1, 1);          // raise high + hard turn at speed
            float maxTilt = 0;
            for (float t = 0; t < 3f; t += Time.deltaTime) { maxTilt = Mathf.Max(maxTilt, Vector3.Angle(fl.transform.up, Vector3.up)); yield return null; }
            Forklift.BotDrive = () => Vector3.zero;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] forklift test: hard corner maxTilt={maxTilt:F0} hasPallet={fl.HasPallet.Value} crates loose={Crate.All.FindAll(c => !c.Attached.Value).Count}");
            fl.ExitRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] forklift test: exited driver={(pl.Driving != null)} at {pl.transform.position:F1}");
            Forklift.BotDrive = null;
        }

        IEnumerator StartOnDay(int n)
        {
            yield return WaitGame();
            PortGame.I.DebugStartDay(n);
            if (n >= 3) PortGame.I.DebugGrantKit();
            Debug.Log($"[PB] started on day {n}");
        }

        // Character close-ups: front, side, walking (side), carrying (front).
        IEnumerator CharShots(string dir)
        {
            Directory.CreateDirectory(dir);
            while (Player.Local == null) yield return null;
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            yield return new WaitForSeconds(1.5f);
            Player.CamOverride = true;
            var cam = Camera.main;
            void Look(Vector3 offset) { var t = pl.transform.position + Vector3.up * 1.1f; cam.transform.position = t + offset; cam.transform.LookAt(t); }
            Look(pl.transform.forward * 3.2f + Vector3.up * 0.4f); yield return Snap(dir, "char-front");
            Look(-pl.transform.right * 3.2f + Vector3.up * 0.4f); yield return Snap(dir, "char-side");
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right * 0.6f };
            for (int i = 0; i < 40; i++) { Look(Vector3.forward * -4f + Vector3.up * 0.5f); yield return null; }
            yield return Snap(dir, "char-walking-side");
            // Jog (normal speed) and sprint, side on, a few frames apart.
            foreach (var (nm, spr) in new[] { ("jog", false), ("sprint", true) })
            {
                pl.DebugTeleport(new Vector3(-40, 0.2f, -30), 90);
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right, sprint = spr };
                for (int i = 0; i < 50; i++) { Look(Vector3.forward * -4.5f + Vector3.up * 0.5f); yield return null; }
                for (int k = 0; k < 3; k++) { for (int i = 0; i < 7; i++) { Look(Vector3.forward * -4.5f + Vector3.up * 0.5f); yield return null; } yield return Snap(dir, $"char-{nm}-{k}"); }
            }
            Player.BotInput = () => new Player.In();
            pl.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            yield return new WaitForSeconds(0.5f);
            pl.DebugGiveCrates(1);
            yield return new WaitForSeconds(1f);
            Look(pl.transform.forward * 3.4f - pl.transform.right * 1.6f + Vector3.up * 0.3f); yield return Snap(dir, "char-carry1");
            pl.DebugGiveCrates(2);
            yield return new WaitForSeconds(1f);
            Look(pl.transform.forward * 4f + Vector3.up * 1.2f); yield return Snap(dir, "char-carrying-front");
            Look(-pl.transform.right * 3.6f + Vector3.up * 0.6f); yield return Snap(dir, "char-carrying-side");
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right * 0.8f };
            for (int i = 0; i < 40; i++) { Look(Vector3.forward * -4.5f + Vector3.up * 0.8f); yield return null; }
            yield return Snap(dir, "char-carrying-walk");
            for (int i = 0; i < 10; i++) { cam.transform.position = pl.transform.position - pl.transform.forward * 5f + Vector3.up * 2.6f + pl.transform.right * 0.9f; cam.transform.LookAt(pl.transform.position + Vector3.up * 1.4f + pl.transform.forward * 2f); yield return null; }
            yield return Snap(dir, "char-carrying-back");
            Player.BotInput = () => new Player.In();
            // Falling in off the quay: the dunk.
            pl.DebugTeleport(new Vector3(-20, 0.2f, -5.2f), 0);
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.forward };
            float t0 = Time.time, giveUp = Time.time + 15f; int shot = 0;
            while (Time.time - t0 < 4f && Time.time < giveUp)
            {
                var c = pl.transform.position; c.y = Water.Level;
                cam.transform.position = c + new Vector3(-5f, 2.2f, 1.5f); cam.transform.LookAt(c + Vector3.up * 0.3f);
                if (pl.Swimming && shot < 5 && Time.time > t0 + 0.2f * shot) { yield return Snap(dir, $"char-dunk-{shot}"); shot++; if (shot == 1) Player.BotInput = () => new Player.In(); continue; }
                if (!pl.Swimming) t0 = Time.time;
                yield return null;
            }
            Player.BotInput = () => new Player.In();
            Player.CamOverride = false;
        }

        // Settings really reach the renderer, camera, audio and input; the player's own values are put back afterwards.
        IEnumerator SettingsTest()
        {
            yield return WaitGame();
            var urp = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            float scale = Settings.RenderScale, fov = Settings.Fov, master = Settings.Master; int shadows = Settings.Shadows; var grab = Settings.KeyFor("grab");
            Settings.RenderScale = 0.5f; Settings.Shadows = 0; Settings.Fov = 80; Settings.Master = 0.3f; Settings.Bind("grab", UnityEngine.InputSystem.Key.G);
            ui.ApplySettings();
            yield return null;
            Debug.Log($"[PB] settings test: renderScale={urp.renderScale:F2} sunShadows={System.Array.Find(FindObjectsByType<Light>(FindObjectsSortMode.None), l => l.type == LightType.Directional)?.shadows} fov={Camera.main.fieldOfView:F0} volume={AudioListener.volume:F2} grabKey={Settings.KeyFor("grab")}");
            Settings.RenderScale = scale; Settings.Shadows = shadows; Settings.Fov = fov; Settings.Master = master; Settings.Bind("grab", grab);
            ui.ApplySettings();
            yield return null;
            Debug.Log($"[PB] settings test: restored renderScale={urp.renderScale:F2} fov={Camera.main.fieldOfView:F0} volume={AudioListener.volume:F2} grabKey={Settings.KeyFor("grab")}");
        }

        // Close-ups of the dock crates: fresh, spoiled, damaged, and the lidded empties by the shed.
        IEnumerator CrateShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            Player.Local.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            var fresh = PortGame.I.SpawnCrate(new Vector3(-2.2f, 0.6f, -1f), 1f);
            var spoiled = PortGame.I.SpawnCrate(new Vector3(-1.2f, 0.6f, -1.4f), 0.15f);
            var damaged = PortGame.I.SpawnCrate(new Vector3(-0.2f, 0.6f, -1f), 1f);
            damaged.Damage.Value = 2;
            yield return new WaitForSeconds(2f);
            Player.CamOverride = true;
            var cam = Camera.main;
            void Look(Vector3 at, Vector3 offset) { cam.transform.position = at + offset; cam.transform.LookAt(at); }
            Look(fresh.transform.position, new Vector3(0.3f, 1.1f, -1.3f)); yield return Snap(dir, "crate-fresh");
            Look(spoiled.transform.position + Vector3.left * 0.5f, new Vector3(0.2f, 1.6f, -2.4f)); yield return Snap(dir, "crate-row-fresh-spoiled-damaged");
            Look(new Vector3(12.7f, 0.9f, -12.8f), new Vector3(2.2f, 0.9f, 2.4f)); yield return Snap(dir, "crate-empties-by-shed");
            // A gull sitting on a post (P5).
            Transform gull = null;
            for (float t = 0; t < 40 && (gull = Gull.AnyPerched()) == null; t += 0.5f) yield return new WaitForSeconds(0.5f);
            if (gull != null) { Look(gull.position, new Vector3(1.2f, 0.5f, -1.6f)); yield return Snap(dir, "gull-perched"); }
            Player.CamOverride = false;
        }

        // Character editor: open it, show a run of looks; each preview render saved (works headless), the whole screen
        // too (the IMGUI sheet only draws in a window).
        IEnumerator LookShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(3f);
            var ui = GameUI.I;
            ui.OpenCharacter();
            var dice = new System.Random(7);
            for (int i = 0; i < 10; i++)
            {
                ui.DebugLook(i < 4 ? Look.Default(i) : Look.Random(dice));
                yield return new WaitForSecondsRealtime(0.6f);
                yield return new WaitForEndOfFrame();
                SaveRT(ui.RenderPreview(), Path.Combine(dir, $"look-{i}.png"));
                if (!Application.isBatchMode && i < 3) yield return Snap(dir, $"editor-{i}");
            }
            Debug.Log("[PB] lookshots done");
        }

        static void SaveRT(RenderTexture rt, string path)
        {
            if (rt == null) return;
            var flat = RenderTexture.GetTemporary(rt.width, rt.height, 0); // resolve the MSAA target first
            Graphics.Blit(rt, flat);
            var old = RenderTexture.active; RenderTexture.active = flat;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = old; RenderTexture.ReleaseTemporary(flat);
            File.WriteAllBytes(path, t.EncodeToPNG()); Destroy(t);
        }

        // In the water by the first ladder, grab it and photograph the climb out from the side.
        IEnumerator LadderShot(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var pl = Player.Local; var l = World.Ladders[0];
            var out_ = l.Top - l.Bottom; out_.y = 0; out_.Normalize();
            pl.DebugTeleport(l.Bottom - out_ * 1.2f + Vector3.up * 0.5f, Quaternion.LookRotation(out_).eulerAngles.y);
            for (int i = 0; i < 60 && !pl.Swimming; i++) yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(1.5f);
            float t0 = Time.time;
            while (Time.time - t0 < 8 && !pl.Climbing)
            {
                var to = l.Bottom - pl.transform.position; to.y = 0;
                bool near = pl.NearLadder() != null;
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = near ? Vector3.zero : to.normalized, grab = near && Time.frameCount % 10 == 0 };
                yield return null;
            }
            Player.BotInput = () => new Player.In();
            Player.CamOverride = true;
            var cam = Camera.main; var side = Vector3.Cross(Vector3.up, out_);
            for (int k = 0; k < 3; k++)
            {
                yield return new WaitForSeconds(0.18f);
                var p = pl.transform.position + Vector3.up * 1f;
                cam.transform.position = p + side * 3.2f - out_ * 1.6f + Vector3.up * 0.3f; cam.transform.LookAt(p);
                yield return Snap(dir, "ladder-" + k);
            }
            Player.CamOverride = false;
            Debug.Log($"[PB] ladder shot: climbing={pl.Climbing} swimming={pl.Swimming}");
        }

        // Co-op look sync test: this process's player wears a random look (not saved); every peer logs what it sees.
        IEnumerator RandomLook()
        {
            while (Player.Local == null) yield return null;
            yield return new WaitForSeconds(1f);
            var l = Look.Random(new System.Random(System.Diagnostics.Process.GetCurrentProcess().Id)).Clamped();
            Player.Local.SetLook(l);
            Debug.Log($"[PB] look sent p{Player.Local.OwnerClientId} {l}");
        }

        IEnumerator Snap(string dir, string name)
        {
            yield return new WaitForEndOfFrame();
            Capture(Path.Combine(dir, name + ".png"));
            yield return null;
            Debug.Log("[PB] snap " + name);
        }

        IEnumerator WobbleTest()
        {
            while (PortGame.I == null || Player.Local == null) yield return null;
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            pl.DebugGiveCrates(5);
            yield return new WaitForSeconds(1);
            string[] phase = { "walk +x", "stop", "sprint +x", "sprint reverse -x", "sprint zigzag", "stop" };
            for (int p = 0; p < phase.Length; p++)
            {
                float peak = 0, t0 = Time.time;
                while (Time.time - t0 < 1.6f)
                {
                    int ph = p;
                    Player.BotInput = () => new Player.In
                    {
                        hasWorldMove = ph != 1 && ph != 5,
                        worldMove = ph == 3 ? Vector3.left : ph == 4 ? (Mathf.Sin(Time.time * 4) > 0 ? new Vector3(1, 0, 1).normalized : new Vector3(1, 0, -1).normalized) : Vector3.right,
                        sprint = ph >= 2,
                    };
                    peak = Mathf.Max(peak, pl.Teeter);
                    yield return null;
                }
                Debug.Log($"[PB] wobble {phase[p]}: carrying={pl.Carrying} peak lean={peak:F2}");
            }
            Player.BotInput = () => new Player.In();
        }

        IEnumerator TrolleyTest()
        {
            while (PortGame.I == null || Player.Local == null) yield return null;
            yield return new WaitForSeconds(1);
            var g = PortGame.I;
            var t = Instantiate(ui.TrolleyPrefab, new Vector3(-12, 0.4f, -5), Quaternion.Euler(0, 90, 0)).GetComponent<Trolley>();
            t.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(1);
            for (int k = 0; k < 3; k++) g.SpawnCrate(t.transform.position + new Vector3(0, 1.2f + k * 0.7f, 0), 1f);
            yield return new WaitForSeconds(2.5f);
            Debug.Log($"[PB] trolley test: load={t.transform.GetComponentsInChildren<Crate>().Length} at {t.transform.position:F2}");
            // Push: stand behind it, walk +x for 3 s.
            Player.Local.DebugTeleport(t.transform.position - t.transform.forward * 1.4f, 90);
            yield return new WaitForSeconds(0.3f);
            t.SetPusher(Player.Local);
            var start = t.transform.position;
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right };
            yield return new WaitForSeconds(3f);
            Player.BotInput = () => new Player.In();
            Debug.Log($"[PB] trolley test: pushed {Vector3.Distance(start, t.transform.position):F1} m, load={t.transform.GetComponentsInChildren<Crate>().Length}, tilt={Vector3.Angle(t.transform.up, Vector3.up):0}");
            // Let go at the top of the slipway, facing downhill.
            t.SetPusher(null);
            var rb = t.GetComponent<Rigidbody>();
            rb.position = new Vector3(-10, 0.45f, 0.6f); rb.rotation = Quaternion.identity; rb.linearVelocity = Vector3.zero;
            Player.Local.DebugTeleport(new Vector3(-10, 0.3f, -3f), 0);
            for (int k = 0; k < 12; k++)
            {
                yield return new WaitForSeconds(0.5f);
                if (t == null || !t.IsSpawned) break;
                Debug.Log($"[PB] trolley test: rolling at {t.transform.position:F2} v={rb.linearVelocity.magnitude:F1}");
            }
            Debug.Log($"[PB] trolley test end: trolley spawned={t != null && t.IsSpawned} inSea={g.InSea.Value}"); // (spawned straight from the prefab: HasTrolley isn't set)
        }

        IEnumerator ForceNight(float after)
        {
            yield return new WaitForSeconds(after);
            var g = PortGame.I;
            if (g != null && g.IsServer) g.EndDayNow();
            if (!Has("-pb-shop") || g == null || !g.IsServer) yield break;
            yield return new WaitForSeconds(3);
            g.Money.Value = Mathf.Max(g.Money.Value, PortGame.TrolleyPrice);
            g.BuyTrolleyRpc();
            yield return new WaitForSeconds(3);
            g.NextDayRpc();
        }

        void Update()
        {
            if (quitAt > 0 && Time.realtimeSinceStartup > quitAt) { Debug.Log("[PB] auto quit"); Application.Quit(); }
            if (Time.realtimeSinceStartup > nextLog)
            {
                nextLog = Time.realtimeSinceStartup + 5;
                var g = PortGame.I; var nm = NetworkManager.Singleton;
                var pl = Player.Local;
                string boats = string.Join(",", Boat.All.ConvertAll(b => $"{b.State.Value}@{b.Berth.Value}"));
                foreach (var c in Crate.All)
                    if (c.transform.position.y > 2.5f && (c.transform.parent == null || c.transform.parent.GetComponent<Player>() == null))
                        Debug.Log($"[PB] high crate {c.NetworkObjectId} at {c.transform.position:F1} parent={(c.transform.parent ? c.transform.parent.name : "-")} attached={c.Attached.Value}");
                Debug.Log($"[PB] t={Time.realtimeSinceStartup:0} fps={1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0} listening={nm.IsListening} server={nm.IsServer} clients={(nm.IsServer ? nm.ConnectedClientsIds.Count : -1)} " +
                          $"players={Player.All.Count} phase={g?.Phase.Value} day={g?.DayNum.Value} dayT={g?.DayT.Value:0} money={g?.Money.Value} sold={g?.Sold.Value} sea={g?.InSea.Value} left={g?.LeftAboard.Value} " +
                          $"crates={Crate.All.Count} boats=[{boats}] me={(pl ? pl.transform.position.ToString("F1") : "-")} carrying={(pl ? pl.Carrying : 0)} bot={bot?.State} " +
                          $"objs={FindObjectsByType<Transform>(FindObjectsSortMode.None).Length} ps={FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length} audio={FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length} water={Water.T:0}");
            }
        }

        // Batchmode has no screen: render the main camera into a texture instead.
        static void Capture(string path)
        {
            if (!Application.isBatchMode) { ScreenCapture.CaptureScreenshot(path); return; }
            var cam = Camera.main;
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            var rt = new RenderTexture(1600, 900, 24) { antiAliasing = urp != null ? Mathf.Max(1, urp.msaaSampleCount) : 1 }; // as the game window draws it
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(rt);
        }

        IEnumerator Shots(string dir, float every)
        {
            Directory.CreateDirectory(dir);
            int i = 0;
            while (true)
            {
                yield return new WaitForSecondsRealtime(every);
                yield return new WaitForEndOfFrame();
                var path = Path.Combine(dir, $"shot_{i++:000}.png");
                Capture(path);
                Debug.Log("[PB] shot " + path);
            }
        }
    }

    // Scripted player for headless tests: fetch crates (pier, then boat), stack 3, carry to the table, drop to sell.
    public class Bot
    {
        public string State = "seek";
        readonly List<Vector3> path = new();
        Crate target;
        float pulse, stuckT, lastProgress;
        Vector3 lastPos;
        int trips;

        static readonly Vector3 QuayMid = new(1.2f, 0, -1.2f), PierB = new(1.2f, 0, 16f), Gang = new(3.4f, 0, 16f), Deck = new(6.2f, 0, 16f), TableApproach = World.BuyerPos + new Vector3(0, 0, 2.4f);

        static bool OnBoat(Vector3 p) => p.x > 4.3f;
        static bool OnPier(Vector3 p) => p.z > 0.3f && !OnBoat(p);

        void Route(Vector3 from, Vector3 goal)
        {
            path.Clear();
            if (OnBoat(from) && !OnBoat(goal)) { path.Add(Gang); path.Add(PierB); }
            if ((OnPier(from) || OnBoat(from)) && !OnPier(goal) && !OnBoat(goal)) { path.Add(new Vector3(1.2f, 0, Mathf.Min(from.z, 16f))); path.Add(QuayMid); }
            if (!OnPier(from) && !OnBoat(from) && (OnPier(goal) || OnBoat(goal))) path.Add(QuayMid);
            if (OnBoat(goal) && !OnBoat(from)) { path.Add(PierB); path.Add(Gang); path.Add(Deck); }
            else if (OnPier(goal)) path.Add(new Vector3(1.2f, 0, Mathf.Clamp(goal.z, 0.5f, 22f)));
            path.Add(goal);
        }

        Crate PickCrate(Vector3 me)
        {
            Crate best = null; float bd = 1e9f;
            foreach (var c in Crate.All)
            {
                var p = c.transform.position;
                if (c.Attached.Value || c.InSea || p.y < Water.Level + 0.6f ) continue;
                var dock0 = Boat.DockedAt[0];
                if (p.x < -4.3f) continue; // the bot only works the right-hand berth
                if (OnBoat(p) && (dock0 == null || Mathf.Abs(dock0.transform.InverseTransformPoint(p).x) > Boat.HalfW - 0.4f)) continue;
                float d = Vector3.Distance(me, p) + (OnBoat(p) ? 4f : 0f);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        public Player.In Tick()
        {
            var i = new Player.In();
            var pl = Player.Local;
            if (pl == null || PortGame.I == null || PortGame.I.Phase.Value != PortGame.Ph.Day) return i;
            var me = pl.transform.position;
            pulse -= Time.deltaTime;
            if (pl.Swimming)
            {
                // Overboard: paddle to the nearest ladder and climb out.
                World.Ladder best = default; float bd = 1e9f;
                foreach (var l in World.Ladders) { float d = (l.Bottom - me).sqrMagnitude; if (d < bd) { bd = d; best = l; } }
                var to = best.Bottom - me; to.y = 0;
                i.hasWorldMove = true; i.worldMove = to.normalized;
                if (pl.NearLadder() != null && pulse <= 0) { i.grab = true; pulse = 1f; path.Clear(); target = null; }
                return i;
            }

            // Unstick: if we haven't moved for a while, re-route.
            if ((me - lastPos).sqrMagnitude > 0.04f) { lastPos = me; lastProgress = Time.time; }
            else if (Time.time - lastProgress > 4f) { path.Clear(); target = null; lastProgress = Time.time; i.jump = true; }

            if (State == "seek")
            {
                if (target == null || !target.IsSpawned || target.Attached.Value || target.InSea)
                {
                    target = PickCrate(me);
                    if (target == null) return pl.Carrying > 0 ? GoSell(i, me) : i;
                    Route(me, target.transform.position);
                }
                var tp = target.transform.position;
                var flat = new Vector3(tp.x - me.x, 0, tp.z - me.z);
                if (path.Count <= 1 && flat.magnitude < 1.3f)
                {
                    i.hasWorldMove = true; i.worldMove = flat.normalized * 0.15f;
                    if (pulse <= 0 && Vector3.Angle(pl.transform.forward, flat) < 30f) { i.grab = true; pulse = 0.4f; }
                    if (pl.FindCrate() == null && pulse <= 0) { i.worldMove = flat.normalized; }
                }
                else Follow(ref i, me, tp);
                if (pl.Carrying >= 3 || (pl.Carrying > 0 && target != null && target.Attached.Value && PickNear(me) == null)) { State = "sell"; path.Clear(); }
                if (target != null && target.Attached.Value) target = null;
                return i;
            }
            return GoSell(i, me);
        }

        Crate PickNear(Vector3 me)
        {
            var c = PickCrate(me);
            return c != null && Vector3.Distance(c.transform.position, me) < 5f ? c : null;
        }

        Player.In GoSell(Player.In i, Vector3 me)
        {
            var pl = Player.Local;
            State = "sell";
            if (pl.Carrying == 0) { State = "seek"; path.Clear(); trips++; return i; }
            if (path.Count == 0) Route(me, TableApproach);
            var flat = new Vector3(TableApproach.x - me.x, 0, TableApproach.z - me.z);
            if (path.Count <= 1 && flat.magnitude < 0.45f)
            {
                i.hasWorldMove = true; i.worldMove = new Vector3(0, 0, -0.5f);
                if (pulse <= 0 && !GameUI.BuyerOpen) { i.grab = true; pulse = 1f; }
                return i;
            }
            Follow(ref i, me, TableApproach);
            return i;
        }

        void Follow(ref Player.In i, Vector3 me, Vector3 goal)
        {
            while (path.Count > 0)
            {
                var w = path[0];
                if (new Vector2(w.x - me.x, w.z - me.z).magnitude < 0.6f && path.Count > 1) path.RemoveAt(0);
                else break;
            }
            var to = path.Count > 0 ? path[0] : goal;
            var d = new Vector3(to.x - me.x, 0, to.z - me.z);
            i.hasWorldMove = true;
            i.worldMove = d.magnitude > 0.1f ? d.normalized : Vector3.zero;
            // Keep the camera behind the bot so screenshots show what it's doing.
            i.look.x = Mathf.DeltaAngle(Player.Local.Yaw, Player.Local.transform.eulerAngles.y) * 0.05f;
        }
    }
}
