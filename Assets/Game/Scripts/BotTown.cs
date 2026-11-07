using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-towntest <dir> (Tushar 7 Nov: the town opens step by step). Day 1, $40: from the port yard a walk (the map
        // walk's A* over what's really there) reaches June's and Pratt's, and nothing else: not Doyle's, Plank & Post, the
        // pub, Hartley's, the post office, nor the waterfront either way; she really walks the Market Lane to Pratt's.
        // $60 made ($100 in the bank): the ropes come down and Doyle's is a walk away; Plank & Post says Day 2. Day 2:
        // Plank & Post opens; Day 5: the pub, Hartley's and the post office. Pictures: the board, a barrier, the lanes.
        // a point on the town quay a few metres past each end of the port's quay (world x, z; map B, build_island.py)
        static readonly Vector3 QuayS = new(25.55f, 1.6f, -40.5f), QuayN = new(28.15f, 1.6f, 40.5f);
        IEnumerator TownTest(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] town: {what}: {(ok ? "ok" : "FAIL")}"); }
            yield return new WaitForSeconds(1f);
            var C = Firm.C; var t = Town.I;
            Ok(t != null && !Town.Open && t.Pieces.Count > 20, $"Day 1, ${C.Cash}: the town is roped off ({t?.Pieces.Count} ropes and barriers)");
            var yard = Landing.I.transform.TransformPoint(new Vector3(0, 1.32f, -20f));     // the middle of the port yard
            bool Reach(Vector3 to) => WalkPath(yard, to) != null;
            Vector3 Door(string id) { var a = Places.Get(id); return a.position + a.forward * 1.5f; }
            foreach (var id in new[] { "tackle", "pratts" }) Ok(Reach(Door(id)), $"Day 1: {id} is a walk from the yard");
            foreach (var id in new[] { "doyle", "plank", "pub", "hartleys", "post" })
            {
                var lp = WalkPath(yard, Door(id)); Ok(lp == null, $"Day 1: {id} can't be walked to");
                if (lp != null) { var sb = new System.Text.StringBuilder(); for (int i = lp.Count - 1; i >= 0; i -= 4) sb.Append($"({lp[i].x:F0},{lp[i].z:F0}) "); Debug.Log($"[PB] town: the way to {id}: {sb}"); }
            }
            Ok(!Reach(new Vector3(31, 1.6f, 66)), "Day 1: the waterfront north can't be walked to");
            Ok(!Reach(new Vector3(26, 1.6f, -46)), "Day 1: the waterfront south can't be walked to");
            // (Tushar 7 Nov take 2: the town quay now runs on to both ends of the port's quay: barred there on Day 1)
            Ok(!Reach(QuayS) && !Reach(QuayN), "Day 1: the town quay past either end of the port's quay can't be walked to");
            Ok(Town.Lock("doyle") != null && Town.Lock("plank") != null && Town.Lock("pub") != null, "Day 1: the later shops are locked");
            // the board at the pier's head: the closed rows say so
            var row = Signs.I.Arrows.Find(a => a.id == "pub").board; var tx = row != null ? row.GetComponentsInChildren<TextMesh>() : null;
            Ok(tx != null && tx.Length > 1 && tx[1].text == "closed", $"the board's pub row reads \"{(tx != null && tx.Length > 1 ? tx[1].text : "?")}\"");
            if (shots != null)
            {
                FlyCam(Signs.I.Post.position + Signs.I.Post.forward * 7 + Vector3.up * 1.6f, Signs.I.Post.position + Vector3.up * 2.2f); yield return new WaitForSeconds(0.4f); yield return Shot("board-day1");
                FlyCam(new Vector3(132, 3.2f, -17.5f), new Vector3(148, 2.5f, -14)); yield return new WaitForSeconds(0.4f); yield return Shot("market-lane-end");
                FlyCam(new Vector3(30, 3.5f, -24), new Vector3(23.8f, 2.4f, -34.4f)); yield return new WaitForSeconds(0.4f); yield return Shot("south-barrier");
                CameraRig.I.enabled = true;
            }
            // she walks the Market Lane to Pratt's counter
            var path = WalkPath(yard, Door("pratts")); me.Teleport(yard + Vector3.up * 0.1f, 0); yield return new WaitForSeconds(0.4f);
            int k = 0; float tw = 0;
            while (path != null && k < path.Count && tw < 90)
            {
                var to = path[k] - me.transform.position; to.y = 0;
                if (to.magnitude < (k == path.Count - 1 ? 0.7f : 1.1f)) { k++; continue; }
                CameraRig.I.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up; tw += Time.deltaTime; yield return null;
            }
            Ctl.BotMove = Vector2.zero;
            Ok(path != null && k >= path.Count, $"walked the Market Lane to Pratt's in {tw:F0} s ({Vector3.Distance(me.transform.position, Door("pratts")):F1} m off)");
            // $60 made: the ropes come down
            C.Earn(Firm.I.NewTx("towntest"), Works.MyName, "Town test", 60, Sky.Minutes); Firm.I.Dirty = true;
            yield return null; yield return null; yield return new WaitForSeconds(0.3f);
            Ok(Town.Open && t.Pieces.Count == 0, $"${C.Cash} in the bank: the ropes are down");
            Ok(Reach(Door("doyle")), "Doyle's is a walk from the yard");
            Ok(Reach(QuayS) && Reach(QuayN), "the town quay is a walk from the port's quay, both ends");
            Ok(Town.Lock("plank") == "opens on Day 2" && Town.Lock("pub") == "opens on Day 5", $"Plank & Post: {Town.Lock("plank")}; the pub: {Town.Lock("pub")}");
            // Day 2, Day 5
            Sky.Jump(DayTime.At(2, 9 * 60)); yield return null; yield return null;
            Ok(Town.Lock("plank") == null && Town.Lock("hartleys") != null, "Day 2: Plank & Post opens, Hartley's not yet");
            Sky.Jump(DayTime.At(5, 9 * 60)); yield return null; yield return null;
            Ok(Town.Lock("pub") == null && Town.Lock("hartleys") == null && Town.Lock("post") == null, "Day 5: the pub, Hartley's, the post office open");
            Debug.Log($"[PB] RESULT towntest: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
