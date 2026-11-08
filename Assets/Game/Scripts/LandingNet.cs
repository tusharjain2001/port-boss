using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M8 step 3, fishing F11 (§4.2): the landing net, co-op flavour, never needed. $150 at June's; once bought it's carried
    // (on your back, like the spare rods: not a hotbar slot). Beside a crewmate who's fighting a fish (2 m), E readies it:
    // they get one more go at the stop and a 10% wider green for the rest of that fight (Fishing.NetReady). The net shows in
    // the helper's hands on every screen, angled at the fish. If the fish comes up Rare, the helper scoops it: the net dips
    // and lifts, a splash, a cheer for both. Solo it does nothing (there's no one to net for). "pb.net" (helper -> host ->
    // all) and "pb.scoop" (angler -> host -> all).
    public class LandingNet : MonoBehaviour
    {
        public static LandingNet I;
        public const float Reach = 2f, Pitch = 35f, ScoopT = 1.3f;
        public static System.Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] net: " + s); OnLog?.Invoke(s); }
        public class Ready { public ulong Helper, Angler; public float T, Scoop = -1; public Transform Net; public Angler Body; }
        public readonly List<Ready> On = new();
        public int Scoops;                                                       // (tests)

        public static void Create() { var g = new GameObject("LandingNet"); I = g.AddComponent<LandingNet>(); }

        static Angler Body(ulong id) => !Coop.On ? null : id == Coop.Me ? Angler.I : Coop.I.Mates.TryGetValue(id, out var a) ? a : null;
        static string NameOf(ulong id) => id == Coop.Me ? Coop.MyName : Body(id)?.NetName ?? "your crewmate";
        static bool Fighting(Fishing f) => f != null && f.State is Fishing.St.Strike or Fishing.St.Heavy or Fishing.St.Minigame;

        // E beside a crewmate who's fighting a fish, with a net of my own
        public static string Prompt(Angler me)
        {
            if (I == null || !Coop.On || me.Remote || me.Swimming || me.AtHelm || me.Busy || me.Fish.Fighting || Firm.I == null || !Firm.I.MyKit.Net) return null;
            foreach (var kv in Coop.I.Mates)
            {
                var a = kv.Value; if (a == null || !Fighting(a.Fish)) continue;
                var d = a.transform.position - me.transform.position; d.y *= 0.5f; if (d.magnitude > Reach) continue;
                if (I.On.Exists(r => r.Angler == kv.Key)) return "The net's ready";
                if (Ctl.Interact) Coop.I.SendNet(Coop.Me, kv.Key);
                return $"Ready the net for {a.NetName}";
            }
            return null;
        }

        // (every machine) helper has the net out for angler's fish
        public void Readied(ulong helper, ulong angler)
        {
            if (On.Exists(r => r.Angler == angler)) return;
            var r = new Ready { Helper = helper, Angler = angler, T = Time.time, Body = Body(helper) };
            if (r.Body != null)
            {
                r.Net = Make().transform; r.Net.SetParent(r.Body.transform, false);
                r.Body.Anim.Grip = r.Net; r.Body.Anim.GripL = new Vector3(0, 0, 0.12f); r.Body.Anim.GripR = new Vector3(0, 0, 0.42f); r.Body.Anim.GripLean = 0;
                Snd.Play(Synth.Whoosh, r.Body.transform.position, 0.4f, 1.3f);
            }
            On.Add(r);
            if (angler == Coop.Me) Angler.I?.Fish.NetReady(NameOf(helper));
            if (helper == Coop.Me) Alerts.Push("Landing net", $"Net ready for {NameOf(angler)}.", Alerts.Kind.Info);
            Log($"{NameOf(helper)} readies the net for {NameOf(angler)}");
        }

        // (every machine) the angler's fish came up Rare: the helper scoops it
        public void Scooped(ulong angler, string species)
        {
            var r = On.Find(x => x.Angler == angler); if (r == null) return;
            r.Scoop = 0; Scoops++;
            var sp = FishDb.All.Find(f => f.Id == species); string fish = sp != null ? sp.Name : "fish";
            string hn = NameOf(r.Helper), an = NameOf(angler);
            if (angler == Coop.Me) Alerts.Push("Scooped!", $"{hn} netted your {fish}.", Alerts.Kind.Info);
            else if (r.Helper == Coop.Me) Alerts.Push("Scooped!", $"You netted {an}'s {fish}.", Alerts.Kind.Info);
            Snd.Play2D(Synth.Fanfare, 0.45f, 1.15f);
            Log($"{hn} scooped {an}'s {fish}");
        }

        void Update()
        {
            for (int i = On.Count - 1; i >= 0; i--)
            {
                var r = On[i]; var af = Body(r.Angler)?.Fish;
                bool live = af != null && (Fighting(af) || af.State is Fishing.St.Reel) || r.Scoop >= 0 && r.Scoop < ScoopT;
                if (r.Scoop >= 0) r.Scoop += Time.deltaTime;
                if (!live && Time.time - r.T > 0.5f || Time.time - r.T > 40 || r.Body == null) { Drop(r); On.RemoveAt(i); continue; }
                if (r.Net == null) continue;
                // the handle from the helper's chest toward the fish, down at the water; the scoop dips it and lifts it
                var b = r.Body.transform; var to = af != null ? af.FishAt - b.position : b.forward; to.y = 0; if (to.sqrMagnitude < 0.01f) to = b.forward; to.Normalize();
                float p = Pitch;
                if (r.Scoop >= 0) { float u = r.Scoop / ScoopT; p = u < 0.4f ? Pitch + 22 * Mathf.Sin(u / 0.4f * Mathf.PI * 0.5f) : Mathf.Lerp(Pitch + 22, -15, Mathf.SmoothStep(0, 1, (u - 0.4f) / 0.6f)); }
                var dir = to * Mathf.Cos(p * Mathf.Deg2Rad) + Vector3.down * Mathf.Sin(p * Mathf.Deg2Rad);
                r.Net.SetPositionAndRotation(b.position + Vector3.up * 1.0f + to * 0.15f + Vector3.Cross(Vector3.up, to) * 0.12f, Quaternion.LookRotation(dir, Vector3.up));
                if (r.Scoop >= 0 && r.Scoop - Time.deltaTime < ScoopT * 0.4f && r.Scoop >= ScoopT * 0.4f)
                { var hoop = r.Net.TransformPoint(new Vector3(0, 0, 1.5f)); Fx.Splash(new Vector3(hoop.x, Sea.Height(hoop), hoop.z), 0.6f, 12); Snd.Play(Synth.Splash, hoop, 0.6f); }
            }
        }
        void Drop(Ready r)
        {
            if (r.Body != null && r.Body.Anim.Grip == r.Net) { r.Body.Anim.Grip = null; r.Body.Anim.GripLean = 14f; }
            if (r.Net != null) Destroy(r.Net.gameObject);
        }

        // the net: an ash handle, a steel hoop, a green mesh bag (code-built; +Z down the handle to the hoop)
        public static GameObject Make()
        {
            var g = new GameObject("landing net"); var toon = Game.ToonMat;
            Color ash = Ponyo.Hex("C9A27A"), steel = Ponyo.Hex("B9C0C7"), mesh = Ponyo.Hex("4E8F6E"), grip = Ponyo.Hex("3F2C27");
            Util.MeshObj("handle", Util.Cylinder(8, 0.018f, 0.016f, 1.3f, ash, ash), toon, g.transform);
            var gr = Util.MeshObj("grip", Util.Cylinder(8, 0.022f, 0.022f, 0.35f, grip, grip), toon, g.transform); gr.transform.localPosition = new Vector3(0, 0, 0.05f);
            const float R = 0.24f; var c = new Vector3(0, 0, 1.3f + R);
            for (int i = 0; i < 16; i++)
            {
                float a0 = i * Mathf.PI * 2 / 16, a1 = (i + 1) * Mathf.PI * 2 / 16;
                var p0 = c + new Vector3(Mathf.Cos(a0) * R, 0, Mathf.Sin(a0) * R); var p1 = c + new Vector3(Mathf.Cos(a1) * R, 0, Mathf.Sin(a1) * R);
                var s = Util.MeshObj("hoop", Util.Cylinder(6, 0.012f, 0.012f, (p1 - p0).magnitude, steel, steel), toon, g.transform);
                s.transform.localPosition = p0; s.transform.localRotation = Quaternion.LookRotation(p1 - p0);
            }
            var bag = Util.MeshObj("bag", Util.Cylinder(14, R * 0.97f, 0.05f, 0.34f, mesh, mesh), toon, g.transform);
            bag.transform.localPosition = c; bag.transform.localRotation = Quaternion.Euler(90, 0, 0);
            foreach (var mr in g.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return g;
        }
    }

    public partial class Coop
    {
        // ---- M8 step 3: the landing net. "pb.net": a helper readies it for an angler; "pb.scoop": the angler's fish came up
        //      Rare with it ready. Client -> host -> everyone else; each machine shows it (LandingNet).
        void RegisterNet(CustomMessagingManager m) { Reg(m, "pb.net", OnNet); Reg(m, "pb.scoop", OnScoop); }

        public void SendNet(ulong helper, ulong angler)
        {
            if (nm.IsServer) { LandingNet.I?.Readied(helper, angler); foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) SendTo(id, "pb.net", w => { w.WriteValueSafe(helper); w.WriteValueSafe(angler); }, NetworkDelivery.ReliableSequenced); }
            else { LandingNet.I?.Readied(helper, angler); Send("pb.net", w => { w.WriteValueSafe(helper); w.WriteValueSafe(angler); }, NetworkDelivery.ReliableSequenced); }
        }
        void OnNet(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong helper); r.ReadValueSafe(out ulong angler);
            Deliver(() =>
            {
                if (nm.IsServer) { helper = from; foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId && id != from) SendTo(id, "pb.net", w => { w.WriteValueSafe(helper); w.WriteValueSafe(angler); }, NetworkDelivery.ReliableSequenced); }
                LandingNet.I?.Readied(helper, angler);
            }, true);
        }
        public void SendScoop(string species)
        {
            ulong me = nm.LocalClientId;
            if (nm.IsServer) { LandingNet.I?.Scooped(me, species); foreach (var id in nm.ConnectedClientsIds) if (id != me) SendTo(id, "pb.scoop", w => { w.WriteValueSafe(me); w.WriteValueSafe(species); }, NetworkDelivery.ReliableSequenced); }
            else { LandingNet.I?.Scooped(me, species); Send("pb.scoop", w => { w.WriteValueSafe(me); w.WriteValueSafe(species); }, NetworkDelivery.ReliableSequenced); }
        }
        void OnScoop(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong angler); r.ReadValueSafe(out string species);
            Deliver(() =>
            {
                if (nm.IsServer) { angler = from; foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId && id != from) SendTo(id, "pb.scoop", w => { w.WriteValueSafe(angler); w.WriteValueSafe(species); }, NetworkDelivery.ReliableSequenced); }
                LandingNet.I?.Scooped(angler, species);
            }, true);
        }
    }
}
