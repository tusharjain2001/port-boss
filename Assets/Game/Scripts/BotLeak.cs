using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-leaktest host|rider <dir> (M3.1, tools/leaktest.sh): Biscuit springs a leak with two aboard. Round 1: the
        // rider patches it (its ask goes to the host, the boat's owner) while the host watches; round 2: the host patches,
        // the rider watches. Each machine checks the marker (spout, ring, pin; the arrow at the edge when the hole is off
        // screen), the patcher's kneel-and-hammer pose (its own, and the crewmate's from the pose), the progress, the plank
        // after; pictures from both.
        IEnumerator LeakTest(string role, string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; bool host = role == "host";
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2, 40);
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT leaktest: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(2f);
            var s = Skiff.I; var c = Coop.I;
            Angler Mate() { foreach (var m in c.Mates.Values) if (m != null) return m; return null; }
            Vector3 Deck(float x, float z) => s.transform.TransformPoint(new Vector3(x, 1.0f, z));
            float Yaw(float d) => s.transform.eulerAngles.y + d;
            IEnumerator Pic(string what, Vector3 look, Vector3 from)
            {
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
                ct.position = from; ct.rotation = Quaternion.LookRotation(look - from);
                NameTags.I?.Place(); LeakFx.PlaceNow(); Canvas.ForceUpdateCanvases();
                Save(Path.Combine(dir, $"{role}-{shotN++:00}-{what}.png"));
                ct.SetPositionAndRotation(keep.position, keep.rotation);
            }
            // a side view of an angler at work: from their right, a little ahead, looking down at their hands
            IEnumerator Side(string what, Angler a) => Pic(what, a.transform.position + Vector3.up * 0.45f + a.transform.forward * 0.25f, a.transform.position + a.transform.right * 1.7f + a.transform.forward * 0.7f + Vector3.up * 1.15f);
            IEnumerator HoleView(string what) => Pic(what, s.Hole, s.Hole + s.transform.rotation * new Vector3(-1.6f, 1.6f, -1.9f));
            bool Marker() { var r = s.transform.Find("leakRing"); var p = s.transform.Find("leakSpout")?.GetComponent<ParticleSystem>(); Debug.Log($"[PB] leak: marker: pin {LeakFx.PinShown}, ring {(r != null && r.gameObject.activeSelf)}, spout {(p != null ? p.particleCount : -1)} drops"); return LeakFx.PinShown && r != null && r.gameObject.activeSelf && p != null && p.particleCount > 0; }
            // watch a crewmate patch: their pose says so for most of the 10 s, with the hammer up and down
            IEnumerator Watch(string name)
            {
                yield return Until(() => Mate() != null && Mate().RepairT >= 0, 40);
                var m = Mate(); float seen = 0, lo = 1, hi = 0, prog = 0; bool shot = false;
                for (float t = 0; s.Leaking && t < Skiff.PatchTime + 6; t += Time.deltaTime)
                {
                    if (m.RepairT >= 0) { seen += Time.deltaTime; lo = Mathf.Min(lo, m.RepairLift); hi = Mathf.Max(hi, m.RepairLift); }
                    prog = Mathf.Max(prog, LeakFx.Progress);
                    if (!shot && seen > 3f) { shot = true; yield return Side($"sees-{name}-patching", m); yield return HoleView($"sees-{name}-patching-pin"); }
                    yield return null;
                }
                var hm = m.GetComponent<Hammer>();
                Check(seen > Skiff.PatchTime * 0.7f, $"I see {name} kneel at the hole and patch it: {seen:F1} s of the pose");
                Check(lo < 0.1f && hi > 0.9f, $"{name}'s hammer swings (lift {lo:F2}..{hi:F2}), a hammer in the hand: {hm != null}");
                Check(prog > 0.6f, $"the pin fills with {name}'s progress (to {prog * 100:0}%)");
            }
            IEnumerator Patch(string who)
            {
                me.Teleport(s.Hole + Vector3.up * 0.15f, Yaw(180)); yield return new WaitForSeconds(0.6f);
                Ctl.BotInteractHeld = true; float t0 = Time.time, seen = 0; bool shot = false; string prompt = "";
                while (s.Leaking && Time.time - t0 < Skiff.PatchTime + 8)
                {
                    if (me.RepairT >= 0) { seen += Time.deltaTime; if (me.Prompt != null) prompt = me.Prompt; }
                    if (!shot && seen > 3f) { shot = true; yield return Side("patching-myself", me); yield return HoleView("patching-myself-pin"); }
                    yield return null;
                }
                Ctl.BotInteractHeld = false;
                float d = Vector3.Distance(Flat(me.transform.position), Flat(s.Hole));
                var hr = me.Anim.HandR; float reach = hr != null ? Vector3.Distance(hr.position, s.Hole) : -1;
                Debug.Log($"[PB] leak: kneeling {d:F2} m from the hole, feet {me.transform.position.y - s.Hole.y:+0.00;-0.00} m over it, the hammer hand {reach:F2} m from it");
                Check(!s.Leaking && seen > Skiff.PatchTime * 0.8f, $"{who}: holding E at the hole, I kneel and patch it ({Time.time - t0:F1} s, {seen:F1} s in the pose, {d:F2} m from the hole; '{prompt}')");
            }

            for (int round = 1; round <= 2; round++)
            {
                bool iPatch = host == (round == 2);
                me.Teleport(Deck(0, host ? -0.9f : 0.95f), Yaw(host ? 0 : 180)); yield return new WaitForSeconds(1f);
                if (host) { s.SpringLeak(); Debug.Log($"[PB] leak: round {round}: sprung"); }
                yield return Until(() => s.Leaking, 10); yield return new WaitForSeconds(1.2f);
                Check(s.Leaking && Marker(), $"round {round}: the leak shows: water spurting in, a red ring, a pin over it");
                yield return HoleView($"r{round}-leak-marker");
                if (round == 1)
                {
                    // look away from it: the pin goes to the screen's edge with an arrow
                    var keep = CameraRig.I.Yaw; CameraRig.I.Yaw = Mathf.Atan2(me.transform.position.x - s.Hole.x, me.transform.position.z - s.Hole.z) * Mathf.Rad2Deg;
                    yield return new WaitForSeconds(0.6f);
                    Check(LeakFx.PinShown && LeakFx.ArrowShown, "looking away, the pin sits at the edge with an arrow to the hole");
                    yield return Shot("leak-offscreen-arrow"); CameraRig.I.Yaw = keep;
                }
                if (iPatch) yield return Patch(host ? "Host" : "Rider");
                else yield return Watch(host ? "Rider" : "Host");
                yield return Until(() => !s.Leaking, 10); yield return new WaitForSeconds(1f);
                Check(!s.Leaking && LeakFx.PlankShown && !LeakFx.PinShown, $"round {round}: patched on my screen too: the plank nailed over the hole, the marker gone");
                yield return HoleView($"r{round}-patched");
                yield return new WaitForSeconds(3f);
            }
            Debug.Log($"[PB] RESULT leaktest {role}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            yield return new WaitForSeconds(host ? 4f : 1f);
            Application.Quit();
        }
    }
}
