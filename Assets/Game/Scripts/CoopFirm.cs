using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M4 in co-op: one company bank for the crew (gdd economy §8). The host's Company is the truth.
    // - "pb.co"   host -> each client: the whole company (gzip JSON) whenever it changed (at most 2 a second). It is a
    //             few KB; it goes in chunks of up to 4 KB (the transport's fragmented message is capped at 6 KB), so it
    //             never matters how long the Ledger gets. ponytail: the whole thing each time; deltas if it ever shows.
    // - "pb.kit"  client -> host: my kit (bait counts, the journal) when it changed, so the host's save has it. The
    //             client's own copy stays the truth for its pouch (it spends bait on every catch).
    // - "pb.sell" / "pb.buy" / "pb.deal" client -> host, "pb.res" host -> client: the answer (a refusal's reason; a buy's
    //             goods, which the client adds to its own kit). A sale's fish come off the hotbar by the host's book.
    // - "pb.copy" host -> each client: the morning save (Save.KeepCopy), the same chunks.
    public partial class Coop
    {
        const int Chunk = 4000;
        float coT; string coLast; string kitLast; float kitT;
        readonly Dictionary<(byte kind, int seq), byte[][]> parts = new();
        int blobSeq;

        void RegisterFirm(CustomMessagingManager m)
        {
            Reg(m, "pb.co", OnBlob); Reg(m, "pb.copy", OnBlob); Reg(m, "pb.kit", OnKit);
            Reg(m, "pb.sell", OnSellAsk); Reg(m, "pb.buy", OnBuyAsk); Reg(m, "pb.deal", OnDealAsk); Reg(m, "pb.res", OnRes);
        }

        // (Update) host: send the company when it changed; client: report my kit when it changed
        void TickFirm()
        {
            if (Firm.I == null) return;
            if (nm.IsServer)
            {
                coT -= Time.unscaledDeltaTime;
                if (coT > 0) return;
                coT = 0.5f;
                if (!Firm.I.Dirty && Roster.Count == coRoster) return;
                if (Roster.Count != coRoster) coForce = true;              // a new crewmate gets the company at once
                coRoster = Roster.Count; Firm.I.Dirty = false;
                var json = Firm.C.ToJson(); if (json == coLast && !coForce) return;
                coLast = json; coForce = false;
                var gz = Save.Gzip(json);
                foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) SendBlob(id, "pb.co", 0, gz);
                return;
            }
            kitT -= Time.unscaledDeltaTime; if (kitT > 0) return; kitT = 1f;
            var w = new Json.W(); Company.WriteKit(w, Firm.I.MyKit); var k = w.ToString();
            if (k == kitLast) return; kitLast = k;
            SendTo(NetworkManager.ServerClientId, "pb.kit", x => x.WriteValueSafe(k), NetworkDelivery.ReliableSequenced);
        }
        int coRoster; bool coForce;

        void SendBlob(ulong to, string msg, byte kind, byte[] data, string tag = "")
        {
            int seq = ++blobSeq, n = Math.Max(1, (data.Length + Chunk - 1) / Chunk);
            for (int i = 0; i < n; i++)
            {
                int off = i * Chunk, len = Math.Min(Chunk, data.Length - off);
                var w = new FastBufferWriter(Chunk + 256, Allocator.Temp);
                try
                {
                    w.WriteValueSafe(kind); w.WriteValueSafe(seq); w.WriteValueSafe((ushort)i); w.WriteValueSafe((ushort)n); w.WriteValueSafe(tag);
                    w.WriteValueSafe(len); var seg = new byte[len]; Array.Copy(data, off, seg, 0, len); w.WriteBytesSafe(seg);
                    BytesOut += w.Length; nm.CustomMessagingManager.SendNamedMessage(msg, to, w, NetworkDelivery.ReliableFragmentedSequenced);
                }
                finally { w.Dispose(); }
            }
        }
        void OnBlob(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte kind); r.ReadValueSafe(out int seq); r.ReadValueSafe(out ushort i); r.ReadValueSafe(out ushort n); r.ReadValueSafe(out string tag);
            r.ReadValueSafe(out int len); var seg = new byte[len]; r.ReadBytesSafe(ref seg, len);
            Deliver(() =>
            {
                if (!parts.TryGetValue((kind, seq), out var p)) parts[(kind, seq)] = p = new byte[n][];
                if (i < p.Length) p[i] = seg;
                foreach (var x in p) if (x == null) return;
                parts.Remove((kind, seq));
                int total = 0; foreach (var x in p) total += x.Length; var all = new byte[total]; int o = 0; foreach (var x in p) { Array.Copy(x, 0, all, o, x.Length); o += x.Length; }
                if (kind == 0) Firm.I?.Sync(Company.Read((Dictionary<string, object>)Json.Parse(Save.Gunzip(all))));
                else Save.KeepCopy(tag, all);
            }, true);
        }

        public void SendCopy(string worldId, byte[] gz) { foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) SendBlob(id, "pb.copy", 1, gz, worldId); Log($"sent the crew a copy of {worldId} ({gz.Length} bytes)"); }

        void OnKit(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out string k);
            Deliver(() =>
            {
                var e = Find(from); if (e == null || Firm.C == null) return;
                var mine = Company.ReadKit((Dictionary<string, object>)Json.Parse(k)); var kit = Firm.C.KitOf(e.Key ?? MyKey(e.Name));
                // the pouch and the journal are the client's; what was bought (the rod, the tackle pouch) the host's
                kit.Bait = mine.Bait; kit.BaitI = mine.BaitI; kit.Best = mine.Best;
            }, true);
        }

        // ---- the asks (a client) and their answers (the host)
        public void AskSell(string tx, Buyer b, List<Fish> fish)
        {
            Sent++;
            var ids = fish.ConvertAll(f => f.Id);
            SendTo(NetworkManager.ServerClientId, "pb.sell", w => { w.WriteValueSafe(tx); w.WriteValueSafe((byte)b); w.WriteValueSafe((byte)ids.Count); foreach (var id in ids) w.WriteValueSafe(id); }, NetworkDelivery.ReliableSequenced);
        }
        void OnSellAsk(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out string tx); r.ReadValueSafe(out byte b); r.ReadValueSafe(out byte n); var ids = new ulong[n]; for (int i = 0; i < n; i++) r.ReadValueSafe(out ids[i]);
            Deliver(() =>
            {
                var e = Find(from); if (e == null) return;
                var fish = new List<Fish>(); string why = null;
                foreach (var id in ids) { if (Book.Is(id, Ledger.Kind.Bar, from) && Book.All.TryGetValue(id, out var f)) fish.Add(f); else { why = "That fish isn't in your hotbar any more."; break; } }
                int[] p = null;
                if (why == null) p = Firm.C.Sell(tx, e.Name, (Buyer)b, fish.ConvertAll(Firm.Lot), Sky.Minutes, out why);
                if (p != null) { Book.Sell(from, ids); SendBar(from); Firm.Sold((Buyer)b, fish.Count, p); Firm.I.Dirty = true; }
                Res(from, 0, tx, p != null, why ?? "", "");
            }, true);
        }
        public void AskBuy(string tx, string item, int qty) { Sent++; SendTo(NetworkManager.ServerClientId, "pb.buy", w => { w.WriteValueSafe(tx); w.WriteValueSafe(item); w.WriteValueSafe(qty); }, NetworkDelivery.ReliableSequenced); }
        void OnBuyAsk(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out string tx); r.ReadValueSafe(out string item); r.ReadValueSafe(out int qty);
            Deliver(() =>
            {
                var e = Find(from); if (e == null) return;
                bool ok = Firm.C.Buy(tx, e.Key ?? MyKey(e.Name), e.Name, item, qty, Sky.Minutes, out var why);
                if (ok) { Firm.I.Dirty = true; Log($"{e.Name} bought {Company.ItemName(item, qty)}"); }
                Res(from, 1, tx, ok, why, item + "|" + qty);
            }, true);
        }
        public void AskDeal(byte op) { Sent++; SendTo(NetworkManager.ServerClientId, "pb.deal", w => w.WriteValueSafe(op), NetworkDelivery.ReliableSequenced); }
        void OnDealAsk(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte op);
            Deliver(() =>
            {
                var e = Find(from); if (e == null) return; var c = Firm.C;
                switch (op)
                {
                    case 0: case 1: if (c.AnswerMari(op == 1, e.Name, Sky.Minutes)) Log($"Mari's mooring: {(op == 1 ? "accepted" : "declined")} by {e.Name}"); break;
                    case 2: c.HaggleMari(Sky.Minutes); break;
                    case 3: case 4: c.MariTracked = op == 3; break;
                }
                Firm.I.Dirty = true;
            }, true);
        }
        void Res(ulong to, byte kind, string tx, bool ok, string why, string what)
        {
            if (to == nm.LocalClientId) return;
            SendTo(to, "pb.res", w => { w.WriteValueSafe(kind); w.WriteValueSafe(tx); w.WriteValueSafe(ok); w.WriteValueSafe(why); w.WriteValueSafe(what); }, NetworkDelivery.ReliableSequenced);
        }
        void OnRes(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte kind); r.ReadValueSafe(out string tx); r.ReadValueSafe(out bool ok); r.ReadValueSafe(out string why); r.ReadValueSafe(out string what);
            Deliver(() =>
            {
                Log($"{(kind == 0 ? "sell" : "buy")} {tx}: {(ok ? "ok" : "refused: " + why)}");
                if (kind == 0) Firm.I.Answered(tx);
                if (!ok) { Alerts.Push(kind == 0 ? "Market" : "Hook, Line & Sinker", why, Alerts.Kind.Info); return; }
                if (kind == 1)
                {
                    var bits = what.Split('|'); string item = bits[0]; int qty = int.Parse(bits[1]); var k = Firm.I.MyKit;
                    if (item.StartsWith("bait:")) { int i = int.Parse(item.Substring(5)); k.Bait[i] = Math.Min(Shelf.BaitMax, k.Bait[i] + qty * Shelf.PackOf[i]); }
                    else if (item.StartsWith("rod:")) k.Rod = int.Parse(item.Substring(4));
                    else if (item == "pouch") k.Pouch5 = true;
                    Firm.I.Bought(item, qty);
                }
            }, true);
        }
    }

    // what Firm and Save call (no co-op: nothing)
    public static class CoopFirm
    {
        public static void AskSell(string tx, Buyer b, List<Fish> fish) => Coop.I?.AskSell(tx, b, fish);
        public static void AskBuy(string tx, string item, int qty) => Coop.I?.AskBuy(tx, item, qty);
        public static void AskDeal(int op) => Coop.I?.AskDeal((byte)op);
        public static void SendCopy(string worldId, byte[] gz) { if (Coop.IsHost) Coop.I.SendCopy(worldId, gz); }
    }
}
