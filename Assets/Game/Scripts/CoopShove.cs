using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M3.1: a friendly shove. The shover's machine says who and how hard; a body is knocked over only on its own machine
    // (its ragdoll then goes out in its pose), so the host passes a client's shove on to the crewmate it was aimed at.
    // Client to host: (to, push). Host to client: (from, push).
    public partial class Coop
    {
        public static void Shove(ulong to, Vector3 push)
        {
            if (!On) return;
            Log($"shove: I shoved {I.Find(to)?.Name}");
            if (IsHost) I.ShoveTo(I.nm.LocalClientId, to, push);
            else I.Send("pb.shove", w => { w.WriteValueSafe(to); w.WriteValueSafe(push); }, NetworkDelivery.ReliableSequenced);
        }

        void ShoveTo(ulong from, ulong to, Vector3 push)
        {
            if (to == nm.LocalClientId) { Angler.I?.Shoved(push, Find(from)?.Name ?? "A crewmate"); return; }
            if (Find(to) == null) return;
            SendTo(to, "pb.shove", w => { w.WriteValueSafe(from); w.WriteValueSafe(push); }, NetworkDelivery.ReliableSequenced);
        }

        void OnShove(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong who); r.ReadValueSafe(out Vector3 push);
            Deliver(() => { if (nm.IsServer) ShoveTo(from, who, push); else Angler.I?.Shoved(push, Find(who)?.Name ?? "A crewmate"); }, true);
        }
    }
}
