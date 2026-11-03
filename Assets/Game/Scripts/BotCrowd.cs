using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-crowdtest host|rider <dir> (M3.1 item 5, tools/crowdtest.sh): crewmates are solid. Four 7 s rounds, both machines
        // on the same clock (from when they meet): 1) on the jetty the rider walks straight into the host: stops at them,
        // no shaking, the host doesn't budge; 2) the rider walks past a little off line: slides round; 3) on Biscuit the rider
        // walks into the host at the rail, out toward the sea: the host stays aboard and put; 4) the same with the host
        // walking into the rider.
        IEnumerator CrowdTest(string role, string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; bool host = role == "host";
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2 && (Coop.IsHost || JoinView.Lifted && JoinView.Cover <= 0), 40); // (a joiner: its join cover gone: under -pb-fps the game clock races the real-time fade)
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT crowdtest: FAIL (never connected)"); Application.Quit(); yield break; }
            float t0 = Time.time + 3f; var s = Skiff.I; var c = Coop.I;
            Angler Mate() { foreach (var m in c.Mates.Values) if (m != null) return m; return null; }
            var land = FindAnyObjectByType<Landing>(); var S = (land.Spawn != null ? land.Spawn : land.transform).position;
            // the jetty's own line from the spawn: of 24 ways, the one with the longest run of solid floor at the spawn's
            // height that isn't Biscuit (toward the boat, the stander ended up on her deck and rode her sway)
            var D = Vector3.forward; float best = -1;
            for (int k = 0; k < 24; k++)
            {
                var d = Quaternion.Euler(0, k * 15, 0) * Vector3.forward; float run = 0;
                for (float x = 0.25f; x <= 5f; x += 0.25f)
                {
                    if (!Physics.Raycast(S + d * x + Vector3.up * 1.5f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(h.point.y - S.y) > 0.3f || h.collider.transform.IsChildOf(s.transform)) break;
                    run = x;
                }
                if (run > best) { best = run; D = d; }
            }
            Debug.Log($"[PB] crowd: the jetty runs {D} from the spawn, {best:F2} m clear");
            var side = Vector3.Cross(Vector3.up, D);
            float Yaw(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Vector3 Deck(float x, float z) => s.transform.TransformPoint(new Vector3(x, 1.0f, z));
            Vector3 Flat(Vector3 v) { v.y = 0; return v; }

            for (int round = 1; round <= 4; round++)
            {
                float r0 = t0 + (round - 1) * 7f;
                while (Time.time < r0) yield return null;
                bool boat = round >= 3, iWalk = round == 4 ? host : !host;
                Vector3 standAt, walkFrom, walkTo;
                if (!boat) { standAt = S + D * 2.0f; walkFrom = S + D * 0.2f + side * (round == 2 ? 0.25f : 0); walkTo = standAt + D * (round == 2 ? 2.2f : 0); }
                else { float z = round == 3 ? 0.3f : -0.5f; standAt = Deck(0.45f, z); walkFrom = Deck(-0.45f, z); walkTo = Deck(1.5f, z); }
                var wd = Flat(walkTo - walkFrom).normalized;
                me.Teleport(iWalk ? walkFrom : standAt, Yaw(iWalk ? wd : -wd)); CameraRig.I.Yaw = Yaw(wd);
                yield return new WaitForSeconds(1.5f);
                Vector3 Here() => boat ? s.transform.InverseTransformPoint(me.transform.position) : me.transform.position;
                float lastMoved = 0; var start = Here(); float minD = 9, path = 0, endPath = 0; Vector3 endFrom = Vector3.zero, prevStep = Vector3.zero; bool endSet = false; float endBack = 0; bool shot = false, offBoat = false; var lastHere = Here(); float walkT = 0;
                if (iWalk) { Ctl.BotMove = new Vector2(0, 1); }
                while (Time.time < r0 + 6.2f)
                {
                    var m = Mate();
                    if (m != null) minD = Mathf.Min(minD, Vector3.Distance(Flat(me.transform.position), Flat(m.transform.position)));
                    if (iWalk)
                    {
                        // keep walking at the goal, as a player holding W would (re-aimed: the boat turns on her mooring)
                        if (boat) { wd = Flat(Deck(1.5f, round == 3 ? 0.3f : -0.5f) - Deck(-0.45f, round == 3 ? 0.3f : -0.5f)).normalized; }
                        CameraRig.I.Yaw = Yaw(wd); walkT += Time.deltaTime;
                        var step = Flat(Here() - lastHere); // (on the boat in her own frame: her sway and turn on the mooring aren't my steps)
                        if (walkT > 3f) { if (!endSet) { endSet = true; endFrom = Here(); } endPath += step.magnitude; if (prevStep.sqrMagnitude > 1e-8f) endBack += Mathf.Max(0, -Vector3.Dot(step, prevStep.normalized)); }
                        if (step.sqrMagnitude > 1e-8f) prevStep = step;
                        path += step.magnitude;
                    }
                    if (boat && (!me.OnBoat || me.Swimming)) offBoat = true;
                    if (!iWalk && m != null) { var mv = Flat(Here() - start).magnitude; if (mv > 0.005f && mv > lastMoved + 0.005f) { lastMoved = mv; Debug.Log($"[PB] crowd: round {round}: standing, moved {mv:F3} m; crewmate {Vector3.Distance(Flat(me.transform.position), Flat(m.transform.position)):F2} m off, I'm {(me.OnBoat ? "aboard" : "ashore")}, grounded {me.Cc.isGrounded}"); } }
                    if (!shot && m != null && minD < 0.75f && walkT > 1.2f || !shot && !iWalk && Time.time > r0 + 4.5f) { shot = true; yield return Bump($"r{round}-{(iWalk ? "walking-into" : "walked-into")}", m); }
                    lastHere = Here();
                    yield return null;
                }
                Ctl.BotMove = Vector2.zero;
                float moved = Flat(Here() - start).magnitude;
                string where = boat ? "on Biscuit" : "on the jetty";
                Debug.Log($"[PB] crowd: round {round} {where}, {(iWalk ? "walking" : "standing")}: closest {minD:F2} m, moved {moved:F2} m, path {path:F2} m (last 2 s {endPath:F2} m)");
                if (iWalk)
                {
                    Check(minD > Angler.Apart - 0.08f, $"round {round} {where}: walking into my crewmate I stop at them ({minD:F2} m apart at the closest)");
                    if (round == 2) { float past = Vector3.Dot(Flat(me.transform.position - standAt), wd); Check(past > 0.3f, $"round 2: a little off line I slide round them and walk on ({past:F2} m past)"); }
                    else Check(endBack < 0.03f, $"round {round} {where}: pressed into them, no shaking: I stop or slide round them smoothly ({endPath:F2} m moved in the last 2 s, {endBack:F3} m of it back against the step before)");
                }
                else
                {
                    Check(moved < 0.05f, $"round {round} {where}: walked into, I don't budge ({moved:F2} m)");
                    if (boat) Check(!offBoat, $"round {round}: walked into at the rail, I stay aboard");
                }
            }
            Debug.Log($"[PB] RESULT crowdtest {role}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            yield return new WaitForSeconds(host ? 3f : 1f);
            Application.Quit();
        }

        // the two of them from the side, at the moment they meet
        IEnumerator Bump(string what, Angler m)
        {
            yield return new WaitForEndOfFrame();
            var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
            var mid = (me.transform.position + m.transform.position) / 2 + Vector3.up * 0.9f;
            var across = me.transform.position - m.transform.position; across.y = 0;
            var sideV = Vector3.Cross(Vector3.up, across.sqrMagnitude > 1e-4f ? across.normalized : Vector3.forward);
            var from = mid + sideV * 3.2f + Vector3.up * 0.8f;
            ct.position = from; ct.rotation = Quaternion.LookRotation(mid - from);
            NameTags.I?.Place(); Canvas.ForceUpdateCanvases();
            Save(Path.Combine(shots, $"{(Coop.IsHost ? "host" : "rider")}-{shotN++:00}-{what}.png"));
            ct.SetPositionAndRotation(keep.position, keep.rotation);
        }
    }
}
