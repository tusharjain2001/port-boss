using System;
using System.Collections.Generic;
using System.Globalization;

namespace PortBoss.M1
{
    // M5 "Shops, upgrades and the wardrobe" (systems M5, §1.1.6, §1.9, §4.8.2; economy §4.0, §4.3, §4.4, §4.6, §8;
    // CHANGES row 3): what the three M5 shops sell and what owning it means, as plain C# (no Unity) so the rules run in
    // tools/companycheck and the EditMode suite.
    // - Everything bought installs itself in its one set spot (Ware.Spot): furniture, beds and paint at once, builds
    //   (warehouse, office, berths, lamps, planks) overnight: order before Ingrid's 18:00 close, built by 07:00 (big
    //   builds take Ware.Nights nights). Doyle's repairs are ready at 07:00, or done now at twice the price.
    // - Paint: 4 channels per boat (Hull, Stripe, Trim, Deck), a colour from 24 swatches each (the stripe also has a
    //   pattern of 8). Doyle's crew walks over and paints it (PaintJob, timed in game minutes so every player sees the
    //   same stroke). Going back to the last colour you owned is free.
    // - Clothes: per player (their Kit): the crew member (one each in co-op), the 6 slots worn and a dye per slot, the
    //   clothes they own. Swapping between owned items is free; Hartley's sells them; dye $25 per item.
    // - The warehouse: company storage, slot = 10 fish of one species + grade; the cold room slows rot to 25%.
    // - Ask the crew (economy §8): in co-op, a buy above min($5,000, 25% of the balance) is a request; any one other
    //   player's OK (or the host's) approves it, a no cancels it (nothing spent); 60 s with no answer approves it when
    //   nobody else is online to ask.
    public enum Store { Doyle, Plank, Hartley }
    public enum Fit { Now, Overnight, Paint, Wear, Boat, Repair, Dye }

    public sealed class Ware
    {
        public string Id, Name, Cat, Sub, Spot, Needs, Lock, Line;   // Lock: why it's not for sale yet ("Act 3"), shown greyed
        public Store At; public Fit Fit; public int Price, Act = 1, Nights = 1, Slot = -1, Ch = -1;
        public string Boat;                                          // Doyle: the boat it is for ("biscuit", "kestrel")
        public string[] Stats = new string[0];                       // the right-pane stat lines ("Speed  4.5 → 6 m/s")
    }

    public static class Catalog
    {
        public static readonly List<Ware> All = new();
        static readonly Dictionary<string, Ware> byId = new();
        public static Ware Get(string id) => id != null && byId.TryGetValue(id, out var w) ? w : null;
        public static IEnumerable<Ware> Of(Store s) { foreach (var w in All) if (w.At == s) yield return w; }

        // 24 swatches (Ponyo Coast, warm and bright; boats and dyes share them)
        public static readonly string[] Swatch = {
            "F3EBDD", "FFFFFF", "2E3A4A", "6B4A35", "C0392B", "E2574C", "F08A4B", "F6B93B",
            "F9E07F", "8FCB6B", "3E8E5E", "2F6E5A", "5FC4C0", "2F8FB0", "3A86E0", "1F4E8C",
            "8E7CC3", "C77DB5", "F2A7B8", "D9CBB0", "9C8F7A", "5E5035", "1C1C1E", "B8C7CF" };
        public static readonly string[] SwatchName = {
            "Sail white", "Bright white", "Harbour navy", "Walnut", "Lobster red", "Coral", "Tangerine", "Mango",
            "Butter", "Kelp green", "Pine", "Bottle green", "Lagoon", "Teal", "Ponyo blue", "Deep sea",
            "Lavender", "Orchid", "Shell pink", "Sand", "Driftwood", "Tar brown", "Pitch", "Fog grey" };
        public static readonly string[] Stain = { "B88A5A", "8A5A3A", "D4B07A", "5E3C28", "A8A39A", "C9A27A", "6F7F5A", "3C2A20" };
        public static readonly string[] StainName = { "Teak", "Mahogany", "Pale oak", "Walnut oil", "Weathered", "Honey", "Sea-green wash", "Ebony" };
        public static readonly string[] Pattern = { "Plain band", "Twin pinstripe", "Racing double", "Wave", "Checker", "Dots", "Sun rays", "Chevron" };
        public static readonly string[] Channel = { "Hull", "Stripe", "Trim", "Deck" };
        public static readonly string[] SlotName = { "Hat", "Top", "Over", "Legs", "Boots", "Gloves" };
        public static readonly string[] Preset = { "Pip", "Bruno", "Stretch", "Rani", "Gus", "Sully" };   // M1Assets.Crew order (M1Build)
        public const int Swatches = 24, Patterns = 8;
        public const int PaintPrice = 150, TrimPrice = 80, StripePrice = 120, StainPrice = 100, DyePrice = 25;

        static Ware Add(Ware w) { All.Add(w); byId[w.Id] = w; return w; }

        static Catalog()
        {
            // ---------------- Doyle Boatworks (economy §4.3)
            Add(new Ware { Id = "boat:kestrel", At = Store.Doyle, Cat = "Boats", Name = "Kestrel", Sub = "T2 launch · 7.5 m · centre console, cuddy cabin with a bunk", Price = 5500, Fit = Fit.Boat, Spot = "doyle_slip",
                Stats = new[] { "Speed|4.5|9|m/s", "Tank|15|50|L", "Aboard|2|4|", "Bunk|0|1|" }, Line = "Walk aboard her on the slipway first." });
            Add(new Ware { Id = "boat:puffin", At = Store.Doyle, Cat = "Boats", Name = "Puffin", Sub = "T3 trawler · 12 m · wheelhouse, net drum", Price = 18000, Fit = Fit.Boat, Lock = "Act 3" });
            Add(new Ware { Id = "boat:swordfish", At = Store.Doyle, Cat = "Boats", Name = "Swordfish", Sub = "T4 sport boat · flybridge, fighting chair", Price = 45000, Fit = Fit.Boat, Lock = "Act 4" });
            foreach (var (boat, bn, f) in new[] { ("biscuit", "Biscuit", 1), ("kestrel", "Kestrel", 2) })
            {
                string Need() => null;   // the Kestrel rows check "Buy the Kestrel first" (WhyNotM5)
                Add(new Ware { Id = $"rep:{boat}:engine", Boat = boat, At = Store.Doyle, Cat = "Repairs", Name = $"{bn}: engine rebuild", Sub = "Fergus: \"Parts only. For Walt.\"", Price = 90, Fit = Fit.Repair, Spot = "boat:" + boat, Lock = boat == "kestrel" ? "Not needed" : null,
                    Stats = new[] { "Speed|4.5|6|m/s" } });
                Add(new Ware { Id = $"rep:{boat}:hull", Boat = boat, At = Store.Doyle, Cat = "Repairs", Name = $"{bn}: hull patch", Sub = "Stops the leak for good", Price = 120, Fit = Fit.Repair, Spot = "boat:" + boat, Lock = boat == "kestrel" ? "Not needed" : null,
                    Stats = new[] { "Leaks|yes|no|" } });
                Add(new Ware { Id = $"up:{boat}:engine1", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: Engine I", Sub = "+15% speed", Price = 300 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Speed|+0|+15|%" } });
                Add(new Ware { Id = $"up:{boat}:engine2", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: Engine II", Sub = "+30% speed (needs Engine I)", Price = 900 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = $"up:{boat}:engine1", Stats = new[] { "Speed|+15|+30|%" } });
                Add(new Ware { Id = $"up:{boat}:tank", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: long-range tank", Sub = "+50% fuel", Price = 250 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Fuel|100|150|%" } });
                Add(new Ware { Id = $"up:{boat}:lights", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: deck lights", Sub = "Night fishing on deck: night fish bite a little more", Price = 300 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Night bites|+0|+10|%" } });
                Add(new Ware { Id = $"up:{boat}:pump", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: bilge pump", Sub = "Bails leaks by itself", Price = 150 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Bailing|by hand|auto|" } });
                Add(new Ware { Id = $"up:{boat}:plating", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: hull plating", Sub = "-50% collision damage", Price = 600 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Knocks|100|50|%" } });
                Add(new Ware { Id = $"up:{boat}:holders", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: rod holders", Sub = "+20% worker catch (workers: M6)", Price = 250 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Worker catch|+0|+20|%" } });
                Add(new Ware { Id = $"up:{boat}:finder", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: fish finder", Sub = "Shows fish shadows and spot quality", Price = 500 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Sonar|-|on|" } });
                Add(new Ware { Id = $"up:{boat}:cap", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: locked fuel cap", Sub = "Stops engine sabotage (rivals: Act 3)", Price = 50 * f, Fit = Fit.Now, Spot = "boat:" + boat, Needs = Need(), Stats = new[] { "Sabotage|open|locked|" } });
                Add(new Ware { Id = $"up:{boat}:autopilot", Boat = boat, At = Store.Doyle, Cat = "Upgrades", Name = $"{bn}: autopilot", Sub = "Sail to a phone waypoint", Price = 1500 * f, Fit = Fit.Now, Spot = "boat:" + boat, Lock = "Act 3" });
                for (int ch = 0; ch < 4; ch++)
                    Add(new Ware { Id = $"paint:{boat}:{ch}", Boat = boat, Ch = ch, At = Store.Doyle, Cat = "Paint", Name = $"{bn}: {(ch == 0 ? "hull paint" : ch == 1 ? "stripe" : ch == 2 ? "trim colour" : "deck stain")}",
                        Sub = ch == 0 ? "24 swatches · Doyle's crew paints her" : ch == 1 ? "8 patterns × 24 colours" : ch == 2 ? "Rails, transom and trim" : "8 wood stains",
                        Price = ch == 0 ? PaintPrice : ch == 1 ? StripePrice : ch == 2 ? TrimPrice : StainPrice, Fit = Fit.Paint, Spot = "boat:" + boat, Needs = Need() });
            }
            // ---------------- Plank & Post (economy §4.4; furniture now, builds overnight)
            Ware P(string id, string cat, string name, int price, Fit fit, string spot, string sub, int act = 1, string needs = null, int nights = 1, string lk = null, params string[] stats) =>
                Add(new Ware { Id = id, At = Store.Plank, Cat = cat, Name = name, Price = price, Fit = fit, Spot = spot, Sub = sub, Act = act, Needs = needs, Nights = nights, Lock = lk, Stats = stats });
            P("dock:planks", "Piers & berths", "Fix three planks (+ a free bench)", 100, Fit.Overnight, "spot_planks", "Ingrid's offer: the first dock upgrade");
            P("dock:b2", "Piers & berths", "Repair berth B2", 250, Fit.Overnight, "spot_b2", "+1 moorer slot", 2, null, 1, null, "Berths|1|2|");
            P("dock:b3", "Piers & berths", "Repair berth B3", 400, Fit.Overnight, "spot_b3", "+1 moorer slot", 2, "dock:b2", 1, null, "Berths|2|3|");
            P("dock:p2", "Piers & berths", "P2 finger piers (6 berths)", 12000, Fit.Overnight, "p2", "Cannery Flats", 3, null, 3, "Act 3");
            for (int i = 1; i <= 4; i++) P($"dock:lamp{i}", "Mooring amenities", $"Dock lamp {(i == 1 ? "" : "#" + i)}".Trim(), 200, Fit.Overnight, $"spot_lamp{i}", "Moorer fee +10% nearby · 4 lamps halve night sabotage", 1, i > 1 ? $"dock:lamp{i - 1}" : null, 1, null, "Light|dark|lit|");
            P("dock:power", "Mooring amenities", "Power post (B1)", 600, Fit.Overnight, "spot_power", "Moorer fee +30%", 2, null, 1, null, "Mari's fee|+0|+30|%");
            P("dock:tap", "Mooring amenities", "Water tap (B1)", 350, Fit.Overnight, "spot_tap", "Moorer fee +20%", 2, null, 1, null, "Mari's fee|+0|+20|%");
            P("wh:1", "Storage", "Warehouse T1: Patched Warehouse", 300, Fit.Overnight, "spot_wh", "New planks, racks on the walls", 1, null, 1, null, "Slots|12|24|", "Cold|0|0|");
            P("wh:2", "Storage", "Warehouse T2: Cold Room", 2500, Fit.Overnight, "spot_wh", "An insulated back room, frosty door", 2, "wh:1", 2, null, "Slots|24|24|", "Cold|0|12|");
            P("wh:3", "Storage", "Warehouse T3: Big Warehouse", 8000, Fit.Overnight, "spot_wh", "Extended shed, a forklift lane", 3, "wh:2", 3, "Act 3");
            P("bed:cot", "Living", "Fix Walt's bed (the cot)", 120, Fit.Now, "spot_cot", "Sleep with the Rested buff", 1, null, 1, null, "Rested|no|yes|");
            P("bed:bunk", "Living", "Bunk bed", 350, Fit.Now, "spot_bunk", "An extra co-op sleep spot (needs the patched office: no room in the shack)", 1, "office:1", 1, null, "Beds|1|2|");
            P("bed:double", "Living", "Double bed", 700, Fit.Now, "spot_double", "Comfort +3 (needs the patched office)", 2, "office:1", 1, null, "Comfort|+0|+3|");
            P("bed:hammock", "Living", "Hammock", 200, Fit.Now, "spot_hammock", "Sleep aboard a boat with a cabin (the Kestrel)", 1, "boat:kestrel", 1, null, "Sleep aboard|no|yes|");
            foreach (var (id, name, price, sub) in new[] {
                ("chair", "Captain's chair", 80, "Comfort +1"), ("desk", "Walt's desk, mended", 250, "Comfort +1"), ("shelves", "Shelves", 150, "Comfort +1"),
                ("wardrobe", "Wardrobe", 400, "Comfort +1 · change clothes here"), ("rug", "Rag rug", 60, "Pure style"), ("plant", "Potted fern", 30, "Pure style"),
                ("lamp", "Table lamp", 90, "Pure style · warm light at night"), ("floorlamp", "Floor lamp", 150, "Pure style · warm light"), ("painting", "Painting: Biscuit at dawn", 200, "Pure style"),
                ("painting2", "Painting: the old lighthouse", 500, "Pure style"), ("stove", "Stove", 400, "Cook fish: the Well-fed buff"), ("maptable", "Map table", 900, "The big map: spots, orders, boats") })
                P("furn:" + id, "Furniture", name, price, Fit.Now, "spot_" + id, sub, id == "maptable" ? 2 : 1, id == "maptable" ? "office:1" : null, 1, null, id == "chair" || id == "desk" || id == "shelves" || id == "wardrobe" ? new[] { "Comfort|+0|+1|" } : new string[0]);
            P("furn:trophy", "Furniture", "Trophy mount", 300, Fit.Now, "spot_trophy", "Display a Trophy fish", 1, null, 1, "First Trophy");
            P("furn:balcony", "Furniture", "Balcony (office)", 3000, Fit.Overnight, "spot_balcony", "The place to look at your port", 2, "office:1");
            P("office:1", "Office", "Office T1: Patched Office", 2000, Fit.Overnight, "office", "Liveable office · the Crew app works fully · 3 staff", 2, null, 1, null, "Staff|0|3|");
            P("office:2", "Office", "Office T2: Harbour Office", 8000, Fit.Overnight, "office", "8 staff · ship orders on the phone", 3, "office:1", 2, null, "Staff|3|8|");
            P("office:3", "Office", "Office T3: Port Authority Building", 40000, Fit.Overnight, "office", "20 staff · consignments", 4, "office:2", 3, "Act 4");
            // ---------------- Hartley's Outfitters (economy §4.6; clothes only, no stats ever)
            void C(int slot, string id, string name, int price, string sub) => Add(new Ware { Id = "wear:" + id, Slot = slot, At = Store.Hartley, Cat = SlotName[slot], Name = name, Price = price, Fit = Fit.Wear, Sub = sub });
            C(0, "beanie", "Rib beanie", 40, "Warm ears, any weather"); C(0, "flatcap", "Flat cap", 60, "Old Saltmouth classic"); C(0, "souwester", "Sou'wester", 90, "The fisher's rain hat");
            C(0, "bucket", "Bucket hat", 70, "Shade for long casts"); C(0, "hardhat", "Hard hat", 120, "Dock work, safety first"); C(0, "captain", "Captain's cap", 250, "White top, black peak, gold anchor");
            C(0, "straw", "Straw hat", 80, "Summer on the bay"); C(0, "bobble", "Bobble hat", 50, "A pompom on top");
            C(1, "tee", "Plain tee", 60, "Soft cotton"); C(1, "flannel", "Plaid flannel", 120, "Checks for chilly mornings"); C(1, "breton", "Striped long-sleeve", 150, "The Breton stripe"); C(1, "hawaiian", "Hawaiian shirt", 300, "Hibiscus, loud");
            C(1, "knit", "Fisher's knit", 180, "Cable-knit jumper");
            C(2, "none", "No over-layer", 0, "Just the top"); C(2, "hivis", "Hi-vis vest", 80, "Bright with silver bands"); C(2, "oilskin", "Oilskin coat", 220, "Long and waxed"); C(2, "puffer", "Puffer jacket", 300, "Quilted, puffy");
            C(2, "captainjk", "Captain's jacket", 400, "Navy with brass buttons");
            C(3, "cargos", "Cargo trousers", 90, "Pockets for everything"); C(3, "bib", "Bib overalls", 180, "Straps and a bib"); C(3, "shorts", "Shorts + socks", 60, "Summer legs, long socks");
            C(4, "rubber", "Rubber boots", 80, "Wellies, glossy"); C(4, "deck", "Deck shoes", 140, "Low and grippy"); C(4, "steel", "Steel-toe boots", 350, "Toe caps, laced");
            C(5, "rubberg", "Rubber gloves", 30, "Fish-proof"); C(5, "leather", "Leather gloves", 90, "Rope work"); C(5, "gauntlet", "Gauntlets", 150, "Long cuffs");
            Add(new Ware { Id = "wear:walt", Slot = 0, At = Store.Hartley, Cat = "Story", Name = "Walt's old captain set", Price = 0, Fit = Fit.Wear, Sub = "Free, from Walt's journal", Lock = "Walt's journal" });
            Add(new Ware { Id = "dye", At = Store.Hartley, Cat = "Dye", Name = "Colour dye", Price = DyePrice, Fit = Fit.Dye, Sub = "Any worn item, 24 swatches. Your player colour band stays." });
        }
        public static bool Unique(Ware w) => w.Fit != Fit.Wear && w.Fit != Fit.Dye && w.Fit != Fit.Paint;
        public static bool ClosedDay(Store s, int day) => s != Store.Doyle && DayTime.DayName(day) == "Sunday";
        public static readonly double[] Opens = { 7 * 60, 7 * 60, 9 * 60 }, Closes = { 20 * 60, 18 * 60, 19 * 60 };
        public static bool IsOpen(Store s, double m) { double h = DayTime.OfDay(m); return !ClosedDay(s, DayTime.Day(m)) && h >= Opens[(int)s] && h < Closes[(int)s]; }
        public static string ClosedSign(Store s, double m)
        {
            int day = DayTime.Day(m); double h = DayTime.OfDay(m); bool later = h < Opens[(int)s];
            int next = later ? day : day + 1; while (ClosedDay(s, next)) next++;
            return $"Back at {DayTime.Clock(DayTime.At(next, Opens[(int)s]))}" + (next == day ? "" : next == day + 1 ? " tomorrow" : $" on {DayTime.DayName(next)}");
        }
        // what a preset wears from the start, slot by slot (the kit's own garments: "pip:hat"), always owned
        public static string Own(int preset, int slot) => Preset[preset].ToLowerInvariant() + ":" + SlotName[slot].ToLowerInvariant();
    }

    // a boat's state owned by the company: paint, upgrades, repairs
    public sealed class BoatSt
    {
        public int[] Paint = { -1, -1, -1, -1 };     // swatch per channel (-1: as built, Walt's Classic); deck = a stain index
        public int[] Last = { -1, -1, -1, -1 };      // the colour before (free to go back to)
        public int Pattern;                          // stripe pattern 0..7
        public readonly HashSet<string> Ups = new(); // "engine1", "pump" ...
        public bool Engine, Hull;                    // Biscuit's story repairs done (the Kestrel comes whole)
        public PaintJob Job;                         // Doyle's crew at work (null: none)
        public BoatSt Copy() { var b = new BoatSt { Paint = (int[])Paint.Clone(), Last = (int[])Last.Clone(), Pattern = Pattern, Engine = Engine, Hull = Hull, Job = Job?.Copy() }; b.Ups.UnionWith(Ups); return b; }
    }
    public sealed class PaintJob
    {
        public int Ch, From, To, PatFrom, PatTo; public double Start, Walk, Paint;  // game minutes (1 game minute = 1 real second)
        public double End => Start + Walk + Paint;
        public double Progress(double m) => Paint <= 0 ? 1 : Math.Clamp((m - Start - Walk) / Paint, 0, 1);
        public PaintJob Copy() => (PaintJob)MemberwiseClone();
    }
    // a build waiting for the morning (Ingrid's crew, Doyle's slipway)
    public sealed class Order { public string Tx, Item, Who; public int Ready; public double At, ReadyAt = -1; }   // ReadyAt (M7): a job done within the hour (Doyle's engine)
    public enum AskSt { Open, Approved, Declined, Lapsed }
    public sealed class Ask { public int Id; public string Key, Who, Item, Tx, Answer; public int Qty, Price, Arg; public double At; public AskSt St; }
    public enum Policy { Anyone, Ask, HostOnly }

    // a player's look (their Kit): the crew member, the 6 slots worn, a dye per slot, the clothes owned
    public sealed class Outfit
    {
        public int Preset = -1;                                       // index into Catalog.Preset, -1 = not picked yet
        public string[] Wear = new string[6];                         // item ids ("wear:beanie") or null = the preset's own
        public int[] Dye = { -1, -1, -1, -1, -1, -1 };                // swatch per slot, -1 = as made
        public readonly HashSet<string> Owned = new();                // bought clothes (+ dyes are per worn item)
        public Outfit Copy() { var l = new Outfit { Preset = Preset, Wear = (string[])Wear.Clone(), Dye = (int[])Dye.Clone() }; l.Owned.UnionWith(Owned); return l; }
        public void Reset() { Wear = new string[6]; Dye = new[] { -1, -1, -1, -1, -1, -1 }; }
    }

    // the warehouse (systems §1.1.6): a slot holds 10 fish of one species + grade
    public sealed class WFish { public string Species; public int Grade; public float Cm, Kg, Value; public double CaughtAt, ColdIn = -1; public ulong Id; public WFish Copy() => (WFish)MemberwiseClone(); }
    public sealed class Store10 { public readonly List<WFish> Fish = new(); public bool Cold; public string Key => Fish.Count == 0 ? null : Fish[0].Species + "/" + Fish[0].Grade; }

    public partial class Company
    {
        public const int AskCap = 5000; public const double AskMinutes = 60;   // 60 real s = 60 game minutes (1 game hour = 1 real minute)
        public const int SlotFish = 10, Racks0 = 12, Racks1 = 24, Cold2 = 12; public const float ColdRot = 0.25f;

        public int Act = 1;                                           // the story act (raised by the story; F7 for testers)
        public Policy Spending = Policy.Ask;
        public readonly HashSet<string> Owned = new();                // company-owned installed items ("wh:1", "furn:desk", "boat:kestrel")
        public readonly List<Order> Orders = new();                   // builds and repairs waiting for 07:00
        public readonly Dictionary<string, BoatSt> Boats = new() { ["biscuit"] = new BoatSt() };
        public readonly List<Ask> Asks = new();
        public readonly List<Store10> Wh = new();                     // racks then cold slots
        public readonly Dictionary<string, Outfit> Outfits = new();       // per player key
        public readonly List<string> Fresh = new();                   // installed today (the 07:00 timelapse plays these)
        int askNext = 1;

        public event Action<string> Installed;                        // an item appeared in its spot (the game pops it in)

        public BoatSt BoatOf(string b) { if (!Boats.TryGetValue(b, out var s)) Boats[b] = s = new BoatSt(); return s; }
        public Outfit OutfitOf(string key) { if (!Outfits.TryGetValue(key, out var l)) Outfits[key] = l = new Outfit(); return l; }
        public bool Has(string item) => item == null || Owned.Contains(item) || (item.StartsWith("up:") && Boats.TryGetValue(item.Split(':')[1], out var b) && b.Ups.Contains(item.Split(':')[2]));
        public bool Ordered(string item) => Orders.Exists(o => o.Item == item);
        public int WhTier => Owned.Contains("wh:3") ? 3 : Owned.Contains("wh:2") ? 2 : Owned.Contains("wh:1") ? 1 : 0;
        public int OfficeTier => Owned.Contains("office:3") ? 3 : Owned.Contains("office:2") ? 2 : Owned.Contains("office:1") ? 1 : 0;
        public int Racks => WhTier == 0 ? Racks0 : WhTier == 3 ? 48 : Racks1;
        public int ColdSlots => WhTier >= 3 ? 24 : WhTier >= 2 ? Cold2 : 0;
        public static int AskLimit(int cash) => Math.Min(AskCap, cash / 4);

        // ---- the M5 shops. price for this player / this choice (arg: a swatch, or a pattern*100+swatch for the stripe)
        public int PriceM5(string key, string item, int arg = 0)
        {
            var w = Catalog.Get(item); if (w == null) return -1;
            if (w.Fit == Fit.Paint) { var b = BoatOf(w.Boat); return (arg % 100) == b.Last[w.Ch] && (w.Ch != 1 || arg / 100 == b.Pattern) ? 0 : w.Price; }
            return w.Price;
        }
        public string WhyNotM5(string key, string item, int arg, double at, bool instant = false)
        {
            var w = Catalog.Get(item); if (w == null) return "Not for sale.";
            if (w.Lock != null) return w.Lock;
            if (w.Act > Act) return "Act " + w.Act;
            if (!Catalog.IsOpen(w.At, at)) return Catalog.ClosedSign(w.At, at);
            if (w.Needs != null && !Has(w.Needs)) { var n = Catalog.Get(w.Needs); return "Needs " + (n?.Name ?? w.Needs); }
            if (w.Boat != null && w.Boat != "biscuit" && !Owned.Contains("boat:" + w.Boat)) return $"Buy the {Cap(w.Boat)} first";
            var look = OutfitOf(key);
            switch (w.Fit)
            {
                case Fit.Wear: if (look.Owned.Contains(item) || item == "wear:none") return "You own it"; break;
                case Fit.Dye: if (arg < 0 || arg / 100 > 5 || arg % 100 >= Catalog.Swatches) return "Pick a colour"; if (look.Dye[arg / 100] == arg % 100) return "It's that colour"; break;
                case Fit.Paint:
                {
                    var b = BoatOf(w.Boat); int sw = arg % 100, pat = arg / 100;
                    if (b.Job != null && b.Job.End > at) return "Doyle's crew is still painting";
                    if (sw < 0 || sw >= (w.Ch == 3 ? Catalog.Stain.Length : Catalog.Swatches) || pat >= Catalog.Patterns) return "Pick a colour";
                    if (b.Paint[w.Ch] == sw && (w.Ch != 1 || b.Pattern == pat)) return "She's that colour";
                    break;
                }
                case Fit.Repair:
                {
                    var b = BoatOf(w.Boat); bool done = item.EndsWith(":engine") ? b.Engine : b.Hull;
                    if (done) return "Done"; if (Ordered(item)) { var od = Orders.Find(o => o.Item == item); return instant ? "" : od.ReadyAt >= 0 ? $"On the slipway: ready by {DayTime.Clock(od.ReadyAt)}" : "On the slipway: ready 07:00"; } break;
                }
                default:
                    if (Has(item)) return "Owned";
                    if (Ordered(item)) return "Built tonight";
                    if (item.StartsWith("up:")) { var b = BoatOf(w.Boat); if (b.Ups.Contains(item.Split(':')[2])) return "Fitted"; }
                    break;
            }
            int price = PriceM5(key, item, arg) * (instant && w.Fit == Fit.Repair ? 2 : 1);
            if (instant && w.Fit == Fit.Repair && Ordered(item)) price = w.Price;     // already paid once: the rush is the other half
            if (price > Cash) return $"Need ${price - Cash} more";
            return "";
        }
        static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // does this buy need the crew's OK? (co-op only: others online; solo never asks)
        public bool NeedsAsk(string item, int arg, string key, bool othersOnline, bool instant = false)
        {
            if (!othersOnline || Spending == Policy.Anyone) return false;
            var w = Catalog.Get(item); if (w == null) return false;
            int price = PriceM5(key, item, arg) * (instant && w.Fit == Fit.Repair ? 2 : 1);
            return Spending == Policy.HostOnly ? true : price > AskLimit(Cash);
        }

        // ---- the buy itself (host): all or nothing, once per transaction id
        public bool BuyM5(string tx, string key, string who, string item, int arg, double at, bool instant, out string why)
        {
            storyNow = Math.Max(storyNow, at);                        // M7: a rushed repair's "fixed" event carries the buy's time
            why = Done.Contains(tx) ? "Already bought." : WhyNotM5(key, item, arg, at, instant);
            if (why != "") return false;
            var w = Catalog.Get(item);
            int price = PriceM5(key, item, arg) * (instant && w.Fit == Fit.Repair ? 2 : 1);
            bool rush = instant && w.Fit == Fit.Repair && Ordered(item); if (rush) price = w.Price;
            string what = w.Fit == Fit.Paint ? $"{w.Name}: {(w.Ch == 3 ? Catalog.StainName[arg % 100] : Catalog.SwatchName[arg % 100])}" + (w.Ch == 1 ? $" {Catalog.Pattern[arg / 100].ToLowerInvariant()}" : "")
                        : w.Fit == Fit.Dye ? $"Dye: {Catalog.SlotName[arg / 100].ToLowerInvariant()} {Catalog.SwatchName[arg % 100].ToLowerInvariant()}"
                        : w.Fit == Fit.Repair && instant ? w.Name + " (rush)" : w.Name;
            if (price > 0) { if (!Spend(tx, who, what, price, at)) { why = "The bank can't cover it."; return false; } }
            else Done.Add(tx);
            switch (w.Fit)
            {
                case Fit.Wear: OutfitOf(key).Owned.Add(item); WearIt(key, item); break;
                case Fit.Dye: OutfitOf(key).Dye[arg / 100] = arg % 100; break;
                case Fit.Paint: StartPaint(w, arg, at); break;
                case Fit.Repair:
                    if (instant) { Orders.RemoveAll(o => o.Item == item); Repair(w); }
                    else Orders.Add(new Order { Tx = tx, Item = item, Who = who, At = at, Ready = DayTime.Day(DayTime.NextWake(at)), ReadyAt = item.EndsWith(":engine") ? at + EngineHour : -1 });   // M7: Fergus does an engine in the hour (world §4.2)
                    break;
                case Fit.Overnight: Orders.Add(new Order { Tx = tx, Item = item, Who = who, At = at, Ready = DayTime.Day(DayTime.NextWake(at)) + w.Nights - 1 }); break;
                default: Install(item); break;
            }
            return true;
        }
        // M7: Doyle's engine rebuild is done within the in-game hour (Manan 31 Oct, world §4.2), not overnight
        public const double EngineHour = 60;
        void TickRepairs(double m)
        {
            for (int i = 0; i < Orders.Count; i++)
            {
                var o = Orders[i]; if (o.ReadyAt < 0 || m < o.ReadyAt) continue;
                Orders.RemoveAt(i--); storyNow = Math.Max(storyNow, o.ReadyAt); var w = Catalog.Get(o.Item); if (w != null) Repair(w);
            }
        }
        void Repair(Ware w) { var b = BoatOf(w.Boat); if (w.Id.EndsWith(":engine")) b.Engine = true; else b.Hull = true; Owned.Add(w.Id); Installed?.Invoke(w.Id); Event("fixed:" + w.Id, storyNow); }
        void Install(string item)
        {
            var w = Catalog.Get(item);
            if (item.StartsWith("up:")) BoatOf(w.Boat).Ups.Add(item.Split(':')[2]);
            else Owned.Add(item);
            if (item == "boat:kestrel") BoatOf("kestrel");
            if (item.StartsWith("wh:")) GrowWarehouse();
            Fresh.Add(item); Installed?.Invoke(item);
        }

        // paint: Doyle's crew walks over (Walk game minutes) and paints (Paint minutes); the colour is the new one at the end
        public static double PaintWalk = 20, PaintTime = 12;          // game minutes = real seconds (the game sets the walk from the distance)
        void StartPaint(Ware w, int arg, double at)
        {
            var b = BoatOf(w.Boat); int sw = arg % 100, pat = arg / 100;
            b.Job = new PaintJob { Ch = w.Ch, From = b.Paint[w.Ch], To = sw, PatFrom = b.Pattern, PatTo = w.Ch == 1 ? pat : b.Pattern, Start = at, Walk = PaintWalk, Paint = PaintTime };
            b.Last[w.Ch] = b.Paint[w.Ch]; b.Paint[w.Ch] = sw; if (w.Ch == 1) b.Pattern = pat;
            Installed?.Invoke(w.Id);
        }

        // ---- clothes: wearing an owned item (free); the preset's own garments are always owned
        public bool WearIt(string key, string item)
        {
            var l = OutfitOf(key); var w = Catalog.Get(item);
            if (w == null || w.Slot < 0 || (!l.Owned.Contains(item) && item != "wear:none")) return false;
            l.Wear[w.Slot] = item; l.Dye[w.Slot] = -1; return true;
        }
        public void WearOwn(string key, int slot) { var l = OutfitOf(key); l.Wear[slot] = null; l.Dye[slot] = -1; }
        // one crew member per player in co-op (§1.9): "" or why not
        public string PickPreset(string key, int preset, IEnumerable<string> online)
        {
            if (preset < 0 || preset >= Catalog.Preset.Length) return "No such crew member";
            foreach (var k in online) if (k != key && Outfits.TryGetValue(k, out var o) && o.Preset == preset) return $"{Catalog.Preset[preset]} is taken";
            var l = OutfitOf(key); if (l.Preset != preset) { l.Preset = preset; l.Reset(); }
            return "";
        }

        // ---- Ask the crew
        public Ask AskCrew(string key, string who, string item, int arg, int qty, double at)
        {
            var a = new Ask { Id = askNext++, Key = key, Who = who, Item = item, Arg = arg, Qty = qty, Price = PriceM5(key, item, arg), At = at, Tx = $"ask:{askNext - 1}" };
            Asks.Add(a);
            Text(who, $"Can I buy {Catalog.Get(item)?.Name ?? item} for ${a.Price}? Needs one OK.", at, offer: "ask:" + a.Id);
            return a;
        }
        // an answer from a player (host or another player; the asker can't approve their own). false: not theirs to answer
        public bool AnswerAsk(int id, string key, bool host, bool yes, string who, double at, out string why)
        {
            why = ""; var a = Asks.Find(x => x.Id == id);
            if (a == null || a.St != AskSt.Open) { why = "Already answered"; return false; }
            if (key == a.Key) { why = "Someone else has to say OK"; return false; }
            a.Answer = who;
            if (!yes) { a.St = AskSt.Declined; Text(a.Who, $"{who}: not now, the {Catalog.Get(a.Item)?.Name}.", at); return true; }   // (in the asker's thread, under the ask)
            a.St = AskSt.Approved;
            bool ok = BuyM5(a.Tx, a.Key, a.Who, a.Item, a.Arg, at, false, out why);
            Text(a.Who, ok ? $"{who}: OK! Bought the {Catalog.Get(a.Item)?.Name}." : $"{who}: OK, but: {why}", at);
            return true;
        }
        // 60 s with no answer approves it when nobody else is online to ask
        public void TickAsks(double at, Func<string, bool> othersOnline)
        {
            foreach (var a in Asks)
            {
                if (a.St != AskSt.Open || at - a.At < AskMinutes) continue;
                if (othersOnline(a.Key)) continue;
                a.St = AskSt.Lapsed; a.Answer = "nobody online";
                BuyM5(a.Tx, a.Key, a.Who, a.Item, a.Arg, at, false, out var why);
                Text(a.Who, why == "" ? $"Nobody was online to ask, so the {Catalog.Get(a.Item)?.Name} is bought." : $"Couldn't buy the {Catalog.Get(a.Item)?.Name}: {why}", at, me: true);
            }
        }

        // ---- the warehouse: rot is slower in the cold room (25%): the fish's clock is moved on 25% of the time in there
        void GrowWarehouse()
        {
            int racks = 0, cold = 0; foreach (var s in Wh) if (s.Cold) cold++; else racks++;
            int at = racks; while (racks < Racks) { Wh.Insert(at++, new Store10()); racks++; }
            while (cold < ColdSlots) { Wh.Add(new Store10 { Cold = true }); cold++; }
        }
        public void EnsureWarehouse() { if (Wh.Count == 0) GrowWarehouse(); }
        public static double AgeOf(WFish f, double now) => f.ColdIn < 0 ? now - f.CaughtAt : (f.ColdIn - f.CaughtAt) + (now - f.ColdIn) * ColdRot;
        // a fish into the store: the first stack of its kind with room, else an empty slot (cold first when preferCold). -1: full
        public int Put(WFish f, double now, bool preferCold = false)
        {
            EnsureWarehouse();
            string k = f.Species + "/" + f.Grade;
            int Find(bool cold) { for (int i = 0; i < Wh.Count; i++) if (Wh[i].Cold == cold && Wh[i].Key == k && Wh[i].Fish.Count < SlotFish) return i; for (int i = 0; i < Wh.Count; i++) if (Wh[i].Cold == cold && Wh[i].Fish.Count == 0) return i; return -1; }
            int s = preferCold ? Find(true) : -1; if (s < 0) s = Find(false); if (s < 0 && !preferCold) s = Find(true);
            if (s < 0) return -1;
            if (Wh[s].Cold) f.ColdIn = now;
            Wh[s].Fish.Add(f); return s;
        }
        // a fish out of the store (its age carried into CaughtAt so the world's normal rot clock goes on from there)
        public WFish Take(int slot, double now)
        {
            if (slot < 0 || slot >= Wh.Count || Wh[slot].Fish.Count == 0) return null;
            var l = Wh[slot].Fish; var f = l[l.Count - 1]; l.RemoveAt(l.Count - 1);
            if (f.ColdIn >= 0) { f.CaughtAt = now - AgeOf(f, now); f.ColdIn = -1; }
            return f;
        }
        public int WhFish { get { int n = 0; foreach (var s in Wh) n += s.Fish.Count; return n; } }

        // ---- the morning (07:00): builds and overnight repairs land; called from RunMorning
        void MorningM5(int day)
        {
            Fresh.Clear();
            for (int i = 0; i < Orders.Count; i++)
            {
                var o = Orders[i]; if (o.Ready > day) continue;
                Orders.RemoveAt(i--); var w = Catalog.Get(o.Item);
                if (w.Fit == Fit.Repair) { Repair(w); Fresh.Add(w.Id); } else Install(o.Item);
            }
            foreach (var a in Asks) if (a.St == AskSt.Open && day > DayTime.Day(a.At) + 1) a.St = AskSt.Lapsed;   // a stale ask doesn't hang around
            Asks.RemoveAll(a => a.St != AskSt.Open && day > DayTime.Day(a.At) + 2);
        }

        public string CheckM5()
        {
            foreach (var s in Wh)
            {
                if (s.Fish.Count > SlotFish) return $"warehouse slot holds {s.Fish.Count} fish";
                foreach (var f in s.Fish) if (f.Species + "/" + f.Grade != s.Key) return "mixed stack in the warehouse";
            }
            if (Wh.Count > 0) { int r = 0, c = 0; foreach (var s in Wh) if (s.Cold) c++; else r++; if (r != Racks || c != ColdSlots) return $"warehouse {r}+{c} slots, tier {WhTier} wants {Racks}+{ColdSlots}"; }
            foreach (var o in Orders) if (Owned.Contains(o.Item)) return $"{o.Item} owned and still ordered";
            var seen = new HashSet<string>(); foreach (var o in Orders) if (!seen.Add(o.Item)) return $"{o.Item} ordered twice";
            foreach (var w in Catalog.All) if (Catalog.Unique(w) && w.Fit != Fit.Repair && Owned.Contains(w.Id) && w.Needs != null && !Has(w.Needs) && w.Needs.StartsWith("boat:") == false && !Owned.Contains(w.Needs)) return $"{w.Id} owned without {w.Needs}";
            foreach (var kv in Outfits) for (int s = 0; s < 6; s++) { var it = kv.Value.Wear[s]; if (it != null && it != "wear:none" && !kv.Value.Owned.Contains(it)) return $"{kv.Key} wears {it} unowned"; }
            return "";
        }

        // ---- save
        void WriteM5(Json.W w)
        {
            w.K("act").V(Act).K("spending").V((int)Spending).K("askNext").V(askNext);
            var own = new List<string>(Owned); own.Sort(StringComparer.Ordinal); w.K("owned").Arr(); foreach (var o in own) w.V(o); w.End();
            w.K("orders").Arr(); foreach (var o in Orders) w.Obj().K("tx").V(o.Tx).K("item").V(o.Item).K("who").V(o.Who).K("ready").V(o.Ready).K("at").V(o.At).K("readyAt").V(o.ReadyAt).End(); w.End();
            var bk = new List<string>(Boats.Keys); bk.Sort(StringComparer.Ordinal);
            w.K("boats").Obj();
            foreach (var k in bk)
            {
                var b = Boats[k]; w.K(k).Obj().K("pattern").V(b.Pattern).K("engine").V(b.Engine).K("hull").V(b.Hull);
                w.K("paint").Arr(); foreach (var p in b.Paint) w.V(p); w.End(); w.K("last").Arr(); foreach (var p in b.Last) w.V(p); w.End();
                var ups = new List<string>(b.Ups); ups.Sort(StringComparer.Ordinal); w.K("ups").Arr(); foreach (var u in ups) w.V(u); w.End();
                if (b.Job != null) w.K("job").Obj().K("ch").V(b.Job.Ch).K("from").V(b.Job.From).K("to").V(b.Job.To).K("pf").V(b.Job.PatFrom).K("pt").V(b.Job.PatTo).K("start").V(b.Job.Start).K("walk").V(b.Job.Walk).K("paint").V(b.Job.Paint).End();
                w.End();
            }
            w.End();
            w.K("asks").Arr(); foreach (var a in Asks) w.Obj().K("id").V(a.Id).K("key").V(a.Key).K("who").V(a.Who).K("item").V(a.Item).K("tx").V(a.Tx).K("answer").V(a.Answer).K("qty").V(a.Qty).K("price").V(a.Price).K("arg").V(a.Arg).K("at").V(a.At).K("st").V((int)a.St).End(); w.End();
            w.K("wh").Arr();
            foreach (var s in Wh) { w.Obj().K("cold").V(s.Cold).K("fish").Arr(); foreach (var f in s.Fish) w.Obj().K("sp").V(f.Species).K("g").V(f.Grade).K("cm").V(f.Cm).K("kg").V(f.Kg).K("v").V(f.Value).K("at").V(f.CaughtAt).K("cold").V(f.ColdIn).K("id").V(f.Id).End(); w.End().End(); }
            w.End();
            var lk = new List<string>(Outfits.Keys); lk.Sort(StringComparer.Ordinal);
            w.K("looks").Obj();
            foreach (var k in lk)
            {
                var l = Outfits[k]; w.K(k).Obj().K("preset").V(l.Preset).K("wear").Arr(); foreach (var x in l.Wear) w.V(x); w.End();
                w.K("dye").Arr(); foreach (var d in l.Dye) w.V(d); w.End();
                var ow = new List<string>(l.Owned); ow.Sort(StringComparer.Ordinal); w.K("owned").Arr(); foreach (var o in ow) w.V(o); w.End(); w.End();
            }
            w.End();
        }
        static int[] Ints(object o, int n, int def)
        {
            var r = new int[n]; for (int i = 0; i < n; i++) r[i] = def;
            if (o is List<object> l) for (int i = 0; i < n && i < l.Count; i++) r[i] = Convert.ToInt32(l[i], CultureInfo.InvariantCulture);
            return r;
        }
        void ReadM5(Dictionary<string, object> o)
        {
            Act = Json.I(o, "act", 1); Spending = (Policy)Json.I(o, "spending", (int)Policy.Ask); askNext = Json.I(o, "askNext", 1);
            if (o.TryGetValue("owned", out var ow) && ow is List<object> ol) foreach (var x in ol) Owned.Add((string)x);
            if (o.TryGetValue("orders", out var od) && od is List<object> odl) foreach (Dictionary<string, object> e in odl) Orders.Add(new Order { Tx = Json.S(e, "tx"), Item = Json.S(e, "item"), Who = Json.S(e, "who"), Ready = Json.I(e, "ready"), At = Json.D(e, "at") , ReadyAt = Json.D(e, "readyAt", -1) });
            if (o.TryGetValue("boats", out var bo) && bo is Dictionary<string, object> bd)
                foreach (var kv in bd)
                {
                    var e = (Dictionary<string, object>)kv.Value;
                    var b = new BoatSt { Pattern = Json.I(e, "pattern"), Engine = Json.B(e, "engine"), Hull = Json.B(e, "hull") };
                    e.TryGetValue("paint", out var p); b.Paint = Ints(p, 4, -1); e.TryGetValue("last", out var la); b.Last = Ints(la, 4, -1);
                    if (e.TryGetValue("ups", out var u) && u is List<object> ul) foreach (var x in ul) b.Ups.Add((string)x);
                    if (e.TryGetValue("job", out var j) && j is Dictionary<string, object> jd)
                        b.Job = new PaintJob { Ch = Json.I(jd, "ch"), From = Json.I(jd, "from"), To = Json.I(jd, "to"), PatFrom = Json.I(jd, "pf"), PatTo = Json.I(jd, "pt"), Start = Json.D(jd, "start"), Walk = Json.D(jd, "walk"), Paint = Json.D(jd, "paint") };
                    Boats[kv.Key] = b;
                }
            if (o.TryGetValue("asks", out var ak) && ak is List<object> al)
                foreach (Dictionary<string, object> e in al) Asks.Add(new Ask { Id = Json.I(e, "id"), Key = Json.S(e, "key"), Who = Json.S(e, "who"), Item = Json.S(e, "item"), Tx = Json.S(e, "tx"), Answer = Json.S(e, "answer"), Qty = Json.I(e, "qty"), Price = Json.I(e, "price"), Arg = Json.I(e, "arg"), At = Json.D(e, "at"), St = (AskSt)Json.I(e, "st") });
            if (o.TryGetValue("wh", out var wh) && wh is List<object> wl)
                foreach (Dictionary<string, object> e in wl)
                {
                    var s = new Store10 { Cold = Json.B(e, "cold") };
                    if (e.TryGetValue("fish", out var fo) && fo is List<object> fl)
                        foreach (Dictionary<string, object> f in fl) s.Fish.Add(new WFish { Species = Json.S(f, "sp"), Grade = Json.I(f, "g"), Cm = (float)Json.D(f, "cm"), Kg = (float)Json.D(f, "kg"), Value = (float)Json.D(f, "v"), CaughtAt = Json.D(f, "at"), ColdIn = Json.D(f, "cold", -1), Id = f.TryGetValue("id", out var id) && id is double di ? (ulong)di : 0 });
                    Wh.Add(s);
                }
            if (o.TryGetValue("looks", out var lo) && lo is Dictionary<string, object> ld)
                foreach (var kv in ld)
                {
                    var e = (Dictionary<string, object>)kv.Value; var l = new Outfit { Preset = Json.I(e, "preset", -1) };
                    if (e.TryGetValue("wear", out var we) && we is List<object> wl2) for (int i = 0; i < 6 && i < wl2.Count; i++) l.Wear[i] = wl2[i] as string;
                    e.TryGetValue("dye", out var dy); l.Dye = Ints(dy, 6, -1);
                    if (e.TryGetValue("owned", out var lo2) && lo2 is List<object> ll2) foreach (var x in ll2) l.Owned.Add((string)x);
                    Outfits[kv.Key] = l;
                }
        }

        // ---------------------------------------------------------------- the M5 shop suite (companycheck + EditMode)
        // the rules, one by one, then n random buys by 3 players with two buying the same thing at the same moment
        public static string ShopTest(int n = 4000, int seed = 5)
        {
            var c = new Company(); double t = DayTime.At(2, 9 * 60);    // a Tuesday, 09:00: every shop open
            string F(string what) => "FAIL " + what;
            c.Earn("seed", "Pip", "test", 20000, t);
            // overnight: the warehouse T1 is built by 07:00 tomorrow, not now
            if (!c.BuyM5("w1", "solo", "Pip", "wh:1", 0, t, false, out var why)) return F("wh:1 " + why);
            if (c.WhTier != 0 || !c.Ordered("wh:1")) return F("wh:1 installed at once");
            if (c.BuyM5("w1b", "solo", "Rani", "wh:1", 0, t, false, out why) || why != "Built tonight") return F("wh:1 bought twice: " + why);
            c.Tick(DayTime.At(3, 7 * 60)); if (c.WhTier != 1 || c.Racks != 24 || c.Wh.Count != 24) return F($"wh:1 not built by 07:00 (tier {c.WhTier}, {c.Wh.Count} slots)");
            if (!c.Fresh.Contains("wh:1")) return F("the 07:00 timelapse list misses wh:1");
            t = DayTime.At(3, 9 * 60);
            // furniture: at once, in its spot; and once only (two players at the same moment: the second is refused)
            int cash = c.Cash;
            bool a1 = c.BuyM5("d1", "pip", "Pip", "furn:desk", 0, t, false, out _), a2 = c.BuyM5("d2", "rani", "Rani", "furn:desk", 0, t, false, out var why2);
            if (!a1 || a2 || why2 != "Owned" || c.Cash != cash - 250) return F($"concurrent desk: {a1} {a2} {why2} ${cash - c.Cash}");
            // the same transaction twice (a lost reply resent): applied once
            if (c.BuyM5("d1", "pip", "Pip", "furn:lamp", 0, t, false, out why) || why != "Already bought.") return F("replay applied: " + why);
            // needs + acts
            if (c.WhyNotM5("solo", "wh:2", 0, t) != "Act 2") return F("wh:2 not act-locked");
            c.Act = 2; if (c.WhyNotM5("solo", "wh:2", 0, t) != "") return F("wh:2 at act 2: " + c.WhyNotM5("solo", "wh:2", 0, t));
            if (c.WhyNotM5("solo", "up:biscuit:engine2", 0, t) != "Needs Biscuit: Engine I") return F("engine2 without engine1: " + c.WhyNotM5("solo", "up:biscuit:engine2", 0, t));
            if (c.WhyNotM5("solo", "up:kestrel:engine1", 0, t) != "Buy the Kestrel first") return F("kestrel upgrade without kestrel: " + c.WhyNotM5("solo", "up:kestrel:engine1", 0, t));
            // repairs: overnight, or now at twice the price; the rush after an order costs the other half
            cash = c.Cash; c.BuyM5("r1", "pip", "Pip", "rep:biscuit:engine", 0, t, false, out _);
            if (c.Boats["biscuit"].Engine || c.Cash != cash - 90) return F("engine rebuild overnight");
            c.BuyM5("r2", "pip", "Pip", "rep:biscuit:engine", 0, t, true, out why); if (!c.Boats["biscuit"].Engine || c.Cash != cash - 180 || c.Ordered("rep:biscuit:engine")) return F($"rush after order: {why} ${cash - c.Cash}");
            cash = c.Cash; c.BuyM5("r3", "pip", "Pip", "rep:biscuit:hull", 0, t, true, out _); if (!c.Boats["biscuit"].Hull || c.Cash != cash - 240) return F("hull rush 2x");
            // paint: the crew paints it; going back to the last colour is free; not while the crew is at it
            cash = c.Cash; if (!c.BuyM5("p1", "pip", "Pip", "paint:biscuit:0", 14, t, false, out why)) return F("paint " + why);
            if (c.Boats["biscuit"].Job == null || c.Cash != cash - 150) return F("paint job");
            if (c.WhyNotM5("pip", "paint:biscuit:0", 4, t) != "Doyle's crew is still painting") return F("paint over a running job");
            t += 40; cash = c.Cash; c.BuyM5("p2", "pip", "Pip", "paint:biscuit:0", 4, t, false, out _); t += 40;
            if (c.PriceM5("pip", "paint:biscuit:0", 14) != 0) return F("back to the last colour not free");
            c.BuyM5("p3", "pip", "Pip", "paint:biscuit:0", 14, t, false, out _); if (c.Cash != cash - 150 || c.Boats["biscuit"].Paint[0] != 14) return F($"last colour free: ${cash - c.Cash}");
            // clothes: own, wear, dye; a second Pip refused
            if (c.PickPreset("pip", 0, new[] { "pip", "rani" }) != "") return F("pick Pip");
            if (c.PickPreset("rani", 0, new[] { "pip", "rani" }) != "Pip is taken") return F("two Pips");
            if (c.PickPreset("rani", 3, new[] { "pip", "rani" }) != "") return F("pick Rani");
            if (c.PickPreset("rani", 0, new[] { "rani" }) != "") return F("Pip free once the first Pip left");
            c.PickPreset("rani", 3, new[] { "pip", "rani" });
            if (!c.BuyM5("h1", "pip", "Pip", "wear:beanie", 0, t, false, out why) || c.OutfitOf("pip").Wear[0] != "wear:beanie") return F("beanie " + why);
            cash = c.Cash; if (!c.BuyM5("h2", "pip", "Pip", "dye", 0 * 100 + 4, t, false, out why) || c.OutfitOf("pip").Dye[0] != 4 || c.Cash != cash - 25) return F("dye " + why);
            c.WearOwn("pip", 0); if (c.OutfitOf("pip").Wear[0] != null) return F("back to own hat");
            if (!c.WearIt("pip", "wear:beanie")) return F("swap back to an owned beanie (free)");
            if (c.WearIt("pip", "wear:captain")) return F("wore an unowned cap");
            // Ask the crew: co-op only, above min($5,000, 25%)
            if (c.Cash > 8000) c.Spend("seed2", "Pip", "test", c.Cash - 8000, t); else c.Earn("seed2", "Pip", "test", 8000 - c.Cash, t); // $8,000: limit $2,000
            if (AskLimit(c.Cash) != 2000) return F("ask limit " + AskLimit(c.Cash));
            if (c.NeedsAsk("furn:maptable", 0, "pip", true)) return F("$900 asked");
            if (!c.NeedsAsk("boat:kestrel", 0, "pip", true) || c.NeedsAsk("boat:kestrel", 0, "pip", false)) return F("kestrel ask rule (co-op asks, solo never)");
            var ask = c.AskCrew("pip", "Pip", "boat:kestrel", 0, 1, t);
            if (c.AnswerAsk(ask.Id, "pip", true, true, "Pip", t, out why)) return F("asker approved their own");
            cash = c.Cash; if (!c.AnswerAsk(ask.Id, "rani", false, false, "Rani", t, out _) || c.Cash != cash || c.Owned.Contains("boat:kestrel")) return F("declined spent");
            var ask2 = c.AskCrew("pip", "Pip", "boat:kestrel", 0, 1, t);
            if (!c.AnswerAsk(ask2.Id, "rani", false, true, "Rani", t, out why) || !c.Owned.Contains("boat:kestrel") || c.Cash != cash - 5500) return F("approved not bought: " + why);
            if (c.AnswerAsk(ask2.Id, "bo", false, true, "Bo", t, out _)) return F("answered twice");
            c.Earn("seed3", "Pip", "test", 20000, t);
            var ask3 = c.AskCrew("pip", "Pip", "up:kestrel:engine2", 0, 1, t);        // needs engine1: approved but refused, nothing spent
            cash = c.Cash; c.TickAsks(t + 0.5, k => false); if (ask3.St != AskSt.Open) return F("lapsed before 60 s");
            c.TickAsks(t + 61, k => true); if (ask3.St != AskSt.Open) return F("lapsed with others online");
            c.TickAsks(t + 61, k => false); if (ask3.St != AskSt.Lapsed || c.Cash != cash) return F("lapse with nobody online: " + ask3.St);
            var ask4 = c.AskCrew("pip", "Pip", "up:kestrel:engine1", 0, 1, t + 70); cash = c.Cash; c.TickAsks(t + 70 + 61, k => false);
            if (ask4.St != AskSt.Lapsed || !c.Has("up:kestrel:engine1") || c.Cash != cash - 600) return F("60 s auto-approve");
            // the warehouse: 10 a slot, one species+grade; the cold room keeps fish at 25% rot
            c.Act = 2; c.BuyM5("w2", "pip", "Pip", "wh:2", 0, t, false, out why); c.Tick(DayTime.At(5, 7 * 60)); t = DayTime.At(5, 8 * 60);
            if (c.WhTier != 2 || c.ColdSlots != 12 || c.Wh.Count != 36) return F($"wh:2 ({why}) tier {c.WhTier} {c.Wh.Count}");
            for (int i = 0; i < 25; i++) c.Put(new WFish { Species = "mackerel", Grade = i % 2, CaughtAt = t }, t);
            int stacks = 0; foreach (var s in c.Wh) if (s.Fish.Count > 0) stacks++;
            if (stacks != 4) return F($"25 mackerel in 2 grades: {stacks} stacks (want 4: 10+3, 10+2)");
            var cf = new WFish { Species = "bream", CaughtAt = t }; int cs = c.Put(cf, t, preferCold: true);
            if (!c.Wh[cs].Cold) return F("cold first");
            var back = c.Take(cs, t + 400); if (Math.Abs((t + 400 - back.CaughtAt) - 100) > 1e-6) return F($"cold room age {t + 400 - back.CaughtAt} (want 100)");
            string inv = c.Check() + c.CheckM5(); if (inv != "") return F(inv);
            // save round trip of the whole M5 state
            var j = c.ToJson(); var c2 = Read((Dictionary<string, object>)Json.Parse(j)); if (c2.ToJson() != j) return F("save round trip");
            // random: 3 players, two buying the same thing in the same instant, mornings, asks
            var r = new Random(seed); string[] keys = { "pip", "rani", "bo" }; var items = new List<string>(); foreach (var w in Catalog.All) items.Add(w.Id);
            int bought = 0;
            for (int i = 0; i < n; i++)
            {
                t += r.NextDouble() * 40; c.Tick(t); c.TickAsks(t, k => r.Next(2) == 0);
                string k1 = keys[r.Next(3)], k2 = keys[r.Next(3)], it = items[r.Next(items.Count)]; int arg = r.Next(8) * 100 + r.Next(24);
                if (it == "dye") arg = r.Next(6) * 100 + r.Next(24);
                int before = c.Cash; bool b1 = c.BuyM5("x" + i, k1, k1, it, arg, t, r.Next(4) == 0, out _), b2 = c.BuyM5("y" + i, k2, k2, it, arg, t, false, out _);
                if (b1 && b2 && Catalog.Unique(Catalog.Get(it)) && Catalog.Get(it).Fit != Fit.Repair) return F($"step {i}: {it} bought twice in one instant");
                if (b1 || b2) bought++;
                if (r.Next(10) == 0) c.Earn("e" + i, k1, "test", r.Next(3000), t);
                if (r.Next(25) == 0) { var a = c.AskCrew(k1, k1, it, arg, 1, t); c.AnswerAsk(a.Id, keys[r.Next(3)], r.Next(2) == 0, r.Next(2) == 0, "x", t, out _); }
                if (r.Next(15) == 0) c.Put(new WFish { Species = r.Next(2) == 0 ? "sprat" : "mullet", Grade = r.Next(3), CaughtAt = t }, t, r.Next(2) == 0);
                if (r.Next(15) == 0) c.Take(r.Next(c.Wh.Count), t);
                if (r.Next(30) == 0) c.Act = Math.Min(4, c.Act + 1);
                inv = c.Check() + c.CheckM5(); if (inv != "") return F($"step {i}: {inv}");
            }
            j = c.ToJson(); c2 = Read((Dictionary<string, object>)Json.Parse(j)); if (c2.ToJson() != j) return F("random save round trip");
            return $"PASS rules + {n} random steps ({bought} buys, {c.Owned.Count} owned, {c.WhFish} fish stored)";
        }
    }
}
