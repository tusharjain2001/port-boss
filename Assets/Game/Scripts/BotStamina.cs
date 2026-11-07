using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-staminatest [dir] (Tushar 7 Nov): stamina, solo. On the pier: a held sprint (back and forth along its longest
        // straight) runs dry in Angler.SprintSecs, then Shift gives only a run until it's back to 35%, and it's full a
        // breather + 5 s after; the ring shows while it isn't full and goes after. In the sea: treading water and a fast
        // swim drain at their rates, and dry in the water Dot fishes you out up a ladder. Pictures with a dir (-pb-fps 60).
        IEnumerator StaminaTest(string dir)
        {
            shots = dir; if (dir != null) Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true; me.Select(1); int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] stamina: {what}: {(ok ? "ok" : "FAIL")}"); }
            bool Near(float got, float want, float tol) => Mathf.Abs(got - want) <= tol;
            bool Ring() => Hud.I != null && Hud.I.transform.Find("stamina") is Transform r && r.gameObject.activeSelf && r.GetComponent<CanvasGroup>().alpha > 0.5f;
            float Sp() => new Vector3(me.Vel.x, 0, me.Vel.z).magnitude;

            // 1) a sprint on the pier: its longest straight through the spawn, from one end
            var from = me.transform.position; float best = 0; Vector3 axis = Vector3.forward;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45 + l.transform.eulerAngles.y, 0) * Vector3.forward;
                float r = Room(from, d) + Room(from, -d);
                if (r > best) { best = r; axis = d; }
            }
            me.Teleport(from - axis * Mathf.Max(0, Room(from, -axis) - 0.6f), Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg + 35;
            Debug.Log($"[PB] stamina: {best:F1} m of straight pier along {axis}");
            yield return new WaitForSeconds(1f);
            Ok(me.Stamina >= 1 && !Ring(), $"full at the start, no ring (stamina {me.Stamina:F2})");
            float sign = 1;
            Vector2 Fwd() { float a = (Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg - CameraRig.I.Yaw) * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * sign; }
            void Steer() { if (Room(me.transform.position, axis * sign) < 2.5f) sign = -sign; Ctl.BotMove = Fwd(); }
            Ctl.BotSprint = true; float t = 0, top = 0; bool half = false;
            while (!me.Winded && t < Angler.SprintSecs + 4)
            {
                Steer(); top = Mathf.Max(top, Sp());
                if (!half && me.Stamina < 0.5f) { half = true; Ok(Ring(), $"the ring shows mid-sprint ({t:F1} s, stamina {me.Stamina:F2})"); if (shots != null) yield return Shot("sprint-half"); }
                t += Time.deltaTime; yield return null;
            }
            Ok(me.Winded && Near(t, Angler.SprintSecs, 0.3f), $"a held sprint runs dry in {t:F2} s (spec {Angler.SprintSecs:F0} ± 0.3), top speed {top:F2} m/s");
            float tDry = Time.time;   // (the breather starts here: winded, Shift gives a run, which doesn't drain)
            if (shots != null) yield return Shot("sprint-winded");
            // winded, Shift still held: a run, never a sprint
            float hi = 0; for (float w = 0; w < 1.5f; w += Time.deltaTime) { Steer(); if (w > 0.5f) hi = Mathf.Max(hi, Sp()); yield return null; }
            Ok(hi <= 4.35f, $"winded with Shift held: at most a run ({hi:F2} m/s, run 4.2)");
            // let go and stand: the breath back at 35%, then full (both timed from running dry), then the ring goes
            Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero;
            float t35 = me.Winded ? -1 : Time.time - tDry, tFull = -1;
            while (Time.time - tDry < 12 && tFull < 0) { if (t35 < 0 && !me.Winded) t35 = Time.time - tDry; if (me.Stamina >= 1) tFull = Time.time - tDry; yield return null; }
            float want35 = Angler.Breather + Angler.WindedTill * Angler.RefillSecs, wantFull = Angler.Breather + Angler.RefillSecs;
            Ok(Near(t35, want35, 0.25f), $"breath back (35%) {t35:F2} s after running dry (spec {want35:F2})");
            Ok(Near(tFull, wantFull, 0.25f), $"full again {tFull:F2} s after running dry (spec {wantFull:F2})");
            yield return Until(() => !Ring(), 4);   // (the HUD fades on real time; -pb-fps races game time past it)
            Ok(!Ring(), "the ring has gone once it's full");

            // 2) the sea, 12 m off the pier's ladder: treading water, then a fast swim
            var lb = l.LadderBottom.position; var outward = lb - l.transform.position; outward.y = 0; outward.Normalize();
            var sea = lb + outward * 12; me.Teleport(new Vector3(sea.x, Sea.Height(sea) - Angler.SwimDepth, sea.z), me.transform.eulerAngles.y);
            yield return Until(() => me.Swimming, 3); yield return new WaitForSeconds(0.5f);
            float a0 = me.Stamina; yield return new WaitForSeconds(3f);
            float tread = (a0 - me.Stamina) / 3f;
            Ok(me.Swimming && Near(tread, 1 / Angler.SwimSecs, 0.0025f), $"treading water drains {tread * 100:F2}%/s (spec {100 / Angler.SwimSecs:F2})");
            if (shots != null) yield return Shot("treading");
            // a fast swim round in a circle (stays out in open water)
            Ctl.BotSprint = true; a0 = me.Stamina; float top2 = 0;
            for (float w = 0; w < 3f; w += Time.deltaTime) { CameraRig.I.Yaw += 40 * Time.deltaTime; Ctl.BotMove = Vector2.up; if (w > 1) top2 = Mathf.Max(top2, Sp()); yield return null; }
            float fast = (a0 - me.Stamina) / 3f;
            Ok(Near(fast, 1 / Angler.FastSwimSecs, 0.003f) && top2 > 2.0f, $"a fast swim drains {fast * 100:F2}%/s (spec {100 / Angler.FastSwimSecs:F2}) at {top2:F2} m/s");
            // 3) dry in the water: Dot fishes you out up a ladder
            me.Stamina = 0.03f; float t0 = Time.time;
            yield return Until(() => !me.Swimming, 6); Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero;
            float took = Time.time - t0; yield return new WaitForSeconds(0.5f); yield return Until(() => Hud.I.FadeA <= 0.001f, 4);
            float toLadder = Vector3.Distance(me.transform.position, l.LadderTop.position);
            Ok(!me.Swimming && toLadder < 2.5f, $"run dry in the water: fished out up the pier ladder in {took:F2} s ({toLadder:F2} m from its top)");
            Ok(Alerts.Queue.Exists(x => x.Sender == "Dot" && x.Text.Contains("Fished you out")), "Dot's word on the left");
            Ok(Hud.I.FadeA <= 0.001f, "the screen is back");
            if (shots != null) yield return Shot("fished-out");
            Debug.Log($"[PB] RESULT stamina: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }

        // -pb-phonemap <dir> (Tushar 7 Nov): the phone's Map on each of its tabs (All / Shops / Crew: no Fish tab), a picture each.
        IEnumerator PhoneMapShots(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(1f);
            Phone.I.Show(Phone.Scr.Map); yield return new WaitForSecondsRealtime(0.5f);
            bool fishTab = Phone.I.Tap("Fish");
            foreach (var tab in new[] { "All", "Shops", "Crew" })
            {
                bool ok = Phone.I.Tap(tab); yield return new WaitForSecondsRealtime(0.4f);
                Debug.Log($"[PB] phonemap: tab {tab} {(ok ? "ok" : "MISSING")}");
                yield return Shot("map-" + tab.ToLower());
            }
            Phone.I.Hide();
            Debug.Log($"[PB] RESULT phonemap: {(!fishTab ? "PASS" : "FAIL")} (a Fish tab: {fishTab})");
            Application.Quit();
        }
    }
}
