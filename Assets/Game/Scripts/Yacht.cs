using UnityEngine;

namespace PortBoss.M1
{
    // M7 step 4: Voss's yacht Efficiency glides past Walt's Landing in the Day 4 fog (gdd world §4.2, §9.1 47:00: "a 4 s
    // beat with input live"; art-src/m7/build_yacht.py -> Art/M5/PB_Boat_efficiency.fbx). Worked out from the clock, so
    // every screen in the crew sees the same crossing without a message: 07:25 to 07:55 on Day 4 it runs across the bay
    // 140 m off the landing, 280 m in 30 in-game minutes (a slow glide in the fog). The first time it's in
    // sight of me (within 260 m) the story hears it (StoryUi.Event("yacht"): Dot's text) and the camera eases toward it
    // for 4 s if I'm not busy (no input taken).
    public class Yacht : MonoBehaviour
    {
        public static Yacht I;
        public const int Day = 4; public const double From = 7 * 60 + 25, To = 7 * 60 + 55;
        Transform boat; float nudge = -1; bool seen;
        public bool Out => boat != null && boat.gameObject.activeSelf;          // (tests)
        public Vector3 At => boat != null ? boat.position : Vector3.zero;

        public static void Create()
        {
            var pf = M1Assets.I != null ? M1Assets.I.M5Prefab("Boat_efficiency") : null;
            var g = new GameObject("Yacht"); I = g.AddComponent<Yacht>();
            if (pf == null) { Debug.Log("[PB] yacht: no model (Art/M5/PB_Boat_efficiency.fbx)"); return; }
            var b = Instantiate(pf, g.transform); b.name = "Efficiency"; I.boat = b.transform;
            Util.Toon(b, Game.ToonMat);
            foreach (var r in b.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var n = Util.Find(b.transform, "socket_name");
            if (n != null) { var t = new GameObject("name").transform; t.SetParent(n, false); t.localRotation = Quaternion.Euler(0, -90, 0); Firm.Sign(t, "EFFICIENCY", Vector3.zero, Ponyo.Paper, 0.0075f); }
            b.SetActive(false);
        }

        // the crossing: where she is at minute m of the day (null: not out)
        public static bool Where(double m, out Vector3 pos, out Quaternion rot)
        {
            pos = default; rot = Quaternion.identity; var l = Landing.I;
            if (l == null || DayTime.Day(m) != Day) return false;
            double h = DayTime.OfDay(m); if (h < From || h > To) return false;
            float k = (float)((h - From) / (To - From));
            Vector3 sea = l.transform.forward; sea.y = 0; sea.Normalize(); var side = Vector3.Cross(Vector3.up, sea);
            var mid = l.transform.position + sea * 140f;
            pos = mid + side * Mathf.Lerp(-140f, 140f, k); pos.y = Sea.Height(pos) - 0.05f;
            rot = Quaternion.LookRotation(side) * Quaternion.Euler(Mathf.Sin((float)m * 0.9f) * 0.8f, 0, Mathf.Sin((float)m * 0.7f) * 1.2f);
            return true;
        }

        void Update()
        {
            if (boat == null) return;
            bool on = Where(Sky.Minutes, out var p, out var r);
            if (boat.gameObject.activeSelf != on) { boat.gameObject.SetActive(on); if (on) Debug.Log($"[PB] yacht: Efficiency out in the bay ({DayTime.Clock(Sky.Minutes)})"); }
            if (!on) return;
            boat.SetPositionAndRotation(p, r);
            var me = Angler.I;
            if (!seen && me != null && !me.Remote && Vector3.Distance(me.transform.position, p) < 260f)
            {
                seen = true; StoryUi.Event("yacht");
                bool busy = me.Fish.Locked || Phone.IsOpen || Shop.Open || StoryUi.LetterOpen || me.AtHelm;
                if (!busy && CameraRig.I != null) nudge = 4f;
                Snd.Play(Synth.Horn, p, 0.5f, 0.82f);
            }
            if (nudge > 0 && CameraRig.I != null)
            {
                nudge -= Time.deltaTime;
                var d = p - me.transform.position; d.y = 0;
                CameraRig.I.Yaw = Mathf.LerpAngle(CameraRig.I.Yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, Time.deltaTime * 1.4f);
            }
        }
    }
}
