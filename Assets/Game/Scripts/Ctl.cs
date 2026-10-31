using UnityEngine;
using UnityEngine.InputSystem;

namespace PortBoss.M1
{
    // Keyboard + mouse and pad, read straight from the devices (gdd systems §1.10). A test bot can drive it through Bot*.
    public static class Ctl
    {
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;
        static Gamepad P => Gamepad.current;
        public static bool PadLast;                       // the last device used (prompt glyphs follow it)

        // bot overrides (the -pb-* scenarios)
        public static bool Bot; public static Vector2 BotMove; public static bool BotUse, BotUsePressed, BotStrike, BotInteract, BotCancel, BotJump; public static int BotSlot = -1;

        static bool K_(Key k) => K != null && K[k].isPressed;
        static bool KD(Key k) => K != null && K[k].wasPressedThisFrame;

        public static Vector2 Move
        {
            get
            {
                if (Bot) return BotMove;
                var v = Vector2.zero;
                if (K_(Key.W)) v.y += 1; if (K_(Key.S)) v.y -= 1; if (K_(Key.D)) v.x += 1; if (K_(Key.A)) v.x -= 1;
                if (K != null && K.anyKey.isPressed) PadLast = false;
                if (P != null) { var s = P.leftStick.ReadValue(); if (s.sqrMagnitude > 0.04f) { v = s; PadLast = true; } }
                return Vector2.ClampMagnitude(v, 1);
            }
        }

        public static Vector2 Look
        {
            get
            {
                if (Bot) return Vector2.zero;
                var v = Vector2.zero;
                if (M != null) v += M.delta.ReadValue() * 0.08f * Settings.MouseSens;
                if (P != null) v += P.rightStick.ReadValue() * 160f * Time.unscaledDeltaTime * Settings.PadSens;
                if (Settings.Invert) v.y = -v.y;
                return v;
            }
        }

        public static bool Sprint => !Bot && (K_(Key.LeftShift) || (P != null && P.leftStickButton.isPressed));
        public static bool Walk => !Bot && K_(Key.LeftCtrl);
        public static bool Jump => Bot ? BotJump : KD(Key.Space) || (P != null && P.buttonSouth.wasPressedThisFrame);
        public static bool Interact => Bot ? BotInteract : KD(Key.E) || (P != null && P.buttonWest.wasPressedThisFrame);
        public static bool InteractHeld => !Bot && (K_(Key.E) || (P != null && P.buttonWest.isPressed));
        public static bool Drop => !Bot && (KD(Key.Q) || (P != null && P.dpad.down.wasPressedThisFrame));
        public static bool LetGo => !Bot && (KD(Key.Q) || (P != null && P.buttonWest.wasPressedThisFrame));
        public static bool Keep => Bot ? BotInteract : KD(Key.E) || (P != null && P.buttonSouth.wasPressedThisFrame);

        // Use = cast / strike (LMB / RT); Strike also takes Space / A (the minigame and the bite)
        public static bool Use => Bot ? BotUse : (M != null && M.leftButton.isPressed) || (P != null && P.rightTrigger.ReadValue() > 0.5f);
        public static bool UsePressed => Bot ? BotUsePressed : (M != null && M.leftButton.wasPressedThisFrame) || (P != null && P.rightTrigger.wasPressedThisFrame);
        public static bool Strike => Bot ? BotStrike : UsePressed || KD(Key.Space) || (P != null && P.buttonSouth.wasPressedThisFrame);
        public static bool CancelHeld => Bot ? BotCancel : (M != null && M.rightButton.isPressed) || (P != null && P.buttonEast.isPressed);
        public static bool CancelPressed => !Bot && ((M != null && M.rightButton.wasPressedThisFrame) || (P != null && P.buttonEast.wasPressedThisFrame));

        // When the strike happened, in realtime seconds: the device's last event time (not this frame's), so the
        // minigame judges the press itself (gdd fishing §3.7).
        public static double StrikeTime
        {
            get
            {
                if (Bot) return Time.realtimeSinceStartupAsDouble;
                double t = 0;
                if (M != null && M.leftButton.wasPressedThisFrame) t = System.Math.Max(t, M.lastUpdateTime);
                if (K != null && K.spaceKey.wasPressedThisFrame) t = System.Math.Max(t, K.lastUpdateTime);
                if (P != null && (P.rightTrigger.wasPressedThisFrame || P.buttonSouth.wasPressedThisFrame)) t = System.Math.Max(t, P.lastUpdateTime);
                return t > 0 ? t : Time.realtimeSinceStartupAsDouble;
            }
        }

        public static int SlotKey
        {
            get
            {
                if (Bot) { int b = BotSlot; BotSlot = -1; return b; }
                for (int i = 0; i < 8; i++) if (KD(Key.Digit1 + i)) return i;
                return -1;
            }
        }
        public static int SlotScroll
        {
            get
            {
                if (Bot) return 0;
                if (P != null) { if (P.rightShoulder.wasPressedThisFrame) return 1; if (P.leftShoulder.wasPressedThisFrame) return -1; }
                float s = M != null ? M.scroll.ReadValue().y : 0;
                return s > 0.1f ? -1 : s < -0.1f ? 1 : 0;
            }
        }

        public static bool Key_(Key k) => KD(k);
        public static bool Escape => KD(Key.Escape) || (P != null && P.startButton.wasPressedThisFrame);

        // helm: throttle notch steps, steering axis
        public static int BotThrottle; public static float BotSteer;
        public static int Throttle
        {
            get
            {
                if (Bot) { int b = BotThrottle; BotThrottle = 0; return b; }
                if (KD(Key.W)) return 1; if (KD(Key.S)) return -1;
                if (P != null) { if (P.rightTrigger.wasPressedThisFrame || P.dpad.up.wasPressedThisFrame) return 1; if (P.leftTrigger.wasPressedThisFrame || P.dpad.down.wasPressedThisFrame) return -1; }
                return 0;
            }
        }
        public static float Steer => Bot ? BotSteer : (K_(Key.D) ? 1 : 0) - (K_(Key.A) ? 1 : 0) + (P != null ? P.leftStick.ReadValue().x : 0);
    }

    // The few settings M1 needs (systems §1.10/§1.12), saved on this PC.
    public static class Settings
    {
        public static float MouseSens = PlayerPrefsGet("m1.mouse", 1f), PadSens = PlayerPrefsGet("m1.pad", 1f);
        public static bool Invert;
        public static float TimingOffsetMs = PlayerPrefsGet("m1.offset", 0f);  // −100…+100, the minigame calibration knob
        static float PlayerPrefsGet(string k, float d) { try { return PlayerPrefs.GetFloat(k, d); } catch { return d; } }
    }
}
