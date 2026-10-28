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
        static readonly float[] BoatTimes = { 5f, 165f, 325f };

        public NetworkVariable<Ph> Phase = new(Ph.Day);
        public NetworkVariable<int> DayNum = new(1), Money = new(0), Earned = new(0), Sold = new(0), InSea = new(0), LeftAboard = new(0), SpoiledSold = new(0);
        public NetworkVariable<float> DayT = new(0);
        public NetworkVariable<bool> HasTrolley = new(false);

        public GameObject CratePrefab => GameUI.I.CratePrefab;
        int nextBoat, activeBoats;

        public override void OnNetworkSpawn()
        {
            I = this;
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
            if (!IsServer || Phase.Value != Ph.Day) return;
            DayT.Value += Time.deltaTime;
            if (nextBoat < BoatTimes.Length && DayT.Value >= BoatTimes[nextBoat])
            {
                var b = Instantiate(GameUI.I.BoatPrefab).GetComponent<Boat>();
                b.CargoCount = 8 + nextBoat * 3 + (DayNum.Value - 1) * 2;
                b.NetworkObject.Spawn(true);
                activeBoats++; nextBoat++;
                Shout("", nextBoat == 1 ? "*HOOOONK*  A boat's coming in!" : nextBoat == 2 ? "*HOOOONK*  Second boat!" : "*HOOOONK*  Last boat of the day!", 3f);
            }
            if (DayT.Value >= DayLength && nextBoat >= BoatTimes.Length && activeBoats <= 0)
            {
                Phase.Value = Ph.Night;
                Shout("BUYER", "That's me done for today. Same time tomorrow!", 3.5f);
            }
        }

        public void BoatLeaving(int leftAboard)
        {
            LeftAboard.Value += leftAboard;
            Shout("CAPTAIN MAGS", leftAboard == 0 ? Pick("Clean deck! Pleasure doing business, boss!", "Not a crate left. You lot are alright!", "Empty! I'll tell the other boats about you.")
                : leftAboard >= 5 ? Pick($"HA! {leftAboard} crates for my supper!", $"{leftAboard} crates still aboard? Your loss, boss!", $"Casting off with {leftAboard} crates. Better luck next tide!")
                : Pick($"Casting off! {leftAboard} crate{(leftAboard == 1 ? "" : "s")} coming home with me.", $"Ropes off! You missed {leftAboard}."), 4f);
        }

        public void BoatGone() => activeBoats--;

        // Test hook: skip to the end of the day.
        public void EndDayNow()
        {
            foreach (var b in Boat.All.ToArray()) b.NetworkObject.Despawn();
            activeBoats = 0; nextBoat = BoatTimes.Length; DayT.Value = DayLength;
        }

        public void CrateInSea() => InSea.Value++;

        public void Sell(Crate c)
        {
            int v = c.Value;
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
