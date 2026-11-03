using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace PortBoss.M1
{
    // Map B "Crescent Bay" tests (Map.cs):
    //   -pb-maptour            the seabed sheet and the axes, then Biscuit round the island (16 legs, ~12 km, checked
    //                          against the seabed for >= 3 m of water) and back to her berth, then a walk from the office
    //                          round the 7 town shops and out to the pier ladder: no falls through, no stuck spots
    //   -pb-mapshots <dir>     the map from the sea and from town in the 6 lighting states, and the fps/draw calls (day, night, storm) at
    //                          5 spots (fps.txt); -pb-mapfilm <dir>: a 60 s flight round the island and down onto the town
    public partial class Bot
    {
        int mapFails;
        void MapCheck(bool ok, string what) { Debug.Log($"[PB] map: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) mapFails++; }

        IEnumerator MapTests()
        {
            if (Arg("-pb-maptour") != null) return MapTour();
            if (Arg("-pb-mapshots") != null) { shots = Arg("-pb-mapshots"); Directory.CreateDirectory(shots); return MapShots(); }
            if (Arg("-pb-mapfilm") != null) return MapFilm(Arg("-pb-mapfilm"));
            if (Arg("-pb-mapview") != null) { shots = Arg("-pb-mapview"); Directory.CreateDirectory(shots); return MapView(Arg("-pb-views")); }
            return null;
        }

        // the boat route: the bay, out of its mouth, north about, outside the Voss no-dock buoy line (1460,500 .. 1680,-50 .. 1480,-650), round the south horn, home
        static readonly Vector2[] Route = {
            new(-252, 221), new(-1028, 273), new(-1287, 738), new(-770, 1437), new(136, 1515), new(861, 1204), new(1352, 583),
            new(1560, 560), new(1780, -50), new(1650, -480), new(1430, -814), new(783, -1384), new(-123, -1694), new(-1028, -1332), new(-1598, -918),
            new(-1184, -452), new(-614, -348), new(-300, -280), new(-150, -60), new(-40, -5) };

        IEnumerator MapTour()
        {
            Ctl.Bot = true; Sleep.Off = true; Sky.ForceWx = 1;
            yield return new WaitForSeconds(0.5f);
            MapCheck(Map.On && Places.All.Count >= 24, $"Crescent Bay is loaded ({Places.All.Count} anchors)");
            // the seabed sheet: 7 spots against the PNG read in Python (bilinear, 16 bit = 6 mm a step)
            var want = new (float x, float z, float h)[] { (-1740, 770, -19.42f), (-340, -150, -1.60f), (600, 0, 215.57f), (400, 800, 158.61f), (153, -20, 10.70f), (0, -1300, -7.58f), (-1200, 1800, -37.68f) };
            foreach (var w in want)
            {
                float h = Seabed.Height(w.x, w.z), ground = float.NaN;
                if (w.h > 0 && Physics.Raycast(new Vector3(w.x, 400, w.z), Vector3.down, out var hit, 500, ~0, QueryTriggerInteraction.Ignore)) ground = hit.point.y;
                MapCheck(Mathf.Abs(h - w.h) < 0.05f && (float.IsNaN(ground) || Mathf.Abs(ground - h) < 2.5f),
                    $"seabed at ({w.x}, {w.z}): {h:F2} m (want {w.h:F2}){(float.IsNaN(ground) ? "" : $", the terrain mesh there {ground:F2}")}");
            }
            var N = Util.Find(Map.Root, "socket_north"); var E = Util.Find(Map.Root, "socket_east");
            MapCheck(N != null && E != null && Vector3.Distance(N.position, new Vector3(0, 0, 500)) < 0.1f && Vector3.Distance(E.position, new Vector3(500, 0, 0)) < 0.1f,
                $"the map's axes: north socket {N?.position}, east socket {E?.position}");
            MapCheck(Vector3.Distance(Landing.I.transform.position, new Vector3(12.352f, 0, 0)) < 0.1f, $"Walt's Landing at its map spot {Landing.I.transform.position}");
            if (Arg("-pb-mapwalkonly") == null) yield return MapBoat();
            yield return MapWalk();
            Debug.Log($"[PB] RESULT maptour: {(mapFails == 0 ? "PASS" : "FAIL")} ({mapFails} fails)");
            Application.Quit();
        }

        IEnumerator MapBoat()
        {
            var s = Skiff.I; bool rend = Arg("-pb-maprender") != null;
            if (!rend) { CameraRig.I.Cam.enabled = false; CameraRig.I.UiCam.enabled = false; } // (no pictures needed: ~10x faster)
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < 4; i++) { Ctl.BotThrottle = 1; yield return null; yield return null; } // cast off, then full
            float t = 0, maxOff = 0, minDepth = 99, log = 0; var p0 = s.transform.position; int leg = 0; bool aboard = true, stuck = false;
            var mark = Flat(p0); float markT = 0;
            while (leg <= Route.Length && t < 3600)
            {
                var p = s.transform.position;
                Vector2 goal = leg < Route.Length ? Route[leg] : new Vector2(Landing.I.Moor.position.x, Landing.I.Moor.position.z);
                var to = new Vector3(goal.x - p.x, 0, goal.y - p.z);
                if (leg < Route.Length && to.magnitude < 25) { Debug.Log($"[PB] map boat: leg {leg + 1}/{Route.Length} at ({goal.x}, {goal.y}) after {t:F0} s"); leg++; continue; }
                if (leg == Route.Length)
                {
                    if (s.Notch > 1 && to.magnitude < 60) { Ctl.BotThrottle = -1; yield return null; yield return null; } // slow in to the berth
                    if (s.CanMoor) { s.Moor(); leg++; Debug.Log($"[PB] map boat: moored back at the berth after {t:F0} s"); break; }
                }
                float head = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                Ctl.BotSteer = Mathf.Clamp(Mathf.DeltaAngle(s.transform.eulerAngles.y, head) / 25f, -1, 1);
                maxOff = Mathf.Max(maxOff, Mathf.Abs(p.y - Sea.Height(p)));
                minDepth = Mathf.Min(minDepth, Seabed.Depth(p.x, p.z));
                bool on = me.OnBoat && !me.Swimming && me.transform.position.y > Sea.Level - 1;
                if (aboard && !on) Debug.Log($"[PB] map boat: OFF the boat at {me.transform.position} (on boat {me.OnBoat}, swimming {me.Swimming}, helm {s.AtHelm}) t {t:F0}");
                aboard &= on;
                if (Vector3.Distance(Flat(p), mark) > 15) { mark = Flat(p); markT = t; }
                if (t - markT > 25) { stuck = true; Debug.Log($"[PB] map boat: STUCK at {p} (leg {leg + 1}, speed {s.Speed:F2}, depth {Seabed.Depth(p.x, p.z):F1})"); break; }
                if (t - log > 120) { log = t; Debug.Log($"[PB] map boat: t {t:F0} s at {p.x:F0} {p.z:F0}, speed {s.Speed:F2}, depth {Seabed.Depth(p.x, p.z):F1} m"); }
                t += Time.deltaTime; yield return null;
            }
            Ctl.BotSteer = 0;
            MapCheck(!stuck && leg > Route.Length, $"Biscuit round the island and moored again: {leg}/{Route.Length + 1} legs in {t:F0} s, {(stuck ? "STUCK" : "never stuck")}");
            MapCheck(maxOff < 1.2f, $"she floats the whole way (worst height off the sea {maxOff:F2} m < 1.2)");
            MapCheck(aboard, "I stay aboard the whole way");
            MapCheck(minDepth > 2.5f, $"the route keeps to water: shallowest {minDepth:F1} m under her");
            me.LeaveHelm(); yield return new WaitForSeconds(0.5f);
            CameraRig.I.Cam.enabled = true; CameraRig.I.UiCam.enabled = true;
        }

        // walk: the office, the 7 town shops (Swell Surf is out at the Cut), down the pier to its ladder. Each leg is planned
        // on a 1 m grid of what is really there (A*: the top surface under each cell, walkable = a slope under 50 deg, under
        // 0.9 m from cell to cell, head room, above the sea), then walked with the normal controls. No path = a shop you can't reach;
        // stuck on the path = a snag; a fall = nothing under me, under the sea, or swimming.
        IEnumerator MapWalk()
        {
            var lt = Landing.I.transform;
            var goals = new List<(string id, Vector3 p)>();
            foreach (var id in new[] { "office", "tackle", "pub", "hartleys", "post", "pratts", "plank", "doyle", "office" })
            { var a = Places.Get(id); if (a != null) goals.Add((id, a.position + a.forward * 1.5f)); else MapCheck(false, "no anchor " + id); }
            goals.Add(("pier root", lt.TransformPoint(new Vector3(0, 1.3f, -9))));   // (M1 landing: the pier runs local +Z from z -9 out to sea)
            goals.Add(("pier end", Landing.I.Spawn.position));
            goals.Add(("pier ladder", Landing.I.LadderTop.position));
            me.Teleport(Places.Get("office").position + Vector3.up * 0.1f, 0); yield return new WaitForSeconds(0.5f);
            int falls = 0, stucks = 0, nopath = 0; float total = 0;
            foreach (var g in goals)
            {
                var path = WalkPath(me.transform.position, g.p);
                if (path == null) { nopath++; Debug.Log($"[PB] map walk: NO PATH from {me.transform.position} to {g.id} {g.p}"); me.Teleport(g.p + Vector3.up * 0.3f, 0); yield return new WaitForSeconds(0.3f); continue; }
                float t = 0, markT = 0; var mark = me.transform.position; bool fell = false; int k = 0;
                while (k < path.Count && t < 120)
                {
                    var p = me.transform.position; var to = path[k] - p; to.y = 0;
                    if (to.magnitude < (k == path.Count - 1 ? 0.7f : 1.1f)) { k++; continue; }
                    CameraRig.I.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up;
                    if (Vector3.Distance(p, mark) > 1.5f) { mark = p; markT = t; }
                    else if (t - markT > 2f) Ctl.BotJump = true;   // (a player would hop a kerb too)
                    if (t - markT > 12) { stucks++; Debug.Log($"[PB] map walk: STUCK on the way to {g.id} at {p} (path node {k}/{path.Count} at {path[k]})"); break; }
                    if (me.Swimming || p.y < Sea.Level - 3 || !Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, 50, ~0, QueryTriggerInteraction.Ignore))
                    { fell = true; Debug.Log($"[PB] map walk: FELL on the way to {g.id} at {p} (swimming {me.Swimming})"); break; }
                    yield return null; Ctl.BotJump = false; t += Time.deltaTime;
                }
                Ctl.BotMove = Vector2.zero; total += t; if (fell) falls++;
                var e = me.transform.position;
                Debug.Log($"[PB] map walk: {g.id}: {(k >= path.Count ? "reached" : "NOT reached")} in {t:F1} s along {path.Count} nodes (at {e.x:F1} {e.y:F2} {e.z:F1}, {Vector3.Distance(Flat(e), Flat(g.p)):F1} m off)");
                if (k < path.Count) { me.Teleport(g.p + Vector3.up * 0.3f, 0); yield return new WaitForSeconds(0.3f); }
                yield return new WaitForSeconds(0.2f);
            }
            MapCheck(falls == 0 && stucks == 0 && nopath == 0, $"the walk office -> 7 shops -> office -> pier ladder: {nopath} unreachable, {stucks} stuck, {falls} falls, {total:F0} s walking");
            MapCheck(!me.Swimming && Vector3.Distance(me.transform.position, Landing.I.LadderTop.position) < 1.5f, $"on the pier by its ladder at the end ({me.transform.position})");
        }

        static List<Vector3> WalkPath(Vector3 from, Vector3 to)
        {
            const float C = 1f; const int pad = 70;
            float x0 = Mathf.Min(from.x, to.x) - pad, z0 = Mathf.Min(from.z, to.z) - pad;
            int nx = Mathf.CeilToInt((Mathf.Abs(from.x - to.x) + 2 * pad) / C), nz = Mathf.CeilToInt((Mathf.Abs(from.z - to.z) + 2 * pad) / C);
            float top = Mathf.Max(from.y, to.y) + 40; var h = new float[nx * nz]; var why = new Color32[nx * nz];
            for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
                {
                    h[j * nx + i] = float.NaN;
                    why[j * nx + i] = new Color32(0, 0, 0, 255);
                    // the highest surface with head room (under an eave or a porch roof is still ground)
                    var hits = Physics.RaycastAll(new Vector3(x0 + i * C, top, z0 + j * C), Vector3.down, 120, ~0, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var hit in hits)
                    {
                        if (hit.point.y < Sea.Level + 0.15f) { why[j * nx + i] = new Color32(20, 60, 140, 255); break; }
                        if (hit.normal.y < 0.64f) { why[j * nx + i] = new Color32(220, 60, 40, 255); continue; }
                        if (Physics.CheckCapsule(hit.point + Vector3.up * 0.75f, hit.point + Vector3.up * 1.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) { why[j * nx + i] = new Color32(240, 200, 40, 255); continue; }
                        h[j * nx + i] = hit.point.y; byte v = (byte)Mathf.Clamp(120 + hit.point.y * 5, 0, 255); why[j * nx + i] = new Color32(v, v, v, 255); break;
                    }
                }
            int Cell(Vector3 p) => Mathf.Clamp(Mathf.RoundToInt((p.z - z0) / C), 0, nz - 1) * nx + Mathf.Clamp(Mathf.RoundToInt((p.x - x0) / C), 0, nx - 1);
            int s = Cell(from), goal = Cell(to);
            // start and goal: the nearest walkable cell (a door step, the pier edge)
            int Near(int c, float y) { if (!float.IsNaN(h[c]) && Mathf.Abs(h[c] - y) < 1.2f) return c; int best = -1; float bd = 9; for (int k = 0; k < h.Length; k++) if (!float.IsNaN(h[k]) && Mathf.Abs(h[k] - y) < 1.2f) { float d = Mathf.Abs(k % nx - c % nx) + Mathf.Abs(k / nx - c / nx); if (d < bd) { bd = d; best = k; } } return best; }
            s = Near(s, from.y); goal = Near(goal, to.y); if (s < 0 || goal < 0) return null;
            var g = new float[h.Length]; var from_ = new int[h.Length]; for (int k = 0; k < g.Length; k++) { g[k] = float.MaxValue; from_[k] = -1; }
            var open = new SortedSet<(float f, int c)>(); g[s] = 0; open.Add((0, s));
            float Hx(int c) => Mathf.Sqrt(Mathf.Pow(c % nx - goal % nx, 2) + Mathf.Pow(c / nx - goal / nx, 2));
            while (open.Count > 0)
            {
                var cur = open.Min; open.Remove(cur); int c = cur.c; if (c == goal) break;
                int ci = c % nx, cj = c / nx;
                for (int dj = -1; dj <= 1; dj++) for (int di = -1; di <= 1; di++)
                    {
                        int i = ci + di, j = cj + dj; if ((di == 0 && dj == 0) || i < 0 || j < 0 || i >= nx || j >= nz) continue;
                        int n = j * nx + i; if (float.IsNaN(h[n]) || Mathf.Abs(h[n] - h[c]) > 0.9f) continue;
                        if (di != 0 && dj != 0 && (float.IsNaN(h[cj * nx + i]) || float.IsNaN(h[j * nx + ci]))) continue; // no corner cutting
                        float ng = g[c] + (di != 0 && dj != 0 ? 1.414f : 1f);
                        if (ng >= g[n]) continue;
                        if (g[n] < float.MaxValue) open.Remove((g[n] + Hx(n), n));
                        g[n] = ng; from_[n] = c; open.Add((ng + Hx(n), n));
                    }
            }
            if (from_[goal] < 0 && goal != s)
            {
                // the grid as a picture (Logs: grey = walkable by height, red = too steep, yellow = no head room, blue = sea,
                // green = what the search reached, magenta = start and goal)
                for (int k = 0; k < g.Length; k++) if (g[k] < float.MaxValue) why[k] = new Color32(60, 200, 80, 255);
                why[s] = why[goal] = new Color32(255, 0, 255, 255);
                var tex = new Texture2D(nx, nz, TextureFormat.RGBA32, false); tex.SetPixels32(why);
                var png = Path.Combine(Application.persistentDataPath, $"walkgrid_{Mathf.RoundToInt(to.x)}_{Mathf.RoundToInt(to.z)}.png");
                File.WriteAllBytes(png, tex.EncodeToPNG()); int seen = 0; foreach (var v in g) if (v < float.MaxValue) seen++;
                Debug.Log($"[PB] map walk: the grid -> {png} (x0 {x0}, z0 {z0}, {nx}x{nz} 1 m cells; start cell {s % nx},{s / nx} h {h[s]}, goal {goal % nx},{goal / nx} h {h[goal]}, {seen} cells searched)");
                return null;
            }
            var path = new List<Vector3>();
            for (int c = goal; c >= 0; c = from_[c]) { path.Add(new Vector3(x0 + c % nx * C, h[c], z0 + c / nx * C)); if (c == s) break; }
            path.Reverse(); path.Add(to);
            var thin = new List<Vector3>(); for (int k = 0; k < path.Count; k++) if (k % 2 == 0 || k == path.Count - 1) thin.Add(path[k]); // every 2 m
            return thin;
        }

        // ------------------------------------------------------------------ pictures and numbers
        // -pb-mapview <dir> -pb-views "name:x,y,z>lx,ly,lz;..." (noon, fair): look at any spot (debugging the map); -pb-hour h, -pb-mari: Mari moored
        IEnumerator MapView(string views)
        {
            Ctl.Bot = true; Sleep.Off = true; Sky.ForceWx = 1; Sky.Jump(DayTime.At(2, ArgF("-pb-hour", 12) * 60)); Time.captureFramerate = 60;
            if (Arg("-pb-mari") != null && Firm.C != null) { Firm.C.Mari = DealSt.Active; Firm.C.MariAccepted = Sky.Minutes - 120; } // her boat moored (the counter shots)
            yield return new WaitForSeconds(0.5f);
            static Vector3 V(string s) { var f = s.Split(','); return new Vector3(float.Parse(f[0], CultureInfo.InvariantCulture), float.Parse(f[1], CultureInfo.InvariantCulture), float.Parse(f[2], CultureInfo.InvariantCulture)); }
            foreach (var v in views.Split(';'))
            {
                var nv = v.Split(':'); var ab = nv[1].Split('>');
                FlyCam(V(ab[0]), V(ab[1])); yield return new WaitForSeconds(0.4f); yield return Shot(nv[0]);
            }
            Debug.Log("[PB] RESULT mapview: PASS"); Application.Quit();
        }

        static void FlyCam(Vector3 at, Vector3 look)
        {
            CameraRig.I.enabled = false;
            CameraRig.I.Cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at)); CameraRig.I.Cam.fieldOfView = 55;
        }

        static readonly (string n, Vector3 at, Vector3 look)[] Views = {
            ("sea-west", new(-950, 22, -160), new(60, 20, 10)),          // from off the bay mouth: the town, the horns
            ("sea-north", new(-500, 40, 2200), new(150, 40, 300)),         // from the open north: the whole crescent, the horizon
            ("sea-town", new(-110, 2.5f, 10), new(80, 10, 5)),              // from a boat in the bay: the waterfront, the quay
            ("town-above", new(120, 45, 60), new(10, 0, -10)),             // over Old Saltmouth's roofs to the bay
            ("town-quay", new(60, 6, 62), new(20, 2, 0)),                  // the waterfront street by Hook, Line & Sinker
            ("town-yard", new(36, 3.2f, 12), new(-10, 1, 0)) };            // Walt's yard: the shack, the pier, Mari's boat
        static void HudShown(bool on) { var u = CameraRig.I.UiCam; if (u != CameraRig.I.Cam) u.cullingMask = on ? 1 << CameraRig.UiLayer : 0; }

        IEnumerator MapShots()
        {
            Ctl.Bot = true; Sleep.Off = true; yield return new WaitForSeconds(0.5f);
            Time.captureFramerate = 60; HudShown(false);   // (the look: no HUD cards over it)
            foreach (var hr in States)
            {
                Sky.ForceWx = hr.wx; Sky.Jump(DayTime.At(2, hr.h * 60));
                foreach (var v in Views) { FlyCam(v.at, v.look); yield return new WaitForSeconds(0.6f); yield return Shot(hr.n + "-" + v.n); }
                Debug.Log($"[PB] mapshots: {hr.n}: state {Sky.I.State}, brightness {LastBright:F0}");
            }
            HudShown(true);
            var lines = new List<string>();
            foreach (var hr in States) if (hr.n is "2-day" or "5-night" or "6-storm") yield return MapPerf(hr, lines);
            File.WriteAllLines(Path.Combine(shots, "fps.txt"), lines);
            CameraRig.I.enabled = true;
            Debug.Log($"[PB] RESULT mapshots: {(mapFails == 0 ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // fps and draw calls at 5 spots (day, fair). Batch mode has no window, so each frame renders the game camera into a
        // 1920x1080 target and waits for the GPU (no CPU/GPU overlap: slower than the real game, never kinder).
        // the 6 lighting states (as -pb-daytest): hour, weather
        static readonly (string n, double h, int wx)[] States = { ("1-dawn", 6.5, 1), ("2-day", 12, 1), ("3-golden", 18.5, 1), ("4-dusk", 20.25, 1), ("5-night", 23.5, 1), ("6-storm", 14, 5) };

        IEnumerator MapPerf((string n, double h, int wx) st, List<string> lines)
        {
            Sky.ForceWx = st.wx; Sky.Jump(DayTime.At(2, st.h * 60)); Time.captureFramerate = 0;
            yield return null; yield return null;                                            // (Sky takes the new state on its next update)
            var spots = new (string n, Vector3 at, Vector3 look)[] {
                ("landing (player height)", Landing.I.Spawn.position + new Vector3(0, 1.7f, 0), new(-300, 0, 0)),
                ("town street", Places.Get("pub").position + new Vector3(0, 1.7f, 0), Places.Get("hartleys").position),
                ("bay looking at town", new(-350, 3, -60), new(120, 15, 0)),
                ("open sea, whole island", new(-500, 40, 2200), new(150, 40, 300)),
                ("aerial over the hills", new(-200, 260, -900), new(300, 60, 300)) };
            if (lines.Count == 0) lines.AddRange(new[] { $"Port Boss map perf, {System.DateTime.Now:yyyy-MM-dd HH:mm}, {SystemInfo.graphicsDeviceName}, quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}, 1920x1080, shadows {QualitySettings.shadowDistance} m",
                "each frame = the scene camera rendered to a 1920x1080 target + a wait for the GPU (serial, pessimistic); draw items = visible renderers x materials incl. shadow casters, before SRP batching; tris incl. the shadow pass" });
            lines.Add($"-- {st.n} ({Sky.I.State})");
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); var c = CameraRig.I.Cam; var keep = c.targetTexture;
            using var tri = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            bool allOk = true;
            foreach (var sp in spots)
            {
                FlyCam(sp.at, sp.look);
                for (int i = 0; i < 20; i++) yield return null; // (LODs, shadows settle)
                var sw = new System.Diagnostics.Stopwatch(); double worst = 0, sum = 0; long tsum = 0; int n = 90;
                for (int i = 0; i < n; i++)
                {
                    sw.Restart();
                    c.targetTexture = rt; c.Render(); c.targetTexture = keep;
                    UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    double ms = sw.Elapsed.TotalMilliseconds; sum += ms; worst = System.Math.Max(worst, ms);
                    yield return null;
                    tsum += tri.LastValue;
                }
                // draw items: what Unity marked visible after these frames (the view + the sun's shadow casters, culled LODs
                // left out), x materials: the draw calls before the SRP batcher joins them (the release player's draw-call
                // counter reads 0 in batch mode)
                int items = 0, rends = 0; foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.isVisible && r.enabled && r.gameObject.layer != CameraRig.UiLayer) { rends++; items += r.sharedMaterials.Length; }
                double avg = sum / n; bool ok = 1000 / avg >= 60;
                allOk &= ok;
                var l = $"{sp.n,-26} {1000 / avg,6:F0} fps (avg {avg:F2} ms, worst {worst:F2} ms)  draw items {items,5} ({rends} renderers)  tris {tsum / n / 1000,5}k";
                lines.Add(l); Debug.Log($"[PB] mapperf: {st.n}: " + l);
            }
            rt.Release();
            MapCheck(allOk, $"{st.n}: 60 fps or better at all 5 spots");
        }

        // -pb-mapfilm <dir>: 60 s at 30 fps (1280x720 jpg frames; tools: ffmpeg -framerate 30 -i f_%05d.jpg): in from the
        // open west sea, round the north horn high, down the east side, over the hills and down into the town and the pier
        IEnumerator MapFilm(string dir)
        {
            Ctl.Bot = true; Sleep.Off = true; Sky.ForceWx = 1; Sky.Jump(DayTime.At(2, (float)ArgF("-pb-filmhour", 16.5f) * 60));
            yield return new WaitForSeconds(0.5f);
            var keys = new (Vector3 at, Vector3 look)[] {
                (new(-2300, 60, -300), new(0, 30, 0)), (new(-1700, 90, 700), new(-200, 30, 200)), (new(-900, 130, 1700), new(100, 40, 300)),
                (new(300, 160, 1900), new(300, 60, 0)), (new(1600, 170, 900), new(200, 60, -100)), (new(1900, 150, -500), new(200, 50, -200)),
                (new(1100, 140, -1600), new(0, 30, -300)), (new(-100, 110, -1100), new(60, 10, 0)), (new(-200, 40, -200), new(80, 8, 30)),
                (new(-40, 12, -30), new(40, 6, 20)), (new(30, 4.5f, -14), new(70, 6, 40)) };
            HudShown(false); StartCoroutine(Film(dir, 60, 1280, 720));
            float t0 = Time.time;
            while (Time.time - t0 < 60.5f)
            {
                float u = Mathf.Clamp01((Time.time - t0) / 60f) * (keys.Length - 1); int i = Mathf.Min((int)u, keys.Length - 2); float f = Mathf.SmoothStep(0, 1, u - i);
                int a = Mathf.Max(i - 1, 0), d = Mathf.Min(i + 2, keys.Length - 1);
                var at = CatRom(keys[a].at, keys[i].at, keys[i + 1].at, keys[d].at, u - i);
                FlyCam(at, Vector3.Lerp(keys[i].look, keys[i + 1].look, f));
                yield return null;
            }
            Debug.Log("[PB] RESULT mapfilm: PASS");
            Application.Quit();
        }
        static Vector3 CatRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) =>
            0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (3 * p1 - p0 - 3 * p2 + p3) * t * t * t);
    }
}
