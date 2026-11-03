using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-rejointest host|fresh|quick|late <dir> (M3.1 item 8, tools/rejointest.sh): what a joiner sees while joining.
        // host: stands at the spawn (in a joiner's first view) till it quits. fresh: a first join; then aboard, and the
        // process dies (no goodbye: the host still holds the old connection). quick: back 2 s later (the host retires the old
        // connection), then quits properly. late: back 12 s after that. Every frame until 1.5 s after the view opens: is
        // what's shown me, as myself (the crew member the roster gave me), with the camera on me? Frames saved at the start,
        // when the roster comes, when the view opens and after. -pb-oldjoin: without the cover (the before).
        IEnumerator RejoinTest(string role, string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true;
            if (role == "host") { Debug.Log("[PB] rejoin: the host waits at the spawn"); yield break; }
            float t0 = Time.unscaledTime, rosterAt = -1, openAt = -1; int frames = 0, shown = 0, wrong = 0; string why = "";
            bool pic0 = false, pic1 = false, pic2 = false, pic3 = false, old = Arg("-pb-oldjoin") != null;
            while (Time.unscaledTime - t0 < 25)
            {
                var c = Coop.I; var e = Coop.On ? c.Find(Coop.Me) : null; float now = Time.unscaledTime;
                if (e != null && rosterAt < 0) rosterAt = now;
                float cover = old ? 0 : JoinView.Cover;
                bool open = old ? rosterAt >= 0 && (Coop.Placed || now - rosterAt > JoinView.NewcomerWait) : JoinView.Lifted;
                if (open && openAt < 0) openAt = now;
                frames++;
                if (cover < 0.98f)
                {
                    shown++;
                    var cam = CameraRig.I.Cam.transform.position; var d = cam - me.transform.position; d.y = 0;
                    string w = e == null ? "before the roster" : Angler.Body != e.Crew ? "in the wrong body" : role != "fresh" && !Coop.Placed ? "not put back yet" : d.magnitude > 12 ? $"the camera {d.magnitude:F0} m off me" : null;
                    if (w != null) { wrong++; if (why == "") why = $"{w} at {now - t0:F2} s"; }
                }
                if (!pic0 && now - t0 > 0.3f) { pic0 = true; yield return Shot($"{role}-0-start"); }
                if (!pic1 && rosterAt >= 0) { pic1 = true; yield return Shot($"{role}-1-roster-came"); }
                if (!pic2 && openAt >= 0 && now > openAt + 0.12f) { pic2 = true; yield return Shot($"{role}-2-view-opens"); }
                if (!pic3 && openAt >= 0 && now > openAt + 0.6f) { pic3 = true; yield return Shot($"{role}-3-in"); }
                if (openAt >= 0 && now > openAt + 1.5f) break;
                yield return null;
            }
            var me2 = Coop.On ? Coop.I.Find(Coop.Me) : null;
            Debug.Log($"[PB] rejoin: {role}{(old ? " (old, no cover)" : "")}: the roster after {rosterAt - t0:F2} s, the view open after {openAt - t0:F2} s, put back: {Coop.Placed}; {shown} of {frames} frames shown, {wrong} of them wrong ({(why == "" ? "none" : why)})");
            Check(openAt > 0, $"{role}: the view opens");
            Check(wrong == 0, $"{role}: nothing shown before I'm me, where I belong ({wrong} wrong frames{(why == "" ? "" : ": first " + why)})");
            if (role != "fresh") Check(Coop.Placed, $"{role}: put back where I was");
            Check(me2 != null && Angler.Body == me2.Crew, $"{role}: in my own body (crew {Angler.Body})");
            Debug.Log($"[PB] RESULT rejointest {role}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            if (role == "fresh")
            {
                // somewhere else to be put back to: aboard, by the stern
                var s = Skiff.I; me.Teleport(s.transform.TransformPoint(new Vector3(0.25f, 1.0f, -0.9f)), s.transform.eulerAngles.y);
                yield return new WaitForSeconds(4f);
                Debug.Log("[PB] rejoin: aboard; gone without a goodbye");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }
    }
}
