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
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2, 40);
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT crowdtest: FAIL (never connected)"); Application.Quit(); yield break; }
            float t0 = Time.time + 3f; var s = Skiff.I; var c = Coop.I;
            Angler Mate() { foreach (var m in c.Mates.Values) if (m != null) return m; return null; }
            var land = FindAnyObjectByType<Landing>(); var S = (land.Spawn != null ? land.Spawn : land.transform).position;
            var D = s.transform.position - S; D.y = 0; D.Normalize(); var side = Vector3.Cross(Vector3.up, D);
            float Yaw(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Vector3 Deck(float x, float z) => s.transform.TransformPoint(new Vector3(x, 1.0f, z));
            Vector3 Flat(Vector3 v) { v.y = 0; return v; }

            for (int round = 1; round <= 4; round++)
            {
                float r0 = t0 + (round - 1) * 7f;
                while (Time.time < r0) yield return null;
                bool boat = round >= 3, iWalk = round == 4 ? host : !host;
                Vector3 standAt, walkFrom, walkTo;
                if (!boat) { standAt = S + D * 0.8f; walkFrom = S - D * 1.6f + side * (round == 2 ? 0.25f : 0); walkTo = standAt + D * (round == 2 ? 2.2f : 0); }
                else { float z = round == 3 ? 0.3f : -0.5f; standAt = Deck(0.45f, z); walkFrom = Deck(-0.45f, z); walkTo = Deck(1.5f, z); }
                var wd = Flat(walkTo - walkFrom).normalized;
                me.Teleport(iWalk ? walkFrom : standAt, Yaw(iWalk ? wd : -wd)); CameraRig.I.Yaw = Yaw(wd);
                yield return new WaitForSeconds(1.5f);
                Vector3 Here() => boat ? s.transform.InverseTransformPoint(me.transform.position) : me.transform.position;
                var start = Here(); float minD = 9, path = 0, endPath = 0; bool shot = false, offBoat = false; var last = me.transform.position; float walkT = 0;
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
                        var step = boat ? Flat(me.transform.position - last) - Flat(s.transform.position - lastBoat) : Flat(me.transform.position - last);
                        if (walkT > 3f) endPath += step.magnitude;
                        path += step.magnitude;
                    }
                    if (boat && (!me.OnBoat || me.Swimming)) offBoat = true;
                    if (!shot && m != null && minD < 0.75f && walkT > 1.2f || !shot && !iWalk && Time.time > r0 + 4.5f) { shot = true; yield return Bump($"r{round}-{(iWalk ? "walking-into" : "walked-into")}", m); }
                    last = me.transform.position; lastBoat = s.transform.position;
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
                    else Check(endPath < 0.12f, $"round {round} {where}: pressed against them I stand still, no shaking ({endPath:F2} m of movement in the last 2 s)");
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
        Vector3 lastBoat;

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
