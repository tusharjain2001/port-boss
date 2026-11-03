using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-shovetest host|rider <dir> (M3.1 item 6, tools/shovetest.sh): the host shoves the rider (G), four times, both
        // machines on one clock (from when they meet). 1) on the jetty: the push, the rider goes over and gets up; a second G
        // inside 3 s does nothing. 2) aboard, the rider's line out waiting: it reels in, bait kept. 3) the rider fighting a
        // fish: it gets away, bait lost. 4) the rider at the rail: flung over it, into the water. Pictures from both.
        IEnumerator ShoveTest(string role, string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; bool host = role == "host";
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2, 40);
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT shovetest: FAIL (never connected)"); Application.Quit(); yield break; }
            float t0 = Time.time + 3f; var s = Skiff.I; var c = Coop.I;
            Angler Mate() { foreach (var m in c.Mates.Values) if (m != null) return m; return null; }
            var land = FindAnyObjectByType<Landing>(); var S = (land.Spawn != null ? land.Spawn : land.transform).position;
            var D = s.transform.position - S; D.y = 0; D.Normalize();
            float Yaw(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Vector3 Deck(float x, float z) => s.transform.TransformPoint(new Vector3(x, 1.0f, z));
            float[] at = { 0, 9, 24, 66 }, len = { 9, 15, 42, 30 };

            for (int round = 1; round <= 4; round++)
            {
                float r0 = t0 + at[round - 1];
                while (Time.time < r0) yield return null;
                Ctl.BotMove = Vector2.zero; Fishing.Bot.ForceHeavy = Fishing.Bot.FightHeavy = round == 3;
                Vector3 riderAt, hostAt;
                if (round == 1) { riderAt = S + D * 0.8f; hostAt = S - D * 0.2f; }
                else if (round < 4) { riderAt = Deck(0.05f, 0.6f); hostAt = Deck(0.05f, -0.5f); }
                else { riderAt = Deck(0.42f, 0.2f); hostAt = Deck(-0.45f, 0.2f); }
                var face = riderAt - hostAt; face.y = 0;
                if (host) { me.Teleport(hostAt, Yaw(face)); CameraRig.I.Yaw = Yaw(face); }
                else
                {
                    if (me.Swimming || me.Down) yield return new WaitForSeconds(2.5f);
                    me.Teleport(riderAt, round is 2 or 3 ? s.transform.eulerAngles.y + 90 : Yaw(-face));
                    if (round is 2 or 3) { me.Select(0); CameraRig.I.Yaw = s.transform.eulerAngles.y + 90; } else me.Select(1);
                }
                yield return new WaitForSeconds(1.2f);
                Debug.Log($"[PB] shove: round {round}: I stand at {s.transform.InverseTransformPoint(me.transform.position)} on the boat (meant {s.transform.InverseTransformPoint(host ? hostAt : riderAt)}), on board {me.OnBoat}");
                if (host && Mate() is Angler mm)
                {
                    var dd = mm.transform.position - me.transform.position; dd.y = 0;
                    Debug.Log($"[PB] shove: round {round}: I see the rider at {s.transform.InverseTransformPoint(mm.transform.position)} on the boat, {dd.magnitude:F2} m off");
                    me.Teleport(me.transform.position, Yaw(dd)); CameraRig.I.Yaw = Yaw(dd); yield return null;
                }
                if (host) yield return ShoveRound(round, r0 + len[round - 1] - 0.5f, Mate);
                else yield return ShovedRound(round, r0 + len[round - 1] - 0.5f);
            }
            Fishing.Bot.ForceHeavy = Fishing.Bot.FightHeavy = false;
            Debug.Log($"[PB] RESULT shovetest {role}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            yield return new WaitForSeconds(host ? 3f : 1f);
            Application.Quit();
        }

        // the host: wait for the moment (round 2: their line out; 3: a fish on), press G, watch them go over
        IEnumerator ShoveRound(int round, float until, System.Func<Angler> mate)
        {
            var m = mate(); if (m == null) { Check(false, $"round {round}: no crewmate"); yield break; }
            if (round == 2) yield return Until(() => m.Fish.State == Fishing.St.Waiting && m.Fish.StateT > 1f, until - Time.time - 4);
            if (round == 3) yield return Until(() => m.Fish.State is Fishing.St.Strike or Fishing.St.Heavy or Fishing.St.Minigame, until - Time.time - 4);
            // round 4: wait till I see them up and at the rail (round 3 can leave them swimming back: the two copies' clocks
            // drift apart under -pb-fps, so the fixed timetable alone shoved the spot they had not reached yet)
            if (round == 4)
            {
                yield return Until(() => { var d = m.transform.position - me.transform.position; d.y = 0; return !m.Down && !m.Swimming && Angler.NearRail(m, d.normalized); }, until - Time.time - 4);
                var dd = m.transform.position - me.transform.position; dd.y = 0; me.Teleport(me.transform.position, Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg); CameraRig.I.Yaw = Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg; yield return null;
            }
            Debug.Log($"[PB] shove: round {round}: shoving the rider ({Vector3.Distance(me.transform.position, m.transform.position):F2} m off, their fishing {m.Fish.State}, at the rail: {Angler.NearRail(m, (m.transform.position - me.transform.position).normalized)})");
            int sent = Angler.ShovesSent; float lo = 99, hi = -1; bool sawDown = false, shot1 = false, shot2 = false;
            Ctl.BotShove = true; yield return null; Ctl.BotShove = false;
            for (float t = 0; t < 2.2f; t += Time.deltaTime)
            {
                if (me.ShoveT >= 0) { lo = Mathf.Min(lo, me.ShoveT); hi = Mathf.Max(hi, me.ShoveT); }
                sawDown |= m.Down || m.Swimming;
                if (!shot1 && me.ShoveT >= Angler.ShoveHit) { shot1 = true; yield return Pair($"r{round}-the-shove", m); }
                if (!shot2 && t > 0.7f) { shot2 = true; yield return Pair($"r{round}-over", m); }
                if (round == 1 && t > 1.0f && t < 1.05f) { int before = Angler.ShovesSent; Ctl.BotShove = true; yield return null; Ctl.BotShove = false; yield return null; Check(me.ShoveT < 0 && Angler.ShovesSent == before, "round 1: a second G inside 3 s does nothing (cooldown)"); }
                yield return null;
            }
            Check(Angler.ShovesSent == sent + 1 && hi > Angler.ShoveHit, $"round {round}: G at my crewmate: the push plays ({lo:F2}..{hi:F2} s) and is sent");
            Check(sawDown, $"round {round}: I see them go over");
            while (Time.time < until) yield return null;
        }

        // the rider: stand (or fish) and be shoved; check what it did to me
        IEnumerator ShovedRound(int round, float until)
        {
            int bait0 = fish.BaitCount[fish.BaitI]; float downAt = -1, upAt = -1; bool wet = false, shot = false; string outcome = null; Fishing.St before = fish.State;
            bool cast = false, struck = false;
            while (Time.time < until)
            {
                if (round is 2 or 3 && !me.Down && downAt < 0)
                {
                    if (fish.State == Fishing.St.Ready && !cast) { cast = true; CameraRig.I.Yaw = Skiff.I.transform.eulerAngles.y + 90; yield return CastOnce(0.6f); bait0 = fish.BaitCount[fish.BaitI]; continue; }
                    if (fish.State == Fishing.St.Ready && cast && round == 3 && Time.time < until - 8) { cast = false; struck = false; } // (missed it: again)
                    if (fish.State == Fishing.St.Bite && !struck && round == 3) { struck = true; yield return new WaitForSeconds(0.3f); Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; }
                    before = fish.State;
                }
                if (me.Down && downAt < 0) { downAt = Time.time; outcome = fish.LastOutcome; }
                if (downAt >= 0 && upAt < 0 && !me.Down) upAt = Time.time;
                wet |= me.Swimming;
                if (!shot && downAt >= 0 && Time.time > downAt + 0.5f) { shot = true; var m = FindMate(); if (m != null) yield return Pair($"r{round}-shoved", m); }
                if (round == 4 && wet && upAt >= 0) break;
                yield return null;
            }
            if (downAt >= 0 && upAt < 0) upAt = Time.time;
            int bait1 = fish.BaitCount[fish.BaitI];
            Debug.Log($"[PB] shove: round {round}: down {(downAt >= 0 ? "yes" : "no")}, {upAt - downAt:F1} s till up, my fishing was {before} -> {fish.State} ('{outcome}'), bait {bait0} -> {bait1}, in the water: {wet}");
            Check(downAt >= 0, $"round {round}: shoved, I go over");
            if (round == 1) Check(upAt - downAt is > 0.8f and < 4f, $"round 1: down, then up again ({upAt - downAt:F1} s with the get-up)");
            if (round == 2) Check(before is Fishing.St.Waiting or Fishing.St.Settle && outcome == "reeled" && bait1 == bait0, "round 2: my line was out waiting: it reels in, the bait kept");
            if (round == 3) Check(before is Fishing.St.Strike or Fishing.St.Heavy or Fishing.St.Minigame && outcome == "knocked" && bait1 == bait0 - 1 && fish.Catch == null, "round 3: a fish on: it gets away, the bait lost");
            if (round == 4) Check(wet, "round 4: shoved at the rail: over it, into the water");
        }
        Angler FindMate() { foreach (var m in Coop.I.Mates.Values) if (m != null) return m; return null; }

        // the two of them from the side
        IEnumerator Pair(string what, Angler m) => Bump(what, m);
    }
}
