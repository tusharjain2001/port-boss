using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-officewalk <dir> (Tushar 7 Nov: "I am not able to enter this house"): from just outside the office's door,
        // walk in to the middle of the room. Logs where she stops and what's in the way; pictures outside and inside.
        IEnumerator OfficeWalk(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir); Ctl.Bot = true; Sleep.Off = true;
            yield return new WaitForSeconds(1.5f);
            var off = Works.I != null ? Works.I.Office : null; var door = off != null ? Util.Find(off, "socket_door") : null;
            if (off == null || door == null) { Debug.Log($"[PB] RESULT officewalk: FAIL (office {off != null}, door socket {door != null})"); Application.Quit(); yield break; }
            var mid = off.position; var to = mid - door.position; to.y = 0; float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            Debug.Log($"[PB] officewalk: office {off.name} at {mid}, door socket {door.position}, {to.magnitude:F2} m to the middle");
            me.Teleport(door.position + Vector3.up * 0.1f - to.normalized * 0.8f, yaw); CameraRig.I.Yaw = yaw;
            yield return new WaitForSeconds(0.8f); yield return Shot("office-door");
            float best = 9; Vector3 last = me.transform.position; float stuck = 0;
            for (float t = 0; t < 6; t += Time.deltaTime)
            {
                var d = mid - me.transform.position; d.y = 0; best = Mathf.Min(best, d.magnitude);
                if (d.magnitude < 0.5f) break;
                CameraRig.I.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up;
                if ((me.transform.position - last).magnitude < 0.002f) stuck += Time.deltaTime; else stuck = 0;
                last = me.transform.position;
                if (stuck > 0.6f)
                {
                    // what's in the way: a capsule cast a step ahead
                    var p = me.transform.position; var fwd = d.normalized;
                    if (Physics.CapsuleCast(p + Vector3.up * 0.35f, p + Vector3.up * 1.4f, 0.3f, fwd, out var hit, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                        Debug.Log($"[PB] officewalk: stuck at {p} ({d.magnitude:F2} m out), blocked by {Path(hit.collider.transform)} ({hit.collider.GetType().Name}) at {hit.point}");
                    else Debug.Log($"[PB] officewalk: stuck at {p} ({d.magnitude:F2} m out), nothing ahead at waist height (a step? feet {p.y:F2}, door {door.position.y:F2})");
                    break;
                }
                yield return null;
            }
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.6f);
            bool inside = best < 0.8f;
            // the roof lifts off while I'm in (and the room reads from the follow camera all round)
            var roof = Util.Find(off, "Roof");
            if (roof != null) yield return Until(() => !roof.GetComponentInChildren<Renderer>().enabled, 3);   // (it fades on real time)
            bool roofGone = roof != null && Works.I.InOffice && !roof.GetComponentInChildren<Renderer>().enabled;
            Debug.Log($"[PB] officewalk: inside {Works.I.InOffice}, roof {(roof == null ? "MISSING" : roofGone ? "lifted off" : "still on")}");
            for (int k = 0; k < 4; k++) { CameraRig.I.Yaw = yaw + 35 + k * 90; yield return new WaitForSeconds(0.5f); yield return Shot($"office-inside-{k * 90}"); }
            // Walt's cot is in here: its get-in spot is inside too
            var cot = Sleep.Walts; var bed = Sleep.BedSide; var lb = office0Floor(off);
            bool cotIn = cot != null && cot.IsChildOf(off) && lb.HasValue && lb.Value.Contains(new Vector3(bed.x, lb.Value.center.y, bed.z));
            Debug.Log($"[PB] officewalk: Walt's cot {(cot != null && cot.IsChildOf(off) ? "inside" : "OUTSIDE")}, get-in spot {bed} {(cotIn ? "in the room" : "NOT in the room")}");
            // walk back out: the roof comes back
            me.Teleport(door.position + Vector3.up * 0.1f - to.normalized * 1.5f, yaw); yield return new WaitForSeconds(0.3f);
            if (roof != null) yield return Until(() => roof.GetComponentInChildren<Renderer>().enabled, 3);
            bool back = roof != null && !Works.I.InOffice && roof.GetComponentInChildren<Renderer>().enabled;
            Debug.Log($"[PB] officewalk: outside again, roof {(back ? "back on" : "STILL OFF")}");
            Debug.Log($"[PB] RESULT officewalk: {(inside && roofGone && cotIn && back ? "PASS" : "FAIL")} (got within {best:F2} m of the room's middle)");
            Application.Quit();
            static Bounds? office0Floor(Transform o) { foreach (var c in o.GetComponentsInChildren<BoxCollider>()) if (c.name.EndsWith("_colDeck")) { var b = c.bounds; b.Expand(new Vector3(0, 4, 0)); return b; } return null; }
            static string Path(Transform t) { var s = t.name; for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s; return s; }
        }
    }
}
