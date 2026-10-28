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
            new GameObject("AutoRun").AddComponent<AutoRun>().ui = ui;
        }

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
        }

        IEnumerator ForceNight(float after)
        {
            yield return new WaitForSeconds(after);
            var g = PortGame.I;
            if (g != null && g.IsServer) { g.DayT.Value = PortGame.DayLength + 1; foreach (var b in Boat.All.ToArray()) { g.BoatGone(); b.NetworkObject.Despawn(); } }
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

        static readonly Vector3 QuayMid = new(1.2f, 0, -1.2f), PierB = new(1.2f, 0, 16f), Gang = new(3.4f, 0, 16f), Deck = new(6.2f, 0, 16f), TableApproach = new(-4f, 0, -2.25f);

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
                if (c.Attached.Value || c.InSea || p.y < Water.Level + 0.6f || World.SellZone.Contains(p)) continue;
                if (OnBoat(p) && Boat.Docked == null) continue;
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
            if (path.Count <= 1 && flat.magnitude < 0.5f)
            {
                i.hasWorldMove = true; i.worldMove = new Vector3(0, 0, -0.3f);
                if (pulse <= 0)
                {
                    // Every third trip, show off: throw the top crate onto the table from here.
                    if (trips % 3 == 2 && pl.Carrying == 1) i.throwP = false;
                    i.drop = true; pulse = 0.5f;
                }
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
