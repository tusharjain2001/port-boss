using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M3 in co-op (gdd systems §1.3, §1.5.4): the host owns the world clock, the world seed and any held weather. It
    // sends them every 2 s and at once after any jump (a sleep skip, F10); the weather itself is never sent: every
    // machine rolls the same blocks from the seed. A client's clock runs on between messages and is set to the host's on
    // each (the gap is a few hundredths of an in-game minute: never seen).
    public partial class Coop
    {
        float dayT; double daySentMin; float daySentAt;

        void RegisterDay(CustomMessagingManager m) => Reg(m, "pb.day", OnDay);

        // host: every 2 s, or now when the clock has jumped (more than half a minute off its own steady run)
        void TickDay()
        {
            if (!nm.IsServer) return;
            dayT -= Time.unscaledDeltaTime;
            double drift = (Sky.Minutes - daySentMin) - (Now - daySentAt) * Time.timeScale;
            if (dayT > 0 && System.Math.Abs(drift) < 0.5) return;
            SendDay();
        }

        void SendDay()
        {
            dayT = 2; daySentMin = Sky.Minutes; daySentAt = Now;
            double m = Sky.Minutes; int seed = Sky.Seed; sbyte force = (sbyte)Sky.ForceWx; float ts = Time.timeScale; double held = Sky.HeldAt;
            foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId)
                SendTo(id, "pb.day", w => { w.WriteValueSafe(m); w.WriteValueSafe(seed); w.WriteValueSafe(force); w.WriteValueSafe(ts); w.WriteValueSafe(held); }, NetworkDelivery.ReliableSequenced);
        }

        void OnDay(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out double m); r.ReadValueSafe(out int seed); r.ReadValueSafe(out sbyte force); r.ReadValueSafe(out float ts); r.ReadValueSafe(out double held);
            Deliver(() =>
            {
                if (seed != Sky.Seed) { Sky.Seed = seed; Sky.W = new Weather(seed); Log($"day: the host's world seed {seed}"); }
                Sky.ForceWx = force; Sky.HeldAt = held;             // (the host's hold time: a held storm's rogue waves come at once on every machine)
                double lag = simDelay * ts;                   // (the simulated one-way lag, in in-game minutes at 1 min/s)
                double want = m + lag;
                if (System.Math.Abs(want - Sky.Minutes) > 2) { Log($"day: clock set {DayTime.Clock(Sky.Minutes)} -> {DayTime.Clock(want)} (Day {DayTime.Day(want)})"); Sky.Jump(want); }
                else Sky.Minutes = want;
            }, true);
        }
    }
}
