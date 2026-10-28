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
            if (IsServer) StartDay(1);
        }

        public override void OnNetworkDespawn() { if (I == this) I = null; }

        void StartDay(int n)
        {
            DayNum.Value = n; DayT.Value = 0; nextBoat = 0;
            Earned.Value = Sold.Value = InSea.Value = LeftAboard.Value = SpoiledSold.Value = 0;
            Phase.Value = Ph.Day;
            if (n == 1)
            {
                // A few crates already on the quay so there's something to do before the first boat.
                for (int i = 0; i < 3; i++) SpawnCrate(new Vector3(-0.8f + i * 0.8f, 0.6f, 3.5f), 1f);
                Shout("Morning, boss. Boat's due any minute. Buyer pays cash at the table!", 4.5f);
            }
            else Shout($"Day {n}. Boats are coming. Keep that fish moving!", 3.5f);
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
                b.CargoCount = 7 + nextBoat * 2 + (DayNum.Value - 1) * 2;
                b.NetworkObject.Spawn(true);
                activeBoats++; nextBoat++;
                Shout(nextBoat == 1 ? "*HOOONK* Boat coming in!" : "*HOOONK* Another boat!", 2.5f);
            }
            if (DayT.Value >= DayLength && nextBoat >= BoatTimes.Length && activeBoats <= 0)
            {
                Phase.Value = Ph.Night;
                Shout("That's the day. The buyer's packing up.", 3f);
            }
        }

        public void BoatLeaving(int leftAboard)
        {
            LeftAboard.Value += leftAboard;
            Shout(leftAboard > 0 ? $"Casting off! {leftAboard} crate{(leftAboard == 1 ? "" : "s")} going back out to sea with me!" : "Casting off! Pleasure doing business.", 3.5f);
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
            SoldRpc(c.transform.position, v, c.IsSpoiled);
            c.NetworkObject.Despawn();
        }

        public void Shout(string text, float secs) => ShoutRpc(text, secs);

        [Rpc(SendTo.ClientsAndHost)]
        void ShoutRpc(string text, float secs) => GameUI.Shout(text, secs);

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
            Shout("There goes the trolley... into the sea.", 3.5f);
        }
    }
}
