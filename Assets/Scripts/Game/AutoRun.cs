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
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), x => x.StartsWith("-pb-"))) return;
            Active = true;
            new GameObject("AutoRun").AddComponent<AutoRun>().ui = ui;
        }

        public static bool Active;
        GameUI ui;
        float quitAt = -1, nextLog;
        Bot bot;

        IEnumerator Start()
        {
            if (float.TryParse(Arg("-pb-quit"), out var q)) quitAt = q;
            if (float.TryParse(Arg("-pb-timescale"), out var ts)) Time.timeScale = ts;
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
            if (dir != null) StartCoroutine(Shots(dir, float.TryParse(Arg("-pb-shots", 2), out var iv) ? iv : 5f));
            if (float.TryParse(Arg("-pb-night"), out var n)) StartCoroutine(ForceNight(n));
            if (Has("-pb-trolleytest")) StartCoroutine(TrolleyTest());
            if (Has("-pb-wobbletest")) StartCoroutine(WobbleTest());
            if (Arg("-pb-charshots") != null) StartCoroutine(CharShots(Arg("-pb-charshots")));
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
            Player.BotInput = () => new Player.In();
            pl.DebugGiveCrates(3);
            yield return new WaitForSeconds(1f);
            Look(pl.transform.forward * 4f + Vector3.up * 1.2f); yield return Snap(dir, "char-carrying-front");
            Player.CamOverride = false;
        }

        IEnumerator Snap(string dir, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
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
            Debug.Log($"[PB] trolley test end: hasTrolley={g.HasTrolley.Value} inSea={g.InSea.Value}");
        }

        IEnumerator ForceNight(float after)
        {
            yield return new WaitForSeconds(after);
            var g = PortGame.I;
            if (g != null && g.IsServer) g.EndDayNow();
            if (!Has("-pb-shop")) yield break;
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
                string boats = string.Join(",", Boat.All.ConvertAll(b => b.State.Value.ToString()));
                foreach (var c in Crate.All)
                    if (c.transform.position.y > 2.5f && (c.transform.parent == null || c.transform.parent.GetComponent<Player>() == null))
                        Debug.Log($"[PB] high crate {c.NetworkObjectId} at {c.transform.position:F1} parent={(c.transform.parent ? c.transform.parent.name : "-")} attached={c.Attached.Value}");
                Debug.Log($"[PB] t={Time.realtimeSinceStartup:0} fps={1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0} listening={nm.IsListening} server={nm.IsServer} clients={(nm.IsServer ? nm.ConnectedClientsIds.Count : -1)} " +
                          $"players={Player.All.Count} phase={g?.Phase.Value} day={g?.DayNum.Value} dayT={g?.DayT.Value:0} money={g?.Money.Value} sold={g?.Sold.Value} sea={g?.InSea.Value} left={g?.LeftAboard.Value} " +
                          $"crates={Crate.All.Count} boats=[{boats}] me={(pl ? pl.transform.position.ToString("F1") : "-")} carrying={(pl ? pl.Carrying : 0)} bot={bot?.State}");
            }
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
                if (Application.isBatchMode)
                {
                    var cam = Camera.main;
                    var rt = new RenderTexture(1600, 900, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Destroy(tex); rt.Release();
                }
                else ScreenCapture.CaptureScreenshot(path);
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

        static readonly Vector3 QuayMid = new(1.2f, 0, -1.2f), PierB = new(1.2f, 0, 16f), Gang = new(3.4f, 0, 16f), Deck = new(6.2f, 0, 16f), TableApproach = new(-4f, 0, -2.9f);

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
                if (OnBoat(p) && (Boat.Docked == null || Mathf.Abs(Boat.Docked.transform.InverseTransformPoint(p).x) > Boat.HalfW - 0.4f)) continue;
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
