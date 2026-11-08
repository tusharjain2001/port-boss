using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M4 in the game: the company (Company.cs) living in the world. The host (or solo) runs its clock; every machine
    // shows it (CoopFirm.cs sends it). Here:
    // - the shop counters on the map's named anchors (map B: anchor_tackle, anchor_pratts, anchor_mari_mooring,
    //   anchor_mari_boat; until map B is in, temporary spots on the M1 Landing): Hook, Line & Sinker (June Pike, bait +
    //   tackle + rods, 05:00-19:00), Pratt's Fish Market (Gideon Pratt, 60%, 05:00-16:00), and Mari's Tacos once she's
    //   moored (100%, 09:00-22:00);
    // - Mari's taco boat: hidden until she ties up (within the in-game hour of "Accept"), then at her berth;
    // - my kit (rod tier, bait pouch) is the company's record of my gear, so it saves with the world;
    // - selling and buying: solo and on the host at once; a client asks the host (CoopFirm), and the host's answer counts;
    // - the LEFT toasts for every new text ("Tab · Open phone"), my sales, my buys and the morning's rent.
    public class Firm : MonoBehaviour
    {
        public static Firm I;
        public static Company C => I != null ? I.c : null;
        public static System.Action<string> OnLog;
        Company c = new();
        public static string MyKey => Coop.LocalKey;
        public static string MyName => Coop.On ? Coop.MyName : "You";
        public Kit MyKit => c.KitOf(MyKey);
        public bool Dirty;                                      // (host) the crew's copies are out of date
        int seenMsg, seenLedger;                                // what has been toasted already
        uint txNext; readonly string txSeed = System.Guid.NewGuid().ToString("N").Substring(0, 6);
        public string NewTx(string what) => $"{what}:{txSeed}:{++txNext}";

        public enum Kind { June, Pratt, Mari, Pelican }
        public class Stall
        {
            public Kind K; public string Id, Name, Owner, Line; public Transform At; public Vector3 Front; public bool Real;
            public double Open, Close;
            public bool IsOpen(double m) { double h = DayTime.OfDay(m); return h >= Open && h < Close; }
        }
        public readonly List<Stall> Stalls = new();
        public Stall Near;                                      // the counter I'm at (the prompt)
        Transform mariBoat; Vector3 mariBerth; Quaternion mariRot; bool ownMariBoat;

        static void Log(string s) { Debug.Log("[PB] firm: " + s); OnLog?.Invoke(s); }

        public static Firm Create()
        {
            var g = new GameObject("Firm"); I = g.AddComponent<Firm>();
            Company.Namer = id => FishDb.Get(id)?.Name ?? id;
            // M7: Pratt's morning price list, at 60% of the Basic price of the everyday fish
            Company.PriceList = day => string.Join(" · ", FishDb.All.FindAll(s => s.Class == FishClass.Everyday).ConvertAll(s => $"{s.Name} ${Market.Price(s.Price, 0, Buyer.Pratt, 0)}"));
            I.BuildStalls(); I.Bind();
            return I;
        }

        // a fresh world (the title's "New game"), or one loaded from a save
        public void Use(Company co)
        {
            c = co; seenMsg = 0; foreach (var m in c.Msgs) seenMsg = Mathf.Max(seenMsg, m.Id); seenLedger = c.Ledger.Count;
            StoryUi.I?.Mark(c);                                 // M7: the story so far isn't news (a loaded world)
            Bind(); Dirty = true;
        }

        // (co-op client) the host's company: the bank, the Ledger, the texts, the deals. My pouch stays mine (I spend
        // bait on every catch); the first time, the host's record of my kit (a world we fished before) wins.
        bool synced;
        public void Sync(Company co)
        {
            var mine = MyKit;
            if (!synced && co.Kits.TryGetValue(MyKey, out var theirs)) { mine.Rod = theirs.Rod; mine.Bait = theirs.Bait; mine.BaitI = theirs.BaitI; mine.Pouch5 = theirs.Pouch5; mine.Best = theirs.Best; }
            co.Kits[MyKey] = mine;
            if (!synced) { seenMsg = 0; foreach (var m in co.Msgs) seenMsg = Mathf.Max(seenMsg, m.Id); seenLedger = co.Ledger.Count; StoryUi.I?.Mark(co); Log($"the crew's bank: ${co.Cash}, {co.Ledger.Count} ledger lines"); }
            synced = true; c = co;
            if (seenLedger > c.Ledger.Count) seenLedger = c.Ledger.Count;
            Bind();
        }

        // my rod and bait pouch are my kit (the company's record): the fishing reads and spends it directly
        public void Bind()
        {
            var me = Angler.I; if (me == null) return;
            var k = MyKit; me.Fish.Kit = k;
            if (me.Fish.RodTier != k.Rod) me.Fish.SetRod(k.Rod, true);
        }

        // ---------------------------------------------------------------- the clock and the toasts
        void Update()
        {
            if (Sky.Authority)
            {
                c.Tick(Sky.Minutes);
                // M7: what the clock changed by itself (a story beat, Mari's texts, the morning's rent, a repair done) goes to
                // the crew too: a cheap fingerprint of the company, checked every frame (only co-op reads Dirty)
                long sig = Sig(c); if (sig != lastSig) { lastSig = sig; Dirty = true; }
            }
            Toasts();
            MariBoat();
            var me = Angler.I; Near = null;
            if (me != null && !me.Remote) foreach (var s in Stalls)
                {
                    if (s.K == Kind.Mari && !c.MariHere(Sky.Minutes)) continue;
                    var d = s.Front - me.transform.position; d.y *= 0.5f;
                    if (d.magnitude < 2.0f) { Near = s; break; }
                }
        }

        long lastSig;
        static long Sig(Company c) => (((((((long)c.Msgs.Count * 131 + c.Ledger.Count) * 131 + c.BeatAt.Count) * 131 + c.Events.Count) * 131 + c.Act) * 131 + (int)c.Mari) * 131 + c.Owned.Count) * 131 + c.Orders.Count;

        void Toasts()
        {
            foreach (var m in c.Msgs)
            {
                if (m.Id <= seenMsg) continue;
                seenMsg = m.Id;
                if (!m.Me) Alerts.Push(m.From, m.Text, Alerts.Kind.Message, "text:" + m.From, null, "Open phone");
            }
            for (; seenLedger < c.Ledger.Count; seenLedger++)
            {
                var e = c.Ledger[seenLedger];
                if (e.Tx.StartsWith("rent:")) Alerts.Push("Mari Duarte", $"Rent's in, landlord. +${e.Amount}", Alerts.Kind.Money, "rent");
                else if (e.Who != MyName) continue;                 // a crewmate's sale: their toast, the night card's line
                else if (e.Amount > 0) { Alerts.Push("Sold", $"+${e.Amount} · {e.What.Replace("Sold ", "")}", Alerts.Kind.Money); Snd.Play2D(Synth.Chime, 0.35f, 1.15f); }
                else Alerts.Push("Bought", $"−${-e.Amount} · {e.What}", Alerts.Kind.Money);
            }
        }

        // ---------------------------------------------------------------- selling (the hotbar's fish)
        public static Company.Lot Lot(Fish f) => new() { Species = f.Species.Id, Everyday = f.Species.Class == FishClass.Everyday, Value = f.Value, Fresh = f.Fresh, Grade = f.Grade };
        public int[] Quote(Buyer b, List<Fish> fish, out string why) => c.Quote(b, fish.ConvertAll(Lot), Sky.Minutes, out why);

        // sells these fish of my hotbar; the toast says how it went (co-op client: when the host answers)
        public void Sell(Buyer b, List<Fish> fish)
        {
            fish = fish.FindAll(f => !Selling.Contains(f.Id));     // a double tap while the host answers: once only
            if (fish.Count == 0) return;
            var tx = NewTx("sell");
            if (Coop.On && !Coop.IsHost) { var ids = fish.ConvertAll(f => f.Id); asked[tx] = ids; Selling.UnionWith(ids); CoopFirm.AskSell(tx, b, fish); return; }
            var p = c.Sell(tx, MyName, b, fish.ConvertAll(Lot), Sky.Minutes, out var why);
            if (p == null) { Alerts.Push(Market.Name[(int)b], why, Alerts.Kind.Info); return; }
            if (Coop.On) Coop.I.Book.Sell(Coop.Me, fish.ConvertAll(f => f.Id)); // the host's hotbar is the book's: it goes from both
            else foreach (var f in fish) Angler.I.Bar.Remove(f);
            Sold(b, fish.Count, p);
            Dirty = true;
        }
        // (co-op client) fish asked for, not answered yet: the shop leaves them out
        public readonly HashSet<ulong> Selling = new(); readonly Dictionary<string, List<ulong>> asked = new();
        public void Answered(string tx) { if (asked.Remove(tx, out var ids)) Selling.ExceptWith(ids); }
        public static void Sold(Buyer b, int n, int[] p)
        {
            int sum = 0; foreach (var x in p) sum += x;
            Log($"sold {n} fish to {Market.Short[(int)b]} for ${sum} ({string.Join(" ", p)})");
        }

        // June's shelf: buys for me from the company bank (co-op client: the host's answer counts)
        public void Buy(string item, int qty)
        {
            var tx = NewTx("buy");
            if (Coop.On && !Coop.IsHost) { CoopFirm.AskBuy(tx, item, qty); return; }
            if (!c.Buy(tx, MyKey, MyName, item, qty, Sky.Minutes, out var why)) { Alerts.Push("Hook, Line & Sinker", why, Alerts.Kind.Info); return; }
            Bought(item, qty);
            Dirty = true;
        }
        public void Bought(string item, int qty)
        {
            Log($"bought {Company.ItemName(item, qty)}: bank ${c.Cash}");
            Snd.Play2D(Synth.Chime, 0.3f, 1.3f);
            if (item.StartsWith("rod:")) Bind();
        }

        public void AnswerMari(bool yes)
        {
            if (Coop.On && !Coop.IsHost) { CoopFirm.AskDeal(yes ? 1 : 0); return; }
            if (c.AnswerMari(yes, MyName, Sky.Minutes)) { Log($"Mari's mooring: {(yes ? "accepted" : "declined")} by {MyName}"); Dirty = true; }
        }
        public void HaggleMari()
        {
            if (Coop.On && !Coop.IsHost) { CoopFirm.AskDeal(2); return; }
            c.HaggleMari(Sky.Minutes); Dirty = true;
        }
        // M7 step 3: fish orders (Orders.cs). Answer one; deliver at the buyer's counter from my hotbar.
        public void AnswerOrd(int id, bool yes)
        {
            if (Coop.On && !Coop.IsHost) { CoopFirm.AskOrd(id, yes); return; }
            if (c.AnswerOrd(id, yes, MyName, Sky.Minutes)) { Log($"order {id}: {(yes ? "accepted" : "declined")} by {MyName}"); Dirty = true; }
        }
        public void Deliver(Company.Ord o)
        {
            var me = Angler.I; if (me == null || o == null) return;
            var mine = new List<Fish>(); foreach (var s in me.Bar.S) foreach (var f in s.Fish) if (!Selling.Contains(f.Id)) mine.Add(f);
            var take = Company.Fits(o, mine.ConvertAll(Lot), out var why);
            if (take == null) { Alerts.Push(o.From, why, Alerts.Kind.Info, "order"); return; }
            var fish = take.ConvertAll(i => mine[i]); var tx = NewTx("deliver");
            if (Coop.On && !Coop.IsHost) { var ids = fish.ConvertAll(f => f.Id); Selling.UnionWith(ids); asked[tx] = ids; CoopFirm.AskDeliver(tx, o.Id, fish); return; }
            if (!c.Deliver(tx, MyName, o.Id, fish.ConvertAll(Lot), Sky.Minutes, out why)) { Alerts.Push(o.From, why, Alerts.Kind.Info, "order"); return; }
            if (Coop.On) Coop.I.Book.Sell(Coop.Me, fish.ConvertAll(f => f.Id)); else foreach (var f in fish) me.Bar.Remove(f);
            Log($"delivered {o.From}'s order {o.Id}: {o.Count} {o.What} for ${o.Total}"); Snd.Play2D(Synth.Fanfare, 0.35f, 1.1f);
            Dirty = true;
        }
        public void Track(bool on) { if (Coop.On && !Coop.IsHost) { CoopFirm.AskDeal(on ? 3 : 4); return; } c.MariTracked = on; Dirty = true; }

        // ---------------------------------------------------------------- the counters
        // M7 step 3: a counter placed later (The Pelican, on Lantern Key: Pelican.cs)
        public void AddStall(Stall s) { s.Front = s.At.position + s.At.forward * 1.1f; Stalls.Add(s); Log($"{s.Name} at {s.At.position.x:F1} {s.At.position.y:F2} {s.At.position.z:F1}"); }
        public static Transform Anchor(string id) { var g = GameObject.Find("anchor_" + id); return g != null ? g.transform : null; }

        // where a counter goes: the map's anchor, else a temporary spot on the M1 Landing (Blender metres in the Landing's
        // frame, the same mapping as Walt's cot), facing the pier
        Transform Spot(string id, Vector2 blender, Vector2 fwd, out bool real)
        {
            var a = Anchor(id); real = a != null;
            if (real) return a;
            var l = Landing.I; var t = new GameObject("anchor_" + id + " (temp)").transform; t.SetParent(transform, false);
            if (l == null || l.Spawn == null || l.LadderTop == null) return t;
            Vector3 B(Vector2 p) => Sky.FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), new Vector3(p.x, p.y, 0));
            var p0 = B(blender); var dir = B(blender + fwd) - p0; dir.y = 0;
            float gy = p0.y; if (Physics.Raycast(new Vector3(p0.x, 60, p0.z), Vector3.down, out var hit, 120, ~0, QueryTriggerInteraction.Ignore)) gy = hit.point.y;
            t.SetPositionAndRotation(new Vector3(p0.x, gy, p0.z), Quaternion.LookRotation(dir.normalized));
            return t;
        }

        void BuildStalls()
        {
            var june = Spot("tackle", new Vector2(-1.2f, 8.8f), new Vector2(0, -1), out bool rj);
            var pratt = Spot("pratts", new Vector2(2.9f, 8.8f), new Vector2(0, -1), out bool rp);
            var moor = Spot("mari_mooring", new Vector2(-1.1f, -14.5f), new Vector2(-1, 0), out bool rm);
            Stalls.Add(new Stall { K = Kind.June, Id = "tackle", Name = "Hook, Line & Sinker", Owner = "June Pike", At = june, Real = rj, Open = 300, Close = 1140,
                Line = "Worms won't win a beauty contest, but the mackerel don't care." });
            Stalls.Add(new Stall { K = Kind.Pratt, Id = "pratts", Name = "Pratt's Fish Market", Owner = "Gideon Pratt", At = pratt, Real = rp, Open = Market.Open[0], Close = Market.Close[0],
                Line = "Take it or watch it rot." });
            Stalls.Add(new Stall { K = Kind.Mari, Id = "mari_mooring", Name = "Mari's Tacos", Owner = "Mari Duarte", At = moor, Real = rm, Open = Market.Open[1], Close = Market.Close[1],
                Line = "Fresh off your boat, straight into a taco." });
            foreach (var s in Stalls)
            {
                s.Front = s.At.position + s.At.forward * (s.K == Kind.Mari ? 0.2f : 1.1f);
                if (s.K != Kind.Mari) Counter(s);
                Log($"{s.Name} at {(s.Real ? "map anchor" : "temporary spot")} anchor_{s.Id} {s.At.position.x:F1} {s.At.position.y:F2} {s.At.position.z:F1}");
            }
            // Mari's boat: map B's blockout (hidden until she moors), else one of our own at the berth
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) if (t.name == "MarisTacoBoat" && t.gameObject.scene.IsValid()) { mariBoat = t; break; }
            var berth = Spot("mari_boat", new Vector2(-3.3f, -14.5f), new Vector2(0, -1), out _);
            if (mariBoat == null) { mariBoat = TacoBoat().transform; ownMariBoat = true; mariBerth = new Vector3(berth.position.x, 0, berth.position.z); mariRot = berth.rotation; }
            mariBoat.gameObject.SetActive(false);
        }

        static Material Mat(Color c) { var m = new Material(Game.ToonMat); m.SetColor("_BaseColor", c); return m; }
        internal static GameObject Box(Transform root, string n, Vector3 p, Vector3 s, Color col, Vector3 rot = default)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube); q.name = n; Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(root, false); q.transform.localPosition = p; q.transform.localRotation = Quaternion.Euler(rot); q.transform.localScale = s;
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(col); return q;
        }
        internal static void Sign(Transform root, string text, Vector3 p, Color col, float size)
        {
            var g = new GameObject("sign text"); g.transform.SetParent(root, false); g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var tm = g.AddComponent<TextMesh>(); tm.text = text; tm.font = UiKit.DisplayBold; tm.fontSize = 96; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = col;
            g.GetComponent<MeshRenderer>().sharedMaterial = SignMat();
        }
        // the font's own material draws through walls in URP (GUI/Text Shader; setting unity_GUIZTestMode on a copy did nothing:
        // the shader doesn't declare it, so the global wins): our own depth-tested text shader (PB_Text) on the font's atlas
        static Material signMat;
        static Material SignMat()
        {
            if (signMat != null) return signMat;
            var sh = M1Assets.I != null ? M1Assets.I.Text : null;
            signMat = sh != null ? new Material(sh) { name = "sign text (depth tested)", mainTexture = UiKit.DisplayBold.material.mainTexture }
                                 : new Material(UiKit.DisplayBold.material) { name = "sign text" };
            Font.textureRebuilt += f => { if (f == UiKit.DisplayBold && signMat != null) signMat.mainTexture = f.material.mainTexture; };
            return signMat;
        }

        // a market counter: a plank counter, a striped awning on two posts, the shop's name painted on a board. +Z is the
        // customer's side. ponytail: code-built until each shop has its Blender model (M5 shops).
        // On a map-B anchor the shop building is map B's (June's house, Pratt's market hall, 1 m behind the anchor): then
        // only the counter and a small name board on it, at the door (Kufu 4 Nov: one building per place, not two).
        void Counter(Stall s)
        {
            var root = new GameObject("Counter " + s.Name).transform; root.SetParent(s.At, false);
            Color wood = Ponyo.Hex("9A7352"), woodDk = Ponyo.Hex("6E5038"), top = Ponyo.Hex("C9A27A");
            Color a1 = s.K == Kind.June ? Ponyo.Hex("46B3A6") : Ponyo.Hex("8D96A3"), a2 = Ponyo.Hex("FFF6E2");
            Box(root, "counter", new Vector3(0, 0.47f, 0.1f), new Vector3(1.9f, 0.94f, 0.62f), wood);
            Box(root, "counter top", new Vector3(0, 0.96f, 0.14f), new Vector3(2.02f, 0.06f, 0.74f), top);
            Box(root, "kick", new Vector3(0, 0.07f, 0.42f), new Vector3(1.86f, 0.14f, 0.02f), woodDk);
            string name = s.K == Kind.June ? "Hook, Line & Sinker" : "Pratt's Fish Market";
            if (s.Real)
            {
                for (int i = -1; i <= 1; i += 2) Box(root, "sign post", new Vector3(0.72f * i, 1.4f, -0.15f), new Vector3(0.05f, 0.82f, 0.05f), woodDk);
                Box(root, "board", new Vector3(0, 1.6f, -0.15f), new Vector3(1.6f, 0.34f, 0.05f), Ponyo.Hex("F3EBDD"));
                Box(root, "board edge", new Vector3(0, 1.6f, -0.185f), new Vector3(1.68f, 0.42f, 0.02f), woodDk);
                Box(root, "board stripe", new Vector3(0, 1.79f, -0.15f), new Vector3(1.68f, 0.05f, 0.06f), a1);
                Sign(root, name, new Vector3(0, 1.6f, -0.12f), Ponyo.Ink, 0.0098f);
            }
            else
            {
                for (int i = -1; i <= 1; i += 2) Box(root, "post", new Vector3(0.98f * i, 1.25f, -0.18f), new Vector3(0.08f, 2.5f, 0.08f), woodDk);
                for (int k = 0; k < 6; k++) Box(root, "awning", new Vector3(-0.85f + k * 0.34f, 2.42f, 0.18f), new Vector3(0.34f, 0.04f, 1.0f), k % 2 == 0 ? a1 : a2, new Vector3(-14, 0, 0));
                Box(root, "board", new Vector3(0, 2.72f, -0.18f), new Vector3(1.9f, 0.42f, 0.06f), Ponyo.Hex("F3EBDD"));
                Box(root, "board edge", new Vector3(0, 2.72f, -0.215f), new Vector3(1.98f, 0.5f, 0.02f), woodDk);
                Sign(root, name, new Vector3(0, 2.72f, -0.15f), Ponyo.Ink, 0.0125f);
            }
            // goods on the counter: bait tubs at June's, an ice tray at Pratt's
            if (s.K == Kind.June) for (int i = 0; i < 3; i++) Box(root, "tub", new Vector3(-0.55f + i * 0.5f, 1.07f, 0.12f), new Vector3(0.3f, 0.16f, 0.3f), i == 0 ? Ponyo.Peach : i == 1 ? Ponyo.Hex("F2B8A0") : Ponyo.Hex("C9D6E0"));
            else { Box(root, "ice", new Vector3(0, 1.02f, 0.14f), new Vector3(1.5f, 0.06f, 0.5f), Ponyo.Hex("DCEFF5")); Box(root, "scale", new Vector3(0.7f, 1.12f, 0.1f), new Vector3(0.24f, 0.16f, 0.2f), Ponyo.Hex("B9B2A6")); }
            var col = root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.5f, 0.1f); col.size = new Vector3(2.0f, 1.0f, 0.7f);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // Mari's Tacos: a little blue-and-yellow boat with an awning and her sign. ponytail: code-built until map B's
        // blockout (or its Blender model) is in; then that one is shown instead.
        GameObject TacoBoat()
        {
            var root = new GameObject("MarisTacoBoat (temp)").transform; root.SetParent(transform, false);
            Color hull = Ponyo.Hex("3A86E0"), trim = Ponyo.Hex("F7C548"), cabin = Ponyo.Hex("FFF6E2"), red = Ponyo.Hex("E2553F");
            Box(root, "hull", new Vector3(0, 0.25f, 0), new Vector3(2.4f, 0.8f, 6.4f), hull);
            Box(root, "bow", new Vector3(0, 0.3f, 3.5f), new Vector3(1.7f, 0.7f, 1.2f), hull, new Vector3(0, 45, 0));
            Box(root, "rail", new Vector3(0, 0.7f, 0), new Vector3(2.5f, 0.1f, 6.5f), trim);
            Box(root, "deck", new Vector3(0, 0.66f, 0), new Vector3(2.2f, 0.05f, 6.2f), Ponyo.Hex("C9A27A"));
            Box(root, "cabin", new Vector3(0, 1.45f, -0.6f), new Vector3(2.0f, 1.5f, 2.6f), cabin);
            Box(root, "hatch", new Vector3(-1.01f, 1.4f, -0.6f), new Vector3(0.04f, 0.8f, 1.8f), trim);
            for (int k = 0; k < 7; k++) Box(root, "awning", new Vector3(-1.4f, 2.1f, -1.8f + k * 0.4f), new Vector3(1.0f, 0.04f, 0.4f), k % 2 == 0 ? red : cabin, new Vector3(0, 0, 18));
            Box(root, "board", new Vector3(0, 2.5f, -0.6f), new Vector3(0.06f, 0.5f, 2.2f), trim);
            var s = new GameObject("sign").transform; s.SetParent(root, false); s.localPosition = new Vector3(-0.05f, 2.5f, -0.6f); s.localRotation = Quaternion.Euler(0, 90, 0);
            Sign(s, "Mari's Tacos", Vector3.zero, red, 0.014f);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return root.gameObject;
        }

        // she motors in over the last 40 in-game minutes before she ties up, then rides the waves at her berth
        void MariBoat()
        {
            if (mariBoat == null) return;
            double m = Sky.Minutes, at = c.MariAccepted + Company.MariComes;
            bool show = c.Mari == DealSt.Active && m >= at - 40;
            if (mariBoat.gameObject.activeSelf != show) { mariBoat.gameObject.SetActive(show); if (show) Log($"Mari's boat {(m >= at ? "at her berth" : "on her way in")}"); }
            if (!show || !ownMariBoat) return;
            float k = Mathf.Clamp01((float)((m - (at - 40)) / 40)); k = 1 - (1 - k) * (1 - k);
            var from = mariBerth - (mariRot * Vector3.forward) * 70;
            var p = Vector3.Lerp(from, mariBerth, k); p.y = Sea.Height(p) - 0.15f;
            mariBoat.SetPositionAndRotation(p, mariRot * Quaternion.Euler(Mathf.Sin(Time.time * 0.9f) * 1.5f, 0, Mathf.Sin(Time.time * 0.7f) * 2f));
        }

        // the E prompt at a counter (Angler.Interact): opens the shop panel
        public static string Prompt(Angler me)
        {
            if (I == null || me.Remote || me.Swimming || me.OnBoat || Shop.Open) return null;
            var s = I.Near; if (s == null) return null;
            if (!s.IsOpen(Sky.Minutes)) return $"{s.Name} · closed, back at {DayTime.Clock(s.Open)}";
            if (s.K == Kind.Pelican)                                    // M7: Amara's kitchen door: deliver her order
            {
                var o = C.OpenOrd(Company.AmaraName);
                if (o == null || o.St != OrdSt.Active) return o != null ? "The Pelican · answer Amara's order on your phone first" : "The Pelican · Amara: \"Nothing on today. I'll text.\"";
                int have = 0; foreach (var sl in me.Bar.S) foreach (var f in sl.Fish) if (f.Species.Id == o.Species && f.Grade >= o.Grade && f.Fresh < 2) have++;
                if (have < o.Count) return $"Amara's order · {o.Count} {(o.Grade > 0 ? "Rare " : "")}{o.What.ToLowerInvariant()} · you have {have}";
                if (Ctl.Interact) I.Deliver(o);
                return $"Deliver Amara's order · {o.Count} {(o.Grade > 0 ? "Rare " : "")}{o.What.ToLowerInvariant()} · ${o.Total}";
            }
            if (Ctl.Interact) Shop.Show(s);
            return s.K == Kind.June ? "Hook, Line & Sinker · bait and tackle" : s.K == Kind.Pratt ? "Sell at Pratt's (60%)" : "Sell to Mari (100%)";
        }
    }
}
