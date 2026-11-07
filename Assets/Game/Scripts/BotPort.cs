using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-porttest <dir> (Tushar 7 Nov, plan A): Walt's Landing port. The quay, the yard, the fence and its gate are
        // there with their colliders; from the pier she walks onto the quay and along it to both ends, across the yard to
        // the gate and out of it into the town lane and back, and the fence holds her in elsewhere; never in the water.
        // Pictures: from the sea, from above, on the quay, at the gate (noon, -pb-fps 60).
        IEnumerator PortTest(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] port: {what}: {(ok ? "ok" : "FAIL")}"); }
            yield return new WaitForSeconds(1f);
            var l = Landing.I; var port = l != null ? l.transform.Find("Port") : null;
            Ok(port != null, "the port model is under the landing");
            if (port == null) { Debug.Log("[PB] RESULT porttest: FAIL"); Application.Quit(); yield break; }
            int decks = 0, fence = 0; foreach (var c in port.GetComponentsInChildren<BoxCollider>()) { if (c.name.EndsWith("_colDeck")) decks++; if (c.name.StartsWith("Fence")) fence++; }
            Ok(decks >= 3 && fence >= 4, $"colliders: {decks} decks (quay x2, yard), {fence} fence runs");
            // landing metres -> world (the port is modelled in them, under the landing)
            Vector3 P(float x, float y) => port.TransformPoint(new Vector3(-x, 1.32f, -y));
            bool dry = true; float lowest = 99;
            void Watch() { if (me.Swimming) dry = false; lowest = Mathf.Min(lowest, me.transform.position.y); }
            // walk to a landing-metres point, steering straight at it; true if she got within 0.8 m
            IEnumerator Walk(float x, float y, float secs)
            {
                var to = P(x, y);
                for (float t = 0; t < secs; t += Time.deltaTime)
                {
                    var d = to - me.transform.position; d.y = 0; Watch();
                    if (d.magnitude < 0.8f) { Ctl.BotMove = Vector2.zero; yield break; }
                    CameraRig.I.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up; yield return null;
                }
                Ctl.BotMove = Vector2.zero;
            }
            float To(float x, float y) { var d = P(x, y) - me.transform.position; d.y = 0; return d.magnitude; }
            // 1) from the pier's root onto the quay, along it to the north end and back to the south end (round the end of
            //    the pier's rope rail, which runs on to its root)
            me.Teleport(P(0, 10.5f), 0); yield return new WaitForSeconds(0.5f);
            yield return Walk(-4, 10, 4); yield return Walk(-4, 3, 4); yield return Walk(-32, 3, 14);
            Ok(To(-32, 3) < 1 && dry, $"along the quay to its north end ({To(-32, 3):F1} m off, dry {dry})");
            if (shots != null) { CameraRig.I.Yaw += 60; yield return new WaitForSeconds(0.6f); yield return Shot("port-quay-north"); }
            yield return Walk(-4, 3, 14); yield return Walk(-4, 10, 4); yield return Walk(4, 10, 4); yield return Walk(4, 3, 4); yield return Walk(32, 3, 14);
            Ok(To(32, 3) < 1 && dry, $"across the root and along to the south end ({To(32, 3):F1} m off, dry {dry})");
            // 2) across the yard to the gate, out into the lane, and back in
            // (the Market gate: open on Day 1, the town gate has its barrier till the town opens: Town.cs)
            yield return Walk(17, 40, 16); yield return Walk(17, 46, 5);
            Ok(To(17, 46) < 1 && dry, $"out through the Market gate onto the Market Lane ({To(17, 46):F1} m off)");
            if (shots != null) { CameraRig.I.Yaw += 180; yield return new WaitForSeconds(0.6f); yield return Shot("port-gate-from-town"); }
            yield return Walk(17, 38, 5); Ok(To(17, 38) < 1, "back in through the Market gate");
            // 3) the fence holds: push at it inland (away from the gate) and at the north side for 3 s each
            foreach (var (fx, fy, tx, ty, what) in new[] { (10f, 40f, 10f, 48f, "the inland fence"), (-30f, 26f, -40f, 26f, "the north fence") })
            {
                me.Teleport(P(fx, fy), 0); yield return new WaitForSeconds(0.3f); yield return Walk(tx, ty, 3);
                var lp = port.InverseTransformPoint(me.transform.position); float bx = -lp.x, by = -lp.z;
                Ok(bx > -34.2f && by < 43.8f, $"{what} holds (stopped at x {bx:F1}, y {by:F1})");
            }
            Ok(lowest > 1.0f, $"never below the deck ({lowest:F2} m)");
            // 3b) the open gate leaves are solid (Tushar 7 Nov take 2): walk along the inside of the fence into the Market
            //     gate's left leaf (hinged at x 14.9, swung in to y 41.6)
            me.Teleport(P(12.3f, 42.6f), 0); yield return new WaitForSeconds(0.3f); yield return Walk(17.5f, 42.6f, 3);
            { var lp = port.InverseTransformPoint(me.transform.position); float bx = -lp.x; Ok(bx < 14.95f, $"the Market gate's open leaf holds (stopped at x {bx:F1})"); }
            // 3c) no casting from the middle of the yard (Tushar 7 Nov take 2), but from the quay's edge as ever
            me.Teleport(P(-8, 25), 0); yield return new WaitForSeconds(0.3f);
            if (!me.Bar.Sel.Rod) me.Select(0);                         // (slot 1, the rod, is index 0)
            for (float w = 0; fish.State != Fishing.St.Ready && w < 3; w += Time.deltaTime) yield return null;
            bool up = fish.State == Fishing.St.Ready, yardWater = fish.WaterNear;
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return new WaitForSeconds(0.3f);
            var st = fish.State; Ctl.BotUse = false; yield return null;
            Ok(up && !yardWater && st == Fishing.St.Ready, $"mid-yard: rod up {up}, water in reach {yardWater}, after LMB the rod is {st}");
            me.Teleport(P(-8, -0.9f), 180); yield return new WaitForSeconds(0.5f);
            Ok(fish.WaterNear, $"at the quay's edge: water in reach {fish.WaterNear}");
            me.Select(1); yield return new WaitForSeconds(0.3f);          // (the rod away: an empty slot)
            // 4) pictures: from the sea, from above
            if (shots != null)
            {
                me.Teleport(P(0, 15), 0); yield return new WaitForSeconds(0.5f);
                FlyCam(P(-10, -60) + Vector3.up * 22, P(0, 20)); yield return new WaitForSeconds(0.5f); yield return Shot("port-from-the-sea");
                FlyCam(P(0, 22) + Vector3.up * 95 + port.TransformDirection(Vector3.forward) * -1, P(0, 22)); yield return new WaitForSeconds(0.5f); yield return Shot("port-from-above");
                FlyCam(P(30, 50) + Vector3.up * 14, P(-5, 8)); yield return new WaitForSeconds(0.5f); yield return Shot("port-from-the-town");
            }
            Debug.Log($"[PB] RESULT porttest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
