using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Host-side game state: day clock, boats, money, selling, the night shop. Spawned by the host.
    public class PortGame : NetworkBehaviour
    {
        public enum Ph : byte { Day, Night }
        public static PortGame I;

        public const float DayLength = 480f;
        public const int TrolleyPrice = 120;
        // Boat schedule per day (arrival time, berth): the difficulty dial. Berths are never double-booked.
        static readonly (float t, byte berth)[][] Schedules =
        {
            new (float, byte)[] { (5f, 0), (165f, 0), (325f, 0) },             // day 1: one at a time
            new (float, byte)[] { (5f, 0), (110f, 1), (250f, 0), (330f, 1) },  // day 2: a second boat while the first is in
            new (float, byte)[] { (5f, 0), (80f, 1), (215f, 0), (300f, 1) },   // day 3+: overlapping all day
        };
        (float t, byte berth)[] Schedule => Schedules[Mathf.Clamp(DayNum.Value - 1, 0, Schedules.Length - 1)];
        // Seconds until the next boat sets off for us (-1: none left today). Works on clients: schedule + DayT are shared.
        public float NextBoatIn { get { foreach (var e in Schedule) if (e.t > DayT.Value) return e.t - DayT.Value; return -1; } }
        // Tomorrow, for the end-of-shift recap: how many boats, and when the first one comes in.
        public (int boats, float first) Tomorrow { get { var s = Schedules[Mathf.Clamp(DayNum.Value, 0, Schedules.Length - 1)]; return (s.Length, s[0].t); } }
        public string NextCaptain => Contracts[Mathf.Clamp(NextContractId.Value, 0, Contracts.Length - 1)].Captain;

        // The goalpost: the crew saves toward the next port stage (docs/PORT-STAGES.md). Stage 2 can be built; later ones are goals.
        public NetworkVariable<byte> Stage = new(1);
        static readonly (string name, int cost)[] Stages = { ("Concrete quay", 1500), ("Crane + warehouse", 4000), ("Container berth", 10000), ("Mega port", 25000) };
        public string NextStageName => Stages[Mathf.Clamp(Stage.Value - 1, 0, Stages.Length - 1)].name;
        public int NextStageCost => Stages[Mathf.Clamp(Stage.Value - 1, 0, Stages.Length - 1)].cost;
        public static int QuayCost => Stages[0].cost;

        [Rpc(SendTo.Server)]
        public void BuyStageRpc()
        {
            if (Phase.Value != Ph.Night || Stage.Value != 1 || Money.Value < QuayCost) return;
            Money.Value -= QuayCost;
            Stage.Value = 2;
            BoughtRpc();
            Shout("BUYER", "A proper shed and a forklift yard! Look at us, boss.", 4f);
        }

        public NetworkVariable<Ph> Phase = new(Ph.Day);
        public NetworkVariable<int> DayNum = new(1), Money = new(0), Earned = new(0), Sold = new(0), InSea = new(0), LeftAboard = new(0), SpoiledSold = new(0);
        public NetworkVariable<float> DayT = new(0);
        public NetworkVariable<bool> HasTrolley = new(false);
        public NetworkVariable<byte> ContractId = new(0), NextContractId = new(0);
        public NetworkVariable<bool> PierRepaired = new(false);
        public NetworkVariable<float> PierSag = new(0); // degrees the old pier end hangs down (host computes)
        public const int RepairPrice = 80, ForkliftPrice = 350;
        public NetworkVariable<bool> HasForklift = new(false);
        public bool ForkliftForSale => DayNum.Value >= 2; // unlocks for day 3
        float loadCheck, raidTimer = 30f;
        // Weather: one 0..1 severity (calm → breezy → overcast → rain → storm), host-driven by a random process
        // seeded per session. Nothing announces it; you read it from the sky and the sea.
        public NetworkVariable<float> Severity = new(0f);
        System.Random wx;
        float wxTarget, wxRate, wxNextPick, forcedSeverity = -1f, worstToday;
        public float ForcedSeverity => forcedSeverity;
        Crate raided; float raidPenaltyAt;

        // The contract board on the shed wall: who brings the fish tomorrow.
        public struct Contract { public string Captain, Pitch; public float Cargo, Price, DockSeconds; }
        public static readonly Contract[] Contracts =
        {
            new() { Captain = "MAGS", Pitch = "Steady boats, relaxed captain.\n2:00 at the pier, normal prices.", Cargo = 1f, Price = 1f, DockSeconds = 120f },
            new() { Captain = "BJORN", Pitch = "Big catches, tight tides.\n1:30 at the pier, +50% crates, +25% price.", Cargo = 1.5f, Price = 1.25f, DockSeconds = 90f },
        };
        public Contract Today => Contracts[Mathf.Clamp(ContractId.Value, 0, Contracts.Length - 1)];
        public string CaptainName => "CAPTAIN " + Today.Captain;

        // What the buyer pays for this crate under today's contract.
        public int PriceFor(Crate c) => Mathf.RoundToInt(c.Value * Today.Price);

        public GameObject CratePrefab => GameUI.I.CratePrefab;
        int nextBoat, activeBoats;

        public override void OnNetworkSpawn()
        {
            I = this;
            PierRepaired.OnValueChanged += (_, on) => World.SetRepaired(on);
            Stage.OnValueChanged += (_, s) => HarbourMap.SetStage(s);
            HarbourMap.SetStage(Stage.Value);
            World.SetRepaired(PierRepaired.Value);
            if (IsServer)
            {
                wx = new System.Random(System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode());
                Severity.Value = PickSeverity() * 0.6f; wxTarget = Severity.Value; wxNextPick = Time.time + 30f;
                Storage.Reset(); StartDay(1); RivalTraffic.Docked += RivalOrder;
            }
        }

        public override void OnNetworkDespawn() { if (I == this) I = null; RivalTraffic.Docked -= RivalOrder; }

        // Another freighter ties up at Marlowe's port across the sea: he makes sure we hear about it.
        void RivalOrder()
        {
            if (Phase.Value != Ph.Day) return;
            Shout("RIVAL MARLOWE", Pick("Another mega-freighter just docked at Marlowe Port! Keep up, boss!",
                "Hear that? 230 metres of ship, all mine. How's the fish, boss?",
                "Ship number... I've lost count! Marlowe Port never sleeps!",
                "Look out to sea, boss. THAT is a port."), 4f);
        }

        // (Marlowe's drive-by taunts are Speedboat's: a speech bubble over his head, same moment on every peer.)

        void StartDay(int n)
        {
            worstToday = 0;
            DayNum.Value = n; DayT.Value = 0; nextBoat = 0;
            ContractId.Value = NextContractId.Value;
            Earned.Value = Sold.Value = InSea.Value = LeftAboard.Value = SpoiledSold.Value = 0;
            Phase.Value = Ph.Day;
            if (n == 1)
            {
                // A few crates already on the quay so there's something to do before the first boat.
                for (int i = 0; i < 3; i++) SpawnCrate(new Vector3(-0.8f + i * 0.8f, 0.6f, 3.5f), 1f);
                Shout("BUYER", "Morning, boss! Bring me fish, I pay cash. Fresh pays best.", 5f);
            }
            else Shout("BUYER", Pick($"Day {n}. Same deal: fresh fish, fresh cash.", $"Day {n}! My customers are hungry, boss.", $"Day {n}. Let's see if the pier holds up."), 4f);
        }

        public Crate SpawnCrate(Vector3 p, float fresh)
        {
            var go = Instantiate(CratePrefab, p, Quaternion.Euler(0, Random.Range(0, 360), 0));
            var c = go.GetComponent<Crate>();
            c.Fresh.Value = fresh;
            c.NetworkObject.Spawn(true);
            return c;
        }

        void Update()
        {
            if (IsServer && (loadCheck -= Time.deltaTime) <= 0) { loadCheck = 0.4f; WeighPierEnd(); }
            if (IsServer) UpdateWeather();
            if (!IsServer || Phase.Value != Ph.Day) return;
            if (Intro.Playing) return; // the host is watching the intro: the day starts when it ends
            if (Severity.Value < 0.55f) GullRaids(); // gulls hide in rough weather
            DayT.Value += Time.deltaTime;
            var sched = Schedule;
            if (nextBoat < sched.Length && DayT.Value >= sched[nextBoat].t)
            {
                var b = Instantiate(GameUI.I.BoatPrefab).GetComponent<Boat>();
                b.Berth.Value = sched[nextBoat].berth;
                if (nextBoat == 0) b.HeadStart = 20f;
                b.CargoCount = Mathf.RoundToInt((8 + nextBoat * 3 + (DayNum.Value - 1) * 2) * Today.Cargo * (Stage.Value >= 2 ? 1.3f : 1f)); // the quay draws bigger catches
                b.NetworkObject.Spawn(true);
                activeBoats++; nextBoat++;
                bool last = nextBoat == sched.Length, left = sched[nextBoat - 1].berth == 1;
                Shout("", last ? "*HOOOONK*  Last boat of the day!" : left ? "*HOOOONK*  A boat for the LEFT side of the pier!" : nextBoat == 1 ? "*HOOOONK*  A boat's coming in!" : "*HOOOONK*  Another boat!", 3f);
            }
            if (DayT.Value >= DayLength && nextBoat >= sched.Length && activeBoats <= 0)
            {
                Phase.Value = Ph.Night;
                Shout("BUYER", "That's me done for today. Same time tomorrow!", 3.5f);
            }
        }

        public void BoatLeaving(int leftAboard)
        {
            LeftAboard.Value += leftAboard;
            Shout(CaptainName, leftAboard == 0 ? Pick("Clean deck! Pleasure doing business, boss!", "Not a crate left. You lot are alright!", "Empty! I'll tell the other boats about you.")
                : leftAboard >= 5 ? Pick($"HA! {leftAboard} crates for my supper!", $"{leftAboard} crates still aboard? Your loss, boss!", $"Casting off with {leftAboard} crates. Better luck next tide!")
                : Pick($"Casting off! {leftAboard} crate{(leftAboard == 1 ? "" : "s")} coming home with me.", $"Ropes off! You missed {leftAboard}."), 4f);
        }

        public void BoatGone() => activeBoats--;

        // The host's lorries (RoadTraffic) for everyone else, twice a second.
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        public void TrafficRpc(float t, float[] s, float[] v) => RoadTraffic.Sync(t, s, v);

        // Mostly calm or breezy, sometimes rain, now and then a storm. Any time, any day; calm can break through a storm.
        float PickSeverity()
        {
            double r = wx.NextDouble(), u = wx.NextDouble();
            return (float)(r < 0.5 ? u * 0.18 : r < 0.78 ? 0.2 + u * 0.25 : r < 0.93 ? 0.5 + u * 0.25 : 0.8 + u * 0.2);
        }

        void UpdateWeather()
        {
            float s = Severity.Value;
            if (forcedSeverity >= 0) s = Mathf.MoveTowards(s, forcedSeverity, Time.deltaTime / 3f);
            else
            {
                if (Time.time >= wxNextPick)
                {
                    wxTarget = PickSeverity();
                    float ease = 20f + (float)wx.NextDouble() * 70f;               // drift there over 20-90 s
                    wxRate = Mathf.Max(0.002f, Mathf.Abs(wxTarget - s) / ease);
                    wxNextPick = Time.time + ease + 25f + (float)wx.NextDouble() * 120f; // then hold a while
                }
                s = Mathf.MoveTowards(s, wxTarget, wxRate * Time.deltaTime);
            }
            if (Mathf.Abs(s - Severity.Value) > 0.004f) Severity.Value = s; // don't sync every tick
            if (Phase.Value == Ph.Day) worstToday = Mathf.Max(worstToday, s);
        }

        // Debug (host): force a severity, or < 0 to hand it back to the random weather.
        public void ForceWeather(float s) { forcedSeverity = s; if (s < 0) wxNextPick = Time.time; }

        public static bool InShed(Vector3 p) => p.x > 6.1f && p.x < 11.9f && p.z > -11.4f && p.z < -6.6f;

        // Host: now and then a gull goes for a crate nobody is watching; unguarded, it loses freshness.
        void GullRaids()
        {
            if (raided != null && Time.time > raidPenaltyAt)
            {
                if (raided.IsSpawned && !raided.Attached.Value && !PlayerWithin(raided.transform.position, 3f))
                {
                    raided.Fresh.Value = Mathf.Max(0, raided.Fresh.Value - 0.3f);
                    Shout("BUYER", Pick("Oi! A gull's in your fish!", "That gull just pinched a fish. Guard your crates!", "Shoo! SHOO! ...too late."), 3f);
                }
                raided = null;
            }
            if ((raidTimer -= Time.deltaTime) > 0 || raided != null) return;
            raidTimer = Random.Range(25f, 40f);
            var candidates = Crate.All.FindAll(c => !c.Attached.Value && !c.Stored && !c.InSea && c.transform.position.y > Water.Level + 0.6f && !PlayerWithin(c.transform.position, 5f));
            if (candidates.Count == 0) return;
            raided = candidates[Random.Range(0, candidates.Count)];
            raidPenaltyAt = Time.time + 7f;
            RaidRpc(raided.NetworkObject);
        }

        static bool PlayerWithin(Vector3 p, float r)
        {
            foreach (var pl in Player.All) if ((pl.transform.position - p).sqrMagnitude < r * r) return true;
            return false;
        }

        [Rpc(SendTo.ClientsAndHost)]
        void RaidRpc(NetworkObjectReference r) { if (r.TryGet(out var no)) Gull.Raid(no.transform); }

        // Host: how much is standing on the old pier end? Crates count 1, people 3 (+ what they carry).
        void WeighPierEnd()
        {
            if (Skin.On) return; // the harbour's jetty is concrete: it doesn't sag
            var end = World.PierEnd;
            if (end == null) return;
            int load = 0;
            bool On(Vector3 w) { var l = end.InverseTransformPoint(w); return Mathf.Abs(l.x) < 3.1f && l.z > 0 && l.z < 10.5f && l.y > -0.5f && l.y < 3f; }
            foreach (var c in Crate.All) if (!c.Attached.Value && On(c.transform.position)) load++;
            foreach (var p in Player.All) if (On(p.transform.position)) load += 3 + p.Carrying;
            int cap = PierRepaired.Value ? 20 : 10;
            float sag = Mathf.Min(9f, Mathf.Max(0, load - cap) * 0.9f);
            sag = Mathf.Round(sag * 2) / 2;
            if (sag > PierSag.Value + 0.1f && PierSag.Value == 0) Shout("BUYER", PortGame.Pick("That old pier end won't take much more!", "Mind the far end, boss, it's groaning!"), 3f);
            if (!Mathf.Approximately(sag, PierSag.Value)) PierSag.Value = sag;
        }

        // Test hook: jump straight to day n (schedule, contract).
        public void DebugStartDay(int n) { EndDayNow(); StartDay(n); }

        // Test hook: skip to the end of the day.
        public void EndDayNow()
        {
            foreach (var b in Boat.All.ToArray()) b.NetworkObject.Despawn();
            activeBoats = 0; nextBoat = Schedule.Length; DayT.Value = DayLength;
        }

        public void CrateInSea() => InSea.Value++;

        public void Sell(System.Collections.Generic.List<Crate> crates)
        {
            int total = 0, spoiled = 0;
            foreach (var c in crates)
            {
                int v = PriceFor(c);
                Debug.Log($"[PB] sold ${v} at {c.transform.position:F2}");
                total += v;
                if (c.IsSpoiled) spoiled++;
                c.NetworkObject.Despawn();
            }
            Money.Value += total; Earned.Value += total; Sold.Value += crates.Count; SpoiledSold.Value += spoiled;
            SoldRpc(World.SellZone.center, total, crates.Count, spoiled);
        }

        public static string Pick(params string[] lines) => lines[Random.Range(0, lines.Length)];

        public void Shout(string who, string text, float secs) { Debug.Log($"[PB] shout {(who == "" ? "-" : who)}: {text}"); ShoutRpc(who, text, secs); }

        [Rpc(SendTo.ClientsAndHost)]
        void ShoutRpc(string who, string text, float secs) => GameUI.Shout(who, text, secs);

        [Rpc(SendTo.ClientsAndHost)]
        public void TootRpc(Vector3 p) => Sfx.Play("horn", p, 0.7f, 1.35f);

        [Rpc(SendTo.ClientsAndHost)]
        void SoldRpc(Vector3 p, int v, int crates, int spoiled)
        {
            bool allSpoiled = spoiled == crates;
            Sfx.Play("kaching", p, 1f, allSpoiled ? 0.8f : 1f);
            string what = crates == 1 ? (allSpoiled ? "  (spoiled)" : "") : $"  for {crates} crates" + (spoiled > 0 ? $" ({spoiled} spoiled)" : "");
            GameUI.Popup(p + Vector3.up * 0.5f, $"+${v}{what}", allSpoiled ? new Color(0.75f, 0.8f, 0.45f) : new Color(1f, 0.85f, 0.3f));
        }

        [Rpc(SendTo.Server)]
        public void BuyTrolleyRpc()
        {
            if (Phase.Value != Ph.Night || HasTrolley.Value || Money.Value < TrolleyPrice) return;
            Money.Value -= TrolleyPrice;
            SpawnTrolley();
            BoughtRpc();
        }

        void SpawnTrolley()
        {
            HasTrolley.Value = true;
            Instantiate(GameUI.I.TrolleyPrefab, World.TrolleySpawn, Quaternion.Euler(0, -90, 0)).GetComponent<NetworkObject>().Spawn(true);
        }

        // Tests: the day-3 kit without the night card.
        public void DebugGrantKit() { if (!HasTrolley.Value) SpawnTrolley(); if (!HasForklift.Value) SpawnForklift(); }

        [Rpc(SendTo.ClientsAndHost)]
        void BoughtRpc() => Sfx.Play2D("buy");

        [Rpc(SendTo.Server)]
        public void BuyForkliftRpc()
        {
            if (Phase.Value != Ph.Night || HasForklift.Value || !ForkliftForSale || Money.Value < ForkliftPrice) return;
            Money.Value -= ForkliftPrice;
            SpawnForklift();
            BoughtRpc();
        }

        void SpawnForklift()
        {
            HasForklift.Value = true;
            Instantiate(GameUI.I.ForkliftPrefab, new Vector3(19f, 0.4f, -12f), Quaternion.Euler(0, -90, 0)).GetComponent<NetworkObject>().Spawn(true);
            // Two pallets by the pier, if there aren't any about.
            if (Pallet.All.Count < 2)
                foreach (var z in new[] { -2.2f, -4.2f })
                    Instantiate(GameUI.I.PalletPrefab, new Vector3(7f, 0.2f, z), Quaternion.identity).GetComponent<NetworkObject>().Spawn(true);
        }

        public void ForkliftLost()
        {
            HasForklift.Value = false;
            Shout("BUYER", Pick("Your forklift... is a submarine now.", "That is a forklift in the harbour. Expensive fish food."), 3.5f);
        }

        [Rpc(SendTo.Server)]
        public void BuyRepairRpc()
        {
            if (Phase.Value != Ph.Night || PierRepaired.Value || Money.Value < RepairPrice) return;
            Money.Value -= RepairPrice;
            PierRepaired.Value = true;
            BoughtRpc();
        }

        [Rpc(SendTo.Server)]
        public void ChooseContractRpc(byte id)
        {
            if (Phase.Value == Ph.Night && id < Contracts.Length) NextContractId.Value = id;
        }

        // One line for the end-of-day card, picked from how the day went.
        public string DayVerdict()
        {
            int sold = Sold.Value, sea = InSea.Value, left = LeftAboard.Value, spoiled = SpoiledSold.Value;
            if (sold == 0) return "The buyer waited all day. He brought sandwiches.";
            if (worstToday > 0.75f && sea >= 4) return $"The storm took {sea} crates. The shed was RIGHT THERE.";
            if (sea >= 5 && sea >= sold) return "More fish went in the sea than in the buyer's van. The gulls send their thanks.";
            if (left >= 10) return $"Captain {Today.Captain} sailed home with {left} crates. Tonight, they eat well.";
            if (spoiled >= 4) return "The flies have started calling you 'boss' too.";
            if (sea >= 3) return $"{sea} crates are drifting toward the rival port. Marlowe says thanks.";
            if (left == 0 && sea == 0) return "Not a crate lost. The harbour is starting to talk about you.";
            return "Not bad for a pier with holes in it.";
        }

        [Rpc(SendTo.Server)]
        public void NextDayRpc()
        {
            if (Phase.Value == Ph.Night) StartDay(DayNum.Value + 1);
        }

        public void TrolleyLost()
        {
            HasTrolley.Value = false;
            Shout("BUYER", "Was that... your trolley? In the sea?", 3.5f);
        }
    }
}
