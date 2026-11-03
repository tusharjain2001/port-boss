using UnityEngine;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M3.1 (Manan + Tushar's test: "on a rejoin the camera first shows the host, then switches"). The camera only ever follows
    // my own body, but until the host's roster comes that body wears the default look: crew 0 in the slot-0 orange, the
    // host's own, standing at the spawn (where the host often is); then the roster re-dresses it and "back where I was"
    // moves it, and a crewmate's body stands where I am until their first pose comes. So a joiner's view waits under a
    // cover (night blue, "Joining the crew…") till the roster has me, the host has put me back (a newcomer: 0.6 s after the
    // roster) and every crewmate has a pose; then the camera cuts to me and the cover fades out in 0.4 s. Never stuck: 20 s
    // at most, and gone with the crew.
    public class JoinView : MonoBehaviour
    {
        public const float NewcomerWait = 0.6f, FadeIn = 0.4f, GiveUp = 20f;
        public static float Cover;          // (tests) how much of the view is covered, 0..1
        public static bool Lifted;          // (tests)
        Image img; Text txt; float t0, rosterAt = -1, liftAt = -1;

        void Start() { t0 = Time.unscaledTime; Cover = 1; Lifted = false; Coop.Placed = false; Build(); }

        void Build()
        {
            if (Hud.I == null || img != null) return;
            img = UiKit.Img(Hud.I.transform, null, Ponyo.Night, "join cover"); img.rectTransform.Stretch(); img.rectTransform.SetAsLastSibling();
            txt = UiKit.Txt(img.rectTransform, "Joining the crew…", UiKit.Display, 40, Ponyo.Paper, TextAnchor.MiddleCenter); txt.rectTransform.Stretch();
        }

        void Update()
        {
            Build(); if (img == null) return;
            float now = Time.unscaledTime;
            if (liftAt < 0)
            {
                var c = Coop.I; bool inRoster = Coop.On && c.Find(Coop.Me) != null;
                if (inRoster && rosterAt < 0) rosterAt = now;
                bool posed = true; if (c != null) foreach (var m in c.Mates.Values) if (m != null && m.Net.Count == 0) posed = false;
                bool ready = inRoster && (Coop.Placed || now - rosterAt > NewcomerWait) && (posed || now - rosterAt > 3f);
                if (ready || now - t0 > GiveUp || c == null)
                {
                    liftAt = now; Lifted = true;
                    Debug.Log($"[PB] coop: join view: lifted after {now - t0:F2} s ({(ready ? Coop.Placed ? "put back where I was" : "a newcomer" : c == null ? "not joining" : "gave up waiting")}), the camera cut to me");
                    var a = Angler.I; var cam = CameraRig.I;
                    if (cam != null && a != null) { cam.Target = a.transform; cam.Yaw = a.transform.eulerAngles.y + 25; cam.Cut(); }
                }
            }
            Cover = liftAt < 0 ? 1 : 1 - Mathf.SmoothStep(0, 1, (now - liftAt) / FadeIn);
            img.color = new Color(img.color.r, img.color.g, img.color.b, Cover); txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, Cover);
            if (Cover <= 0) Destroy(this);
        }

        void OnDestroy() { if (img != null) Destroy(img.gameObject); Cover = 0; }
    }
}
