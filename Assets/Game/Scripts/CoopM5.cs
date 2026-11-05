using System.Collections.Generic;
using Unity.Netcode;

namespace PortBoss.M1
{
    // M5 in co-op: the host's Company is the truth (it reaches everyone in "pb.co", so every machine sees every buy,
    // paint job and outfit as soon as the host has it). A client asks:
    // - "pb.m5"  client -> host: op 0 buy (tx, item, arg, now), 1 ask the crew (item, arg), 2 answer an ask (id, yes),
    //            3 wear (item or "" + slot: back to my own), 4 store fish (ids from my hotbar), 5 take fish (slot, all)
    // - "pb.m5r" host -> client: the answer (op, item, ok, why) for YShop's feedback
    public partial class Coop
    {
        void RegisterM5(CustomMessagingManager m) { Reg(m, "pb.m5", OnM5); Reg(m, "pb.m5r", OnM5Res); }

        public void AskM5(byte op, string tx, string item, int arg, bool flag, List<ulong> ids = null)
        {
            Sent++;
            SendTo(NetworkManager.ServerClientId, "pb.m5", w =>
            {
                w.WriteValueSafe(op); w.WriteValueSafe(tx ?? ""); w.WriteValueSafe(item ?? ""); w.WriteValueSafe(arg); w.WriteValueSafe(flag);
                w.WriteValueSafe((byte)(ids?.Count ?? 0)); if (ids != null) foreach (var id in ids) w.WriteValueSafe(id);
            }, NetworkDelivery.ReliableSequenced);
        }
        void OnM5(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte op); r.ReadValueSafe(out string tx); r.ReadValueSafe(out string item); r.ReadValueSafe(out int arg); r.ReadValueSafe(out bool flag);
            r.ReadValueSafe(out byte n); var ids = new ulong[n]; for (int i = 0; i < n; i++) r.ReadValueSafe(out ids[i]);
            Deliver(() =>
            {
                var e = Find(from); if (e == null || Firm.C == null) return; var c = Firm.C; string key = e.Key ?? MyKey(e.Name);
                bool ok = true; string why = "";
                switch (op)
                {
                    case 0:
                        if (c.NeedsAsk(item, arg, key, true, flag)) { var a = c.AskCrew(key, e.Name, item, arg, 1, Sky.Minutes); why = "ask:" + a.Id; ok = false; Log($"{e.Name} asked the crew: {item} ${a.Price}"); }
                        else ok = Works.HostBuy(tx, key, e.Name, item, arg, flag, out why);
                        break;
                    case 1: { var a = c.AskCrew(key, e.Name, item, arg, 1, Sky.Minutes); why = "ask:" + a.Id; Log($"{e.Name} asked the crew: {item} ${a.Price}"); break; }
                    case 2: ok = c.AnswerAsk(arg, key, false, flag, e.Name, Sky.Minutes, out why); break;
                    case 3: if (item == "") c.WearOwn(key, arg); else c.WearIt(key, item); break;
                    case 4:
                        {
                            var fish = new List<Fish>();
                            foreach (var id in ids) if (Book.Is(id, Ledger.Kind.Bar, from) && Book.All.TryGetValue(id, out var f)) fish.Add(f);
                            int k = Works.HostStore(from, fish); ok = k == ids.Length; why = ok ? "" : k == 0 ? "No room in the warehouse." : $"Stored {k}. No room for the rest.";
                            break;
                        }
                    case 5: ok = Works.HostTake(from, arg, flag) > 0; why = ok ? "" : "Hotbar full."; break;
                }
                Firm.I.Dirty = true;
                if (from != nm.LocalClientId) SendTo(from, "pb.m5r", w => { w.WriteValueSafe(op); w.WriteValueSafe(item); w.WriteValueSafe(ok); w.WriteValueSafe(why ?? ""); }, NetworkDelivery.ReliableSequenced);
            }, true);
        }
        void OnM5Res(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte op); r.ReadValueSafe(out string item); r.ReadValueSafe(out bool ok); r.ReadValueSafe(out string why);
            Deliver(() =>
            {
                Log($"m5 op {op} {item}: {(ok ? "ok" : why)}");
                bool asked = why.StartsWith("ask:");
                if (op == 0 || op == 1) Works.Done?.Invoke(item, ok, asked ? "" : why);
                if (asked) Alerts.Push("Ask the crew", $"Asked the crew: {Catalog.Get(item)?.Name}. One OK buys it.", Alerts.Kind.Info, why);
                else if (!ok && why != "") Alerts.Push(op >= 4 ? "Warehouse" : "Shop", why, Alerts.Kind.Info);
            }, true);
        }
    }

    public static class CoopM5
    {
        public static void Ask(int op, string tx, string item, int arg, bool flag) => Coop.I?.AskM5((byte)op, tx, item, arg, flag);
        public static void Store(List<Fish> fish) => Coop.I?.AskM5(4, "", "", 0, false, fish.ConvertAll(f => f.Id));
        public static void Take(int slot, bool all) => Coop.I?.AskM5(5, "", "", slot, all);
    }
}
