using System;
using System.Collections.Generic;
using PortBoss.M1;
using UnityEditor;
using UnityEngine;

namespace PortBoss.EditorTools
{
    // The angler's fishing clips, keyed here and saved as humanoid .anim assets (Assets/Game/Anim, open them in the
    // Animation window). Each clip is Mixamo's Breathing Idle (real breathing, real balance) with keyed body offsets on
    // top: the hips shift and turn with both feet held where they stand (leg IK), the spine twists / leans / side-bends
    // over its three joints, the head looks. The arms come from the hand-on-rod IK in the game, so they are left as idle.
    // Offsets are in the character's own frame: x = right, y = up, z = forward; degrees: yaw + = turn right,
    // pitch + = lean forward / look down, roll + = bend toward the right side.
    public static class M1Anim
    {
        const string Dir = "Assets/Game/Anim";

        public struct P
        {
            public Vector3 Hip; public float HipYaw, Yaw, Pitch, Roll, HeadYaw, HeadPitch;
            public static P operator +(P a, P b) => new() { Hip = a.Hip + b.Hip, HipYaw = a.HipYaw + b.HipYaw, Yaw = a.Yaw + b.Yaw, Pitch = a.Pitch + b.Pitch, Roll = a.Roll + b.Roll, HeadYaw = a.HeadYaw + b.HeadYaw, HeadPitch = a.HeadPitch + b.HeadPitch };
            public static P operator *(P a, float k) => new() { Hip = a.Hip * k, HipYaw = a.HipYaw * k, Yaw = a.Yaw * k, Pitch = a.Pitch * k, Roll = a.Roll * k, HeadYaw = a.HeadYaw * k, HeadPitch = a.HeadPitch * k };
            public static P Lerp(P a, P b, float t) => a * (1 - t) + b * t;
        }
        static P Pz(float hx = 0, float hy = 0, float hz = 0, float hipYaw = 0, float yaw = 0, float pitch = 0, float roll = 0, float headYaw = 0, float headPitch = 0)
            => new() { Hip = new Vector3(hx, hy, hz), HipYaw = hipYaw, Yaw = yaw, Pitch = pitch, Roll = roll, HeadYaw = headYaw, HeadPitch = headPitch };

        // Keys eased in and out (smoothstep between neighbours); `t` past the last key holds it.
        static P Keys(float t, params (float t, P p)[] k)
        {
            if (t <= k[0].t) return k[0].p;
            for (int i = 0; i < k.Length - 1; i++)
                if (t < k[i + 1].t) { float u = (t - k[i].t) / (k[i + 1].t - k[i].t); return P.Lerp(k[i].p, k[i + 1].p, u * u * (3 - 2 * u)); }
            return k[^1].p;
        }
        static float S(float t, float period, float phase = 0) => Mathf.Sin((t / period + phase) * 2 * Mathf.PI);

        // The stance every rod clip shares: knees soft, a touch forward over the rod, chin level (the game aims the eyes:
        // CharAnim.LookAt; Manan 3 Nov: the head was keyed 8° down on top of the lean and he stared at the deck).
        static readonly P Stance = Pz(hy: -0.025f, pitch: 1, headPitch: -6); // -6: cancels Breathing Idle's own ~6° nod (look audit 3 Nov)
        // Braced against a fish: sat back on the heels, knees bent, chest back.
        static readonly P Brace = Pz(hy: -0.07f, hz: -0.05f, pitch: -9, headPitch: 6);

        const float HelmDrop = 0.5f, HelmBack = 0.26f;
        const float CastApex = Fishing.CastApex, CastWhip = Fishing.CastWhip, CastLen = Fishing.CastLen; // the rod follows these beats in Fishing

        public static void Make(GameObject worker, AnimationClip idle, M1Assets a)
        {
            if (worker == null || idle == null) { Debug.LogWarning("[M1Anim] no worker or idle clip: fishing clips not made"); return; }
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Game", "Anim");
            float L = idle.length; // loops last one idle cycle, so the breathing underneath loops too
            var rig = new Rig(worker, idle);
            try
            {
                // Rod in hand: weight drifting slowly from foot to foot, a breath, the odd glance.
                a.RodIdle = rig.Save("Fish_RodIdle", L, true, 10, t => Stance
                    + Pz(hx: 0.028f * S(t, L), roll: -2.2f * S(t, L), hipYaw: 2f * S(t, L, 0.25f), headYaw: 6f * S(t, L, 0.1f)));
                // Waiting for a bite: settled onto one hip, then the other; a slow sway with the boat; head drifting.
                a.Wait = rig.Save("Fish_Wait", L, true, 10, t => Stance
                    + Pz(hx: 0.045f * S(t, L), hy: -0.01f * Mathf.Abs(S(t, L)), roll: -3.5f * S(t, L), yaw: 3f * S(t, L, 0.3f), pitch: 0.8f * S(t, L * 0.5f),
                         headYaw: -5f * S(t, L, 0.15f), headPitch: 1.5f * S(t, L * 0.5f, 0.2f))); // a small sway: it never dips her to the deck
                // The cast: wind-up (weight to the back foot, chest coiled right and back, eyes on the target), held
                // while the power swings, then the whip (hips drive forward, chest uncoils past square, lean in),
                // follow-through low, and up into the wait.
                var windup = Pz(hx: 0.04f, hy: -0.045f, hz: -0.07f, hipYaw: 14, yaw: 26, pitch: -10, roll: 3, headYaw: -30, headPitch: 6);
                var whip = Pz(hx: -0.03f, hy: -0.05f, hz: 0.08f, hipYaw: -8, yaw: -14, pitch: 14, roll: -3, headYaw: 12, headPitch: 10);
                var follow = Pz(hx: -0.02f, hy: -0.04f, hz: 0.07f, hipYaw: -5, yaw: -8, pitch: 12, headYaw: 6, headPitch: -4); // eyes lift with the flying float
                a.Cast = rig.Save("Fish_Cast", CastLen, false, 30, t => Keys(t,
                    (0f, Stance), (CastApex, windup), (CastApex + 0.04f, windup), (CastWhip, whip), (0.85f, follow), (CastLen, Stance + Pz(hx: 0.0f))));
                // Strike: a sharp jerk back (0.12 s), then braced.
                a.Strike = rig.Save("Fish_Strike", 0.6f, false, 30, t => Keys(t,
                    (0f, Stance), (0.12f, Pz(hy: -0.08f, hz: -0.09f, hipYaw: 4, yaw: 6, pitch: -16, headPitch: -4)), (0.6f, Brace)));
                // The fight: braced, pumping the rod (lean back, ease forward), weight shifting with the fish.
                a.Fight = rig.Save("Fish_Fight", 1.2f, true, 15, t => Brace
                    + Pz(hx: 0.03f * S(t, 1.2f), hz: -0.02f * S(t, 0.6f), roll: -3f * S(t, 1.2f), pitch: 4f * S(t, 0.6f), yaw: 4f * S(t, 1.2f, 0.25f), headYaw: -4f * S(t, 1.2f, 0.3f)));
                // Reeling in: braced, a small bob with each turn of the crank (2 turns per loop, as the game cranks).
                a.Reel = rig.Save("Fish_Reel", 0.8f, true, 15, t => Brace * 0.8f
                    + Pz(hy: -0.012f * S(t, 0.4f), pitch: 2.5f * S(t, 0.4f, 0.25f), yaw: -3f * S(t, 0.8f), roll: 1.5f * S(t, 0.8f)));
                // The hold-up: stand tall and proud, chest out and back, looking at the fish, then a glance up.
                a.HoldUp = rig.Save("Fish_HoldUp", 2.4f, false, 15, t => Keys(t,
                    (0f, Brace * 0.6f), (0.35f, Pz(hy: 0.01f, hz: 0.02f, pitch: -8, roll: 2, headPitch: 18, headYaw: 4)),
                    (1.2f, Pz(hy: 0.01f, hz: 0.02f, pitch: -9, roll: 2, headPitch: 14, headYaw: 2)), (2.4f, Pz(hy: 0.005f, hz: 0.015f, pitch: -6, roll: 1, headPitch: 2, headYaw: -10))));
                // At the tiller: sat on Biscuit's stern bench (hips down and back over it, feet left on the deck, so the
                // knees come up), leaning a little forward, swaying with the boat, glancing about. The game moves the
                // body back onto the bench and puts the right hand on the tiller, the left on the thigh.
                a.Helm = rig.Save("Fish_Helm", L, true, 10, t => Pz(hy: -HelmDrop, hz: -HelmBack, pitch: 10, headPitch: -6)
                    + Pz(hx: 0.012f * S(t, L), roll: -2.5f * S(t, L), pitch: 1.5f * S(t, L * 0.5f), yaw: 4f * S(t, L, 0.2f), headYaw: 12f * S(t, L, 0.1f)));
            }
            finally { rig.Dispose(); }
            Verify(worker, a.RodIdle); Verify(worker, a.Cast); Verify(worker, a.Helm);
            AssetDatabase.SaveAssets();
            Debug.Log($"[M1Anim] clips made in {Dir}: idle base {idle.name} {L:F2} s");
        }

        // Plays a saved clip back on a fresh worker, the way the game will, and logs the knee / hips (self-check).
        static void Verify(GameObject worker, AnimationClip c)
        {
            var go = UnityEngine.Object.Instantiate(worker); go.hideFlags = HideFlags.HideAndDontSave;
            var an = go.GetComponentInChildren<Animator>();
            Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
            AnimationMode.StartAnimationMode();
            AnimationMode.SampleAnimationClip(an.gameObject, c, 0.2f);
            float knee = Vector3.Angle(B(HumanBodyBones.LeftUpperLeg).position - B(HumanBodyBones.LeftLowerLeg).position, B(HumanBodyBones.LeftFoot).position - B(HumanBodyBones.LeftLowerLeg).position);
            Debug.Log($"[M1Anim] verify {c.name}: knee {knee:F0}, hips {B(HumanBodyBones.Hips).position}, foot {B(HumanBodyBones.LeftFoot).position}, head {B(HumanBodyBones.Head).position}");
            AnimationMode.StopAnimationMode();
            UnityEngine.Object.DestroyImmediate(go);
        }

        // Poses the worker in the editor: Breathing Idle straight from its muscle curves (SetHumanPose, a full reset every
        // sample), then the offsets, then reads the humanoid pose back.
        class Rig : IDisposable
        {
            readonly GameObject go; readonly Animator an; readonly AnimationCurve[] idleMus; readonly AnimationCurve[] idleRoot;
            readonly HumanPoseHandler hph; readonly AnimationClip idle;
            readonly Transform hips, spine, chest, upper, neck, head, lu, ll, lf, ru, rl, rf;
            readonly string[] names;

            public Rig(GameObject worker, AnimationClip idleClip)
            {
                idle = idleClip;
                go = UnityEngine.Object.Instantiate(worker); go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                an = go.GetComponentInChildren<Animator>(); an.runtimeAnimatorController = null; an.applyRootMotion = false;
                hph = new HumanPoseHandler(an.avatar, an.transform);
                Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
                hips = B(HumanBodyBones.Hips); spine = B(HumanBodyBones.Spine); chest = B(HumanBodyBones.Chest); upper = B(HumanBodyBones.UpperChest);
                neck = B(HumanBodyBones.Neck); head = B(HumanBodyBones.Head);
                lu = B(HumanBodyBones.LeftUpperLeg); ll = B(HumanBodyBones.LeftLowerLeg); lf = B(HumanBodyBones.LeftFoot);
                ru = B(HumanBodyBones.RightUpperLeg); rl = B(HumanBodyBones.RightLowerLeg); rf = B(HumanBodyBones.RightFoot);
                names = new string[HumanTrait.MuscleCount];
                for (int i = 0; i < names.Length; i++) names[i] = ClipName(HumanTrait.MuscleName[i]);
                AnimationCurve C(string n) => AnimationUtility.GetEditorCurve(idle, EditorCurveBinding.FloatCurve("", typeof(Animator), n));
                idleMus = new AnimationCurve[names.Length]; for (int i = 0; i < names.Length; i++) idleMus[i] = C(names[i]);
                idleRoot = new AnimationCurve[7]; for (int i = 0; i < 7; i++) idleRoot[i] = C(RootNames[i]);
                int got = 0; foreach (var c in idleMus) if (c != null) got++;
                Debug.Log($"[M1Anim] idle curves: {got}/{names.Length} muscles, root {(idleRoot[1] != null ? "yes" : "NO")}");
                // self-check: the idle pose read back must be a sane human pose
                var h0 = hips.position; var hp0 = Pose(0, default); var h1 = hips.position; var hp1 = Pose(1.3f, default);
                float lo = 9, hi = -9; foreach (var m in hp1.muscles) { lo = Mathf.Min(lo, m); hi = Mathf.Max(hi, m); }
                var odd = new System.Text.StringBuilder();
                for (int i = 0; i < hp1.muscles.Length; i++) if (Mathf.Abs(hp1.muscles[i]) > 1.05f) odd.Append($"{names[i]}={hp1.muscles[i]:F2} (idle {(idleMus[i] != null ? idleMus[i].Evaluate(1.3f) : float.NaN):F2}); ");
                Debug.Log("[M1Anim] out of range: " + odd);
                float Knee() => Vector3.Angle(lu.position - ll.position, lf.position - ll.position);
                Pose(1.3f, default); float k0 = Knee(); var f0 = lf.position;
                var pp = Pose(1.3f, Stance); float k1 = Knee(); var f1 = lf.position;
                var q = new HumanPose(); hph.SetHumanPose(ref pp); hph.GetHumanPose(ref q); float k2 = Knee();
                Debug.Log($"[M1Anim] knee idle {k0:F0} stance {k1:F0} after roundtrip {k2:F0}");
                Calibrate();
                Debug.Log($"[M1Anim] check: hips rest {h0} posed {h1}, body {hp0.bodyPosition} / {hp1.bodyPosition} rot {hp1.bodyRotation.eulerAngles}, muscles {lo:F2}..{hi:F2}, humanScale {an.humanScale:F2}, avatar {an.avatar?.name} human {an.avatar?.isHuman}");
            }

            static bool UseSampler;
            // Foot IK goals (LeftFootT/Q, RightFootT/Q): the game plays every clip with foot IK, so ours must carry
            // goals too, or the feet are pulled to a zero goal and the knees fold. The goal is the foot in the body's
            // (RootT/RootQ) frame in human-scale units, plus a constant per-avatar offset calibrated on the idle.
            static readonly string[] GoalNames = { "LeftFootT.x", "LeftFootT.y", "LeftFootT.z", "LeftFootQ.x", "LeftFootQ.y", "LeftFootQ.z", "LeftFootQ.w",
                                                   "RightFootT.x", "RightFootT.y", "RightFootT.z", "RightFootQ.x", "RightFootQ.y", "RightFootQ.z", "RightFootQ.w" };
            AnimationCurve[] idleGoal; Vector3 offL, offR; Quaternion qL = Quaternion.identity, qR = Quaternion.identity;
            (Vector3, Quaternion) RawGoal(Transform foot, HumanPose hp)
            {
                var inv = Quaternion.Inverse(hp.bodyRotation); float hs = an.humanScale;
                var root = an.transform;
                var p = root.InverseTransformPoint(foot.position) / hs - hp.bodyPosition;
                return (inv * p, inv * (Quaternion.Inverse(root.rotation) * foot.rotation));
            }
            Vector3 IdleT(int k, float t) => new(idleGoal[k].Evaluate(t), idleGoal[k + 1].Evaluate(t), idleGoal[k + 2].Evaluate(t));
            Quaternion IdleQ(int k, float t) => new Quaternion(idleGoal[k + 3].Evaluate(t), idleGoal[k + 4].Evaluate(t), idleGoal[k + 5].Evaluate(t), idleGoal[k + 6].Evaluate(t)).normalized;
            void Calibrate()
            {
                idleGoal = new AnimationCurve[GoalNames.Length];
                for (int i = 0; i < GoalNames.Length; i++) idleGoal[i] = AnimationUtility.GetEditorCurve(idle, EditorCurveBinding.FloatCurve("", typeof(Animator), GoalNames[i]));
                if (Array.Exists(idleGoal, c => c == null)) { Debug.LogWarning("[M1Anim] idle has no foot goal curves"); idleGoal = null; return; }
                var hp = Pose(0.5f, default); var (pl, rl) = RawGoal(lf, hp); var (pr, rr) = RawGoal(rf, hp);
                offL = IdleT(0, 0.5f) - pl; offR = IdleT(7, 0.5f) - pr;
                qL = Quaternion.Inverse(rl) * IdleQ(0, 0.5f); qR = Quaternion.Inverse(rr) * IdleQ(7, 0.5f);
                // check at another time: the calibrated goals must match the idle's own
                hp = Pose(6.1f, default); var g = Goals(hp);
                float ep = Vector3.Distance(new Vector3(g[0], g[1], g[2]), IdleT(0, 6.1f)), eq = Quaternion.Angle(new Quaternion(g[3], g[4], g[5], g[6]), IdleQ(0, 6.1f));
                Debug.Log($"[M1Anim] foot goal calibration: offset L {offL} R {offR}; check at 6.1 s: {ep * 1000:F1} mm, {eq:F2} deg");
            }
            float[] Goals(HumanPose hp)
            {
                var (pl, rl) = RawGoal(lf, hp); var (pr, rr) = RawGoal(rf, hp);
                pl += offL; pr += offR; rl *= qL; rr *= qR;
                return new[] { pl.x, pl.y, pl.z, rl.x, rl.y, rl.z, rl.w, pr.x, pr.y, pr.z, rr.x, rr.y, rr.z, rr.w };
            }
            static readonly string[] RootNames = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };

            // "Left Thumb 1 Stretched" (HumanTrait) is "LeftHand.Thumb.1 Stretched" in a clip.
            static string ClipName(string m)
            {
                var w = m.Split(' ');
                if (w.Length >= 3 && (w[0] == "Left" || w[0] == "Right") && Array.IndexOf(new[] { "Thumb", "Index", "Middle", "Ring", "Little" }, w[1]) >= 0)
                    return $"{w[0]}Hand.{w[1]}.{string.Join(" ", w, 2, w.Length - 2)}";
                return m;
            }

            HumanPose Pose(float t, P o)
            {
                t = Mathf.Repeat(t, idle.length);
                float R(int i, float d) => idleRoot[i] != null ? idleRoot[i].Evaluate(t) : d;
                var basePose = new HumanPose
                {
                    bodyPosition = new Vector3(R(0, 0), R(1, 1), R(2, 0)),
                    bodyRotation = new Quaternion(R(3, 0), R(4, 0), R(5, 0), R(6, 1)).normalized,
                    muscles = new float[names.Length],
                };
                for (int i = 0; i < names.Length; i++) basePose.muscles[i] = idleMus[i] != null ? idleMus[i].Evaluate(t) : 0;
                if (UseSampler) AnimationMode.SampleAnimationClip(an.gameObject, idle, t); // the importer's settings + foot IK, as the game plays it
                else hph.SetHumanPose(ref basePose);
                Vector3 lfP = lf.position, rfP = rf.position; Quaternion lfR = lf.rotation, rfR = rf.rotation;
                // the character's frame from its feet (toes ahead of the ankles), whatever way the model is turned
                var lt = an.GetBoneTransform(HumanBodyBones.LeftToes); var rt = an.GetBoneTransform(HumanBodyBones.RightToes);
                var fwd = (lt != null && rt != null ? (lt.position - lfP) + (rt.position - rfP) : an.transform.forward); fwd.y = 0; fwd.Normalize();
                var up = Vector3.up; var right = Vector3.Cross(up, fwd);
                hips.position += right * o.Hip.x + up * o.Hip.y + fwd * o.Hip.z;
                hips.rotation = Quaternion.AngleAxis(o.HipYaw, up) * hips.rotation;
                // feet stay planted: knees bend out a little and forward
                Limb(lu, ll, lf, lfP, fwd - right * 0.15f); lf.rotation = lfR;
                Limb(ru, rl, rf, rfP, fwd + right * 0.15f); rf.rotation = rfR;
                var spines = upper != null ? new[] { (spine, 0.35f), (chest, 0.35f), (upper, 0.3f) } : new[] { (spine, 0.5f), (chest, 0.5f) };
                foreach (var (b, f) in spines)
                {
                    if (b == null) continue;
                    // axes turn with what's below, so lean / bend follow the twist
                    b.rotation = Quaternion.AngleAxis(o.Yaw * f, up) * b.rotation;
                    var r = Quaternion.AngleAxis(o.Yaw * f + o.HipYaw, up) * right; var fw = Quaternion.AngleAxis(o.Yaw * f + o.HipYaw, up) * fwd;
                    b.rotation = Quaternion.AngleAxis(o.Pitch * f, r) * Quaternion.AngleAxis(-o.Roll * f, fw) * b.rotation;
                }
                var hr = Quaternion.AngleAxis(o.Yaw + o.HipYaw + o.HeadYaw, up) * right;
                if (neck != null) neck.rotation = Quaternion.AngleAxis(o.HeadPitch * 0.4f, hr) * Quaternion.AngleAxis(o.HeadYaw * 0.4f, up) * neck.rotation;
                if (head != null) head.rotation = Quaternion.AngleAxis(o.HeadPitch * 0.6f, hr) * Quaternion.AngleAxis(o.HeadYaw * 0.6f, up) * head.rotation;
                var hp = new HumanPose(); hph.GetHumanPose(ref hp);
                return hp;
            }

            public AnimationClip Save(string name, float len, bool loop, int fps, Func<float, P> f)
            {
                int n = Mathf.Max(2, Mathf.RoundToInt(len * fps) + 1);
                int ng = idleGoal != null ? GoalNames.Length : 0;
                var curves = new AnimationCurve[names.Length + 7 + ng];
                for (int i = 0; i < curves.Length; i++) curves[i] = new AnimationCurve();
                for (int k = 0; k < n; k++)
                {
                    float t = len * k / (n - 1);
                    var hp = Pose(t, f(loop && k == n - 1 ? 0 : t)); // a loop's last key is its first (seamless)
                    for (int i = 0; i < names.Length; i++) curves[i].AddKey(t, hp.muscles[i]);
                    var bp = hp.bodyPosition; var br = hp.bodyRotation;
                    curves[names.Length + 0].AddKey(t, bp.x); curves[names.Length + 1].AddKey(t, bp.y); curves[names.Length + 2].AddKey(t, bp.z);
                    curves[names.Length + 3].AddKey(t, br.x); curves[names.Length + 4].AddKey(t, br.y); curves[names.Length + 5].AddKey(t, br.z); curves[names.Length + 6].AddKey(t, br.w);
                    if (ng > 0) { var g = Goals(hp); for (int i = 0; i < ng; i++) curves[names.Length + 7 + i].AddKey(t, g[i]); }
                }
                var clip = new AnimationClip { name = name, frameRate = 30 };
                var root = RootNames;
                for (int i = 0; i < curves.Length; i++)
                {
                    var c = curves[i];
                    for (int k = 0; k < c.length; k++) c.SmoothTangents(k, 0);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), i < names.Length ? names[i] : i < names.Length + 7 ? root[i - names.Length] : GoalNames[i - names.Length - 7]), c);
                }
                // the idle's own settings (its facing turn, root locks), so ours face and stand exactly as it does;
                // the body's sideways shifts stay in the pose (baked), the root never moves
                var s = AnimationUtility.GetAnimationClipSettings(idle);
                s.loopTime = loop; s.startTime = 0; s.stopTime = len; s.loopBlendPositionXZ = true; s.keepOriginalPositionXZ = true;
                AnimationUtility.SetAnimationClipSettings(clip, s);
                string path = $"{Dir}/{name}.anim";
                AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(clip, path);
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }

            static void Limb(Transform a, Transform b, Transform c, Vector3 target, Vector3 hint)
            {
                Vector3 ap = a.position;
                float la = Vector3.Distance(ap, b.position), lb = Vector3.Distance(b.position, c.position);
                var to = target - ap;
                float d = Mathf.Clamp(to.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
                var dir = to.normalized;
                var bend = Vector3.ProjectOnPlane(hint, dir); if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(b.position - ap, dir); bend.Normalize();
                float cos = Mathf.Clamp((la * la + d * d - lb * lb) / (2 * la * d), -1f, 1f);
                var knee = ap + dir * (cos * la) + bend * (Mathf.Sqrt(1 - cos * cos) * la);
                a.rotation = Quaternion.FromToRotation(b.position - ap, knee - ap) * a.rotation;
                b.rotation = Quaternion.FromToRotation(c.position - b.position, ap + dir * d - b.position) * b.rotation;
            }

            public void Dispose() { if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode(); hph.Dispose(); UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
