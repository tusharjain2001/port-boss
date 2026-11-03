using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    // Headless test scenarios (-pb-* switches, the AutoRun pattern, smaller). Every run ends with [PB] RESULT lines.
    //   -pb-fishtest N [csv]  a bot casts N times from Biscuit's rail and plays the bar with σ = -pb-sigma ms (70)
    //   -pb-shots <dir>       the bot fishes and saves screenshots at each step (cast, bobber, bite, minigame, catch, hotbar)
    //   -pb-boattest          drive, turn, anchor: logs roll/pitch and the anchor's hold
    //   -pb-timescale x, -pb-quit s, -pb-rod n, -pb-bait id, -pb-fightheavy, -pb-film <dir> <secs> (30 fps frames)
    public partial class Bot : MonoBehaviour
    {
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length; i++) if (a[i] == n) return i + 1 < a.Length ? a[i + 1] : ""; return null; }
        static float ArgF(string n, float d) { var s = Arg(n); return s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : d; }

        Fishing fish; Angler me; float sigma; string shots; int shotN;
        readonly List<string> rows = new();

        IEnumerator Start()
        {
            // -pb-solofish n: n fish caught "solo" (Id 0) in the hotbar before joining a crew (code audit P0: joining must keep them)
            if (Arg("-pb-solofish") != null && Angler.I != null)
            {
                int k = (int)ArgF("-pb-solofish", 1); for (int i = 0; i < k; i++) Angler.I.Bar.Add(new Fish { Species = FishDb.All[1], Grade = 0, Cm = 77.7f + i, Kg = 0.1f }); // (77.7 cm, 78.7 ...: found again by length)
                Debug.Log($"[PB] solo fish: {k} in my hotbar before joining (frame {Time.frameCount}, joined {Coop.Joined})");
            }
            var ts = Arg("-pb-timescale"); if (ts != null) Time.timeScale = ArgF("-pb-timescale", 1);
            var q = Arg("-pb-quit"); if (q != null) StartCoroutine(QuitAfter(ArgF("-pb-quit", 60)));
            if (Arg("-pb-fps") != null) Time.captureFramerate = (int)ArgF("-pb-fps", 60); // -pb-fps n: game time steps 1/n s a frame, like a real PC (batch mode otherwise runs ~1000 fps)
            yield return null; yield return null;
            me = Angler.I; fish = me.Fish;
            var rod = Arg("-pb-rod"); if (rod != null) fish.SetRod((int)ArgF("-pb-rod", 1));
            var bait = Arg("-pb-bait"); if (bait != null) fish.BaitI = Mathf.Max(0, System.Array.FindIndex(FishDb.Baits, b => b.Id == bait));
            Fishing.Bot.FightHeavy = Arg("-pb-fightheavy") != null; Fishing.Bot.LetGo = Arg("-pb-letgo") != null; // -pb-letgo: every catch thrown back
            Fishing.Bot.OldFacing = Arg("-pb-oldfacing") != null; Fishing.Bot.NoGaze = Arg("-pb-nogaze") != null; Fishing.Bot.ForceHeavy = Arg("-pb-forceheavy") != null; // test: every hook is heavy
            sigma = ArgF("-pb-sigma", 70) / 1000f;
            fish.OnLog += s => Debug.Log("[PB] fish " + s);
            var icons = Arg("-pb-icons"); // the painted rod icon, as a file (proof)
            if (icons != null) { Directory.CreateDirectory(icons); for (int t = 1; t <= 5; t++) File.WriteAllBytes(Path.Combine(icons, $"rod-icon-tier{t}.png"), UiKit.RodIcon(t).EncodeToPNG()); }
                        var film = Arg("-pb-film"); if (film != null) StartCoroutine(Film(film, ArgF("-pb-filmsecs", 60)));
            if (Arg("-pb-fishtest") != null) yield return FishTest((int)ArgF("-pb-fishtest", 50), Arg("-pb-csv"));
            else if (Arg("-pb-shots") != null) { shots = Arg("-pb-shots"); Directory.CreateDirectory(shots); yield return ShotTour(); }
            else if (Arg("-pb-boattest") != null) yield return BoatTest();
            else if (Arg("-pb-swimtest") != null) yield return SwimTest();
            else if (Arg("-pb-laddertest") != null) yield return LadderTest(Arg("-pb-laddertest"));
            else if (Arg("-pb-camtest") != null) yield return CamTest((int)ArgF("-pb-camtest", 6));
            else if (Arg("-pb-helmshots") != null) { shots = Arg("-pb-helmshots"); Directory.CreateDirectory(shots); yield return HelmShots(); }
            else if (Arg("-pb-scrolltest") != null) yield return ScrollTest();
            else if (Arg("-pb-idleshots") != null) { shots = Arg("-pb-idleshots"); Directory.CreateDirectory(shots); yield return IdleShots(); }
            else if (Arg("-pb-cast8") != null) { shots = Arg("-pb-cast8"); Directory.CreateDirectory(shots); yield return Cast8(); }
            else if (Arg("-pb-tour") != null) yield return Tour(Arg("-pb-tour"), Arg("-pb-side") != null);
            else if (Arg("-pb-feeltest") != null) yield return FeelTest(Arg("-pb-feeltest"));
            else if (Arg("-pb-knocktest") != null) yield return KnockTest(Arg("-pb-knocktest"));
            else if (Arg("-pb-crewshots") != null) { shots = Arg("-pb-crewshots"); Directory.CreateDirectory(shots); yield return CrewShots(); }
            else if (Arg("-pb-waitcam") != null) { shots = Arg("-pb-waitcam"); Directory.CreateDirectory(shots); yield return WaitCam(); }
            else if (Arg("-pb-gazeshots") != null) { shots = Arg("-pb-gazeshots"); Directory.CreateDirectory(shots); yield return GazeShots(); }
            else if (Arg("-pb-fadetest") != null) { shots = Arg("-pb-fadetest"); Directory.CreateDirectory(shots); yield return FadeTest(); }
            else if (Arg("-pb-cooptest") != null) yield return CoopTest(Arg("-pb-cooptest"), (int)ArgF("-pb-coopn", 1));
            else if (Arg("-pb-tosstest") != null) yield return TossTest();
            else if (Arg("-pb-menutest") != null) yield return MenuTest(Arg("-pb-menutest"));
            else if (Arg("-pb-invitetest") != null) yield return InviteTest(Arg("-pb-invitetest"));
            else if (Arg("-pb-rodtest") != null) yield return RodTest(Arg("-pb-rodtest"), Arg("-pb-rodshots") ?? "rodshots");
            else if (Arg("-pb-crowdtest") != null) yield return CrowdTest(Arg("-pb-crowdtest"), Arg("-pb-crowdshots") ?? "crowdshots");
            else if (Arg("-pb-shovetest") != null) yield return ShoveTest(Arg("-pb-shovetest"), Arg("-pb-shoveshots") ?? "shoveshots");
            else if (Arg("-pb-rejointest") != null) yield return RejoinTest(Arg("-pb-rejointest"), Arg("-pb-rejoinshots") ?? "rejoinshots");
            else if (Arg("-pb-leaktest") != null) yield return LeakTest(Arg("-pb-leaktest"), Arg("-pb-leakshots") ?? "leakshots");
            else if (DayTests() is IEnumerator m3) yield return m3; // M3 (BotDay.cs)
        }

        // -pb-menutest host|join [-pb-menuaddr <address>] [-pb-menushots <dir>] (tools/menutest.sh, M2 step 6): plays the
        // title menu as a player would. host: "Host on this network". join: "Join on this network", first a wrong address
        // (the menu must come back saying so), then the right one. Both: in, the other in the crew list, out.
        IEnumerator MenuTest(string what)
        {
            Ctl.Bot = true; shots = Arg("-pb-menushots"); if (shots != null) Directory.CreateDirectory(shots);
            bool ok = true; void Check(string w, bool c) { Debug.Log($"[PB] menu: {w}: {(c ? "ok" : "FAIL")}"); ok &= c; }
            IEnumerator Wait(System.Func<bool> until, float secs) { for (float t = 0; !until() && t < secs; t += Time.unscaledDeltaTime) yield return null; }
            yield return Wait(() => Title.Shown, 5); Check("the title menu is up", Title.Shown);
            yield return new WaitForSecondsRealtime(0.5f);
            if (shots != null && what == "host") yield return Shot("title-main");
            if (what == "host") Title.I.Choose(3);
            else
            {
                Title.I.GoTo("address"); yield return null; Title.I.Type("127.0.0.1:7999"); Title.I.Choose(1); // nobody there
                yield return Wait(() => Coop.Status != "" && Coop.I == null, 20);
                yield return new WaitForSecondsRealtime(0.3f);
                Check($"a wrong address comes back to the menu saying \"{Coop.Status}\"", Title.Shown && Coop.I == null && Coop.Status != "");
                if (shots != null) yield return Shot("title-couldnt-connect");
                Title.I.GoTo("address"); yield return null; Title.I.Type(Arg("-pb-menuaddr") ?? "127.0.0.1");
                if (shots != null) yield return Shot("title-address");
                Title.I.Choose(1);
            }
            yield return Wait(() => Coop.Joined && !Title.Shown, 15);
            Check($"{(what == "host" ? "hosting" : "joined")} from the menu, the menu gone, the world running (time {Time.timeScale})", Coop.Joined && !Title.Shown && Time.timeScale == 1);
            yield return Wait(() => Coop.I != null && Coop.I.Roster.Count >= 2, 30);
            yield return new WaitForSecondsRealtime(1.5f);
            Check($"the crew list shows both: {NameTags.I?.CrewNames}", NameTags.I != null && NameTags.I.CrewRows == 2);
            if (what == "host") { float apart = 99; foreach (var m in Coop.I.Mates.Values) if (m != null) apart = Mathf.Min(apart, Vector3.Distance(m.transform.position, me.transform.position)); Check($"the joiner stands apart from me, not inside me: {apart:F2} m", apart >= 0.9f && apart < 5f); }
            if (shots != null) yield return Shot("crew-list-" + what);
            if (what == "join") { yield return new WaitForSecondsRealtime(2f); Debug.Log($"[PB] RESULT menu join: {(ok ? "PASS" : "FAIL")}"); Application.Quit(); yield break; }
            yield return Wait(() => Coop.I != null && Coop.I.Roster.Count < 2, 30);
            Check("the joiner left and the crew list shrank to one", Coop.I != null && Coop.I.Roster.Count == 1);
            Debug.Log($"[PB] RESULT menu host: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-tosstest (solo, M2 step 4): on Biscuit's deck with fish in hand: tap Q drops one at the feet, E picks it up,
        // hold Q tosses one onto the deck, hold Q over the rail tosses one into the sea (it swims away).
        IEnumerator TossTest()
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; var s = Skiff.I; bool ok = true;
            me.Teleport(s.transform.TransformPoint(new Vector3(0, 1.0f, 0.3f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(1f);
            for (int i = 0; i < 4; i++) me.Bar.S[1].Fish.Add(new Fish { Species = FishDb.All[0], Cm = 12, Kg = 0.02f });
            me.Select(1); yield return new WaitForSeconds(0.5f);
            void Check(string what, bool c) { Debug.Log($"[PB] toss: {what}: {(c ? "ok" : "FAIL")}"); ok &= c; }
            int n = me.Bar.FishCount;
            Ctl.BotDropHeld = true; yield return new WaitForSeconds(0.1f); Ctl.BotDropHeld = false; yield return new WaitForSeconds(0.5f);
            Check($"tap Q dropped one at my feet ({n} -> {me.Bar.FishCount}, {FishPickup.All.Count} on the deck)", me.Bar.FishCount == n - 1 && FishPickup.All.Count == 1);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return new WaitForSeconds(0.3f);
            Check($"E picked it up ({me.Bar.FishCount}, {FishPickup.All.Count} on the deck)", me.Bar.FishCount == n && FishPickup.All.Count == 0);
            // toward the stern, along the deck: 2 m (just past the hold)
            CameraRig.I.Yaw = s.transform.eulerAngles.y + 180; yield return null;
            Ctl.BotDropHeld = true; yield return new WaitForSeconds(0.3f); Ctl.BotDropHeld = false;
            yield return new WaitForSeconds(0.2f); Check($"the throw started ({me.ThrowT:F2} s in)", me.ThrowT >= 0);
            if (shots != null) { yield return new WaitForSeconds(0.55f); yield return Shot("toss-solo"); }
            yield return new WaitForSeconds(2f);
            Check($"hold Q tossed one onto the deck ({me.Bar.FishCount}, {FishPickup.All.Count} on the deck)", me.Bar.FishCount == n - 1 && FishPickup.All.Count == 1);
            // out over the side, far: the sea
            CameraRig.I.Yaw = s.transform.eulerAngles.y + 90; yield return null;
            Ctl.BotDropHeld = true; yield return new WaitForSeconds(1.3f); Ctl.BotDropHeld = false;
            yield return new WaitForSeconds(2.5f);
            Check($"hold Q over the side tossed one into the sea ({me.Bar.FishCount}, {FishPickup.All.Count} on the deck)", me.Bar.FishCount == n - 2 && FishPickup.All.Count == 1);
            Debug.Log($"[PB] RESULT toss: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-cooptest host|rider -pb-coopn <n>, with -pb-host / -pb-join (tools/cooptest.sh: a host and n bot clients on
        // this PC over LAN, -pb-netsim 150 2 for the lag standard; gdd systems §5.2 M2). The riders stand still on Biscuit's
        // deck while the host drives her out, turning; every machine watches each crewmate's body in its own copy of the
        // boat, and it must hold its deck spot within 5 cm. Then the tiller moves to rider 1 (it drives 6 s), and back.
        // One crew member each. Then (M2 step 2) rider 2 goes over the side, swims and climbs the ladder back aboard, and
        // rider 3 is knocked over and gets up: everyone else must see it (afloat on their own sea, the ragdoll lying where
        // its owner's lies: tools/cooptest.sh compares the rest spots across the logs). Then (step 3) riders 1 and 2 fish
        // from either side for ~25 s: everyone else must see the cast, the float (where it really is: the script compares),
        // the bite, the fight with its ghost bar, and the host must have rolled every fish and pass every catch; then riders
        // 1 and 3 cast at once from the starboard rail, their lines converging: both must be told "Tangled!".
        IEnumerator CoopTest(string role, int n)
        {
            Application.targetFrameRate = 60; Ctl.Bot = true;
            if (role == "rejoin") { yield return Rejoin(n); yield break; }
            var s = Skiff.I; bool host = role == "host"; int fails = 0;
            for (float w = 0; !Coop.On && w < 20; w += Time.unscaledDeltaTime) yield return null;
            if (!Coop.On) { Debug.Log("[PB] RESULT coop: FAIL (never connected)"); Application.Quit(); yield break; }
            var c = Coop.I;
            // everyone in, and on the boat
            for (float w = 0; c.Roster.Count < n + 1 && w < 60; w += Time.unscaledDeltaTime) yield return null;
            var crews = new HashSet<int>(); foreach (var e in c.Roster) crews.Add(e.Crew);
            Debug.Log($"[PB] coop: roster {c.Roster.Count}: {string.Join(", ", c.Roster.ConvertAll(e => $"{e.Name}={M1Assets.I.CrewNames[e.Crew]}/slot{e.Slot}"))}; me {M1Assets.I.CrewNames[Angler.Body]}");
            if (c.Roster.Count < n + 1 || crews.Count != c.Roster.Count) { fails++; Debug.Log("[PB] coop: FAIL roster incomplete or a crew member twice"); }
            if (!host && Arg("-pb-solofish") != null)
            {
                // a newcomer who caught fish alone first: they're still in the hotbar, now booked by the host (Id != 0)
                for (float w = 0; me.Bar.FishCount > 0 && FirstFish().Id == 0 && w < 10; w += Time.unscaledDeltaTime) yield return null;
                int k = (int)ArgF("-pb-solofish", 1); bool kept = me.Bar.FishCount >= k && FirstFish() != null && FirstFish().Id != 0;
                Debug.Log($"[PB] coop: joined holding {k} solo fish: {me.Bar.FishCount} in my hotbar, booked as id {FirstFish()?.Id}: {(kept ? "kept" : "FAIL")}");
                if (!kept) fails++;
            }
            int idx = c.Roster.FindIndex(e => e.Id == Coop.Me), myIdx = idx; bool shotToss = false;
            var crew = new List<Coop.Entry>(c.Roster); ulong meId = Coop.Me; // (the host leaving clears the roster and my id before the riders report)
            if (!host)
            {
                // a deck spot of my own, then still
                me.Teleport(s.transform.TransformPoint(new Vector3(idx % 2 == 0 ? -0.45f : 0.45f, 1.0f, idx < 3 ? 0.9f : -0.3f)), s.transform.eulerAngles.y + 90);
            }
            for (float w = 0; w < 30; w += Time.unscaledDeltaTime)
            {
                bool all = host ? c.Mates.Count == n : me.OnBoat;
                if (host) foreach (var m in c.Mates.Values) all &= m != null && m.OnBoat;
                if (all) break; yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            // each crewmate's spot in my copy of the boat, while she's under way (stood still: it must not move); per phase:
            // 0 the host drives, 1 a client drives (its boat relayed through the host)
            var lo = new Dictionary<(Angler, int), Vector3>(); var hi = new Dictionary<(Angler, int), Vector3>();
            void Watch(Angler m) { if (m == null || !m.OnBoat || m.AtHelm || Mathf.Abs(s.Speed) < 0.5f) return; var k = (m, c.BoatOwner == c.Roster[0].Id ? 0 : 1); var l = s.transform.InverseTransformPoint(m.transform.position); lo[k] = lo.TryGetValue(k, out var a) ? Vector3.Min(a, l) : l; hi[k] = hi.TryGetValue(k, out var b) ? Vector3.Max(b, l) : l; }
            void Own() { if (me.OnBoat && !me.AtHelm && Mathf.Abs(s.Speed) > 0.5f) Watch(me); }
            float Spread(Angler m, int ph) => lo.ContainsKey((m, ph)) ? (hi[(m, ph)] - lo[(m, ph)]).magnitude : -1;
            string Report(int ph, ref int f)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var k in lo.Keys) if (k.Item2 == ph)
                {
                    float sp = Spread(k.Item1, ph); bool mine = k.Item1 == me; sb.Append($"{(mine ? "me" : k.Item1.NetName)} {sp * 100:F1} cm; ");
                    if (!mine && sp > 0.05f) f++;
                }
                return sb.ToString();
            }
            Vector3 start = s.transform.position; float maxSpeed = 0;
            // -pb-coopshots <dir>: how the crew looks from here (player colours, bodies on the deck), from beside the boat
            var cshots = Arg("-pb-coopshots"); int cshot = 0;
            Vector3? shotFrom = null; // (a picture from in front: a fish held up faces away from the boat-side view)
            IEnumerator CoopShot(string what, Vector3? look = null)
            {
                if (cshots == null) yield break;
                Directory.CreateDirectory(cshots); yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
                if (look is Vector3 lk) { ct.position = shotFrom ?? lk + s.transform.rotation * new Vector3(-2.2f, 1.7f, -2.6f); ct.rotation = Quaternion.LookRotation(lk - ct.position); shotFrom = null; }
                else { ct.position = s.transform.TransformPoint(new Vector3(-4.2f, 2.6f, 1.2f)); ct.rotation = Quaternion.LookRotation(s.transform.TransformPoint(new Vector3(0, 0.9f, -0.2f)) - ct.position); }
                var cams = new[] { CameraRig.I.Cam, CameraRig.I.UiCam }; foreach (var cm in cams) cm.aspect = 16f / 9f; // as the picture
                NameTags.I?.Place(); Canvas.ForceUpdateCanvases(); // the tags over the heads, seen from here
                Save(Path.Combine(cshots, $"{role}{(host ? "" : idx.ToString())}-{cshot++}-{what}.png"));
                foreach (var cm in cams) cm.ResetAspect();
                ct.SetPositionAndRotation(keep.position, keep.rotation);
            }
            // step 2: what this machine sees of the crewmate who swims (slot 2) and the one knocked over (slot 3)
            var swimmer = c.Roster.Find(e => e.Slot == 2); var knocked = c.Roster.Find(e => e.Slot == 3);
            bool hadSw = false, hadKn = false; // (seen at all: the host leaving clears the crewmates before the report)
            Angler Mate(Coop.Entry e)
            {
                var m = e != null && e.Id != Coop.Me && c.Mates.TryGetValue(e.Id, out var mm) ? mm : null;
                if (m != null) { if (e == swimmer) hadSw = true; if (e == knocked) hadKn = true; }
                return m;
            }
            bool sawSwim = false, sawAboard = false, sawDown = false, sawUp = false; float floatOff = 0, floatOffAt = 0, swimT = 0, downT = 0; Vector3 rest = default;
            bool shotSwim = false, shotDown = false; (string what, Vector3 at)? shotNow = null; // a picture to take this frame
            void Seen()
            {
                var sw = Mate(swimmer);
                if (sw != null && sw.Swimming)
                {
                    sawSwim = true; swimT += Time.deltaTime;
                    // off the float line on my own sea, once settled (past the drop in and the dunk); when the worst came is logged
                    // (3 Nov: always ~5.9 s in, at 150 ms 21-31 cm, M2.1 28 cm: near the 30 cm limit; cause not found yet)
                    if (swimT > 1f) { float off = Mathf.Abs(sw.transform.position.y - (Sea.Height(sw.transform.position) - Angler.SwimDepth)); if (off > floatOff) { floatOff = off; floatOffAt = swimT; } }
                    if (swimT > 1.5f && !shotSwim) { shotSwim = true; shotNow = ("crewmate-swims", sw.transform.position + Vector3.up * 0.5f); }
                }
                else if (sw != null && sawSwim && sw.OnBoat) sawAboard = true;
                var kn = Mate(knocked);
                if (kn != null && kn.Limp)
                {
                    sawDown = true; downT += Time.deltaTime;
                    var hp = kn.Anim.GetComponent<Ragdoll>().Hips.position; rest = s.transform.InverseTransformPoint(hp);
                    if (downT > 0.6f && !shotDown) { shotDown = true; shotNow = ("crewmate-down", hp); }
                }
                else if (kn != null && sawDown && !kn.Down) sawUp = true;
            }
            string Step2(ref int f)
            {
                var sb = new System.Text.StringBuilder();
                if (hadSw)
                {
                    // (her own body lags the waves ~10 cm, the clocks agree to within a trip, her pose is 0.1-0.2 s old: ~25 cm at
                    // 150 ms; on another sea she'd be off by up to the waves' 0.5 m)
                    bool ok = sawSwim && sawAboard && floatOff < 0.3f; if (!ok) f++;
                    sb.Append($"{swimmer.Name} swam {(sawSwim ? "yes" : "NO")} (at most {floatOff * 100:F0} cm off the float line on my sea, {floatOffAt:F1} s into a {swimT:F1} s swim), back aboard {(sawAboard ? "yes" : "NO")}; ");
                }
                if (hadKn)
                {
                    bool ok = sawDown && sawUp; if (!ok) f++;
                    sb.Append($"{knocked.Name} knocked over {(sawDown ? "yes" : "NO")}, up again {(sawUp ? "yes" : "NO")}");
                    if (sawDown) Debug.Log($"[PB] coop: rest {knocked.Name} {rest.x:F3} {rest.y:F3} {rest.z:F3}");
                }
                return sb.ToString();
            }
            // my own part in step 2
            IEnumerator GoSwim()
            {
                var keep = s.transform.InverseTransformPoint(me.transform.position);
                var side = s.transform.TransformPoint(new Vector3(-2.4f, 0, 1.6f)); side.y = Sea.Height(side) + 1.2f;
                me.Teleport(side, s.transform.eulerAngles.y); CameraRig.I.Yaw = s.transform.eulerAngles.y;
                for (float w = 0; !me.Swimming && w < 4; w += Time.deltaTime) yield return null;
                Debug.Log($"[PB] coop: over the side: in the water {(me.Swimming ? "yes" : "NO")}");
                yield return new WaitForSeconds(2.5f); // treading water
                Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(2f); Ctl.BotMove = Vector2.zero;
                var lb = s.LadderBottom.position; me.Teleport(new Vector3(lb.x, Sea.Height(lb) - Angler.SwimDepth, lb.z), me.transform.eulerAngles.y);
                yield return new WaitForSeconds(1f);
                Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
                yield return new WaitForSeconds(1.5f);
                Debug.Log($"[PB] coop: up the ladder: aboard {(me.OnBoat ? "yes" : "NO")}");
                if (!me.OnBoat) fails++;
                me.Teleport(s.transform.TransformPoint(keep), s.transform.eulerAngles.y + 90);
            }
            // step 3: what this machine sees of the two who fish (slots 1 and 2) and the two who tangle (slots 1 and 3)
            var fSeen = new Dictionary<ulong, HashSet<Fishing.St>>(); var fGhost = new HashSet<ulong>(); var fPrev = new Dictionary<ulong, Fishing.St>();
            var fCasts = new Dictionary<ulong, int>(); var tangledSeen = new HashSet<ulong>(); bool shotGhost = false, shotHold = false;
            string myName = c.Find(Coop.Me)?.Name; int myCasts = 0, byHost = 0;
            void Seen3()
            {
                foreach (var e in c.Roster)
                {
                    if (e.Id == Coop.Me || !c.Mates.TryGetValue(e.Id, out var m) || m == null || m.Fish == null) continue;
                    var f = m.Fish; var st = f.State;
                    if (!fSeen.TryGetValue(e.Id, out var set)) fSeen[e.Id] = set = new();
                    set.Add(st);
                    if (NameTags.I != null && NameTags.I.GhostOn(m)) { fGhost.Add(e.Id); if (!shotGhost && f.StateT > 0.5f) { shotGhost = true; shotNow = ("ghost-bar", m.transform.position + Vector3.up * 1.3f); } }
                    if (st == Fishing.St.Reveal && f.StateT > 0.5f && !shotHold) { shotHold = true; shotNow = ("crewmate-holds-up", m.transform.TransformPoint(new Vector3(0, 1.15f, 0.4f))); shotFrom = m.transform.TransformPoint(new Vector3(0.9f, 1.7f, 2.6f)); }
                    if (st == Fishing.St.Waiting && fPrev.TryGetValue(e.Id, out var was) && was != Fishing.St.Waiting)
                    {
                        int k = fCasts[e.Id] = (fCasts.TryGetValue(e.Id, out var kk) ? kk : 0) + 1;
                        Debug.Log($"[PB] coop: bob {e.Name} {k} {f.BobPos.x:F3} {f.BobPos.z:F3} seen");
                    }
                    if (f.Tangled) tangledSeen.Add(e.Id);
                    fPrev[e.Id] = st;
                }
            }
            string Y(bool b) => b ? "yes" : "NO";
            string Step3(ref int f)
            {
                var sb = new System.Text.StringBuilder(); bool anyHeld = false;
                foreach (var e in crew.FindAll(e => e.Slot == 1 || e.Slot == 2))
                {
                    if (e.Id == meId) continue;
                    var set = fSeen.TryGetValue(e.Id, out var ss) ? ss : new HashSet<Fishing.St>();
                    bool ok = set.Contains(Fishing.St.Flight) && set.Contains(Fishing.St.Waiting) && set.Contains(Fishing.St.Bite) && set.Contains(Fishing.St.Minigame) && fGhost.Contains(e.Id);
                    if (!ok) f++;
                    anyHeld |= set.Contains(Fishing.St.Reveal);
                    sb.Append($"{e.Name}: cast {Y(set.Contains(Fishing.St.Flight))}, float {Y(set.Contains(Fishing.St.Waiting))}, bite {Y(set.Contains(Fishing.St.Bite))}, fight {Y(set.Contains(Fishing.St.Minigame))} with its ghost bar {Y(fGhost.Contains(e.Id))}, reeled in {Y(set.Contains(Fishing.St.Reel))}, held up {Y(set.Contains(Fishing.St.Reveal))}; ");
                }
                if (sb.Length > 0 && !anyHeld) { f++; sb.Append("NO fish held up; "); }
                foreach (var e in crew.FindAll(e => e.Slot == 1 || e.Slot == 3))
                {
                    if (e.Id == meId) continue;
                    if (!tangledSeen.Contains(e.Id)) f++;
                    sb.Append($"{e.Name} tangled {Y(tangledSeen.Contains(e.Id))}; ");
                }
                return sb.ToString();
            }
            void LogMyFloat() { myCasts++; if (fish.RolledByHost) byHost++; Debug.Log($"[PB] coop: bob {myName} {myCasts} {fish.BobPos.x:F3} {fish.BobPos.z:F3} self"); }
            // my own part in step 3: fish over one side (+1 starboard, -1 port) until `until`
            IEnumerator GoFish(float side, float until)
            {
                me.Select(0); yield return new WaitForSeconds(0.8f);
                while (Time.time < until)
                {
                    CameraRig.I.Yaw = s.transform.eulerAngles.y + 90 * side + Random.Range(-15f, 15f);
                    yield return CastOnce(0.6f + 0.3f * Random.value);
                    for (float w = 0; fish.State != Fishing.St.Waiting && w < 5; w += Time.deltaTime) yield return null;
                    if (fish.State != Fishing.St.Waiting) continue;
                    LogMyFloat();
                    string row = null; yield return PlayFish(r => row = r);
                    Debug.Log("[PB] coop: fished " + row);
                    if (me.Bar.Full) for (int k = 1; k < 8; k++) me.Bar.S[k].Fish.Clear();
                }
                me.Select(1);
            }
            // riders 1 and 3 at once from the starboard rail, 1.2 m apart, aiming 12° in toward each other's line
            IEnumerator GoTangle(int who, float at)
            {
                while (Time.time < at) yield return null;
                float yaw = s.transform.eulerAngles.y + 90 + (who == 1 ? 12 : -12);
                me.Teleport(s.transform.TransformPoint(new Vector3(0.45f, 1.0f, who == 1 ? 0.9f : -0.3f)), yaw);
                me.Select(0); CameraRig.I.Yaw = yaw;
                while (Time.time < at + 1.5f) yield return null;
                yield return CastOnce(0.95f);
                for (float w = 0; fish.State != Fishing.St.Waiting && w < 5; w += Time.deltaTime) yield return null;
                if (fish.State == Fishing.St.Waiting) LogMyFloat();
                bool tangled = false;
                for (float w = 0; w < 8 && !tangled; w += Time.deltaTime) { tangled = fish.Tangled; yield return null; }
                Debug.Log($"[PB] coop: tangle: {Y(tangled)} (my line {(fish.State == Fishing.St.Recover ? "reeling in" : fish.State.ToString())})");
                if (!tangled) fails++;
                for (float w = 0; fish.State != Fishing.St.Ready && w < 4; w += Time.deltaTime) yield return null;
                me.Select(1);
            }
            // ---- step 4: the hotbar host-side (gdd systems §1.1.3, §1.1.7): give, toss, the deck race, 10,000 requests ----
            Angler MateBy(int slot) { var e = crew.Find(x => x.Slot == slot); return e != null && c.Mates.TryGetValue(e.Id, out var m) ? m : null; }
            ulong IdOf(int slot) => crew.Find(x => x.Slot == slot)?.Id ?? 0;
            int FishSlot() { for (int i = 0; i < Hotbar.Slots; i++) if (me.Bar.S[i].Fish.Count > 0) return i; return -1; }
            void Face(Angler m) { var d = m.transform.position - me.transform.position; d.y = 0; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; me.Teleport(me.transform.position + Vector3.up * 0.02f, yaw); CameraRig.I.Yaw = yaw; }
            int step4Fails = 0; string step4 = "";
            void Expect(string what, bool ok) { step4 += $"{what} {(ok ? "ok" : "FAIL")}; "; if (!ok) step4Fails++; }
            // all four at once: random gives (one or a stack), tosses (at a crewmate or anywhere near), drops, pickups,
            // fish let go, and replays of my last request, `rate` a second
            IEnumerator Stress(float secs, float rate, int seed, int want)
            {
                var r = new System.Random(seed); float budget = 0; int sent = 0, sent0 = Coop.Sent;
                for (float t = 0; t < secs && Coop.Sent - sent0 < want; t += Time.deltaTime)
                {
                    budget += rate * Time.deltaTime;
                    for (; budget >= 1; budget -= 1)
                    {
                        sent++; int op = r.Next(100);
                        var mine = new List<(int slot, Fish f)>(); for (int i = 0; i < Hotbar.Slots; i++) foreach (var f in me.Bar.S[i].Fish) mine.Add((i, f));
                        var ids = new List<ulong>(c.Mates.Keys);
                        if (op < 5) { Coop.Replay(); continue; }
                        if (op < 20) { if (c.Deck.Count > 0) { var keys = new List<ulong>(c.Deck.Keys); Coop.Pickup(keys[r.Next(keys.Count)]); } continue; }
                        if (mine.Count == 0) continue;
                        var (sl, fish) = mine[r.Next(mine.Count)];
                        if (op < 55 && ids.Count > 0) Coop.Give(ids[r.Next(ids.Count)], r.Next(10) < 3 ? new List<Fish>(me.Bar.S[sl].Fish) : new List<Fish> { fish });
                        else if (op < 82)
                        {
                            // at a crewmate (a catch, if they face me and have room), else somewhere on the deck, now and then the sea
                            Vector3 to; var from = me.transform.position + Vector3.up * 1.4f; int k = r.Next(10);
                            if (ids.Count > 0 && k < 4) to = c.Mates[ids[r.Next(ids.Count)]].transform.position + Vector3.up * 1.0f;
                            else if (k < 9) { var p = s.transform.TransformPoint(new Vector3(((float)r.NextDouble() - 0.5f) * 1.2f, 0.8f, ((float)r.NextDouble() - 0.5f) * 3f)); to = Coop.Ground(p, out var g) ? g : p; }
                            else { var p = me.transform.position + Quaternion.Euler(0, r.Next(360), 0) * Vector3.forward * 5f; to = new Vector3(p.x, Sea.Height(p), p.z); }
                            Coop.Toss(fish, from, to);
                        }
                        else if (op < 97) { var p = me.transform.position + new Vector3((float)r.NextDouble() - 0.5f, 0.3f, (float)r.NextDouble() - 0.5f); if (Coop.Ground(p, out var g)) Coop.DropFish(fish, g); }
                        else Coop.FreeFish(fish);
                    }
                    yield return null;
                }
                Debug.Log($"[PB] coop: stress: I sent {Coop.Sent - sent0} requests ({sent} tries)");
            }
            // ---- step 5: proximity voice (systems §3.6), with the made-up test voice (-pb-voicetest, never the mic) ----
            // The host, rider 2 and rider 3 (its mic off: -pb-voice off) talk 5 s aboard; then rider 2 talks 20 m off the
            // boat, then 45 m off. Everyone else hears it: packets, decoded, the speaking ring, the volume at that distance.
            int step5Fails = 0; string step5 = ""; bool step5On = false;
            void Expect5(string what, bool ok) { step5 += $"{what} {(ok ? "ok" : "FAIL")}; "; if (!ok) step5Fails++; }
            IEnumerator Step5()
            {
                float t5 = Time.time; AudioListener.volume = 0; // (nothing is played aloud on this PC)
                int dropped = Coop.VoiceDropped; c.TestBurst(); // code audit F: a 1500-byte voice burst never throws, it's dropped
                Expect5($"a 1500-byte voice burst dropped ({Coop.VoiceDropped - dropped})", Coop.VoiceDropped == dropped + 1);
                IEnumerator Until(float t) { while (Time.time < t5 + t) yield return null; }
                // (by name: cooptest.sh starts "Rider3" with its mic off, whatever slot it joins in; the one who swims off and
                // talks from 20 m and 45 m is the slot-2 rider unless that's the mic-off one)
                var off = crew.Find(e => e.Name == "Rider3");
                var far = crew.Find(e => e.Slot == 2 && e != off) ?? crew.Find(e => e.Slot == 3 && e != off) ?? crew.Find(e => e.Slot == 1);
                var talkers = crew.FindAll(e => e.Slot == 0 || e == far || e == off);
                bool iTalk = myIdx == 0 || far != null && meId == far.Id || off != null && meId == off.Id, iFar = far != null && meId == far.Id;
                var heard = new Dictionary<ulong, (int p0, int l0, bool ring, float gain)>();
                void Start() { heard.Clear(); foreach (var e in talkers) if (e.Id != meId && c.Mates.TryGetValue(e.Id, out var m) && m != null) { var v = m.GetComponent<VoiceOut>(); heard[e.Id] = (v != null ? v.Packets : 0, v != null ? v.Lost : 0, false, 0); } }
                void Listen()
                {
                    foreach (var id in new List<ulong>(heard.Keys))
                    {
                        if (!c.Mates.TryGetValue(id, out var m) || m == null) continue; var v = m.GetComponent<VoiceOut>(); var h = heard[id];
                        if (v != null && v.Speaking && v.Src != null)
                        {
                            float d = Vector3.Distance(CameraRig.I.Cam.transform.position, v.Src.transform.position), dm = Vector3.Distance(me.Head.position, v.Src.transform.position);
                            h.gain = Mathf.Max(h.gain, v.Src.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(Mathf.Min(d, dm) / VoiceOut.Silent));
                        }
                        h.ring |= NameTags.I != null && NameTags.I.RingOn(m) && NameTags.I.CrewRing(id); heard[id] = h; // (on the name tag and in the crew list)
                    }
                }
                (int packets, float lost, bool ring, float gain) Got(ulong id)
                {
                    if (!heard.TryGetValue(id, out var h) || !c.Mates.TryGetValue(id, out var m) || m == null) return (0, 0, false, 0);
                    var v = m.GetComponent<VoiceOut>(); int p = (v != null ? v.Packets : 0) - h.p0, l = (v != null ? v.Lost : 0) - h.l0;
                    return (p, p + l > 0 ? l / (float)(p + l) : 0, h.ring, h.gain);
                }
                string Name(ulong id) => crew.Find(e => e.Id == id)?.Name;
                // aboard, 5 s
                yield return Until(1f); int sent0 = c.VoiceSent, bytes0 = c.VoiceBytes; Start();
                Coop.TestTalk = iTalk;
                bool shotTalk = false;
                while (Time.time < t5 + 6f)
                {
                    Listen();
                    // a picture of rider 2 talking (its ring on the name tag; my own mic chip bottom-left)
                    var m2 = far != null && c.Mates.TryGetValue(far.Id, out var fm) ? fm : null; var v2 = m2 != null ? m2.GetComponent<VoiceOut>() : null;
                    if (!shotTalk && myIdx == 0 && Time.time > t5 + 3f && v2 != null && v2.Speaking) { shotTalk = true; yield return CoopShot("speaking-ring", m2.transform.position + Vector3.up * 1.4f); }
                    yield return null;
                }
                Coop.TestTalk = false;
                if (iTalk)
                {
                    int sent = c.VoiceSent - sent0;
                    if (off != null && meId == off.Id) Expect5($"my mic is off: sent {sent} voice packets", sent == 0);
                    else Expect5($"I talked: {sent} packets, {(c.VoiceBytes - bytes0) / 5f / 1024:F1} KB/s", sent >= 50);
                }
                yield return Until(6.3f);
                foreach (var e in talkers)
                {
                    if (e.Id == meId) continue; var g = Got(e.Id);
                    if (e == off) Expect5($"{e.Name} (mic off): {g.packets} packets", g.packets == 0);
                    else Expect5($"heard {e.Name} aboard: {g.packets} packets, {g.lost:P0} lost, ring {(g.ring ? "yes" : "NO")}, volume {g.gain:F2}", g.packets >= 50 && g.lost < 0.1f && g.ring && g.gain > 0.95f);
                }
                // rider 2 off the boat: 20 m (fainter), then 45 m (past hearing)
                var s0 = Skiff.I; var r2 = far;
                foreach (var (dist, lo, hi, at) in new[] { (20f, 0.12f, 0.35f, 6.5f), (45f, -0.01f, 0.01f, 10.5f) })
                {
                    yield return Until(at);
                    if (iFar) { var p = s0.transform.TransformPoint(new Vector3(-dist, 0, 0.9f)); me.Teleport(new Vector3(p.x, Sea.Height(p) - Angler.SwimDepth, p.z), me.transform.eulerAngles.y); }
                    yield return Until(at + 0.5f); Start(); if (iFar) Coop.TestTalk = true;
                    while (Time.time < t5 + at + 3.5f) { Listen(); yield return null; }
                    Coop.TestTalk = false; yield return Until(at + 3.8f);
                    if (r2 != null && r2.Id != meId) { var g = Got(r2.Id); Expect5($"heard {r2.Name} {dist:0} m off: {g.packets} packets, volume {g.gain:F2}", g.packets >= 25 && g.gain >= lo && g.gain <= hi); }
                }
                yield return Until(15f);
            }
            // ---- step 6: rider 1 quits and a new copy of it (the same name) joins: its hotbar, crew member and place come back
            //      (tools/cooptest.sh compares its before and after); everyone's crew list shows it go and come back ----
            int step6Fails = 0; string step6 = ""; bool leaveNow = false, step6On = false;
            void Expect6(string what, bool ok) { step6 += $"{what} {(ok ? "ok" : "FAIL")}; "; if (!ok) step6Fails++; }
            IEnumerator Step6()
            {
                var r1 = crew.Find(e => e.Name == "Rider1"); // (by name: cooptest.sh brings "Rider1" back)
                if (meId == r1.Id)
                {
                    var ids = new List<ulong>(); foreach (var sl in me.Bar.S) foreach (var f in sl.Fish) ids.Add(f.Id); ids.Sort();
                    var l = s.transform.InverseTransformPoint(me.transform.position);
                    Debug.Log($"[PB] coop: rejoin before: ids={string.Join(",", ids)} at {l.x:F3} {l.y:F3} {l.z:F3} crew {M1Assets.I.CrewNames[Angler.Body]} slot 1");
                    // (M3.1) it goes with the rod out, the bobber dangling: the others must not keep a stray bobber in mid-air
                    me.Select(0); yield return new WaitForSeconds(1.2f); Debug.Log($"[PB] coop: rejoin: leaving with the rod out ({me.Fish.State})");
                    yield return new WaitForSeconds(0.5f); leaveNow = true; step6On = true; yield break;
                }
                // the others: rider 1 comes back as a new connection while its old one is still open (as a quit Steam hasn't
                // noticed yet: code audit E). The host retires the old entry at once: one Rider1, its own colour and crew
                // member, never a stranger; the list is whole throughout
                Expect6($"the crew list shows all {n + 1}: {NameTags.I?.CrewNames}", NameTags.I != null && NameTags.I.CrewRows == n + 1);
                for (float w = 0; !c.Roster.Exists(e => e.Name == r1.Name && e.Id != r1.Id) && w < 90; w += Time.deltaTime) yield return null;
                yield return new WaitForSeconds(2.5f);
                var back = c.Roster.Find(e => e.Name == r1.Name);
                Expect6($"Rider1 is back: {NameTags.I?.CrewNames}, as {(back != null ? M1Assets.I.CrewNames[back.Crew] : "?")} in slot {back?.Slot}", back != null && back.Id != r1.Id && back.Crew == r1.Crew && back.Slot == r1.Slot && NameTags.I.CrewRows == n + 1 && c.Roster.FindAll(e => e.Name == r1.Name).Count == 1);
                int bobs = FindObjectsByType<BobberView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, rods = FindObjectsByType<Fishing>(FindObjectsSortMode.None).Length;
                int shown = FindObjectsByType<BobberView>(FindObjectsSortMode.None).Length;
                Expect6($"no stray bobber left by Rider1's old self ({bobs} bobbers for {rods} anglers, {shown} in view)", bobs == rods);
                if (host) Expect6($"the book still balances: {(c.Book.Check() == "" ? "yes" : c.Book.Check())}", c.Book.Check() == "");
                if (host && back != null) Expect6($"Rider1's fish are booked to the new connection ({c.Book.Bar(back.Id).FishCount})", c.Book.Bar(back.Id).FishCount > 0 && !c.Book.Bars.ContainsKey(r1.Id));
                if (host && back != null && cshots != null) yield return CoopShot("crew-list-after-rejoin", c.Mates.TryGetValue(back.Id, out var bm) ? bm.transform.position + Vector3.up * 1.2f : me.transform.position);
                step6On = true;
            }
            // a rider's part, from the moment the host's grant of 6 fish arrives
            IEnumerator Step4(float t4)
            {
                ulong tossBefore = c.LastToss.TryGetValue(crew.Find(e => e.Slot == 0).Id, out var tb) ? tb : 0;
                IEnumerator Until(float t) { while (Time.time < t4 + t) yield return null; }
                if (myIdx == 1)
                {
                    // give one by holding E on rider 2
                    yield return Until(1f); var r2 = MateBy(2); int before = me.Bar.FishCount; me.Select(FishSlot()); Face(r2);
                    yield return null; Ctl.BotInteractHeld = true; yield return new WaitForSeconds(0.6f); Ctl.BotInteractHeld = false;
                    yield return new WaitForSeconds(1f); Expect($"gave Rider2 one by holding E ({before} -> {me.Bar.FishCount})", me.Bar.FishCount == before - 1);
                    // face rider 3 for its toss, and catch it
                    yield return Until(4.5f); Face(MateBy(3)); bool caught = false; void Got(Alerts.A a) { if (a.Sender == "Caught!") caught = true; } Alerts.OnPush += Got;
                    yield return Until(7.5f); Alerts.OnPush -= Got; Expect($"caught Rider3's toss (\"Caught!\" {(caught ? "came" : "never came")})", caught);
                }
                if (myIdx == 2)
                {
                    // give the whole stack back by holding Shift + E on rider 1
                    yield return Until(3f); var r1 = MateBy(1); int sl = FishSlot(); for (int i = 0; i < Hotbar.Slots; i++) if (me.Bar.S[i].Fish.Count > me.Bar.S[sl].Fish.Count) sl = i;
                    int stack = me.Bar.S[sl].Fish.Count, before = me.Bar.FishCount; me.Select(sl); Face(r1);
                    yield return null; Ctl.BotSprint = true; Ctl.BotInteractHeld = true; yield return new WaitForSeconds(0.6f); Ctl.BotInteractHeld = false; Ctl.BotSprint = false;
                    yield return new WaitForSeconds(1f); Expect($"gave Rider1 a stack of {stack} with Shift ({before} -> {me.Bar.FishCount})", me.Bar.FishCount == before - stack);
                }
                if (myIdx == 3)
                {
                    // toss one to rider 1 (who faces me), then hold Q and let go: one out over the side
                    // (checked by the fish's id: other fish may arrive meanwhile)
                    bool Mine(ulong id) => System.Array.Exists(me.Bar.S, x => x.Fish.Exists(f => f.Id == id));
                    yield return Until(5f); var r1 = MateBy(1); me.Select(FishSlot()); Face(r1); ulong f1 = me.Bar.Sel.Fish[0].Id;
                    yield return new WaitForSeconds(0.2f); me.TossAt(r1.transform.position + Vector3.up * 1.0f);
                    yield return new WaitForSeconds(1.8f); Expect($"tossed fish {f1} to Rider1 (gone from my hotbar)", !Mine(f1));
                    me.Select(FishSlot()); ulong f2 = me.Bar.Sel.Fish[0].Id;
                    var yaw = s.transform.eulerAngles.y + 90; me.Teleport(me.transform.position + Vector3.up * 0.02f, yaw); CameraRig.I.Yaw = yaw;
                    yield return null; Ctl.BotDropHeld = true; yield return new WaitForSeconds(1.3f); Ctl.BotDropHeld = false;
                    yield return new WaitForSeconds(2f); Expect($"held Q and tossed fish {f2} into the sea (gone from my hotbar, not on the deck)", !Mine(f2) && !c.Deck.ContainsKey(f2));
                }
                if (myIdx == 1 || myIdx == 2)
                {
                    // the host throws one onto the deck (its toss after step 4 began); once it lies there both grab it at once
                    // (one gets it: is that fish in my hotbar now?)
                    ulong raced = 0, hostId = crew.Find(e => e.Slot == 0).Id;
                    for (float w = 0; raced == 0 && w < 20; w += Time.deltaTime) { if (c.LastToss.TryGetValue(hostId, out var tid) && tid != tossBefore && c.Deck.ContainsKey(tid)) raced = tid; yield return null; }
                    if (raced != 0) Coop.Pickup(raced);
                    yield return new WaitForSeconds(1.5f);
                    bool got = System.Array.Exists(me.Bar.S, sl => sl.Fish.Exists(f => f.Id == raced));
                    Debug.Log($"[PB] coop: deck race for fish {raced}: I {(got ? "got it" : "didn't get it")}");
                }
                yield return Until(12f);
                yield return Stress(55f, 90f, 1000 + myIdx, 2800);
                for (float w = 0; Coop.AuditResult == null && Coop.On && w < 45; w += Time.deltaTime) yield return null; // (the host audits once everyone's done)
                Expect($"my hotbar after the stress matches the host's book: {Coop.AuditResult ?? "no audit came"}", Coop.AuditResult != null && Coop.AuditResult.StartsWith("ok"));
                yield return Step5(); step5On = true;
                yield return Step6();
            }
            IEnumerator GoKnock()
            {
                yield return new WaitForSeconds(1.5f);
                var keep = s.transform.InverseTransformPoint(me.transform.position);
                me.Knock(s.transform.forward * 1.8f + Vector3.up * 1.2f);
                var hips = me.Anim.GetComponent<Ragdoll>().Hips; Vector3 r = default;
                while (me.Down) { if (me.Limp) r = s.transform.InverseTransformPoint(hips.position); yield return null; }
                Debug.Log($"[PB] coop: rest self {r.x:F3} {r.y:F3} {r.z:F3}");
                yield return new WaitForSeconds(1f);
                me.Teleport(s.transform.TransformPoint(keep), s.transform.eulerAngles.y + 90);
            }
            if (host)
            {
                me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y); yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.4f);
                Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.6f); Ctl.BotThrottle = 1;
                for (float t = 0; t < 14f; t += Time.deltaTime)
                {
                    Ctl.BotSteer = t < 3.5f ? -0.6f : t < 6f ? 0.45f : t < 10 ? 0.8f : -0.5f;
                    foreach (var m in c.Mates.Values) Watch(m);
                    if (Mathf.Abs(t - 8f) < Time.deltaTime) yield return CoopShot("host-drives");
                    maxSpeed = Mathf.Max(maxSpeed, s.Speed); yield return null;
                }
                Ctl.BotSteer = 0; Ctl.BotThrottle = -1; yield return null; Ctl.BotThrottle = -1; yield return null;
                for (float w = 0; Mathf.Abs(s.Speed) > 0.05f && w < 8; w += Time.deltaTime) { foreach (var m in c.Mates.Values) Watch(m); yield return null; }
                me.LeaveHelm(); me.Teleport(s.transform.TransformPoint(new Vector3(0, 1.0f, 0.3f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.8f); // between the riders' spots
                Debug.Log($"[PB] coop: drove {Vector3.Distance(start, s.transform.position):F0} m, top speed {maxSpeed:F1} m/s");
                Debug.Log($"[PB] coop: while I drove, each crewmate's spot on my deck moved: {Report(0, ref fails)}");
                if (lo.Count < n) { fails++; Debug.Log($"[PB] coop: FAIL watched only {lo.Count} riders under way"); }
                // the tiller to rider 1, then back
                var r1 = c.Roster.Find(e => e.Slot == 1); ulong owner = 0; float ownT = -1; var at = s.transform.position;
                var myLo = Vector3.one * 99; var myHi = -myLo;
                bool askedAnchor = false; float stillFor = 0; var lastAt = s.transform.position;
                for (float w = 0; w < 25; w += Time.deltaTime)
                {
                    // code audit D: once rider 1 has stopped the boat (still at the tiller), I ask for the anchor: the ask
                    // goes to the driver, the boat's owner, who drops it
                    var mv = s.transform.position - lastAt; mv.y = 0; lastAt = s.transform.position; // (it heaves on the waves even stopped)
                    stillFor = Time.deltaTime > 0 && mv.magnitude / Time.deltaTime < 0.3f ? stillFor + Time.deltaTime : 0;
                    if (!askedAnchor && ownT >= 0 && r1 != null && c.BoatOwner == r1.Id && Vector3.Distance(at, s.transform.position) > 3 && stillFor > 0.3f)
                    { askedAnchor = true; Coop.Request(Coop.Ask.Anchor); Debug.Log($"[PB] coop: I asked for the anchor while {r1.Name} has the tiller"); }
                    if (c.BoatOwner != Coop.Me && ownT < 0) { owner = c.BoatOwner; ownT = w; }
                    if (ownT >= 0 && c.BoatOwner == Coop.Me && w > ownT + 3) break;
                    if (ownT >= 0 && me.OnBoat) { var l = s.transform.InverseTransformPoint(me.transform.position); myLo = Vector3.Min(myLo, l); myHi = Vector3.Max(myHi, l); Own(); }
                    foreach (var m in c.Mates.Values) Watch(m);
                    yield return null;
                }
                float moved = Vector3.Distance(at, s.transform.position), mySp = (myHi - myLo).magnitude;
                bool handOk = r1 != null && owner == r1.Id && c.BoatOwner == Coop.Me && moved > 3 && mySp < 0.05f;
                Debug.Log($"[PB] coop: tiller hand-off {(handOk ? "ok" : "FAIL")}: owner went to {(owner == r1?.Id ? r1.Name : owner.ToString())}, the boat moved {moved:F1} m under them, back to the host {(c.BoatOwner == Coop.Me ? "yes" : "NO")}; I held the deck within {mySp * 100:F1} cm");
                if (!handOk) fails++;
                Debug.Log($"[PB] coop: while {r1?.Name} drove, each crewmate's spot on my deck moved: {Report(1, ref fails)}");
                // step 2: watch the swim and the knock (both under way within a few seconds of the boat stopping)
                for (float w = 0; w < 30; w += Time.deltaTime)
                {
                    Seen(); if (shotNow is var (sw, sa)) { shotNow = null; yield return CoopShot(sw, sa); }
                    if ((Mate(swimmer) == null || sawAboard) && (Mate(knocked) == null || sawUp) && w > 3) break;
                    yield return null;
                }
                Debug.Log($"[PB] coop: step 2: {Step2(ref fails)}");
                // step 3: the fishing, then the tangle (~60 s after the swim began); done once a tangle has played out
                for (float w = 0, after = -1; w < 90; w += Time.deltaTime)
                {
                    Seen(); Seen3(); if (shotNow is var (sw, sa)) { shotNow = null; yield return CoopShot(sw, sa); }
                    if (c.TangleCount > 0 && after < 0) after = w;
                    if (after >= 0 && w > after + 3) break;
                    yield return null;
                }
                Debug.Log($"[PB] coop: step 3: {Step3(ref fails)}");
                Debug.Log($"[PB] coop: step 3: I checked {c.Checked} catches against my rolls, {c.Flagged} flagged; {c.TangleCount} tangle(s)");
                if (c.Checked == 0 || c.Flagged > 0 || c.TangleCount == 0) fails++;
                // step 4: the book's own test, then 6 fish each (the riders' signal), the deck race, the stress, the audit
                var st = Ledger.SelfTest(10000); Debug.Log("[PB] coop: step 4: the book's self-test, 10,000 random transfers: " + st); if (!st.StartsWith("PASS")) fails++;
                float t4 = Time.time; c.Grant(6); c.LogPickups = true;
                while (Time.time < t4 + 8f) { Seen(); yield return null; }
                // a fish onto the deck in the middle, where nobody faces it: riders 1 and 2 race for it
                me.Select(FishSlot()); var deckAt = s.transform.TransformPoint(new Vector3(0, 0.6f, -1.0f)); Coop.Ground(deckAt, out deckAt);
                int yes = 0, no = 0; ulong raced = me.Bar.Sel.Fish.Count > 0 ? me.Bar.Sel.Fish[0].Id : 0; // (the fish I toss for them)
                void Race(string l) { if (l.Contains($"picked up fish {raced}: yes")) yes++; if (l.Contains($"picked up fish {raced}: no")) no++; }
                Coop.OnLog += Race;
                Coop.Toss(me.Bar.Sel.Fish[0], me.transform.position + Vector3.up * 1.4f, deckAt);
                // pictures: mid-air, and on the deck the moment it lands (before the riders race for it)
                for (float w = 0; w < 1.5f && c.Deck.Count == 0; w += Time.deltaTime)
                {
                    if (!shotToss && w > 0.35f) foreach (var fl in FishFlight.Live.Values) { shotToss = true; yield return CoopShot("toss-in-flight", fl.transform.position); break; }
                    yield return null;
                }
                foreach (var dk in c.Deck.Values) if (dk != null) { yield return CoopShot("fish-on-deck", dk.transform.position); break; }
                for (float w = 0; yes + no < 2 && w < 20; w += Time.deltaTime) yield return null; // (both riders' grabs answered)
                Coop.OnLog -= Race; c.LogPickups = false;
                Expect($"the deck race: {yes} got it, {no} turned away", yes == 1 && no == 1);
                while (Time.time < t4 + 13f) yield return null; // (the riders' own give and toss checks are done)
                c.Grant(12); yield return new WaitForSeconds(0.5f);
                IEnumerator Supply() { for (float w = 0; w < 55f; w += 1.5f) { c.Grant(5, 8); yield return new WaitForSeconds(1.5f); } }
                StartCoroutine(Supply()); // (fish do go back to the sea: anyone running low gets a few more)
                yield return Stress(55f, 90f, 1000, 2800);
                yield return new WaitForSeconds(4f); // the last tosses land, the last answers arrive
                string chk = c.Book.Check(); int held = c.Book.All.Count;
                Debug.Log($"[PB] coop: step 4: {c.Applied + c.Refused + c.Replays} requests: {c.Applied} applied, {c.Refused} refused, {c.Replays} replays refused; {c.Book.Made} fish made, {c.Book.Freed} back to the sea, {held} held; the book {(chk == "" ? "balances" : "BROKEN: " + chk)}");
                Expect("the book balances", chk == ""); Expect("10,000 requests", c.Applied + c.Refused + c.Replays >= 10000);
                c.Audit(); yield return Step5();
                Debug.Log($"[PB] coop: step 5: {step5}"); fails += step5Fails;
                yield return Step6();
                Debug.Log($"[PB] coop: step 6: {step6}"); fails += step6Fails;
                Debug.Log($"[PB] coop: step 4: {step4}");
                fails += step4Fails;
                Debug.Log($"[PB] RESULT coop host: {(fails == 0 ? "PASS" : fails + " FAIL")} ({n} riders)");
                yield return new WaitForSeconds(3f); // (the riders finish before the host goes)
                Application.Quit(); yield break;
            }
            // rider: watch the others while the host drives; rider 1 takes the tiller once she's stopped; once rider 1 has
            // driven and the boat is the host's again and stopped, rider 2 swims and rider 3 is knocked over
            bool sawMove = false, clientDrove = false, step2 = false, step4On = false; float still = 0, hostStill = 0; ulong hostId = c.Roster.Find(e => e.Slot == 0)?.Id ?? 0;
            int lastCount = me.Bar.FishCount;
            while (Coop.On && !leaveNow)
            {
                foreach (var m in c.Mates.Values) Watch(m); Own();
                Seen(); Seen3(); if (shotNow is var (sw, sa)) { shotNow = null; yield return CoopShot(sw, sa); }
                if (c.BoatOwner != hostId) clientDrove = true;
                hostStill = clientDrove && c.BoatOwner == hostId && Mathf.Abs(s.Speed) < 0.1f ? hostStill + Time.deltaTime : 0;
                if (!step2 && hostStill > 2.5f)
                {
                    step2 = true;
                    if (myIdx == 2) StartCoroutine(GoSwim());
                    if (myIdx == 3) StartCoroutine(GoKnock());
                    // step 3 (after the swim and the knock, ~10 s): 1 and 2 fish, then 1 and 3 tangle
                    float t0 = Time.time;
                    IEnumerator Fish1() { while (Time.time < t0 + 13) yield return null; yield return GoFish(1, t0 + 36); yield return GoTangle(1, t0 + 60); }
                    IEnumerator Fish2() { while (Time.time < t0 + 13) yield return null; yield return GoFish(-1, t0 + 36); }
                    if (myIdx == 1) StartCoroutine(Fish1());
                    if (myIdx == 2) StartCoroutine(Fish2());
                    if (myIdx == 3) StartCoroutine(GoTangle(3, t0 + 60));
                }
                // step 4 begins when the host's grant of 6 fish arrives
                int count = me.Bar.FishCount; if (step2 && !step4On && count - lastCount >= 6) { step4On = true; StartCoroutine(Step4(Time.time)); } lastCount = count;
                if (s.Speed > 1) sawMove = true;
                still = sawMove && !s.AtHelm && Mathf.Abs(s.Speed) < 0.1f ? still + Time.deltaTime : 0;
                if (idx == 1 && still > 1f)
                {
                    var keep = s.transform.InverseTransformPoint(me.transform.position);
                    me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y); yield return null; me.TakeHelm();
                    for (float w = 0; s.Remote && w < 3; w += Time.deltaTime) yield return null;
                    clientDrove = true; // (I am the client driving: the loop's own check doesn't run while I do)
                    Debug.Log($"[PB] coop: I have the tiller ({(s.Remote ? "STILL REMOTE" : "my copy is the real one")})");
                    Ctl.BotThrottle = 1; yield return null; Ctl.BotThrottle = 0; yield return new WaitForSeconds(0.5f); Ctl.BotThrottle = 1; yield return null; Ctl.BotThrottle = 0;
                    for (float t = 0; t < 6; t += Time.deltaTime) { Ctl.BotSteer = 0.5f; if (Mathf.Abs(t - 4f) < Time.deltaTime) yield return CoopShot("i-drive"); yield return null; }
                    Ctl.BotSteer = 0; Ctl.BotThrottle = -1; yield return null; Ctl.BotThrottle = 0; yield return null; Ctl.BotThrottle = -1; yield return null; Ctl.BotThrottle = 0;
                    for (float w = 0; Mathf.Abs(s.Speed) > 0.05f && w < 8; w += Time.deltaTime) yield return null;
                    // code audit D: the host asks for the anchor while I hold the tiller; it must come down on my boat
                    for (float w = 0; !s.Anchored && w < 4; w += Time.deltaTime) yield return null;
                    Debug.Log($"[PB] coop: the host dropped the anchor while I had the tiller: {(s.Anchored ? "ok" : "FAIL")}");
                    if (!s.Anchored) fails++; else s.ToggleAnchor(); // (up again for the steps after)
                    me.LeaveHelm(); me.Teleport(s.transform.TransformPoint(keep), s.transform.eulerAngles.y + 90);
                    sawMove = false; idx = -1;
                }
                yield return null;
            }
            Debug.Log($"[PB] coop: while the host drove, spots on my deck moved: {Report(0, ref fails)}");
            Debug.Log($"[PB] coop: while a crewmate drove: {Report(1, ref fails)}");
            Debug.Log($"[PB] coop: step 2: {Step2(ref fails)}");
            Debug.Log($"[PB] coop: step 3: {Step3(ref fails)}");
            if (myCasts > 0) { Debug.Log($"[PB] coop: step 3: I cast {myCasts} times, the host rolled {byHost} of the fish"); if (byHost != myCasts) fails++; }
            Debug.Log($"[PB] coop: step 4: {(step4On ? step4 : "NEVER STARTED")}"); fails += step4On ? step4Fails : 1;
            Debug.Log($"[PB] coop: step 5: {(step5On ? step5 : "NEVER FINISHED")}"); fails += step5On ? step5Fails : 1;
            if (leaveNow)
            {
                // code audit E: I stay connected (a quit Steam hasn't noticed yet) while a new copy of me joins; the host must
                // take the new one as me and close this old connection
                bool retired = false; for (float w = 0; w < 60; w += Time.unscaledDeltaTime) { if (!Coop.On) { retired = true; break; } yield return null; }
                Debug.Log($"[PB] coop: step 6: the host closed my old connection when the new copy of me came back: {(retired ? "ok" : "FAIL")}"); if (!retired) fails++;
            }
            Debug.Log($"[PB] coop: step 6: {(step6On ? (leaveNow ? "I left (a new copy of me comes back)" : step6) : "NEVER FINISHED")}"); fails += step6On ? step6Fails : 1;
            if (lo.Count < n) { fails++; Debug.Log($"[PB] coop: FAIL watched only {lo.Count} crewmates under way"); }
            Debug.Log($"[PB] RESULT coop rider: {(fails == 0 ? "PASS" : fails + " FAIL")}");
            Application.Quit();
        }

        Fish FirstFish() { foreach (var sl in me.Bar.S) if (sl.Fish.Count > 0) return sl.Fish[0]; return null; }
        // the -pb-solofish fish (77.7 cm, 78.7 ...) in my hotbar, booked, or lying on the deck (no room)
        static bool Solo(Fish f) => f.Cm > 77f && f.Cm < 90f && Mathf.Abs(f.Cm - Mathf.Round(f.Cm - 0.7f) - 0.7f) < 0.01f;
        int SoloKept() { int k = 0; foreach (var sl in me.Bar.S) foreach (var f in sl.Fish) if (Solo(f) && f.Id != 0) k++; foreach (var p in FishPickup.All) if (p != null && Solo(p.F)) k++; return k; }

        // M2 step 6: rider 1 back (a new copy, the same name): its hotbar, crew member and place as it left them
        IEnumerator Rejoin(int n)
        {
            for (float w = 0; !Coop.Joined && w < 20; w += Time.unscaledDeltaTime) yield return null;
            var c = Coop.I; int fails = 0;
            for (float w = 0; (c == null || c.Roster.Count < n + 1 || !Coop.Placed) && w < 30; w += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSeconds(2f);
            var s = Skiff.I; var ids = new List<ulong>(); foreach (var sl in me.Bar.S) foreach (var f in sl.Fish) ids.Add(f.Id); ids.Sort();
            var l = s.transform.InverseTransformPoint(me.transform.position); var e = c?.Find(Coop.Me);
            Debug.Log($"[PB] coop: rejoin after: ids={string.Join(",", ids)} at {l.x:F3} {l.y:F3} {l.z:F3} crew {M1Assets.I.CrewNames[Angler.Body]} slot {e?.Slot} solo {(int)ArgF("-pb-solofish", 0)} kept {SoloKept()}");
            if (!Coop.Placed) { fails++; Debug.Log("[PB] coop: FAIL the host never put me back"); }
            if (NameTags.I == null || NameTags.I.CrewRows != n + 1) { fails++; Debug.Log($"[PB] coop: FAIL the crew list shows {NameTags.I?.CrewRows} of {n + 1}"); }
            else Debug.Log($"[PB] coop: rejoin: the crew list shows {NameTags.I.CrewNames}");
            while (Coop.On) yield return null;
            Debug.Log($"[PB] RESULT coop rejoin: {(fails == 0 ? "PASS" : fails + " FAIL")}");
            Application.Quit();
        }

        IEnumerator QuitAfter(float s) { yield return new WaitForSecondsRealtime(s); Debug.Log("[PB] quit"); Application.Quit(); }

        // ------------------------------------------------------------------ the fishing bot
        void Board()
        {
            // stand at Biscuit's starboard rail facing out to sea
            var s = Skiff.I;
            bool blockedRight = Physics.Raycast(s.transform.position + Vector3.up * 1.5f, s.transform.right, 12f);
            var spot = (blockedRight ? s.Rails[0] : s.Rails[1]) ?? s.transform; var outward = blockedRight ? -s.transform.right : s.transform.right;
            me.Teleport(spot.position + Vector3.up * 0.05f, Quaternion.LookRotation(outward).eulerAngles.y);
            CameraRig.I.Yaw = Quaternion.LookRotation(outward).eulerAngles.y; CameraRig.I.Pitch = 22;
        }

        // Take the tiller, cast off, motor out past the pier end into open water, stop, drop the anchor at the bow,
        // then stand at a rail facing the open sea (the M1 MUST: fishing from the boat at sea).
        IEnumerator SailOut(bool shotsOn)
        {
            Ctl.Bot = true;
            var s = Skiff.I;
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.4f);
            if (shotsOn) yield return Shot("0-at-the-tiller");
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.6f); Ctl.BotThrottle = 1; // cast off, then 2/3
            var start = s.transform.position;
            float t = 0;
            while (t < 16f) { Ctl.BotSteer = t < 3.5f ? -0.6f : t < 6f ? 0.45f : 0; /* away from the pier (on the right at the berth), then straighten */ t += Time.deltaTime; if (shotsOn && Mathf.Abs(t - 7f) < Time.deltaTime) yield return Shot("0-motoring-out"); yield return null; }
            Ctl.BotSteer = 0; Ctl.BotThrottle = -1; yield return null; Ctl.BotThrottle = -1; yield return null;
            for (float w = 0; Mathf.Abs(s.Speed) > 0.05f && w < 8; w += Time.deltaTime) yield return null;
            me.LeaveHelm(); yield return new WaitForSeconds(0.3f);
            var bow = s.Rails[2] != null ? s.Rails[2].position : s.AnchorPt.position - s.transform.forward * 0.8f;
            me.Teleport(bow + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return new WaitForSeconds(0.2f);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            for (float w = 0; !s.Anchored && w < 4; w += Time.deltaTime) yield return null;
            if (!s.Anchored) Debug.LogWarning("[PB] anchor did not drop (bot not at the bow?)");
            Debug.Log($"[PB] sailed out {Vector3.Distance(Flat(start), Flat(s.transform.position)):F0} m from the berth, anchored at {s.transform.position}");
            Board(); yield return new WaitForSeconds(0.8f);
            if (shotsOn) yield return Shot("0-anchored-open-sea");
            DumpNear();
        }

        // -pb-dumpnear x,y (viewport): log every renderer covering that screen point, at the anchored shot and the cast charge
        void DumpNear()
        {
            if (Arg("-pb-dumpnear") == null) return;
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = r.bounds; if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    // only what covers the screen point -pb-dumpnear x,y (viewport) at this shot
                    var at = Arg("-pb-dumpnear").Split(','); var vp = new Vector2(float.Parse(at[0], CultureInfo.InvariantCulture), float.Parse(at[1], CultureInfo.InvariantCulture));
                    var cam = CameraRig.I.Cam; float x0 = 9, x1 = -9, y0 = 9, y1 = -9; bool front = false;
                    for (int c = 0; c < 8; c++) { var w = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) * 2 - 1, (c >> 1 & 1) * 2 - 1, (c >> 2 & 1) * 2 - 1)); var v = cam.WorldToViewportPoint(w); if (v.z > 0) front = true; x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
                    if (!front || vp.x < x0 || vp.x > x1 || vp.y < y0 || vp.y > y1 || b.size.x > 500) continue;
                    Debug.Log($"[PB] near: {r.name} ({r.GetType().Name}) parent {(r.transform.parent ? r.transform.parent.name : "-")} mat {(r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "none")} at {b.center} size {b.size}");
                }
        }

        // the wind-up held, close: from her right side and from behind, framing the head, hat, mitts and rod (look audit:
        // the rod and the mitt went through the hat brim)
        IEnumerator ChargeClose()
        {
            yield return new WaitForEndOfFrame();
            var ct = CameraRig.I.Cam.transform; var a = me.transform; var k = (ct.position, ct.rotation);
            float h = me.Size * 1.78f; var hd = me.Head != null ? me.Head.position : a.position + Vector3.up * 0.8f * h;
            foreach (var (tag, off) in new[] { ("side", a.right * 1.1f + a.forward * 0.25f), ("back", -a.forward * 1.0f + a.right * 0.35f + Vector3.up * 0.35f) })
            {
                ct.position = hd + off * h; ct.rotation = Quaternion.LookRotation(hd + Vector3.up * 0.05f * h - ct.position);
                Save(Path.Combine(shots, $"charge-{tag}.png"));
            }
            ct.SetPositionAndRotation(k.position, k.rotation);
            Debug.Log($"[PB] charge: the rod moved off the head on {Fishing.HeadPushes} frames so far");
        }

        IEnumerator CastOnce(float power)
        {
            Ctl.Bot = true;
            while (fish.State != Fishing.St.Ready) yield return null;
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
            // hold for the wanted power on the 1.1 s ping-pong (first rise)
            while (fish.State == Fishing.St.Charging && fish.Power < power - 0.01f && fish.StateT < Fishing.ChargePeriod) yield return null;
            if (shots != null) { yield return Shot("1-charge"); yield return ChargeClose(); } DumpNear();
            Ctl.BotUse = false; yield return null;
        }

        // Plays one fish to the end. Strikes on the bite after a human-ish reaction, never on a nibble; stops the bar
        // aiming at the zone's centre with a normal error of sigma (the "practised player" of fishing §3.7 is 70 ms).
        IEnumerator PlayFish(System.Action<string> row)
        {
            float settle = -1, bite = -1, planned = 0; int nibbles = 0; string sp = ""; int grade = -1;
            double plannedPress = -1; var errs = new List<float>();
            float t0 = Time.time;
            while (true)
            {
                var st = fish.State;
                if (st == Fishing.St.Waiting && settle < 0) { settle = fish.WaitStart; planned = fish.BiteWait; nibbles = fish.Nibbles; sp = fish.Hooked.Id; }
                if (st == Fishing.St.Bite)
                {
                    if (bite < 0) { bite = fish.BiteSeenAt; if (shots != null && shotN < 30) yield return Shot("3-bite"); }
                    if (Time.time - bite > 0.3f && Time.time - bite < 1.0f) { Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; }
                }
                if (st == Fishing.St.Minigame && !fish.Frozen)
                {
                    if (plannedPress < 0)
                    {
                        // when does the pointer next cross the zone centre? (scan ahead in 2 ms steps)
                        double now = Time.timeAsDouble; double hitT = now;
                        for (double t = now; t < now + 3.0 / fish.PtrSpeed; t += 0.002)
                            if (Mathf.Abs(fish.PointerAt(t) - fish.ZoneAt(t)) < 0.004f && t > now + 0.15) { hitT = t; break; }
                        float e = Gauss() * sigma; errs.Add(e * 1000);
                        plannedPress = hitT + e;
                    }
                    if (Time.timeAsDouble >= plannedPress)
                    {
                        Bot.PressAt = plannedPress; Ctl.BotStrike = true;
                        if (shots != null && fish.Stop == 0) yield return Shot("4-minigame");
                        yield return null; Ctl.BotStrike = false; plannedPress = -1;
                    }
                }
                if (st == Fishing.St.Reveal && fish.Catch != null) grade = fish.Catch.Grade;
                if (st == Fishing.St.Reveal && fish.StateT > Mathf.Max(0.7f, HoldReveal))
                {
                    if (shots != null && fish.StateT < 1.0f) { yield return Shot("5-catch"); }
                    Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
                }
                if ((st == Fishing.St.Ready || st == Fishing.St.Recover) && Time.time - t0 > 1f && settle >= 0)
                {
                    string outcome = fish.LastOutcome ?? "landed";
                    row(string.Join(",", sp, settle >= 0 ? planned.ToString("F3", CultureInfo.InvariantCulture) : "", bite >= 0 && settle >= 0 ? (bite - settle).ToString("F3", CultureInfo.InvariantCulture) : "",
                        nibbles, outcome, fish.Hits, grade >= 0 ? grade.ToString() : "", string.Join("|", errs.ConvertAll(x => x.ToString("F0", CultureInfo.InvariantCulture)))));
                    fish.LastOutcome = null;
                    yield break;
                }
                if (Time.time - t0 > 60f) { row(string.Join(",", sp, "", "", 0, "stuck " + st, 0, "", "")); Debug.LogWarning("[PB] fishtest stuck in " + st); yield break; }
                yield return null;
            }
        }
        public static double PressAt;
        public static float HoldReveal; // (-pb-rodtest: let the catch be shown off and the rod stowed first)

        static float Gauss() { float u1 = 1f - Random.value, u2 = Random.value; return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2); }

        IEnumerator FishTest(int n, string csv)
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false); me.Select(0);
            // nothing to look at in a 500-cast run: stop drawing so game time runs fast with small, even frames
            CameraRig.I.Cam.enabled = false; CameraRig.I.UiCam.enabled = false;
            var lines = new List<string> { "cast,species,planned_wait_s,measured_wait_s,nibbles,outcome,hits,grade,stop_err_ms" };
            var waits = new List<float>(); var grades = new int[2]; int landed = 0; float pmin = 99, pmax = 0, lagMax = 0;
            for (int i = 0; i < n; i++)
            {
                yield return CastOnce(0.55f + 0.4f * Random.value);
                string row = null;
                yield return PlayFish(r => row = r);
                lines.Add(i + 1 + "," + row);
                var f = row.Split(',');
                if (float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) waits.Add(w);
                if (float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pw)) { pmin = Mathf.Min(pmin, pw); pmax = Mathf.Max(pmax, pw); lagMax = Mathf.Max(lagMax, w - pw); }
                if (int.TryParse(f[6], out var g)) { grades[g]++; landed++; }
                if (me.Bar.Full) { for (int k = 1; k < 8; k++) me.Bar.S[k].Fish.Clear(); } // sold, so to speak
                if (i % 25 == 24) Debug.Log($"[PB] fishtest {i + 1}/{n}");
            }
            csv ??= Path.Combine(Application.isEditor ? Application.dataPath + "/../Logs" : Path.GetDirectoryName(Application.dataPath) + "/Logs", "fishtest.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(csv)); File.WriteAllLines(csv, lines);
            waits.Sort();
            float min = waits.Count > 0 ? waits[0] : -1, max = waits.Count > 0 ? waits[^1] : -1, mean = 0; foreach (var w in waits) mean += w; mean /= Mathf.Max(1, waits.Count);
            bool waitOk = min >= fish.Rod.BiteLo && max <= fish.Rod.BiteHi;
            // expected grade mix: each stop a hit when |error| ≤ half the window (window = zone / speed, rod-scaled)
            string mix = ""; float[] exp = ExpectedMix(); bool mixOk = true;
            for (int g = 0; g < 2; g++) { float got = grades[g] / (float)Mathf.Max(1, landed); mix += $"{Ponyo.GradeName[g]} {got:P0} (model {exp[g]:P0}) "; if (Mathf.Abs(got - exp[g]) > 0.03f + 1.5f * Mathf.Sqrt(exp[g] * (1 - exp[g]) / Mathf.Max(1, landed))) mixOk = false; }
            Debug.Log($"[PB] fishtest csv {csv}");
            Debug.Log($"[PB] RESULT bites {waits.Count}/{n}: seen on screen after min {min:F3} max {max:F3} mean {mean:F2} s (rod {fish.Rod.BiteLo}-{fish.Rod.BiteHi}, timescale {Time.timeScale}) -> {(waitOk ? "PASS" : "FAIL")}");
            Debug.Log($"[PB] RESULT bite timer (planned) min {pmin:F3} max {pmax:F3} s -> {(pmin >= fish.Rod.BiteLo && pmax <= fish.Rod.BiteHi ? "PASS" : "FAIL")}; frame lag max {lagMax * 1000:F0} ms");
            Debug.Log($"[PB] RESULT grade mix (landed {landed}, sigma {sigma * 1000:0} ms, Everyday on {fish.Rod.Name}): {mix}-> {(mixOk ? "PASS" : "CHECK")}");
            Application.Quit();
        }

        float[] ExpectedMix()
        {
            // Everyday on this rod (the starter's three fish are all Everyday): ONE stop, a hit when |error| <= half the
            // window (window = zone / speed, rod-scaled, zone 1/8 of the old width) -> Rare, else Basic
            float zone = FishDb.Green[0] * fish.Rod.GreenMul * Fishing.ZoneScale, sp = FishDb.Speed[0] * fish.Rod.SpeedMul * Fishing.SpeedScale;
            float p = Erf(zone / 2 / sp / (sigma * Mathf.Sqrt(2)));
            return new[] { 1 - p, p };
        }
        static float Erf(float x) { float t = 1 / (1 + 0.3275911f * Mathf.Abs(x)); float y = 1 - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x); return x >= 0 ? y : -y; }

        // ------------------------------------------------------------------ screenshots
        IEnumerator ShotTour()
        {
            yield return new WaitForSeconds(1.5f);
            yield return Shot("0-start");
            me.Select(0); yield return new WaitForSeconds(0.6f);
            yield return Shot("0-rod-in-hand");
            yield return SailOut(true);
            for (int i = 0; i < 9; i++)
            {
                yield return CastOnce(i == 0 ? 0.97f : 0.6f + 0.35f * Random.value);
                while (fish.State == Fishing.St.Flight) yield return null;
                if (i == 0) { yield return new WaitForSeconds(0.9f); yield return Shot("2-bobber"); }
                yield return PlayFish(r => Debug.Log("[PB] shots fish " + r));
                yield return new WaitForSeconds(0.3f);
                if (i == 0) { me.Select(1); yield return new WaitForSeconds(0.5f); yield return Shot("6-hotbar-fish"); me.Select(0); yield return new WaitForSeconds(0.5f); }
            }
            Hud.I.ShowDebug = true; yield return new WaitForSeconds(0.2f);
            yield return Shot("6-hotbar");
            Hud.I.ShowDebug = false;
            // the boat from the sea
            CameraRig.I.Yaw += 140; CameraRig.I.Pitch = 12; yield return new WaitForSeconds(1.2f); yield return Shot("7-wide");
            Debug.Log("[PB] RESULT shots " + shotN);
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(shots ?? ".", $"{shotN++:00}-{name}.png");
            Save(path);
        }

        public static double LastBright;
        public static void Save(string path, int w = 1920, int h = 1080)
        {
            if (!Application.isBatchMode || CameraRig.I == null) { ScreenCapture.CaptureScreenshot(path); return; }
            // batchmode draws nothing by itself: render the scene camera, then the UI camera, each into its own fresh
            // texture, and lay the (premultiplied) UI over the scene, as the stacked cameras do in the game
            var tex = Grab(CameraRig.I.Cam, w, h, false);
            // URP writes alpha 1 for the UI camera's empty background, so recover the UI's coverage by drawing it over
            // black and over white: alpha = 1 - (white - black), colour (premultiplied) = the black pass
            var uc = CameraRig.I.UiCam; var bg = uc.backgroundColor;
            uc.backgroundColor = Color.black; var ub = Grab(uc, w, h, false);
            uc.backgroundColor = Color.white; var uw = Grab(uc, w, h, false);
            uc.backgroundColor = bg;
            var a = tex.GetPixels32(); var b = ub.GetPixels32(); var wh = uw.GetPixels32(); long sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i].r + a[i].g + a[i].b;
                int k = Mathf.Clamp(((wh[i].r - b[i].r) + (wh[i].g - b[i].g) + (wh[i].b - b[i].b)) / 3, 0, 255); // 255 - alpha
                a[i] = new Color32((byte)Mathf.Min(255, b[i].r + a[i].r * k / 255), (byte)Mathf.Min(255, b[i].g + a[i].g * k / 255), (byte)Mathf.Min(255, b[i].b + a[i].b * k / 255), 255);
            }
            tex.SetPixels32(a); tex.Apply();
            File.WriteAllBytes(path, path.EndsWith(".jpg") ? tex.EncodeToJPG(90) : tex.EncodeToPNG()); Destroy(tex); Destroy(ub); Destroy(uw);
            LastBright = sum / (3.0 * a.Length);
            if (!path.EndsWith(".jpg")) Debug.Log($"[PB] shot {path} (scene brightness {LastBright:F0})");
        }

        static Texture2D Grab(Camera c, int w, int h, bool alpha)
        {
            var keep = c.targetTexture;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            c.targetTexture = rt; c.Render(); c.targetTexture = keep;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        // -pb-tour <dir> [-pb-side]: the animation proof. Films (30 fps game time, 1280x720 jpg) walking the deck,
        // two full casts to the catch, then the tiller and a short drive. -pb-side films it all from a fixed 3/4
        // camera beside the angler instead of the game camera (for the contact sheets).
        IEnumerator Tour(string dir, bool side)
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true;
            me.Select(0); yield return new WaitForSeconds(0.8f);
            filmSide = side; StartCoroutine(Film(dir, 999, 1280, 720));
            StartCoroutine(LogBeats());
            yield return new WaitForSeconds(0.8f);
            // walk the deck: aft along the rail, turn, back, then turn to the sea
            float yaw0 = CameraRig.I.Yaw;
            Debug.Log("[PB] tour walk " + Time.time);
            Ctl.BotMove = new Vector2(1, 0); yield return new WaitForSeconds(0.9f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.7f);
            Ctl.BotMove = new Vector2(-1, 0); yield return new WaitForSeconds(0.9f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f);
            // once more, then stand a moment (the idle: breathing, the float hanging from the tip)
            Ctl.BotMove = new Vector2(1, 0); yield return new WaitForSeconds(0.8f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.5f);
            Ctl.BotMove = new Vector2(-1, 0); yield return new WaitForSeconds(0.8f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(2.0f);
            for (int i = 0; i < 2; i++)
            {
                Debug.Log($"[PB] tour cast {i + 1} at {Time.time:F2}");
                yield return CastOnce(i == 0 ? 0.85f : 0.6f);
                yield return PlayFish(r => Debug.Log("[PB] tour fish " + r));
                yield return new WaitForSeconds(0.8f);
            }
            // to the tiller: stow the rod, walk to the helm, take it, anchor up, motor off
            Debug.Log("[PB] tour helm " + Time.time);
            me.Select(1); yield return new WaitForSeconds(0.5f);
            float t = 0;
            while (t < 6 && me.Prompt != "Take the tiller")
            {
                var to = s.Helm.position - me.transform.position; to.y = 0;
                var local = Quaternion.Euler(0, -CameraRig.I.Yaw, 0) * to.normalized;
                Ctl.BotMove = to.magnitude > 0.3f ? new Vector2(local.x, local.z) * 0.6f : Vector2.zero;
                t += Time.deltaTime; yield return null;
            }
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.3f);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            if (s.Anchored) s.ToggleAnchor();
            yield return new WaitForSeconds(1.8f);
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.4f); Ctl.BotThrottle = 1;
            for (t = 0; t < 26; t += Time.deltaTime) { Ctl.BotSteer = t > 3 && t < 7 ? 0.5f : t > 11 && t < 15 ? -0.6f : 0; yield return null; } // a turn each way
            Debug.Log("[PB] RESULT tour done " + Time.time);
            Application.Quit();
        }
        static bool filmSide;
        // the tour's beats with their times, so the proof script can cut contact sheets of them
        IEnumerator LogBeats()
        {
            var last = fish.State;
            while (true)
            {
                if (fish.State != last && fish.State is Fishing.St.Heavy or Fishing.St.Minigame or Fishing.St.Reel or Fishing.St.Reveal or Fishing.St.Bite)
                    Debug.Log($"[PB] tour beat {fish.State} at {Time.time:F2}");
                last = fish.State; yield return null;
            }
        }

        // -pb-cast8 <dir>: stand mid-deck facing the bow, aim the camera at 8 headings round the boat (0 = over the bow,
        // 180 = behind him, over the motor) and cast. The body must face where the line goes, from the charge to the
        // bobber settling: logs the worst angle between his facing and the cast, PASS under 25 degrees.
        IEnumerator Cast8()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true; me.Select(0);
            float worstAll = 0;
            for (int k = 0; k < 8; k++)
            {
                me.Teleport(s.transform.position + Vector3.up * 0.2f, s.transform.eulerAngles.y);
                float aim = s.transform.eulerAngles.y + 45 * k;
                CameraRig.I.Yaw = aim; CameraRig.I.Pitch = 22;
                yield return new WaitForSeconds(0.8f);
                while (fish.State != Fishing.St.Ready) yield return null;
                Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
                while (fish.State == Fishing.St.Charging && fish.StateT < 0.75f) yield return null;
                float atRelease = Off(); Ctl.BotUse = false; yield return null;
                float worst = 0; bool shot = false;
                for (float t = 0; t < 2.2f; t += Time.deltaTime)
                {
                    if (t > 0.6f) worst = Mathf.Max(worst, Off()); // after the turn-in toward a snapped marker
                    if (!shot && t > 0.25f) { shot = true; yield return Shot($"cast8-{45 * k:000}"); }
                    yield return null;
                }
                var m = fish.Marker - me.transform.position; m.y = 0;
                Debug.Log($"[PB] cast8 aim {45 * k,3} deg (camera vs bow): marker at {Mathf.DeltaAngle(s.transform.eulerAngles.y, Mathf.Atan2(m.x, m.z) * Mathf.Rad2Deg):F0} deg, body off the cast at release {atRelease:F0}, worst after {worst:F0} deg");
                worstAll = Mathf.Max(worstAll, worst);
                Ctl.BotCancel = true; yield return new WaitForSeconds(0.5f); Ctl.BotCancel = false;
                while (fish.State != Fishing.St.Ready) yield return null;
            }
            Debug.Log($"[PB] RESULT cast8 worst facing error {worstAll:F0} deg -> {(worstAll < 25 ? "PASS" : "FAIL")}");
            Application.Quit();
        }
        // -pb-helmshots <dir>: at the tiller, motoring, the tiller hard over each way; 4 close cameras round the stern.
        IEnumerator HelmShots()
        {
            yield return new WaitForSeconds(0.5f);
            var s = Skiff.I; Ctl.Bot = true;
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.6f);
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.5f); Ctl.BotThrottle = 1;
            Ctl.BotSteer = -0.4f; yield return new WaitForSeconds(4f); // off the berth
            var views = new[] { (new Vector3(-1.6f, 1.3f, 1.4f), "front-left"), (new Vector3(1.7f, 1.3f, 1.2f), "front-right"), (new Vector3(-1.4f, 1.6f, -1.8f), "back-left"), (new Vector3(1.3f, 1.8f, -1.6f), "back-right") };
            for (int k = 0; k < views.Length; k++)
            {
                Ctl.BotSteer = k % 2 == 0 ? 1 : -1; yield return new WaitForSeconds(1.0f);
                var bt = s.transform;
                // clearance: the tiller bar (pivot to grip) against the body (not the hand that holds it)
                var an = me.Anim.GetComponent<Animator>(); float clear = 9; string near = "";
                foreach (var (b, r) in new[] { (HumanBodyBones.Chest, 0.17f), (HumanBodyBones.Spine, 0.16f), (HumanBodyBones.Hips, 0.17f), (HumanBodyBones.LeftUpperArm, 0.07f), (HumanBodyBones.LeftLowerArm, 0.06f), (HumanBodyBones.RightUpperArm, 0.07f), (HumanBodyBones.LeftUpperLeg, 0.09f), (HumanBodyBones.LeftLowerLeg, 0.07f), (HumanBodyBones.Head, 0.12f) })
                {
                    var bt0 = an.GetBoneTransform(b); if (bt0 == null) continue;
                    var a0 = s.Motor.position; var g0 = s.TillerGrip.position; var ab = g0 - a0; float h = Mathf.Clamp01(Vector3.Dot(bt0.position - a0, ab) / ab.sqrMagnitude);
                    float d = Vector3.Distance(bt0.position, a0 + ab * h) - r - 0.035f; // bar radius 3.5 cm
                    if (d < clear) { clear = d; near = b.ToString(); }
                }
                Debug.Log($"[PB] helm clearance {views[k].Item2}: {clear * 100:F0} cm (nearest {near}) -> {(clear > 0.02f ? "ok" : "CLIPS")}");
                Debug.Log($"[PB] helm geo: me {bt.InverseTransformPoint(me.transform.position)} grip {bt.InverseTransformPoint(s.TillerGrip.position)} motor {bt.InverseTransformPoint(s.Motor.position)} rudder {s.Rudder:F0} lhand {bt.InverseTransformPoint(me.Anim.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftHand).position)} rhand {bt.InverseTransformPoint(me.Anim.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).position)}");
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var a = me.transform;
                ct.position = a.TransformPoint(views[k].Item1);
                ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0, 0.75f, -0.15f)) - ct.position);
                Save(Path.Combine(shots, $"helm-{k}-{views[k].Item2}-steer{(k % 2 == 0 ? "R" : "L")}.png"));
            }
            Debug.Log("[PB] RESULT helmshots 4");
            Application.Quit();
        }

        // -pb-scrolltest: the mouse wheel through the real input path (a virtual mouse -> Input System -> Ctl -> Angler).
        // Wheel down = next slot, up = previous, wrapping 8 -> 1; one slot per notch; nothing while a cast/fight is on
        // (charging, minigame, reel, reveal all lock the bar) or while paused.
        IEnumerator ScrollTest()
        {
            yield return new WaitForSeconds(0.5f);
            var m = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PBTestMouse"); m.MakeCurrent();
            int fails = 0;
            IEnumerator Wheel(float notches, int expect, string what)
            {
                UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent(m.scroll, new Vector2(0, -notches * WheelUnit));
                yield return null; yield return null;
                bool ok = me.Bar.Selected == expect; if (!ok) fails++;
                Debug.Log($"[PB] scroll {what}: slot {me.Bar.Selected + 1} (want {expect + 1}) {(ok ? "ok" : "FAIL")}");
            }
            me.Select(0); yield return null;
            for (int i = 1; i < 8; i++) yield return Wheel(1, i, $"down x{i}");
            yield return Wheel(1, 0, "down past 8 wraps to 1");
            yield return Wheel(-1, 7, "up from 1 wraps to 8");
            yield return Wheel(-1, 6, "up");
            yield return Wheel(3, 1, "3 notches in one frame");
            // locked while fishing: start a cast (Charging locks the bar, as do the minigame, the reel and the reveal)
            me.Select(0); yield return new WaitForSeconds(0.6f);
            Ctl.Bot = true; Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return null;
            bool locked = fish.Locked;
            yield return Wheel(1, 0, $"while charging (locked {locked})");
            Ctl.BotUse = false; Ctl.BotCancel = true; yield return new WaitForSeconds(2.5f); Ctl.BotCancel = false;
            while (fish.State != Fishing.St.Ready) yield return null;
            // line out, waiting for a bite: the wheel switches (and reels in, bait kept)
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return new WaitForSeconds(0.5f); Ctl.BotUse = false;
            while (fish.State != Fishing.St.Waiting) yield return null;
            yield return Wheel(1, 1, "while waiting for a bite (reels in)");
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] scroll: after switching away the line is {(fish.State is Fishing.St.Stowed or Fishing.St.Recover ? "in" : "STILL OUT (" + fish.State + ")")}");
            if (fish.State is not (Fishing.St.Stowed or Fishing.St.Recover)) fails++;
            me.Select(0); yield return new WaitForSeconds(0.6f);
            // a whole catch, with the wheel turned mid-minigame: nothing moves
            bool mgDone = false;
            IEnumerator DuringMinigame()
            {
                while (fish.State != Fishing.St.Minigame) yield return null;
                yield return Wheel(2, 0, "during the minigame"); mgDone = true;
            }
            StartCoroutine(DuringMinigame());
            yield return CastOnce(0.7f); yield return PlayFish(_ => { });
            if (!mgDone) { fails++; Debug.Log("[PB] scroll: minigame never reached"); }
            while (fish.State != Fishing.St.Ready) yield return null;
            me.Select(0); yield return new WaitForSeconds(0.3f);
            Ctl.Bot = false;
            Time.timeScale = 0; yield return Wheel(1, 0, "while paused"); Time.timeScale = 1;
            yield return Wheel(1, 1, "after unpausing");
            Debug.Log($"[PB] RESULT scroll test: {(fails == 0 ? "PASS" : fails + " FAIL")} (wheel unit {WheelUnit})");
            Application.Quit();
        }
        static float WheelUnit => Ctl.WheelNotch;

        // -pb-idleshots <dir>: rod in hand at rest and then with the line out, from 3/4 front at 3 distances (Kufu:
        // the rod must read as a full straight rod with the line hanging from the tip).
        IEnumerator IdleShots()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false); Ctl.Bot = true; me.Select(0);
            yield return new WaitForSeconds(1.5f);
            IEnumerator Three(string what)
            {
                foreach (var d in new[] { 1.8f, 3.5f, 6f })
                {
                    yield return new WaitForEndOfFrame();
                    var ct = CameraRig.I.Cam.transform; var a = me.transform; var keep = (ct.position, ct.rotation);
                    ct.position = a.TransformPoint(new Vector3(-0.7f, 0.25f, 0.7f).normalized * d + Vector3.up * 1.2f);
                    ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0, 1.25f, 0.6f)) - ct.position);
                    Save(Path.Combine(shots, $"idle-{what}-{d:0.0}m.png"));
                    ct.SetPositionAndRotation(keep.position, keep.rotation);
                }
            }
            if (Arg("-pb-norod") != null)
            {
                // empty hands (the arms hang free: do they clear the body?), standing, then walking the deck
                me.Select(1); yield return new WaitForSeconds(1.5f);
                yield return Three("hands");
                Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.6f);
                yield return Three("walk");
                Ctl.BotMove = Vector2.zero;
                Debug.Log("[PB] RESULT idleshots 6 (no rod)");
                Application.Quit(); yield break;
            }
            yield return Three("ready");
            yield return CastOnce(0.6f); while (fish.State != Fishing.St.Waiting) yield return null;
            yield return new WaitForSeconds(1.0f);
            yield return Three("waiting");
            Debug.Log("[PB] RESULT idleshots 6");
            Application.Quit();
        }

        float Off()
        {
            var to = (fish.State is Fishing.St.Charging or Fishing.St.Flight ? fish.Marker : fish.BobPos) - me.transform.position; to.y = 0;
            return Mathf.Abs(Mathf.DeltaAngle(me.transform.eulerAngles.y, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg));
        }

        IEnumerator Film(string dir, float secs, int w = 1920, int h = 1080)
        {
            Directory.CreateDirectory(dir); Time.captureFramerate = 30; int i = 0; Debug.Log($"[PB] film start {Time.time:F3}");
            float end = Time.time + secs;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
                if (filmSide && Angler.I != null)
                {
                    // a fixed 3/4 front view from the angler's left, framing head to feet and the rod
                    var a = Angler.I.transform;
                    ct.position = a.TransformPoint(new Vector3(-2.9f, 1.55f, 2.2f));
                    ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0.1f, 1.15f, 0.3f)) - ct.position);
                }
                Save(Path.Combine(dir, $"f_{i++:00000}{(w < 1920 ? ".jpg" : ".png")}"), w, h);
                ct.SetPositionAndRotation(keep.position, keep.rotation);
            }
            Time.captureFramerate = 0; Debug.Log($"[PB] film {i} frames");
        }

        // ------------------------------------------------------------------ the boat
        IEnumerator BoatTest()
        {
            yield return new WaitForSeconds(0.5f);
            var s = Skiff.I; shots = Arg("-pb-boatshots");
            if (shots != null) Directory.CreateDirectory(shots);
            float maxRoll = 0, maxPitch = 0;
            // at rest, moored: level and floating
            for (float t = 0; t < 5; t += Time.deltaTime) { maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); maxPitch = Mathf.Max(maxPitch, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.x))); yield return null; }
            float draft = s.transform.position.y - Sea.Height(s.transform.position);
            Debug.Log($"[PB] boat moored: max roll {maxRoll:F1}°, pitch {maxPitch:F1}°, height vs sea {draft:F3} m");
            if (shots != null) yield return Shot("boat-moored");
            // drive out: cast off, full, a wide turn
            s.AtHelm = true; s.CastOff(); s.Notch = 3; var start = s.transform.position; maxRoll = 0;
            for (float t = 0; t < 12; t += Time.deltaTime) { s.Steer = t < 3 ? -0.6f : t > 6 ? 0.6f : 0; maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); yield return null; }
            Debug.Log($"[PB] boat drive: {Vector3.Distance(start, s.transform.position):F1} m in 12 s, speed {s.Speed:F2} m/s, max roll {maxRoll:F1}° (Clear limit 6 + turn lean 3)");
            if (shots != null) yield return Shot("boat-driving");
            s.Notch = 0; s.Steer = 0; s.AtHelm = false;
            for (float t = 0; t < 5; t += Time.deltaTime) yield return null;
            s.ToggleAnchor(); for (float t = 0; t < 2; t += Time.deltaTime) yield return null;
            var a = s.transform.position; float far = 0;
            for (float t = 0; t < 30; t += Time.deltaTime) { far = Mathf.Max(far, Vector3.Distance(Flat(a), Flat(s.transform.position))); yield return null; }
            Debug.Log($"[PB] RESULT boat anchored {s.Anchored}: drifted at most {far:F2} m in 30 s (limit 6) -> {(s.Anchored && far <= 6.05f ? "PASS" : "FAIL")}");
            Application.Quit();
        }
        static Vector3 Flat(Vector3 v) => new(v.x, 0, v.z);

        // Jump over the side from the rail, swim to the stern ladder, climb back aboard (Manan's M1 check).
        IEnumerator SwimTest()
        {
            shots = Arg("-pb-swimshots"); if (shots != null) Directory.CreateDirectory(shots);
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true; me.Select(1);
            CameraRig.I.Yaw = me.transform.eulerAngles.y;
            Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.25f);
            Ctl.BotJump = true; yield return null; Ctl.BotJump = false;
            float t = 0; while (!me.Swimming && t < 3) { t += Time.deltaTime; yield return null; }
            Ctl.BotMove = Vector2.zero;
            Debug.Log($"[PB] swim: jumped overboard {(me.Swimming ? "yes" : "NO")} in {t:F2} s");
            yield return new WaitForSeconds(1f);
            if (shots != null) yield return Shot("swim-in-water");
            // swim round to the ladder (the bot steers straight at it)
            t = 0;
            while (t < 25 && Vector3.Distance(Flat(me.transform.position), Flat(s.LadderBottom.position)) > 1.2f)
            {
                var to = s.LadderBottom.position - me.transform.position; to.y = 0;
                CameraRig.I.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up;
                t += Time.deltaTime; yield return null;
            }
            Ctl.BotMove = Vector2.zero;
            Debug.Log($"[PB] swim: reached the ladder in {t:F1} s (prompt: {me.Prompt})");
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            yield return new WaitForSeconds(1f);
            if (shots != null) yield return Shot("swim-back-aboard");
            Debug.Log($"[PB] RESULT swim: back aboard {(me.OnBoat ? "PASS" : "FAIL")} (swimming {me.Swimming})");
            Application.Quit();
        }

        // -pb-laddertest <dir>: in the water by the pier ladder, E: straight up onto the pier (Tushar 1 Nov). Shots before
        // and after; PASS = standing on the pier deck, not swimming.
        IEnumerator LadderTest(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true;
            var inward = l.LadderTop.position - l.LadderBottom.position; inward.y = 0; inward.Normalize();
            float from = float.TryParse(Arg("-pb-ladderfrom"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lf) ? lf : 1.2f;
            var at = l.LadderBottom.position - inward * from; at.y = Sea.Height(at) - Angler.SwimDepth;
            me.Teleport(at, Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg + (float.TryParse(Arg("-pb-ladderyaw"), out var ly) ? ly : 55); CameraRig.I.Pitch = 14;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] ladder: in the water {me.Swimming}, prompt '{me.Prompt}'");
            yield return Shot("ladder-0-water");
            // the camera on the way up (Tushar 2 Nov: from right by the pier ladder it zoomed in and stuttered): how far it
            // sits from her head each frame; it must glide, never snap in
            var cam = CameraRig.I.Cam.transform; float Gap() => Vector3.Distance(cam.position, me.transform.position + Vector3.up * 1.2f * me.Size);
            // filmed at a steady 30 fps (headless runs free at ~1500 fps: no stutter shows), from 0.3 s before the E
            StartCoroutine(Film(Path.Combine(dir, "film"), 2.2f, 960, 540)); yield return new WaitForSeconds(0.3f);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            yield return null;
            float g0 = Gap(), gMin = g0, jerk = 0, turn = 0; string by = null;
            var lastP = cam.position; var lastR = cam.rotation; Vector3 lastV = Vector3.zero;
            var trace = new System.Text.StringBuilder("t,gap,speed,turn,blocker\n");
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                yield return null;
                float dt = Mathf.Max(Time.deltaTime, 1e-3f), g = Gap();
                var v = (cam.position - lastP) / dt; float r = Quaternion.Angle(cam.rotation, lastR) / dt;
                if (t > 0.05f) { jerk = Mathf.Max(jerk, (v - lastV).magnitude / dt); turn = Mathf.Max(turn, r); }
                lastP = cam.position; lastR = cam.rotation; lastV = v;
                if (g < gMin) { gMin = g; by = CameraRig.I.Blocker; }
                trace.Append($"{t:F3},{g:F3},{v.magnitude:F2},{r:F1},{CameraRig.I.Blocker}\n");
            }
            File.WriteAllText(Path.Combine(dir, "camera.csv"), trace.ToString());
            while (Time.captureFramerate != 0) yield return null;
            int n = 1; yield return Shot($"ladder-{n++}-after");
            // a glide: no sudden kick in the camera's speed (a pull-in snaps it), it never swings fast, never squashed in
            bool camOk = jerk < 120f && turn < 120f && gMin > 2.5f;
            Debug.Log($"[PB] RESULT ladder camera: {(camOk ? "PASS" : "FAIL")} (from {from:F1} m: {g0:F2} m from her head on top, closest {gMin:F2} m{(by != null ? " pulled in by " + by : "")}, worst kick {jerk:F0} m/s², fastest turn {turn:F0}°/s)");
            var p = me.transform.position;
            float deckY = l.LadderTop.position.y, inPast = Vector3.Dot(p - l.LadderTop.position, inward);
            bool ok = !me.Swimming && me.Cc.isGrounded && Mathf.Abs(p.y - deckY) < 0.15f && inPast > 0.3f;
            Debug.Log($"[PB] RESULT ladder: {(ok && camOk ? "PASS" : "FAIL")} (swimming {me.Swimming}, grounded {me.Cc.isGrounded}, feet {p.y - deckY:F2} m from the deck, {inPast:F2} m in from the ladder top)");
            Application.Quit();
        }

        // -pb-feeltest <dir> [-pb-side]: the feel spec (replan/feel) measured on the pier's longest straight, filmed:
        // from a stand to a run (0 -> 90% in 0.22 s), the stop (to 10% in 0.18 s), the sprint's run-up (0.6 s), a hard
        // reverse (a skid), a held and a tapped jump (0.8 / 0.45 m after a 0.08 s crouch), then 5 crates' worth of fish
        // (50 kg): start 0.45 s, stop 0.55 s, top speed 2.5 m/s, no sprint. Crossing times are interpolated between frames.
        IEnumerator FeelTest(string dir)
        {
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true; me.Select(1);
            // the longest straight run of level pier through the spawn (8 headings), started from its far end
            var from = me.transform.position; float best = 0; Vector3 axis = Vector3.forward;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45 + l.transform.eulerAngles.y, 0) * Vector3.forward;
                float r = Room(from, d) + Room(from, -d);
                if (r > best) { best = r; axis = d; }
            }
            var start = from - axis * Mathf.Max(0, Room(from, -axis) - 0.6f);
            float axisYaw = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
            me.Teleport(start, axisYaw);
            CameraRig.I.Yaw = axisYaw + 35; // 3/4 behind, so the runs read side-on too
            Debug.Log($"[PB] feel: {best:F1} m of straight pier along {axis}");
            yield return new WaitForSeconds(1f);
            filmSide = Arg("-pb-side") != null; StartCoroutine(Film(dir, 999, 1280, 720));
            yield return new WaitForSeconds(0.5f);
            bool ok = true; float sign = 1;
            void Check(string what, float got, float want, float tol) { bool p = Mathf.Abs(got - want) <= tol; ok &= p; Debug.Log($"[PB] feel {what}: {got:F2} (spec {want:F2} ± {tol:F2}) {(p ? "ok" : "OFF")}"); }
            // the stick for "along the axis" (sign = which way), whatever the camera's yaw
            Vector2 Fwd() { float a = (axisYaw - CameraRig.I.Yaw) * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * sign; }
            float Sp() => new Vector3(me.Vel.x, 0, me.Vel.z).magnitude;
            // time from now until the speed crosses `thr` (up or down), interpolated between frames; -1 after `max` s
            float tOut = -1;
            IEnumerator Until(float thr, bool up, float max)
            {
                float t0 = Time.time, lt = 0, lv = Sp(); tOut = -1;
                while (Time.time - t0 < max)
                {
                    yield return null;
                    float t = Time.time - t0, v = Sp();
                    if (up ? v >= thr : v <= thr) { tOut = lt + (thr - lv) / (v - lv + 1e-6f) * (t - lt); yield break; }
                    lt = t; lv = v;
                }
            }
            IEnumerator StartStop(string tag, float run, float wantStart, float wantStop, float tol)
            {
                Ctl.BotMove = Fwd(); yield return Until(0.9f * run, true, 2); Check(tag + " start 0 -> 90%", tOut, wantStart, tol);
                yield return new WaitForSeconds(0.8f);
                Check(tag + " top speed", Sp(), run, 0.12f);
                Ctl.BotMove = Vector2.zero; yield return Until(0.1f * run, false, 2); Check(tag + " stop to 10%", tOut, wantStop, tol);
                yield return new WaitForSeconds(0.6f); sign = -sign;
            }
            Debug.Log("[PB] feel run " + Time.time);
            yield return StartStop("run", 4.2f, 0.22f, 0.18f, 0.035f);
            // sprint: from a run, the run-up to 90% of the way to 5.8 m/s
            Debug.Log("[PB] feel sprint " + Time.time);
            Ctl.BotMove = Fwd(); yield return new WaitForSeconds(0.6f);
            Ctl.BotSprint = true; yield return Until(4.2f + 0.9f * 1.6f, true, 2); Check("sprint run-up to 90%", tOut, 0.6f, 0.15f);
            yield return new WaitForSeconds(0.4f); Check("sprint top speed", Sp(), 5.8f, 0.12f);
            Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f); sign = -sign;
            // a hard reverse at a run: the skid, then turned round and running the other way
            Debug.Log("[PB] feel skid " + Time.time);
            Ctl.BotMove = Fwd(); yield return new WaitForSeconds(0.9f);
            sign = -sign; Ctl.BotMove = Fwd(); float ts = Time.time; bool skid = false;
            while (Time.time - ts < 1.0f) { skid |= me.Skidding; yield return null; }
            float off = Vector3.Angle(me.transform.forward, axis * sign);
            Debug.Log($"[PB] feel skid: {(skid ? "yes" : "NO")}, then facing {off:F0}° off the new way {(skid && off < 15 ? "ok" : "OFF")}");
            ok &= skid && off < 15;
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f); sign = -sign;
            // jumps, standing: held, then tapped
            foreach (bool held in new[] { true, false })
            {
                Debug.Log($"[PB] feel jump {(held ? "held" : "tap")} " + Time.time);
                float y0 = me.transform.position.y, peak = y0, tp = Time.time, up = -1;
                Ctl.BotJumpHeld = held; Ctl.BotJump = true; yield return null; Ctl.BotJump = false;
                while (Time.time - tp < 2f)
                {
                    float y = me.transform.position.y; peak = Mathf.Max(peak, y);
                    if (up < 0 && y > y0 + 0.005f) up = Time.time - tp;
                    if (up > 0 && me.Cc.isGrounded && Time.time - tp > up + 0.1f) break;
                    yield return null;
                }
                Check((held ? "held" : "tapped") + " jump height", peak - y0, held ? 0.8f : 0.45f, 0.06f);
                if (held) Check("crouch before leaving the ground (+ a frame)", up, 0.08f + Time.deltaTime, 0.04f);
                Ctl.BotJumpHeld = true; yield return new WaitForSeconds(0.8f);
            }
            // 5 crates' worth of fish (50 kg in slot 8)
            Debug.Log("[PB] feel loaded " + Time.time);
            for (int i = 0; i < 5; i++) me.Bar.S[7].Fish.Add(new Fish { Species = FishDb.All[0], Kg = 10 });
            Debug.Log($"[PB] feel load {me.Load:F1} crates");
            yield return StartStop("loaded", 4.2f * (1 - 0.08f * 5), 0.45f, 0.55f, 0.05f);
            Ctl.BotMove = Fwd(); Ctl.BotSprint = true; yield return new WaitForSeconds(1.2f);
            Check("loaded, sprint held: speed", Sp(), 2.52f, 0.12f);
            Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(1f);
            me.Bar.S[7].Fish.Clear();
            Debug.Log($"[PB] RESULT feel: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // How far the pier runs level and clear from `p` along `d` (deck under the feet, nothing in the way), up to 30 m.
        static float Room(Vector3 p, Vector3 d)
        {
            float r = 0;
            for (; r < 30; r += 0.5f)
            {
                var q = p + d * (r + 0.5f);
                if (!Physics.Raycast(q + Vector3.up * 1.2f, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y - p.y) > 0.25f) break;
                bool blocked = false;
                foreach (var h in Physics.SphereCastAll(p + Vector3.up * 0.9f + d * r, 0.35f, d, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider.GetComponentInParent<Angler>() == null) blocked = true;
                if (blocked) break;
            }
            return r;
        }

        // -pb-knocktest <dir>: ragdoll-lite (replan/feel §4), filmed. On the pier, knocked over forward, back and sideways,
        // then off the tiller as a crash would. Each must really go down (hips under 0.45 m off the floor), be up within
        // 4.5 s (or treading water, if it flung her off the edge), stand up where she came to rest (within 1 m of her hips)
        // and walk again.
        IEnumerator KnockTest(string dir)
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; me.Select(1);
            CameraRig.I.Yaw = me.transform.eulerAngles.y + 60;
            yield return new WaitForSeconds(1f);
            StartCoroutine(Film(dir, 999, 1280, 720));
            yield return new WaitForSeconds(0.5f);
            var hips = me.Anim.GetComponent<Ragdoll>().Hips;
            bool ok = true;
            foreach (var way in new[] { "forward", "back", "side", "tiller" })
            {
                if (way == "tiller") { me.TakeHelm(); yield return new WaitForSeconds(1.5f); }
                var a = me.transform; var p0 = a.position; float floor = p0.y;
                Debug.Log($"[PB] knock {way} {Time.time:F2}");
                var push = way switch
                {
                    "forward" => a.forward * 2.5f + Vector3.up * 1.5f,
                    "back" => -a.forward * 2.5f + Vector3.up * 1.5f,
                    "side" => a.right * 2f + Vector3.up * 1.2f,
                    _ => Skiff.I.transform.forward * 4.5f * 0.25f + Vector3.up * 0.8f, // as Skiff: a full-speed crash
                };
                me.Knock(push);
                if (!me.Down) { Debug.Log($"[PB] knock {way}: NOT knocked over"); ok = false; continue; }
                float t = 0, low = 9; var rest = hips.position;
                while (me.Down && t < 6) { low = Mathf.Min(low, hips.position.y - floor); if (me.Limp) rest = hips.position; t += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.3f);
                float moved = Vector3.Distance(Flat(rest), Flat(me.transform.position)), stand = hips.position.y - me.transform.position.y;
                var w0 = me.transform.position; Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.6f); Ctl.BotMove = Vector2.zero;
                float walked = Vector3.Distance(Flat(w0), Flat(me.transform.position));
                float k = me.Size / 0.826f; // the limits were set on Pip: scaled to the body
                bool pass = low < 0.45f * k && t < 4.5f && (moved < 1f * k || me.Swimming) && (stand > 0.55f * k || me.Swimming) && walked > 0.4f;
                ok &= pass;
                Debug.Log($"[PB] knock {way}: hips down to {low:F2} m, up in {t:F2} s, up {moved:F2} m from where she lay, hips {stand:F2} m up after, walked {walked:F2} m{(me.Swimming ? " (in the water)" : "")}{(me.OnBoat ? " (on the boat)" : "")} {(pass ? "ok" : "OFF")}");
                yield return new WaitForSeconds(1f);
                if (me.Swimming || me.OnBoat) { me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, me.transform.eulerAngles.y); yield return new WaitForSeconds(0.8f); }
            }
            // walking off the pier's end into the sea (Falling Into Pool's topple, then treading water)
            var l = Landing.I; var outward = l.LadderBottom.position - l.LadderTop.position; outward.y = 0; outward.Normalize();
            me.Teleport(l.LadderTop.position - outward * 0.8f, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg - 70; yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] knock walk-off {Time.time:F2}");
            Ctl.BotMove = new Vector2(Mathf.Sin(70 * Mathf.Deg2Rad), Mathf.Cos(70 * Mathf.Deg2Rad)) * 0.5f;
            float tw = 0; while (!me.Swimming && tw < 4) { tw += Time.deltaTime; yield return null; }
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] knock walk-off: in the water {(me.Swimming ? "yes" : "NO")} after {tw:F2} s");
            ok &= me.Swimming;
            Debug.Log($"[PB] RESULT knock: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-crewshots <dir>: every crew body on the pier, in the game's light and toon shader: a 3/4 front close-up, the
        // game camera, and the game camera walking (the kit's proof in the real game, not Blender's preview).
        int backFails;
        IEnumerator CrewShots()
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; me.Select(1);
            var names = M1Assets.I.CrewNames ?? new string[0];
            if (Arg("-pb-dumpwater") != null) // debug: what lies across the sea surface round the pier end (the look audit's dark oval)
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var bb = r.bounds; var m = Landing.I.Moor.position; float sea = Sea.Height(m);
                    if (!r.enabled || !r.gameObject.activeInHierarchy || bb.size.x > 60 || Vector2.Distance(new Vector2(bb.center.x, bb.center.z), new Vector2(m.x, m.z)) > 12 || bb.min.y > sea + 0.3f || bb.max.y < sea - 0.3f) continue;
                    Debug.Log($"[PB] water: {r.name} ({r.GetType().Name}) parent {(r.transform.parent ? r.transform.parent.name : "-")} mat {(r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "none")} at {bb.center} size {bb.size}");
                }
            for (int i = 0; i < names.Length; i++)
            {
                me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, Landing.I.Spawn.eulerAngles.y + 180);
                me.Wear(i, me.Colour); CameraRig.I.Yaw = me.transform.eulerAngles.y; CameraRig.I.Pitch = 30;
                yield return new WaitForSeconds(1.2f);
                Debug.Log($"[PB] crew {names[i]}: size {me.Size:F3}");
                { var top = me.Model.TransformPoint(new Vector3(me.HeadC.x, me.HeadHi, me.HeadC.z)).y; float over = fish.RodTip.y - top; Debug.Log($"[PB] crew {names[i]}: the rod on the back: tip {over:F2} m over the head/hat {(over <= 0.25f ? "ok" : "FAIL")}"); backFails += over <= 0.25f ? 0 : 1; }
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var a = me.transform; var keep = (ct.position, ct.rotation);
                float h = me.Size * 1.78f;
                ct.position = a.TransformPoint(new Vector3(-0.9f, 0.62f, 1.6f) * h + Vector3.up * 0.0f);
                ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0, 0.5f * h, 0)) - ct.position);
                Save(Path.Combine(shots, $"crew-{i}-{names[i]}-34.png"));
                ct.SetPositionAndRotation(keep.position, keep.rotation);
                yield return Shot($"crew-{i}-{names[i]}-game");
                Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.7f);
                yield return Shot($"crew-{i}-{names[i]}-walk");
                Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.5f);
            }
            Debug.Log($"[PB] RESULT crewshots {names.Length} bodies; back rod tip under 0.25 m over the head: {(backFails == 0 ? "PASS" : backFails + " FAIL")}");
            Application.Quit();
        }

        // -pb-waitcam <dir>: with the line out, the player orbits the camera round to her front (Tushar 2 Nov: stuck looking
        // down at the water, couldn't tilt up). PASS = from the front the view centres on her (within 15°), and tilting up
        // to 55° actually raises the camera. Shots of both.
        IEnumerator WaitCam()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false); Ctl.Bot = true; me.Select(0);
            yield return new WaitForSeconds(1.5f);
            yield return CastOnce(0.7f); float t = 0; while (fish.State != Fishing.St.Waiting && t < 6) { t += Time.deltaTime; yield return null; }
            var cam = CameraRig.I; bool ok = fish.State == Fishing.St.Waiting;
            cam.Yaw = me.transform.eulerAngles.y + 180; yield return new WaitForSeconds(2f);   // round to her face
            var head = me.transform.position + Vector3.up * me.Size * 1.78f * 0.8f;
            float off = Vector3.Angle(cam.transform.forward, head - cam.transform.position);
            Debug.Log($"[PB] waitcam front: {off:F1}° off her head (state {fish.State})");
            yield return Shot("waitcam-front");
            ok &= off < 15;
            float y0 = cam.transform.position.y - me.transform.position.y;
            cam.Pitch = 55; yield return new WaitForSeconds(1.5f);
            float y1 = cam.transform.position.y - me.transform.position.y;
            Debug.Log($"[PB] waitcam tilt: camera {y0:F2} m -> {y1:F2} m above her at pitch {cam.Pitch:F0}");
            yield return Shot("waitcam-high");
            ok &= y1 > y0 + 1f && fish.State == Fishing.St.Waiting;
            Debug.Log($"[PB] RESULT waitcam: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-gazeshots <dir> (Manan 3 Nov: "looks a little too much down while holding a fishing rod"): every body on
        // Biscuit, rod in hand, then one fish played out. Logs where the face points (degrees below level, from the head's
        // bind pose: CharAnim.FaceDown) in each rod pose, the mean and the lowest of its sway, and saves a profile and a
        // 3/4 close-up of each. PASS = where the eyes aim (rod idle, waiting, bite, the hold-up) the face is never more than
        // 12° off the line to its target, and rod idle / waiting average under 8° down and never dip past 10°.
        // Run it with -pb-fps 60: at batch mode's ~1000 fps the standing-still deck hold never lands (a 2 mm step down a
        // frame), so the jump tuck blends in and bends her over; -pb-gazenoshots measures without the screenshot frames.
        IEnumerator GazeShots()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false); Ctl.Bot = true;
            var names = M1Assets.I.CrewNames ?? new string[0]; bool ok = names.Length > 0;
            var sk = Skiff.I.transform; var keep = sk.InverseTransformPoint(me.transform.position); float keepYaw = me.transform.eulerAngles.y - sk.eulerAngles.y; // boat-relative: she swings on the anchor
            for (int i = 0; i < names.Length; i++)
            {
                me.Select(1); yield return new WaitForSeconds(0.4f); // rod away before the body swap (as F9 does)
                me.Teleport(sk.TransformPoint(keep), sk.eulerAngles.y + keepYaw); me.Wear(i, me.Colour); yield return null; me.Select(0);
                yield return new WaitForSeconds(1.5f);
                var seen = new Dictionary<Fishing.St, (float sum, int n, float lo, float hi)>(); var shot = new HashSet<Fishing.St>();
                IEnumerator Close(string what)
                {
                    yield return new WaitForEndOfFrame();
                    var ct = CameraRig.I.Cam.transform; var a = me.transform; var k = (ct.position, ct.rotation);
                    float h = me.Size * 1.78f; var hd = me.Head.position;
                    foreach (var (tag, off) in new[] { ("side", a.right * 0.75f + a.forward * 0.1f), ("34", a.forward * 0.6f - a.right * 0.45f) })
                    {
                        ct.position = hd + off * h + Vector3.up * 0.05f * h;
                        ct.rotation = Quaternion.LookRotation(hd - Vector3.up * 0.12f * h + a.forward * 0.15f * h - ct.position);
                        Save(Path.Combine(shots, $"gaze-{i}-{names[i]}-{what}-{tag}.png"));
                    }
                    ct.SetPositionAndRotation(k.position, k.rotation);
                }
                bool sampling = true, dipShot = false; int spikes = 0, landing = 0; var airS = new Dictionary<Fishing.St, (float sum, float max)>(); var offMax = new Dictionary<Fishing.St, float>(); bool noShots = Arg("-pb-gazenoshots") != null;
                IEnumerator Sample()
                {
                    while (sampling)
                    {
                        var st = fish.State; float d = me.Anim.FaceDown;
                        float air = me.Anim.AirW; // the jump tuck / landing crouch blended in (a long frame drops her off the rolling deck)
                        if (!float.IsNaN(d) && fish.StateT > 0.4f) { var aw = airS.TryGetValue(st, out var av0) ? av0 : default; airS[st] = (aw.sum + air, Mathf.Max(aw.max, air)); if (air > 0.03f) landing++; }
                        if (!float.IsNaN(d) && fish.StateT > 0.4f)
                        {
                            var s = seen.TryGetValue(st, out var v) ? v : (sum: 0f, n: 0, lo: 99f, hi: -99f);
                            seen[st] = (s.sum + d, s.n + 1, Mathf.Min(s.lo, d), Mathf.Max(s.hi, d));
                            if (st is Fishing.St.Ready or Fishing.St.Waiting or Fishing.St.Bite or Fishing.St.Reveal) offMax[st] = Mathf.Max(offMax.TryGetValue(st, out var om) ? om : 0, Mathf.Abs(d - me.Anim.GazeTo)); // the face against the line to its target
                            if (d > 10f && st is Fishing.St.Ready or Fishing.St.Waiting && spikes++ < 12)
                                Debug.Log($"[PB] gaze dip {names[i]} {st} t {fish.StateT:F2}: face {d:F1}° (before the aim {me.Anim.GazeRaw:F1}°, aim {me.Anim.GazeWant:F1}°), dt {Time.deltaTime * 1000:F0} ms, lean {me.Model.localEulerAngles}, clip {me.Anim.Act?.name}, frame {Time.frameCount}");
                            if (d > 20f && !dipShot && !noShots) { dipShot = true; Debug.Log($"[PB] gaze big dip {names[i]} {st} t {fish.StateT:F2}: {d:F1}° (before the aim {me.Anim.GazeRaw:F1}°), clip {me.Anim.Act?.name}"); yield return Close("dip-" + st.ToString().ToLowerInvariant()); }
                            if (!noShots && fish.StateT > 0.8f && shot.Add(st) && st is Fishing.St.Ready or Fishing.St.Waiting or Fishing.St.Minigame or Fishing.St.Reel or Fishing.St.Reveal)
                                yield return Close(st.ToString().ToLowerInvariant());
                        }
                        yield return null;
                    }
                }
                var sampler = StartCoroutine(Sample());
                yield return new WaitForSeconds(4f); // a rod-idle sway
                yield return CastOnce(0.6f);
                yield return PlayFish(r => { });
                yield return new WaitForSeconds(0.5f);
                sampling = false; StopCoroutine(sampler);
                foreach (var (st, s) in seen)
                {
                    float mean = s.sum / s.n; bool calm = st is Fishing.St.Ready or Fishing.St.Waiting;
                    bool aimed = offMax.TryGetValue(st, out var off);
                    bool good = (!aimed || off <= 12f) && (!calm || (mean >= -1f && mean <= 8f && s.hi <= 10f));
                    ok &= good;
                    var ai = airS.TryGetValue(st, out var av) ? av : default;
                    Debug.Log($"[PB] gaze {names[i]} {st}: face {mean:F1}° down on average, {s.lo:F1}..{s.hi:F1}° (lowest dip {s.hi:F1}°){(aimed ? $", at most {off:F1}° off its target" : "")}{(calm || aimed ? good ? " ok" : " FAIL" : "")}; jump/land blend mean {ai.sum / s.n:F3} max {ai.max:F2}");
                }
                Debug.Log($"[PB] gaze {names[i]}: {landing} frames with the jump tuck / landing blended in over 3%");
                ok &= seen.ContainsKey(Fishing.St.Ready) && seen.ContainsKey(Fishing.St.Waiting);
                while (fish.State != Fishing.St.Ready) yield return null;
            }
            Debug.Log($"[PB] RESULT gazeshots {names.Length} bodies: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-fadetest <dir>: walls between the camera and her fade (feel spec). Behind the shack, then swimming beside the
        // boat with the camera across it: shots, and PASS when the blocker is fading (and nothing fades in the open).
        IEnumerator FadeTest()
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; me.Select(1);
            bool ok = true;
            IEnumerator Look(string name, Vector3 at, Vector3 from, bool swim)
            {
                var y = swim ? Sea.Height(at) - Angler.SwimDepth : at.y;
                if (!swim && Physics.Raycast(at + Vector3.up * 3, Vector3.down, out var g, 6f, ~0, QueryTriggerInteraction.Ignore)) y = g.point.y;
                var dir = at - from; dir.y = 0;
                me.Teleport(new Vector3(at.x, y, at.z), Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                CameraRig.I.Yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; CameraRig.I.Pitch = swim ? 12 : 22;
                yield return new WaitForSeconds(1.5f);
                int n = CameraRig.I.Fading;
                Debug.Log($"[PB] fade {name}: {n} model(s) fading");
                yield return Shot("fade-" + name);
                ok &= n > 0;
            }
            // behind the shack, looking across it
            var shack = Util.Find(Landing.I.transform, "Shack").GetComponent<Renderer>().bounds;
            var away = shack.center - Landing.I.Spawn.position; away.y = 0; away.Normalize();
            yield return Look("shack", shack.center + away * (Mathf.Max(shack.extents.x, shack.extents.z) + 1.2f), shack.center - away * 6, false);
            // in the water beside Biscuit, the camera across the boat
            // the open-water spot round the boat farthest from the pier (nothing overhead), the camera across the boat from it
            var hull = Skiff.I.Hull; var c = Skiff.I.transform.TransformPoint(hull.center); c.y = Skiff.I.transform.position.y;
            Vector3 side = Vector3.zero; float far = -1;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45, 0) * Vector3.forward;
                var q = c + d * 1.7f;
                if (Physics.Raycast(q + Vector3.down * 0.3f, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                float dist = Vector3.Distance(Flat(q), Flat(Landing.I.Spawn.position));
                if (dist > far) { far = dist; side = d; }
            }
            yield return Look("boat", c + side * 1.7f, c - side * 6, true);
            // out in the open on the pier: nothing fades
            me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, 0); CameraRig.I.Pitch = 30;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] fade open: {CameraRig.I.Fading} model(s) fading");
            ok &= CameraRig.I.Fading == 0;
            yield return Shot("fade-open");
            Debug.Log($"[PB] RESULT fade: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-camtest N: N casts from the pier at 60 fps, the camera's turn per frame measured round each release
        // (Tushar 1 Nov: "the screen stutters when I cast"). A snap is one frame turning far more than its neighbours.
        IEnumerator CamTest(int n)
        {
            Application.targetFrameRate = 60;
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true;
            var outward = l.LadderBottom.position - l.LadderTop.position; outward.y = 0;
            me.Teleport(l.LadderTop.position - outward.normalized * 0.6f, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
            me.Select(0); yield return new WaitForSeconds(1f);
            StartCoroutine(CamWatch());
            for (int i = 0; i < n; i++)
            {
                CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + (i - n / 2) * 12; // the test turns the view: not counted
                yield return new WaitForSeconds(0.8f);
                camWorst = camJerk = camMove = 0; camJerkState = "";
                yield return CastOnce(0.3f + 0.65f * i / Mathf.Max(1, n - 1));
                while (fish.State is Fishing.St.Flight or Fishing.St.Settle) yield return null;
                yield return new WaitForSeconds(0.5f);
                Ctl.BotCancel = true; float h = 0; while (fish.State != Fishing.St.Ready && h < 4) { h += Time.deltaTime; yield return null; }
                Ctl.BotCancel = false; yield return new WaitForSeconds(0.6f);
                Debug.Log($"[PB] camtest cast {i}: biggest turn in one frame {camWorst:F2} deg, biggest jump in turn speed {camJerk:F2} deg/frame (in {camJerkState}), biggest move in one frame {camMove:F2} m");
                worstAll = Mathf.Max(worstAll, camWorst); jerkAll = Mathf.Max(jerkAll, camJerk);
            }
            Debug.Log($"[PB] RESULT camtest: {(jerkAll < 1.0f ? "PASS" : "FAIL")} (worst one-frame turn {worstAll:F2} deg, worst jump in turn speed {jerkAll:F2} deg/frame, 60 fps)");
            Application.Quit();
        }
        float camWorst, camJerk, camMove, worstAll, jerkAll; string camJerkState;

        // Every frame after the camera moved: how far it turned, and how much that differs from the frame before.
        IEnumerator CamWatch()
        {
            var cam = CameraRig.I.transform; var last = cam.forward; var lastP = cam.position; float lastA = -1;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                float a = Vector3.Angle(last, cam.forward); last = cam.forward;
                camWorst = Mathf.Max(camWorst, a); camMove = Mathf.Max(camMove, Vector3.Distance(lastP, cam.position)); lastP = cam.position;
                if (lastA >= 0 && Mathf.Abs(a - lastA) > camJerk) { camJerk = Mathf.Abs(a - lastA); camJerkState = fish.State + (CameraRig.I.Blocker != null ? ", camera pulled in by " + CameraRig.I.Blocker : ""); }
                lastA = a;
            }
        }
    }
}
