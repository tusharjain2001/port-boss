using System;
using System.Collections.Generic;
using System.Globalization;

namespace PortBoss.M1
{
    // M7 step 1: the story driver (gdd world §4 the acts, §7 the phone as the story channel, §9 the first hour).
    // Plain C# in the company like M4-M6. So the host (or solo) runs it on its clock, the crew get it in the company blob
    // (CoopFirm "pb.co"), it saves with the world, and tools/companycheck runs StoryTest outside Unity.
    // - Beats: one table of story moments. Each fires once when its time (and its condition) comes: a text, a letter, a
    //   call, a journal page, an act. A beat has a window. If a sleep skip jumps past it, or its condition is false when it
    //   comes due, it lapses unseen instead of firing hours late (no "lights out here" at 7 AM).
    // - Events: what the world tells the story ("rod", "cast", "sold", "fixed:rep:biscuit:engine"). Only the first time
    //   counts, with when it happened. A crewmate's machine sends its events to the host (CoopFirm "pb.ev").
    // - Acts (world §4.0; the gates are soft, nothing times out): Act 1 = the Prologue + The Mooring. Act 2 begins at 6 PM
    //   on Day 4 at the earliest, once Act 1's goal is met: Biscuit's engine rebuilt; Mari moored, or she gave up; Amara's
    //   first order delivered. It begins on Day 5 at 6 PM at the latest. Act 3 on is M9's (the rival).
    // - Calls ring on every machine. Each player answers or misses theirs (StoryUi). The transcript is the same for all.
    public partial class Company
    {
        // M7 step 5: the Steam demo (Tushar 8 Nov: it ends on the evening of Day 4, as Act 2 begins). The game sets it from
        // the demo build's marker (StreamingAssets/demo) or -pb-demo; the full game plays on.
        public static bool Demo;
        public const string UnknownName = "Unknown number";   // (M8: Kai Rennick is KaiName, Surf.cs)
        public const string WaltName = "Grandpa Walt", JuneName = "June Pike", PrattName = "Gideon Pratt", FergusName = "Fergus Doyle", IngridName = "Ingrid Marsh", DotName = "Dot";

        public class Letter { public int Id; public string From, Title, Where = "mail"; public string[] Lines; public double At; }
        public class Call { public int Id; public string From; public string[] Lines; public double At; }
        public class Page { public int Id; public string Key, Title, Text, Keys; public double At; }   // Keys: "LMB|hold to cast;Space|stop the bar"

        public readonly Dictionary<string, double> BeatAt = new();    // beat id -> when it fired (-1: it lapsed unseen)
        public readonly Dictionary<string, double> Events = new();    // world event -> the first time it happened
        public readonly List<Letter> Letters = new();
        public readonly List<Call> Calls = new();
        public readonly List<Page> Pages = new();
        public double ActAt;                                          // when the current act began
        int storyNext = 1; bool storyBusy; double storyNow;

        // Pratt's morning price list (the game sets it from FishDb; the default is the gdd's Day 2 line)
        public static Func<int, string> PriceList = day => "Sprat $4 · Mullet $5 · Mackerel $6";

        public bool Seen(string ev) => Events.ContainsKey(ev);
        public double EvAt(string ev, double plus = 0) => Events.TryGetValue(ev, out var t) ? t + plus : double.NaN;
        public bool Fired(string beat) => BeatAt.TryGetValue(beat, out var t) && t >= 0;
        public bool EngineFixed => Boats.TryGetValue("biscuit", out var b) && b.Engine;
        public bool HullFixed => Boats.TryGetValue("biscuit", out var b) && b.Hull;
        public bool Act1Done => EngineFixed && (Mari == DealSt.Active || Mari == DealSt.GaveUp) && Seen("amara:1");

        // the world tells the story something happened; false when it had already (only the first time counts)
        public bool Event(string ev, double m)
        {
            if (string.IsNullOrEmpty(ev) || Events.ContainsKey(ev)) return false;
            Events[ev] = m; TickStory(m); return true;
        }

        public void SetAct(int act, double m)
        {
            if (act == Act) return;
            Act = act; ActAt = m;
        }

        // ---- the story's channels
        public Call Ring(string from, double m, params string[] lines) { var c = new Call { Id = storyNext++, From = from, Lines = lines, At = m }; Calls.Add(c); return c; }
        public Letter Post(string from, string title, double m, string where, params string[] lines) { var l = new Letter { Id = storyNext++, From = from, Title = title, Lines = lines, At = m, Where = where }; Letters.Add(l); return l; }
        public Page Note(string key, string title, string text, string keys, double m)
        {
            var had = Pages.Find(p => p.Key == key); if (had != null) return had;
            var p = new Page { Id = storyNext++, Key = key, Title = title, Text = text, Keys = keys, At = m }; Pages.Add(p); return p;
        }

        // (co-op host) a friend joined: Walt welcomes each one by name, once (world §7.2's co-op variant)
        public void StoryJoin(string name, int crew, double m)
        {
            string id = "join:" + name; if (BeatAt.ContainsKey(id)) return; BeatAt[id] = m;
            Text(WaltName, crew <= 2 ? $"Who's this then?? {name}? Any friend of my grandkid eats at my table. Dont let them steer"
                                     : $"How many of you ARE there. Dot says the boat's sitting low. Hello {name}", m);
        }

        // ---- the beats (world §7.2's threads, in the game's own rules: Basic/Rare, Mari on Day 3 or at $250 earned)
        class Beat
        {
            public string Id; public Func<Company, double> Due; public Func<Company, bool> If; public double Window = 180;
            public Action<Company, double> Run;
        }
        static double At(int day, int h, int min = 0) => DayTime.At(day, h * 60 + min);
        const double Forever = 1e12;

        static readonly List<Beat> Beats = new()
        {
            // Day 1, 07:00: Walt's letter on the desk (world §4.1 beat 1: six lines; the opening opens it, StoryUi)
            new Beat { Id = "walt:letter:0", Due = c => At(1, 7), Window = 24 * 60, Run = (c, m) =>
                c.Post(WaltName, "Walt's letter, on the desk", m, "desk",
                    "Kid,",
                    "The key's on the cork float. The office is yours, the pier is yours, and so is Biscuit. She still floats. Mostly.",
                    "Bail her out before you start her, or she sulks. Three good pulls on the cord.",
                    "Fish the bay. Sell to Pratt if you must, then get Fergus to look at her engine.",
                    "I'll text. The nurse says I text too much.",
                    "—W.") },
            // Day 1: Walt's first texts (07:02), Biscuit's name, the leak
            new Beat { Id = "walt:hello", Due = c => At(1, 7, 2), Window = 24 * 60, Run = (c, m) =>
            {
                c.Text(WaltName, "Morning!! Did the key work. The door sticks pull up then push", m);
                c.Text(WaltName, "The boat is called Biscuit. She leaks. Bail her first or you will be swiming", m);
                c.Text(WaltName, "*swimming. The nurse says I have to proof read", m);
            } },
            // the first time the rod comes up: Walt's journal, page 1 (the side card, never a tutorial screen)
            new Beat { Id = "page:cast", Due = c => c.EvAt("rod"), Window = Forever, Run = (c, m) =>
                c.Note("cast", "Page 1 · The float", "Fling it out where the water's dark. Watch the float. When she dunks, strike. Then stop the needle in the green, and don't be greedy.",
                       "LMB|hold to cast, let go;LMB|strike on the dunk;Space|stop the needle", m) },
            // the engine catches for the first time (SkiffStart: the third pull)
            new Beat { Id = "walt:engine", Due = c => c.EvAt("engine"), Window = 24 * 60, Run = (c, m) =>
                c.Text(WaltName, "Is that my Biscuit I can hear from here?? (I cant. But I can feel it.)", m) },
            new Beat { Id = "walt:cast", Due = c => c.EvAt("cast", 2), Window = 12 * 60, Run = (c, m) =>
                c.Text(WaltName, "Dot says she can see you from the lighthouse. Wave", m) },
            new Beat { Id = "walt:lowball", Due = c => At(1, 13), If = c => !c.Seen("sold"), Run = (c, m) =>
                c.Text(WaltName, "Pratt'll lowball you. Sell a few anyway, the engine wont fix itself", m) },
            new Beat { Id = "walt:sale", Due = c => c.EvAt("sold", 1), Window = 24 * 60, Run = (c, m) =>
            {
                c.Text(WaltName, "Pratt gave you 60 cents on the dollar didnt he. He gave me 6 for a mackerel in 1998. Some things never change except the fish", m);
                c.Text(WaltName, "Write this down. THE PELICAN. Lantern Key. Amara pays real money. Bring her good fish only", m);
            } },
            new Beat { Id = "walt:night1", Due = c => At(1, 21), Window = 150, Run = (c, m) =>
                c.Text(WaltName, "lights out here. Dont stay up", m) },
            // M7 step 4: June's shrimp (Day 1, 6 PM: it's in on Day 2)
            new Beat { Id = "june:shrimp", Due = c => At(1, 18), Window = 4 * 60, Run = (c, m) =>
                c.Text(JuneName, "Shrimp in stock tomorrow. Mackerel like shrimp. So does everyone, so come early", m) },
            // Mari ties up (an hour after yes): her line to catch (StoryUi: E at her bow) or she ties it herself
            new Beat { Id = "mari:here", Due = c => c.Mari == DealSt.Active ? c.MariAccepted + MariComes : double.NaN, Window = 24 * 60, Run = (c, m) =>
                c.Text(MariName, "here! can someone grab my line??", m) },
            new Beat { Id = "mari:tied", Due = c => c.EvAt("mari:tied"), Window = 24 * 60, Run = (c, m) =>
                c.Text(MariName, "thank youuu! ok this pier is... vintage. love it though", m) },
            new Beat { Id = "mari:self", Due = c => c.Fired("mari:here") && !c.Seen("mari:tied") ? c.BeatAt["mari:here"] + 12 : double.NaN, If = c => !c.Seen("mari:tied"), Run = (c, m) =>
                c.Text(MariName, "got it myself, no worries! this pier is... vintage. love it though", m) },
            new Beat { Id = "mari:day4", Due = c => At(4, 21, 40), If = c => c.MariHere(At(4, 21, 40)), Window = 120, Run = (c, m) =>
                c.Text(MariName, "people keep asking who owns the pier. I say \"the Hales are back\" and they get weird and smiley", m) },
            // Day 3, 7 PM: Ingrid's first dock job (Plank & Post: three planks, a bench thrown in)
            new Beat { Id = "ingrid:planks", Due = c => At(3, 19), If = c => !c.Has("dock:planks") && !c.Ordered("dock:planks"), Window = 3 * 60, Run = (c, m) =>
                c.Text(IngridName, "Saw your pier from the causeway. Three planks are rotten through. $100 and I'll fix them overnight, and I'll throw in a bench. Plank & Post, by the market.", m) },
            // what was seen in the world (StoryUi plays them where you are): the conger at night, the swordfish, Voss's yacht
            new Beat { Id = "june:conger", Due = c => c.EvAt("conger", 1), Window = 12 * 60, Run = (c, m) =>
                c.Text(JuneName, "Was that a conger under Walt's pier? Not on Walt's bamboo, love. Fibreglass.", m) },
            new Beat { Id = "dot:swordfish", Due = c => c.EvAt("swordfish", 1), Window = 12 * 60, Run = (c, m) =>
                c.Text(DotName, "Saw that leap from here. Swordfish. Not on that rod.", m) },
            new Beat { Id = "dot:yacht", Due = c => c.EvAt("yacht", 2), Window = 12 * 60, Run = (c, m) =>
                c.Text(DotName, "That's Voss's yacht. Efficiency. Don't wave. He won't.", m) },
            // M7 step 5, the demo only: 45 s into Act 2 (after Hugo & Fifi's request) a number you don't know texts the
            // goons' line (world §7.2's first rival texts, Act 3 in the full game); 12 s later the demo ends (StoryUi's card)
            new Beat { Id = "demo:unknown", Due = c => Demo && c.Act >= 2 && c.Fired("act:2") ? c.ActAt + 45 : double.NaN, Window = Forever, Run = (c, m) =>
            {
                c.Text(UnknownName, "nice dock. be a shame", m);
                c.Text(UnknownName, "sorry", m);
                c.Text(UnknownName, "ok", m);
            } },
            new Beat { Id = "demo:end", Due = c => c.Fired("demo:unknown") ? c.BeatAt["demo:unknown"] + 12 : double.NaN, Window = Forever, Run = (c, m) => c.Event("demo:end", m) },
            // Days 2-3: Pratt's price list at 7, Walt's call at 8, Fergus about the hull
            new Beat { Id = "pratt:prices:2", Due = c => At(2, 7), Window = 240, Run = (c, m) =>
                c.Text(PrattName, $"Good morning. Today at the Fish Market: {PriceList(2)}. Thank you for choosing Voss Maritime.", m) },
            new Beat { Id = "walt:call:2", Due = c => At(2, 8), Window = 240, Run = (c, m) =>
                c.Ring(WaltName, m,
                    "You sound tired. Good. Tired is honest.",
                    "Listen. The Rare ones, you feel them in your wrist before the needle's even in the green.",
                    "Your grandmother could land three in a row with her eyes shut.",
                    "...",
                    "Go on then. Ring me when you've got something to brag about.") },
            // Day 2, 11:30: Amara's first order (Orders.cs); delivered: Walt that evening (world §7.2: Day 3 19:30)
            new Beat { Id = "amara:order:1", Due = c => At(2, 11, 30), If = c => c.FishOrders.Count == 0, Window = 2 * 24 * 60, Run = (c, m) => c.OfferAmara(m) },
            new Beat { Id = "walt:amara", Due = c => c.Seen("amara:1") ? Math.Max(c.EvAt("amara:1", 30), DayTime.At(DayTime.Day(c.EvAt("amara:1")), 19 * 60 + 30)) : double.NaN, Window = 24 * 60, Run = (c, m) =>
                c.Text(WaltName, "AMARA CALLED ME. She said \"your grandkid has hands\". Thats the nicest thing shes ever said about a Hale", m) },
            new Beat { Id = "fergus:hull", Due = c => At(2, 10), If = c => !c.HullFixed, Run = (c, m) =>
                c.Text(FergusName, "Hull's still weeping. $120 and you can put the bucket away.", m) },
            new Beat { Id = "pratt:prices:3", Due = c => At(3, 7), Window = 240, Run = (c, m) =>
                c.Text(PrattName, $"Good morning. Today: {PriceList(3)}. Supply is high. Thank you for choosing Voss Maritime.", m) },
            // whenever Doyle's rebuild is done (the morning after, or rushed)
            new Beat { Id = "fergus:engine", Due = c => c.EvAt("fixed:rep:biscuit:engine"), Window = 12 * 60, Run = (c, m) =>
                c.Text(FergusName, "She's done. Full speed again. Don't thank me, thank Walt.", m) },
            // Day 4, 07:00: Walt's letter with the photo (world §7.4), in the mailbox
            new Beat { Id = "walt:letter:4", Due = c => At(4, 7), Window = 3 * 24 * 60, Run = (c, m) =>
                c.Post(WaltName, "Walt's letter, with a photo", m, "mail",
                    "Kid,",
                    "That's the Landing in '81 in the photo. Forty boats. The cannery whistle at six. Your grandmother selling coffee off the end of the pier like she owned the place (she did).",
                    "I'm not asking you to bring that back. I'm asking you to have one good day out there. Then another one.",
                    "The Cut was deep enough for a freighter once. Ask Dot about that sometime.",
                    "—W.",
                    "P.S. Don't let Pratt rob you. He's not clever, he's just the only one there.") },
            // Act 2: Act 1's goal met, from Day 4 at 6 PM; Day 5 at 6 PM at the latest
            new Beat { Id = "act:2", Due = c => c.Act1Done ? At(4, 18) : At(5, 18), If = c => c.Act < 2, Window = Forever, Run = (c, m) =>
            {
                c.SetAct(2, m);
                if (c.Hugo == DealSt.None) c.OfferHugo(m);                 // M7 step 4: the second moorers open Act 2 (Moorers.cs)
                c.Text(WaltName, "Rosie at the Mooring Line rang me. Says someone keeps asking her for work. Go and see her board kid. Two pairs of hands catch more than one", m);
            } },
            // M8 step 4: Kai, the morning after Act 2 begins (world §10.3's hook; he texts at dawn and dusk only)
            new Beat { Id = "kai:hello", Due = c => c.Act >= 2 && c.ActAt >= 0 ? DayTime.At(DayTime.Day(c.ActAt) + 1, 6 * 60 + 30) : double.NaN, Window = 3 * 24 * 60, Run = (c, m) =>
            {
                c.Text(KaiName, "hey, Kai here. Swell Surf Co, down by the Cut", m);
                c.Text(KaiName, "Walt says you're good in the water. there's a foam board at the shop with your name on it. free. come grab it", m);
                c.Text(KaiName, "I text a surf report most mornings. no surf, no text", m);
            } },
        };

        public void TickStory(double m)
        {
            if (storyBusy) return; storyBusy = true; storyNow = m;
            try
            {
                foreach (var b in Beats)
                {
                    if (BeatAt.ContainsKey(b.Id)) continue;
                    double due = b.Due(this); if (double.IsNaN(due) || m < due) continue;
                    if (m > due + b.Window || (b.If != null && !b.If(this))) { BeatAt[b.Id] = -1; continue; }
                    BeatAt[b.Id] = m; b.Run(this, m);
                }
            }
            finally { storyBusy = false; }
        }

        // ---- invariants: "" when they hold
        public string CheckStory()
        {
            var ids = new HashSet<int>();
            foreach (var l in Letters) if (!ids.Add(l.Id) || l.Id >= storyNext) return $"letter id {l.Id}";
            foreach (var c in Calls) if (!ids.Add(c.Id) || c.Id >= storyNext) return $"call id {c.Id}";
            foreach (var p in Pages) if (!ids.Add(p.Id) || p.Id >= storyNext) return $"page id {p.Id}";
            var keys = new HashSet<string>(); foreach (var p in Pages) if (!keys.Add(p.Key)) return $"page {p.Key} twice";
            if (Act < 1 || Act > 5) return $"act {Act}";
            return "";
        }

        // ---- save
        void WriteStory(Json.W w)
        {
            w.K("story").Obj().K("next").V(storyNext).K("actAt").V(ActAt);
            void Map(string k, Dictionary<string, double> d) { w.K(k).Obj(); var ks = new List<string>(d.Keys); ks.Sort(StringComparer.Ordinal); foreach (var x in ks) w.K(x).V(d[x]); w.End(); }
            void Lines(string[] l) { w.K("lines").Arr(); foreach (var s in l) w.V(s); w.End(); }
            Map("beats", BeatAt); Map("events", Events);
            w.K("letters").Arr(); foreach (var l in Letters) { w.Obj().K("id").V(l.Id).K("from").V(l.From).K("title").V(l.Title).K("where").V(l.Where).K("at").V(l.At); Lines(l.Lines); w.End(); } w.End();
            w.K("calls").Arr(); foreach (var c in Calls) { w.Obj().K("id").V(c.Id).K("from").V(c.From).K("at").V(c.At); Lines(c.Lines); w.End(); } w.End();
            w.K("pages").Arr(); foreach (var p in Pages) w.Obj().K("id").V(p.Id).K("key").V(p.Key).K("title").V(p.Title).K("text").V(p.Text).K("keys").V(p.Keys).K("at").V(p.At).End(); w.End();
            w.End();
        }
        void ReadStory(Dictionary<string, object> o)
        {
            var s = Json.O(o, "story"); if (s == null) return;           // a save from before M7: the story starts where the clock is
            storyNext = Json.I(s, "next", 1); ActAt = Json.D(s, "actAt");
            void Map(string k, Dictionary<string, double> d) { var m = Json.O(s, k); if (m != null) foreach (var kv in m) d[kv.Key] = Convert.ToDouble(kv.Value, CultureInfo.InvariantCulture); }
            static string[] Lines(Dictionary<string, object> e) => Json.L(e, "lines").ConvertAll(x => (string)x).ToArray();
            Map("beats", BeatAt); Map("events", Events);
            foreach (Dictionary<string, object> e in Json.L(s, "letters")) Letters.Add(new Letter { Id = Json.I(e, "id"), From = Json.S(e, "from"), Title = Json.S(e, "title"), Where = Json.S(e, "where", "mail"), At = Json.D(e, "at"), Lines = Lines(e) });
            foreach (Dictionary<string, object> e in Json.L(s, "calls")) Calls.Add(new Call { Id = Json.I(e, "id"), From = Json.S(e, "from"), At = Json.D(e, "at"), Lines = Lines(e) });
            foreach (Dictionary<string, object> e in Json.L(s, "pages")) Pages.Add(new Page { Id = Json.I(e, "id"), Key = Json.S(e, "key"), Title = Json.S(e, "title"), Text = Json.S(e, "text"), Keys = Json.S(e, "keys"), At = Json.D(e, "at") });
        }

        // ---------------------------------------------------------------- the story suite (companycheck + EditMode): "" or what broke
        public static string StoryTest()
        {
            int Count(Company c, string from) => c.Msgs.FindAll(x => x.From == from && !x.Me).Count;
            // 1) Day 1: nothing before 07:02; Walt's 3 texts at 07:02, once
            var c = new Company(); c.Tick(At(1, 7, 1));
            if (Count(c, WaltName) != 0) return "Walt texted before 07:02";
            c.Tick(At(1, 7, 2)); c.Tick(At(1, 7, 3)); if (Count(c, WaltName) != 3) return $"expected Walt's 3 morning texts, got {Count(c, WaltName)}";
            // 2) the rod: page 1 once; the first cast: Walt 2 minutes later; a second cast changes nothing
            c.Event("rod", At(1, 7, 30)); c.Event("rod", At(1, 7, 40));
            if (c.Pages.Count != 1 || c.Pages[0].Key != "cast") return "page 1 (the float) should come with the rod, once";
            c.Event("cast", At(1, 8)); c.Tick(At(1, 8, 1)); if (Count(c, WaltName) != 3) return "Walt's cast text came early";
            c.Tick(At(1, 8, 2)); if (Count(c, WaltName) != 4) return "no 'wave' text 2 minutes after the first cast";
            c.Event("cast", At(1, 9)); c.Tick(At(1, 9, 5)); if (Count(c, WaltName) != 4) return "a second cast texted again";
            // 3) the first sale (Company.Sell raises "sold"): Walt's two texts a minute later; 13:00's "Pratt'll lowball you" lapses
            var mk = new Lot { Species = "mackerel", Everyday = true, Value = 10, Fresh = 0 };
            if (c.Sell("s1", "Pip", Buyer.Pratt, new[] { mk }, At(1, 11), out _) == null) return "the sale was refused";
            if (!c.Seen("sold") || !c.Seen("sold:Pratt")) return "a sale should raise sold + sold:Pratt";
            c.Tick(At(1, 11, 1)); if (Count(c, WaltName) != 6) return $"expected Walt's 2 texts after the first sale (have {Count(c, WaltName)})";
            c.Tick(At(1, 13, 5)); if (Count(c, WaltName) != 6 || c.BeatAt["walt:lowball"] != -1) return "the lowball text should lapse once you've sold";
            // 4) a save mid-day round-trips the story exactly
            var j1 = c.ToJson(); var back = Read((Dictionary<string, object>)Json.Parse(j1));
            if (back.ToJson() != j1) return "the save round trip changed the story";
            if (back.Pages.Count != 1 || back.Events["cast"] != At(1, 8)) return "the loaded story lost a page or an event";
            back.Tick(At(1, 13, 30)); if (Count(back, WaltName) != 6) return "a loaded story repeated a beat";
            // 5) sleeping at 20:30 skips "lights out" (it lapses: no 9 PM text at 7 AM); the 07:00 price list still comes
            c.Tick(At(1, 20, 30)); c.Tick(At(2, 7));
            if (c.BeatAt["walt:night1"] != -1) return "lights out should lapse when you slept through it";
            if (Count(c, PrattName) != 1) return "no price list at Day 2 07:00";
            // 6) Day 2: Walt calls at 08:00 (one call, his 5 lines); Fergus at 10:00 only while the hull leaks
            c.Tick(At(2, 8)); if (c.Calls.Count != 1 || c.Calls[0].From != WaltName || c.Calls[0].Lines.Length != 5) return "Walt's Day 2 call";
            var h = new Company(); h.BoatOf("biscuit").Hull = true; h.Tick(At(2, 10, 1)); if (Count(h, FergusName) != 0) return "Fergus texted about a hull that's patched";
            c.Tick(At(2, 10, 1)); if (Count(c, FergusName) != 1) return "Fergus's hull text";
            // 7) Day 4, 07:00: Walt's letter in the mailbox, once
            if (c.Letters.Count != 1 || c.Letters[0].Where != "desk" || c.Letters[0].Lines.Length != 6) return "Walt's letter on the desk, Day 1 07:00 (six lines)";
            c.Tick(At(4, 7)); c.Tick(At(4, 8)); if (c.Letters.Count != 2 || c.Letters[1].Where != "mail" || c.Letters[1].Lines[0] != "Kid,") return "Walt's Day 4 letter";
            // 8) the acts: nothing done -> Act 2 on Day 5 at 18:00, not before; Act 1's goal met -> Day 4 at 18:00
            c.Tick(At(5, 17, 59)); if (c.Act != 1) return "Act 2 came early";
            c.Tick(At(5, 18)); if (c.Act != 2 || c.ActAt != At(5, 18)) return "Act 2 should begin on Day 5 at 6 PM at the latest";
            if (!c.Msgs.Exists(x => x.From == WaltName && x.Text.StartsWith("Rosie"))) return "no Rosie text with Act 2";
            var g = new Company(); g.BoatOf("biscuit").Engine = true; g.Mari = DealSt.Active; g.Event("amara:1", At(3, 19));
            g.Tick(At(4, 17, 59)); if (g.Act != 1) return "Act 2 before Day 4 6 PM";
            g.Tick(At(4, 18)); if (g.Act != 2) return "Act 1's goal met: Act 2 should begin on Day 4 at 6 PM";
            var f = new Company { Act = 2 }; f.Tick(At(6, 9)); if (f.Act != 2 || f.Fired("act:2")) return "a tester's Act 2 (F7) should not run the act beat again";
            // 8b) the engine's first catch: Walt's text, once
            var e1 = new Company(); e1.Tick(At(1, 9)); int w0 = Count(e1, WaltName);
            e1.Event("engine", At(1, 9, 30)); e1.Event("engine", At(1, 11)); e1.Tick(At(1, 12));
            if (Count(e1, WaltName) != w0 + 1 || !e1.Msgs[^1].Text.StartsWith("Is that my Biscuit")) return "Walt's 'Is that my Biscuit' text when she first catches";
            // 9) a sleep across many days (an old save loaded on Day 10): every timed beat lapses or fires once; never twice
            var o = new Company(); o.Tick(At(10, 7)); o.Tick(At(10, 8));
            if (o.Act != 2 || o.Letters.Count != 0 || o.Calls.Count != 0) return $"a Day 10 world: act {o.Act}, {o.Letters.Count} letters, {o.Calls.Count} calls (the old beats should lapse)";
            // 10) co-op joins: one welcome per friend
            var j = new Company(); j.StoryJoin("Rani", 2, At(1, 9)); j.StoryJoin("Rani", 2, At(1, 9, 5)); j.StoryJoin("Bo", 3, At(1, 10));
            if (Count(j, WaltName) != 2) return "one welcome per friend";
            if (c.CheckStory() != "") return c.CheckStory();
            return "";
        }
    }
}
