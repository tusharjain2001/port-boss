using UnityEngine;

namespace PortBoss.M1
{
    // M7 step 2 (gdd world §4.1 beats 4-5, §9.1 minutes 1:30-2:15): Biscuit's first morning.
    // - A new game finds her half full of rainwater and her engine cold. Bail her (hold E on deck: SkiffLeak), then pull
    //   the cord at the tiller (W). Before Doyle's rebuild the third pull catches; after it, the first. Flooded (a quarter
    //   full or more) she coughs and won't catch.
    // - Every morning she's cold again (moored or anchored, nobody at the tiller).
    // - Until Doyle's hull patch ($120) the old hull weeps: a little water every hour, so the bucket stays in use (Fergus:
    //   "put the bucket away").
    // - Only Biscuit, and only in a real game (ColdStarts). The tests' boats start warm and dry unless they ask
    //   (-pb-coldstart). The real copy owns it; Cold rides in the pose's flags (bit 7), Water as before.
    public partial class Skiff
    {
        public static bool ColdStarts;
        public const float FloodAt = 0.25f, WeepPerMin = 0.0003f;     // ~0.43 of a hull a day: a few scoops each morning
        public bool Cold; public int Pulls;
        float pullCool; int coldDay; bool floodSaid;
        public bool Old => Index == 0 && ColdStarts;                   // Biscuit, in a real game
        public int PullsNeeded => Firm.C != null && Firm.C.BoatOf(Id).Engine ? 1 : 3;

        // a new game: half full, cold
        public void Fresh() { if (!Old) return; Water = 0.5f; Cold = true; Pulls = 0; floodSaid = false; Debug.Log("[PB] biscuit: a new game: half full of rainwater, her engine cold"); }

        // the real copy, every frame
        void StartTick(float dt)
        {
            if (!Old) return;
            pullCool -= dt;
            if (Crewed) { Cold = false; return; }                     // (M6: a deckhand's run: they start her)
            if (!Sound && !Pump) Water = Mathf.Min(1, Water + WeepPerMin * dt);
            int day = DayTime.Day(Sky.Minutes - DayTime.Wake);
            if (coldDay == 0) { coldDay = day; if (!AtHelm && Notch == 0) Cold = true; }   // (a new game, a loaded one: she starts cold)
            if (day != coldDay) { coldDay = day; if (!AtHelm && Notch == 0 && (Moored || Anchored)) { Cold = true; Pulls = 0; } }
        }

        // the cord (the driver at the tiller: W). True when she caught.
        public bool Pull()
        {
            if (!Cold || pullCool > 0) return false;
            pullCool = 0.6f;
            var at = Motor != null ? Motor.position + Vector3.up * 0.25f : transform.position;
            Snd.Play(Synth.Ratchet, at, 0.55f, 0.75f);                  // the cord zips out
            if (Water >= FloodAt)
            {
                Snd.Play(Synth.Thup, at, 0.6f, 0.7f); PortBoss.M1.Fx.Dust(at, 3, 0.5f);
                if (!floodSaid) { floodSaid = true; Alerts.Push(Name, "She coughs and dies. She's flooded: bail her out first.", Alerts.Kind.Info, "flooded"); }
                Debug.Log($"[PB] biscuit: a pull, flooded ({Water:F2})");
                return false;
            }
            Pulls++;
            if (Pulls >= PullsNeeded)
            {
                Cold = false; Pulls = 0; floodSaid = false;
                Snd.Play(Synth.Thup, at, 0.8f, 1.1f); PortBoss.M1.Fx.Dust(at, 12, 1.3f); CameraRig.Shake(0.08f);
                Alerts.Push(Name, "She caught! W/S for the throttle.", Alerts.Kind.Info, "started", "flooded");
                StoryUi.Event("engine");
                Debug.Log("[PB] biscuit: she caught");
                return true;
            }
            Snd.Play(Synth.Thup, at, 0.55f, 0.85f + 0.1f * Pulls); PortBoss.M1.Fx.Dust(at, 5, 0.8f);
            Debug.Log($"[PB] biscuit: a pull ({Pulls}/{PullsNeeded})");
            return false;
        }
    }
}
