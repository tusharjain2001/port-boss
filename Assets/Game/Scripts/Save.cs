using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace PortBoss.M1
{
    // M4 Save / Continue (gdd systems §5.1). One folder per world:
    //   <persistentDataPath>/Saves/<worldId>/  world.json.gz (+ .bak1..3, the three before), meta.json (what the title
    //   shows + the CRC-32 of world.json.gz), thumb.png
    // Written to .tmp first, then swapped in (File.Replace keeps the old one as .bak1), so a crash mid-save loses nothing.
    // A world: its seed and clock, the company (bank, Ledger, Mari, the kits), every player's hotbar and where they
    // stand (by their key: steam:<id> / name:<name>), Biscuit (where, moored / anchored, water in her), the deck's fish.
    // When: 07:00 after the night (Sleep), Save & quit (pause, Q), the window closing, and the host leaving co-op.
    // Co-op: only the host saves the world; at each 07:00 save it sends the crew a copy (CoopFirm, in chunks), which
    // they keep as "<worldId>-copy" (Continue on their own, from that morning).
    // schemaVersion + Migrate_N_to_M: every older file is brought up step by step on load (tests/fixtures).
    // The tests' batch mode only saves with -pb-savedir <dir> (never into Manan's own saves).
    public static class Save
    {
        public const int Schema = 2;
        public static string Current;                     // the world being played (null: a fresh one, no folder yet)
        public static string CurrentName;
        public static bool IsCopy;
        public static Action<string> OnLog;
        public static string LastHash;                    // (tests) the hash of the last save / load
        static void Log(string s) { Debug.Log("[PB] save: " + s); OnLog?.Invoke(s); }

        static string Arg(string k) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        public static string Root
        {
            get
            {
                var d = Arg("-pb-savedir");
                return d != null ? d : Application.isBatchMode ? null : Path.Combine(Application.persistentDataPath, "Saves");
            }
        }
        public static bool Enabled => Root != null;

        // ---------------------------------------------------------------- the world list (the title)
        public class Meta { public string Id, Name; public int Day, Cash; public double Minute; public string Saved; public bool Copy; public uint Crc; public int Schema; }
        public static List<Meta> Worlds()
        {
            var l = new List<Meta>(); var r = Root; if (r == null || !Directory.Exists(r)) return l;
            foreach (var d in Directory.GetDirectories(r))
            {
                try
                {
                    var p = Path.Combine(d, "meta.json"); if (!File.Exists(p)) continue;
                    var o = (Dictionary<string, object>)Json.Parse(File.ReadAllText(p));
                    l.Add(new Meta
                    {
                        Id = Json.S(o, "id") ?? Path.GetFileName(d), Name = Json.S(o, "name") ?? "Harbour", Day = Json.I(o, "day", 1), Cash = Json.I(o, "cash"), Minute = Json.D(o, "minute"),
                        Saved = Json.S(o, "saved") ?? "", Copy = Json.B(o, "copy"), Crc = (uint)Json.D(o, "crc"), Schema = Json.I(o, "schema", 1),
                    });
                }
                catch (Exception e) { Log($"skipped {Path.GetFileName(d)}: {e.Message}"); }
            }
            l.Sort((a, b) => string.CompareOrdinal(b.Saved, a.Saved));          // newest first (ISO times sort)
            return l;
        }
        public static string Ago(string iso)
        {
            if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)) return "";
            var d = DateTime.UtcNow - t.ToUniversalTime();
            return d.TotalMinutes < 1 ? "just now" : d.TotalHours < 1 ? $"{(int)d.TotalMinutes} min ago" : d.TotalDays < 1 ? $"{(int)d.TotalHours} h ago" : $"{(int)d.TotalDays} days ago";
        }

        // ---------------------------------------------------------------- what a world is (the live game -> JSON)
        static double R2(float v) => Math.Round(v, 2);
        static double Yaw(float y) { var r = Math.Round(y, 2) % 360; return r < 0 ? r + 360 : r; }   // 359.999 reads back as 0, not 360
        public static string Capture(bool forHash = false)
        {
            var w = new Json.W(); w.Obj();
            w.K("schema").V(Schema);
            if (!forHash) w.K("id").V(Current ?? "").K("name").V(CurrentName ?? "").K("copy").V(IsCopy).K("saved").V(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            w.K("seed").V(Sky.Seed).K("minute").V(Sky.Minutes);
            w.K("company"); if (Firm.C != null) Firm.C.Write(w); else w.Obj().End();
            // players: me (solo) or the whole crew (the host's book)
            w.K("players").Obj();
            var people = new List<(string key, string name, Hotbar bar, Angler body)>();
            if (Coop.On && Coop.IsHost) foreach (var e in Coop.I.Roster) { Coop.I.Book.Bars.TryGetValue(e.Id, out var b); people.Add((e.Key ?? Coop.MyKey(e.Name), e.Name, b, e.Id == Coop.Me ? Angler.I : Coop.I.Mates.TryGetValue(e.Id, out var m) ? m : null)); }
            else if (Angler.I != null) people.Add((Firm.MyKey, Coop.MyName, Angler.I.Bar, Angler.I));
            people.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            foreach (var (key, name, bar, body) in people)
            {
                w.K(key).Obj().K("name").V(name);
                if (body != null) { var p = body.transform.position; w.K("x").V(R2(p.x)).K("y").V(R2(p.y)).K("z").V(R2(p.z)).K("yaw").V(Yaw(body.transform.eulerAngles.y)).K("boat").V(body.OnBoat); }
                w.K("bar").Arr();
                if (bar != null) for (int i = 0; i < Hotbar.Slots; i++) foreach (var f in bar.S[i].Fish) WriteFish(w, f, i);
                w.End().K("sel").V(bar != null ? bar.Selected : 0).End();
            }
            w.End();
            var s = Skiff.I;
            if (s != null)
            {
                var p = s.transform.position; w.K("boat").Obj();
                // moored: she's at her berth (the scene's), so where she bobbed on her lines isn't saved
                if (!s.Moored) w.K("x").V(R2(p.x)).K("z").V(R2(p.z)).K("yaw").V(Yaw(s.transform.eulerAngles.y));
                w.K("moored").V(s.Moored).K("anchored").V(s.Anchored)
                    .K("water").V(R2(s.Water)).K("leaking").V(s.Leaking).End();
            }
            w.K("deck").Arr();
            var deck = new List<FishPickup>(FishPickup.All); deck.RemoveAll(d => d == null || d.F == null);
            deck.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (var d in deck) { var p = d.transform.position; WriteFish(w, d.F, -1, p); }
            w.End();
            w.End();
            return w.ToString();
        }
        static void WriteFish(Json.W w, Fish f, int slot, Vector3? at = null)
        {
            w.Obj().K("sp").V(f.Species.Id).K("g").V(f.Grade).K("cm").V(R2(f.Cm)).K("kg").V(R2(f.Kg)).K("at").V(Math.Round(f.CaughtAt, 3));
            if (slot >= 0) w.K("slot").V(slot);
            if (at.HasValue) w.K("x").V(R2(at.Value.x)).K("y").V(R2(at.Value.y)).K("z").V(R2(at.Value.z));
            w.End();
        }
        static Fish ReadFish(Dictionary<string, object> o)
        {
            var sp = FishDb.Get(Json.S(o, "sp")); if (sp == null) return null;
            return new Fish { Species = sp, Grade = Json.I(o, "g"), Cm = Json.F(o, "cm"), Kg = Json.F(o, "kg"), CaughtAt = Json.F(o, "at") };
        }

        // the hash the save test compares: the world as the game holds it (not the file's bytes)
        public static string Hash() => Crc32(Encoding.UTF8.GetBytes(Capture(true))).ToString("x8");

        // ---------------------------------------------------------------- writing
        public static string NewId() => "w" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);

        public static bool Write(string why)
        {
            if (!Enabled) return false;
            if (Coop.On && !Coop.IsHost) return false;                       // a client's copy comes from the host
            try
            {
                if (Current == null) { Current = NewId(); CurrentName ??= $"{Coop.MyName}'s harbour"; }
                var json = Capture(); var gz = Gzip(json);
                WriteFiles(Current, gz, json, why);
                LastHash = Hash();
                Log($"saved {Current} ({why}): {gz.Length} bytes gz, {json.Length} json, Day {DayTime.Day(Sky.Minutes)} {DayTime.Clock(Sky.Minutes)}, bank ${Firm.C?.Cash}, hash {LastHash}");
                if (Coop.IsHost && why == "morning") CoopFirm.SendCopy(Current, gz);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[PB] save: FAILED " + e); Alerts.Push("Save", "Couldn't save: " + e.Message, Alerts.Kind.Warning); return false; }
        }

        // into the folder: .tmp then swapped in (the old one becomes .bak1, .bak1 -> .bak2 -> .bak3)
        public static void WriteFiles(string id, byte[] gz, string json, string why)
        {
            var dir = Path.Combine(Root, id); Directory.CreateDirectory(dir);
            string world = Path.Combine(dir, "world.json.gz"), tmp = world + ".tmp";
            File.WriteAllBytes(tmp, gz);
            if (File.Exists(world))
            {
                for (int i = 3; i > 1; i--) { string a = world + ".bak" + (i - 1), b = world + ".bak" + i; if (File.Exists(a)) { if (File.Exists(b)) File.Delete(b); File.Move(a, b); } }
                File.Replace(tmp, world, world + ".bak1");
            }
            else File.Move(tmp, world);
            var o = (Dictionary<string, object>)Json.Parse(json);
            var co = Json.O(o, "company");
            var m = new Json.W().Obj().K("schema").V(Schema).K("id").V(id).K("name").V(Json.S(o, "name") ?? id).K("copy").V(Json.B(o, "copy"))
                .K("day").V(DayTime.Day(Json.D(o, "minute"))).K("minute").V(Json.D(o, "minute")).K("cash").V(co != null ? Json.I(co, "cash") : 0)
                .K("saved").V(Json.S(o, "saved") ?? "").K("why").V(why).K("crc").V((long)Crc32(gz)).End().ToString();
            string meta = Path.Combine(dir, "meta.json"); File.WriteAllText(meta + ".tmp", m);
            if (File.Exists(meta)) File.Replace(meta + ".tmp", meta, null); else File.Move(meta + ".tmp", meta);
            Thumb(dir);
        }

        // a 320 x 180 picture of the moment (the title's world list)
        static void Thumb(string dir)
        {
            try
            {
                var cam = CameraRig.I != null ? CameraRig.I.Cam : Camera.main; if (cam == null) return;
                var rt = RenderTexture.GetTemporary(320, 180, 24); var old = cam.targetTexture; var act = RenderTexture.active;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
                RenderTexture.active = rt; var t = new Texture2D(320, 180, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); t.Apply();
                RenderTexture.active = act; RenderTexture.ReleaseTemporary(rt);
                File.WriteAllBytes(Path.Combine(dir, "thumb.png"), t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
            }
            catch (Exception e) { Log("no thumb: " + e.Message); }
        }

        // ---------------------------------------------------------------- reading
        // the world's JSON (migrated to the current schema): world.json.gz if its CRC matches, else the newest .bak that reads
        public static Dictionary<string, object> ReadWorld(string id, out string from)
        {
            var dir = Path.Combine(Root, id); string world = Path.Combine(dir, "world.json.gz"); from = null;
            uint want = 0; try { want = (uint)Json.D((Dictionary<string, object>)Json.Parse(File.ReadAllText(Path.Combine(dir, "meta.json"))), "crc"); } catch { }
            foreach (var f in new[] { world, world + ".bak1", world + ".bak2", world + ".bak3" })
            {
                if (!File.Exists(f)) continue;
                try
                {
                    var bytes = File.ReadAllBytes(f);
                    if (f == world && want != 0 && Crc32(bytes) != want) { Log($"{id}: world.json.gz fails its CRC, trying the backups"); continue; }
                    var o = Parse(bytes); from = Path.GetFileName(f); return o;
                }
                catch (Exception e) { Log($"{id}: {Path.GetFileName(f)} unreadable ({e.Message})"); }
            }
            return null;
        }
        // a world file's bytes (gzip or plain JSON) -> the current schema
        public static Dictionary<string, object> Parse(byte[] bytes)
        {
            string json = bytes.Length > 2 && bytes[0] == 0x1f && bytes[1] == 0x8b ? Gunzip(bytes) : Encoding.UTF8.GetString(bytes);
            return Migrate((Dictionary<string, object>)Json.Parse(json));
        }

        // ---- the migrations, oldest first. Each takes a world of schema N and returns it at N+1.
        public static Dictionary<string, object> Migrate(Dictionary<string, object> o)
        {
            int v = Json.I(o, "schema", 1);
            if (v > Schema) throw new Exception($"made by a newer Port Boss (save schema {v}, this game reads up to {Schema})");
            if (v < 2) { o = Migrate_1_to_2(o); v = 2; }
            o["schema"] = (double)v;
            return o;
        }
        // schema 1 (the early M4 build): the bank and gear sat at the top: { cash, earned, rod, bait[5], player{x,y,z,yaw,bar[]} }.
        // 2: the company object (bank, Ledger, deals, kits by player key) and players by key.
        static Dictionary<string, object> Migrate_1_to_2(Dictionary<string, object> o)
        {
            string key = Json.S(o, "key") ?? "name:you";
            var kit = new Dictionary<string, object> { ["rod"] = (double)Json.I(o, "rod", 1), ["baitI"] = 0.0, ["pouch5"] = false, ["bait"] = o.TryGetValue("bait", out var b) ? b : new List<object> { 10.0, 0.0, 0.0, 0.0, 0.0 } };
            int cash = Json.I(o, "cash", Company.StartCash);
            var ledger = new List<object>();
            if (cash != Company.StartCash) ledger.Add(new Dictionary<string, object> { ["tx"] = "migrate:1", ["who"] = "Port Boss", ["what"] = "Balance carried over", ["at"] = Json.D(o, "minute", DayTime.Start), ["amount"] = (double)(cash - Company.StartCash), ["balance"] = (double)cash });
            var co = new Dictionary<string, object>
            {
                ["cash"] = (double)cash, ["earned"] = (double)Math.Max(0, cash - Company.StartCash),   // (the carried-over line: the Ledger must sum) ["morning"] = (double)DayTime.Day(Json.D(o, "minute", DayTime.Start) - DayTime.Wake),
                ["ledger"] = ledger, ["kits"] = new Dictionary<string, object> { [key] = kit },
            };
            var players = new Dictionary<string, object>();
            if (o.TryGetValue("player", out var p) && p is Dictionary<string, object> pd) players[key] = pd;
            var n = new Dictionary<string, object>
            {
                ["schema"] = 2.0, ["id"] = Json.S(o, "id") ?? "", ["name"] = Json.S(o, "name") ?? "Harbour", ["seed"] = (double)Json.I(o, "seed", 1), ["minute"] = Json.D(o, "minute", DayTime.Start),
                ["company"] = co, ["players"] = players, ["deck"] = new List<object>(),
            };
            Log("migrated a schema 1 world to 2");
            return n;
        }

        // ---------------------------------------------------------------- loading into the running game
        public static bool Load(string id)
        {
            var o = ReadWorld(id, out var from);
            if (o == null) { Alerts.Push("Save", "That world couldn't be read.", Alerts.Kind.Warning); return false; }
            Apply(o);
            Current = id; CurrentName = Json.S(o, "name") ?? id; IsCopy = Json.B(o, "copy");
            LastHash = Hash();
            Log($"loaded {id} from {from}: Day {DayTime.Day(Sky.Minutes)} {DayTime.Clock(Sky.Minutes)}, bank ${Firm.C.Cash}, hash {LastHash}");
            return true;
        }

        public static void Apply(Dictionary<string, object> o)
        {
            Sky.Seed = Json.I(o, "seed", 1); Sky.W = new Weather(Sky.Seed);
            Sky.Jump(Json.D(o, "minute", DayTime.Start));
            var co = Json.O(o, "company"); var c = co != null ? Company.Read(co) : new Company();
            // my things: under my key (or, from a world someone else started on this PC, its only player)
            var players = Json.O(o, "players") ?? new Dictionary<string, object>();
            string key = Firm.MyKey;
            if (!players.ContainsKey(key) && players.Count == 1) foreach (var k in players.Keys) { if (c.Kits.TryGetValue(k, out var kk)) { c.Kits.Remove(k); c.Kits[key] = kk; } key = k; }
            Firm.I.Use(c);
            var me = Angler.I;
            if (me != null && players.TryGetValue(key, out var po) && po is Dictionary<string, object> p)
            {
                foreach (var s in me.Bar.S) s.Fish.Clear();
                foreach (var fo in Json.L(p, "bar")) { if (fo is not Dictionary<string, object> fd) continue; var f = ReadFish(fd); if (f == null) continue; int slot = Json.I(fd, "slot", -1); if (slot > 0 && slot < Hotbar.Slots && !me.Bar.S[slot].Rod) me.Bar.S[slot].Fish.Add(f); else me.Bar.Add(f); }
                me.Bar.Selected = Mathf.Clamp(Json.I(p, "sel"), 0, Hotbar.Slots - 1);
                if (p.ContainsKey("x")) PlaceMe(me, new Vector3(Json.F(p, "x"), Json.F(p, "y"), Json.F(p, "z")), Json.F(p, "yaw"), Json.B(p, "boat"));
            }
            var s0 = Skiff.I; var bo = Json.O(o, "boat");
            if (s0 != null && bo != null)
            {
                s0.Moored = Json.B(bo, "moored"); s0.Anchored = Json.B(bo, "anchored"); s0.Water = Json.F(bo, "water"); s0.Leaking = Json.B(bo, "leaking");
                if (!s0.Moored) { var at = new Vector3(Json.F(bo, "x"), s0.transform.position.y, Json.F(bo, "z")); s0.transform.SetPositionAndRotation(at, Quaternion.Euler(0, Json.F(bo, "yaw"), 0)); }
                s0.Notch = 0; s0.Speed = 0; s0.TakeOver();
            }
            foreach (var d in new List<FishPickup>(FishPickup.All)) if (d != null) UnityEngine.Object.Destroy(d.gameObject);
            FishPickup.All.Clear();
            foreach (var fo in Json.L(o, "deck"))
            {
                if (fo is not Dictionary<string, object> fd) continue; var f = ReadFish(fd); if (f == null) continue;
                FishPickup.Drop(f, new Vector3(Json.F(fd, "x"), Json.F(fd, "y"), Json.F(fd, "z")), Quaternion.identity);
            }
            Firm.I.Bind();
        }
        // where I stood: on Biscuit (her deck, wherever she is now) or ashore
        static void PlaceMe(Angler me, Vector3 at, float yaw, bool onBoat)
        {
            var s = Skiff.I;
            if (onBoat && s != null && !s.Inside(at, 0.5f)) at = s.transform.TransformPoint(new Vector3(0, 0.6f, 0));
            me.Teleport(at, yaw);   // exactly where they stood (the saved spot was a resting one), so the hash matches
        }

        // ---------------------------------------------------------------- the moments it saves
        static bool quitSaved;
        public static void OnQuit() { if (quitSaved || Title.Shown) return; quitSaved = true; Write("quit"); }   // (never from the title: nothing played)
        public static void Morning() => Write("morning");

        // a fresh world (the title's New game): the start of Day 1, $40, June's worms
        public static void NewWorld()
        {
            Current = null; CurrentName = null; IsCopy = false;
            Firm.I.Use(new Company()); Log("a new world");
        }

        // a co-op copy (the host's morning save, on a crewmate's PC)
        public static void KeepCopy(string hostId, byte[] gz)
        {
            if (!Enabled) return;
            try
            {
                var o = Parse(gz); o["copy"] = true; o["name"] = (Json.S(o, "name") ?? "Crew") + " (crew copy)";
                var json = Json.Write(o); string id = hostId + "-copy";
                WriteFiles(id, Gzip(json), json, "crew copy");
                Current = id; CurrentName = Json.S(o, "name"); IsCopy = true;
                Log($"kept the host's world as {id} ({gz.Length} bytes)");
            }
            catch (Exception e) { Debug.LogWarning("[PB] save: the crew copy FAILED " + e); }
        }

        // ---------------------------------------------------------------- bytes
        public static byte[] Gzip(string s)
        {
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) { var b = Encoding.UTF8.GetBytes(s); gz.Write(b, 0, b.Length); }
            return ms.ToArray();
        }
        public static string Gunzip(byte[] b)
        {
            using var ms = new MemoryStream(b); using var gz = new GZipStream(ms, CompressionMode.Decompress); using var r = new StreamReader(gz, Encoding.UTF8);
            return r.ReadToEnd();
        }
        static uint[] crcT;
        public static uint Crc32(byte[] b)
        {
            if (crcT == null) { crcT = new uint[256]; for (uint i = 0; i < 256; i++) { uint c = i; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1; crcT[i] = c; } }
            uint x = 0xFFFFFFFFu; foreach (var v in b) x = crcT[(x ^ v) & 0xFF] ^ (x >> 8); return ~x;
        }
    }
}
