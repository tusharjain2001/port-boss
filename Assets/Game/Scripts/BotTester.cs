using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-testerkeys (Tushar 7 Nov): the tester keys on a virtual keyboard. Ctrl+F7 adds $100,000 and leaves the act;
        // Ctrl+F10 jumps to the next morning, 7:00; plain F7 still raises the act and adds $5,000; plain F10 still +1 hour.
        IEnumerator TesterKeys()
        {
            Ctl.Bot = true; Sleep.Off = true; int fails = 0;
            void Ok(bool ok, string what) { if (!ok) fails++; Debug.Log($"[PB] testerkeys: {what}: {(ok ? "ok" : "FAIL")}"); }
            yield return new WaitForSeconds(1f);
            var kb = InputSystem.AddDevice<Keyboard>("TesterKeyboard"); kb.MakeCurrent();
            IEnumerator Press(params Key[] keys)
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(keys)); yield return null; yield return null;
                InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return null; yield return null;
            }
            double OfDay() => Sky.Minutes - DayTime.At(Sky.Day, 0);
            var C = Firm.C; int cash0 = C.Cash, act0 = C.Act;
            yield return Press(Key.LeftCtrl, Key.F7);
            Ok(C.Cash == cash0 + 100000 && C.Act == act0, $"Ctrl+F7: bank ${cash0} -> ${C.Cash}, act {act0} -> {C.Act}");
            int day0 = Sky.Day;
            yield return Press(Key.LeftCtrl, Key.F10);
            Ok(Sky.Day == day0 + 1 && OfDay() >= 420 && OfDay() < 425, $"Ctrl+F10: Day {day0} -> Day {Sky.Day} {DayTime.Clock(Sky.Minutes)}");
            yield return Press(Key.LeftCtrl, Key.F10);
            Ok(Sky.Day == day0 + 2 && OfDay() < 425, $"Ctrl+F10 again: Day {Sky.Day} {DayTime.Clock(Sky.Minutes)}");
            Ok(Town.Open && Town.Lock("plank") == null, "Day 3: the ropes are down and Plank & Post is open");
            yield return Press(Key.F7);
            Ok(C.Act == act0 + 1 && C.Cash == cash0 + 105000, $"F7: act {C.Act}, bank ${C.Cash}");
            double m0 = Sky.Minutes;
            yield return Press(Key.F10);
            Ok(Sky.Minutes - m0 >= 60 && Sky.Minutes - m0 < 62, $"F10: +{Sky.Minutes - m0:F1} min");
            Debug.Log($"[PB] RESULT testerkeys: {(fails == 0 ? "PASS" : "FAIL")} ({fails} off)");
            Application.Quit();
        }
    }
}
