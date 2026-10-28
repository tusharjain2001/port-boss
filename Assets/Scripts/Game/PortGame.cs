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

        public NetworkVariable<Ph> Phase = new(Ph.Day);
        public NetworkVariable<int> DayNum = new(1), Money = new(0), Earned = new(0), Sold = new(0), InSea = new(0), LeftAboard = new(0), SpoiledSold = new(0);
        public NetworkVariable<float> DayT = new(0);
        public NetworkVariable<bool> HasTrolley = new(false);
        public NetworkVariable<byte> ContractId = new(0), NextContractId = new(0);
        public NetworkVariable<bool> PierRepaired = new(false);
        public NetworkVariable<float> PierSag = new(0); // degrees the old pier end hangs down (host computes)
        public const int RepairPrice = 80;
        float loadCheck, raidTimer = 30f;
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
            World.SetRepaired(PierRepaired.Value);
            if (IsServer) { StartDay(1); Speedboat.Arrived += RivalTaunt; }
        }

        public override void OnNetworkDespawn() { if (I == this) I = null; Speedboat.Arrived -= RivalTaunt; }

        void RivalTaunt()
        {
            if (Phase.Value != Ph.Day) return;
            Shout("RIVAL MARLOWE", Pick("Nice little pier! Shame if something... rocked it.", "Ha! Still carrying crates by hand?", "Make way! Real port coming through!", "Mind the wake, boss!"), 3.5f);
        }

        void StartDay(int n)
        {
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
            if (!IsServer || Phase.Value != Ph.Day) return;
            GullRaids();
            DayT.Value += Time.deltaTime;
            var sched = Schedule;
            if (nextBoat < sched.Length && DayT.Value >= sched[nextBoat].t)
            {
                var b = Instantiate(GameUI.I.BoatPrefab).GetComponent<Boat>();
                b.Berth.Value = sched[nextBoat].berth;
                b.CargoCount = Mathf.RoundToInt((8 + nextBoat * 3 + (DayNum.Value - 1) * 2) * Today.Cargo);
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
            var candidates = Crate.All.FindAll(c => !c.Attached.Value && !c.InSea && c.transform.position.y > Water.Level + 0.6f && !PlayerWithin(c.transform.position, 5f));
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

        public void Sell(Crate c)
        {
            int v = PriceFor(c);
            Debug.Log($"[PB] sold ${v} at {c.transform.position:F2}");
            Money.Value += v; Earned.Value += v; Sold.Value++;
            if (c.IsSpoiled) SpoiledSold.Value++;
            SoldRpc(World.SellZone.center, v, c.IsSpoiled);
            c.NetworkObject.Despawn();
        }

        public static string Pick(params string[] lines) => lines[Random.Range(0, lines.Length)];

        public void Shout(string who, string text, float secs) => ShoutRpc(who, text, secs);

        [Rpc(SendTo.ClientsAndHost)]
        void ShoutRpc(string who, string text, float secs) => GameUI.Shout(who, text, secs);

        [Rpc(SendTo.ClientsAndHost)]
        public void TootRpc(Vector3 p) => Sfx.Play("horn", p, 0.7f, 1.35f);

        [Rpc(SendTo.ClientsAndHost)]
        void SoldRpc(Vector3 p, int v, bool spoiled)
        {
            Sfx.Play("kaching", p, 1f, spoiled ? 0.8f : 1f);
            GameUI.Popup(p + Vector3.up * 0.5f, spoiled ? $"+${v}  (spoiled)" : $"+${v}", spoiled ? new Color(0.75f, 0.8f, 0.45f) : new Color(1f, 0.85f, 0.3f));
        }

        [Rpc(SendTo.Server)]
        public void BuyTrolleyRpc()
        {
            if (Phase.Value != Ph.Night || HasTrolley.Value || Money.Value < TrolleyPrice) return;
            Money.Value -= TrolleyPrice;
            HasTrolley.Value = true;
            var t = Instantiate(GameUI.I.TrolleyPrefab, World.TrolleySpawn, Quaternion.Euler(0, -90, 0));
            t.GetComponent<NetworkObject>().Spawn(true);
            BoughtRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void BoughtRpc() => Sfx.Play2D("buy");

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
