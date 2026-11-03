using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-rodtest host|rider <dir> (M3.1 item 4, tools/rodtest.sh): the rider fishes off Biscuit's side until it lands
        // one; the host stands by and watches. Both check the catch on their own screen: for the first second the rod is
        // still in the right hand and the fish hangs from the left, then the rod goes over the shoulder onto the back and
        // both hands come to the fish. Pictures of each beat from both.
        IEnumerator RodTest(string role, string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; bool host = role == "host";
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2, 40);
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT rodtest: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(2f);
            var s = Skiff.I; var c = Coop.I;
            Angler Mate() { foreach (var m in c.Mates.Values) if (m != null) return m; return null; }
            var yaw = s.transform.eulerAngles.y + 90;
            me.Teleport(s.transform.TransformPoint(new Vector3(host ? -0.2f : 0.45f, 1.0f, host ? -0.9f : 0.5f)), host ? yaw - 60 : yaw);
            yield return new WaitForSeconds(1f);

            Angler a = host ? null : me;
            if (!host)
            {
                HoldReveal = 2.6f; me.Select(0); yield return new WaitForSeconds(0.8f);
                StartCoroutine(Beats("mine", me));
                for (int tries = 0; tries < 6; tries++)
                {
                    CameraRig.I.Yaw = yaw; yield return CastOnce(0.7f);
                    for (float w = 0; fish.State != Fishing.St.Waiting && w < 5; w += Time.deltaTime) yield return null;
                    if (fish.State != Fishing.St.Waiting) continue;
                    string row = null; yield return PlayFish(r => row = r);
                    Debug.Log("[PB] rod: fished " + row);
                    if (beatsDone) break;
                }
            }
            else
            {
                yield return Until(() => Mate() != null && Mate().Fish.State == Fishing.St.Reveal, 120);
                a = Mate(); if (a != null) yield return Beats("rider's", a);
            }
            yield return Until(() => beatsDone, 10);
            Check(beatsDone, "a catch was shown off");
            Debug.Log($"[PB] RESULT rodtest {role}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            yield return new WaitForSeconds(host ? 2f : 4f);
            Application.Quit();
        }

        bool beatsDone;
        // one catch, seen on this machine (mine or a crewmate's copy): measured through the reveal, a picture per beat
        IEnumerator Beats(string whose, Angler a)
        {
            var f = a.Fish;
            while (f.State != Fishing.St.Reveal) yield return null;
            bool big = f.Catch != null && f.Catch.Cm > 140;
            float showGrip = 0, showHang = 0, holdGrip = 0; int nShow = 0, nHold = 0; bool showInHand = true, stowedOnBack = false; bool p1 = false, p2 = false, p3 = false;
            while (f.State == Fishing.St.Reveal)
            {
                float t = f.StateT;
                if (t > 0.3f && t < Fishing.ShowT - 0.05f) { showInHand &= f.RodInHand; showGrip = Mathf.Max(showGrip, f.HandToGrip); if (!big) showHang = Mathf.Max(showHang, f.FishToHandL); nShow++; }
                if (t > Fishing.HandsFree + Fishing.HandOver + 0.1f) { stowedOnBack = f.RodOnBack; nHold++; }
                if (!p1 && t > 0.6f) { p1 = true; yield return Look($"{whose}-1-showing-off", a); }
                if (!p2 && t > Fishing.ShowT + Fishing.StowLen * 0.45f) { p2 = true; yield return Look($"{whose}-2-stowing", a); }
                if (!p3 && t > Fishing.HandsFree + Fishing.HandOver + 0.25f) { p3 = true; yield return Look($"{whose}-3-holding", a); }
                yield return null;
            }
            Debug.Log($"[PB] rod: {whose} catch: {nShow} frames shown off, {nHold} held (a {(big ? "big" : "small")} fish)");
            Check(nShow > 10 && showInHand && showGrip < 0.12f, $"{whose}: the first second the rod stays in the right hand (hand ≤ {showGrip:F2} m off its grip)");
            Check(big || showHang < 0.14f, $"{whose}: the fish hangs from the left hand meanwhile (hand ≤ {showHang:F2} m off it)");
            Check(nHold > 10 && stowedOnBack, $"{whose}: then the rod is on the back and both hands hold the fish");
            beatsDone = true;
        }

        // a 3/4 front picture of an angler, from 2.4 m off, at chest height
        IEnumerator Look(string what, Angler a)
        {
            yield return new WaitForEndOfFrame();
            var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
            var look = a.transform.position + Vector3.up * 1.15f;
            var from = look + Quaternion.Euler(0, a.transform.eulerAngles.y + 35, 0) * new Vector3(0, 0.35f, 2.4f);
            ct.position = from; ct.rotation = Quaternion.LookRotation(look - from);
            NameTags.I?.Place(); Canvas.ForceUpdateCanvases();
            Save(Path.Combine(shots, $"{(Coop.IsHost ? "host" : "rider")}-{shotN++:00}-{what}.png"));
            ct.SetPositionAndRotation(keep.position, keep.rotation);
        }
    }
}
