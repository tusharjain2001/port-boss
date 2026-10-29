using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PortBoss
{
    // Drives a humanoid with the Playables API (no Animator Controller asset): a foot-synced locomotion blend
    // (idle > walk > jog > sprint by ground speed), one clip per other state, then IK on top for the hands on what
    // they hold (a crate from underneath, the trolley's bar, ladder rungs, under the water) and the feet planted when
    // squatting or on a ladder. Bones come from the Humanoid avatar, so any humanoid works (the pack's workers, our
    // own dock worker) with any humanoid clips (Quaternius UAL1 now, Mixamo later).
    [DefaultExecutionOrder(1000)] // LateUpdate after the crates and the trolley have posed themselves this frame
    public class CharAnim : MonoBehaviour
    {
        // On foot (Idle, or any gait) the gaits blend by speed; Land plays by itself after a fall; Climb is the body on a
        // ladder (the stand-in is Idle, with the limbs put on the rungs).
        public enum Slot { Idle, Walk, Run, Jump, Swim, SwimIdle, Drive, Push, Sprint, Jog, Land, Climb }

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationLayerMixerPlayable layers; // [0] the body, [1] the arms holding a box (upper body only, when there's a clip)
        AnimationClipPlayable[] plays;
        float[] w;
        Animator animator;
        Transform hips, spine, head, armLS, armRS, armLU, armLL, armLH, armRU, armRL, armRH, legLU, legLL, legLF, legRU, legRL, legRF, fingerL, fingerR;
        public float Scale = 1f;      // the model's scale (only for the fallback gait speeds)
        public Transform Facing;      // the player (the model itself is turned to face the pack's way)
        public float Throw01 = -1;
        public bool Flail;            // out of stamina in the water
        public float Laugh;           // 0..1: Marlowe gloating: doubled up laughing, pointing at PointAt
        public Vector3 PointAt;

        // Set by the player each frame (null / NaN = off); every weight eases in and out here.
        public Transform Box;         // crate (its visual) to hold from underneath: hands on its sides, near the bottom
        public float BoxHalf, BoxW;   // its half size; 0..1 how much the hands hold it (fades out after letting go)
        public Transform Grip;        // something to hold with both hands at GripL / GripR (local to it): the trolley bar
        public Vector3 GripL, GripR;
        public World.Ladder? Ladder;  // on this ladder: hands and feet on the rungs, from how high the player is
        public float LadderW;
        public float WaterY = float.NaN; // treading water at this surface height: arms down in the water, sculling

        void LateUpdate() { if (armLU != null && armRU != null) PostPose(); }

        public void Init(Animator a, Skin k)
        {
            animator = a;
            a.runtimeAnimatorController = null; a.applyRootMotion = false; a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var clips = new[] { k.Idle, k.Walk, k.Run, k.Jump, k.Swim, k.SwimIdle, k.Drive, k.Push, k.Sprint, k.Jog, k.Land, k.Climb };
            // Missing: the gaits, landing and climbing just aren't used; the rest stand in with something close.
            if (clips[(int)Slot.SwimIdle] == null) clips[(int)Slot.SwimIdle] = clips[(int)Slot.Swim];
            for (int i = 0; i < clips.Length; i++) if (clips[i] == null && !IsGait((Slot)i) && i != (int)Slot.Land && i != (int)Slot.Climb) clips[i] = clips[0];
            graph = PlayableGraph.Create("char");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length);
            layers = AnimationLayerMixerPlayable.Create(graph, 2);
            graph.Connect(mixer, 0, layers, 0); layers.SetInputWeight(0, 1);
            if (k.Carry != null)
            {
                var arms = new AvatarMask();
                for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++) arms.SetHumanoidBodyPartActive(part, false);
                foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                    arms.SetHumanoidBodyPartActive(part, true);
                graph.Connect(AnimationClipPlayable.Create(graph, k.Carry), 0, layers, 1);
                layers.SetLayerMaskFromAvatarMask(1, arms);
            }
            AnimationPlayableOutput.Create(graph, "anim", a).SetSourcePlayable(layers);
            plays = new AnimationClipPlayable[clips.Length];
            w = new float[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null) continue;
                plays[i] = AnimationClipPlayable.Create(graph, clips[i]);
                graph.Connect(plays[i], 0, mixer, i);
            }
            w[0] = 1; mixer.SetInputWeight(0, 1);
            graph.Play();
            Transform B(HumanBodyBones b) => a.GetBoneTransform(b);
            hips = B(HumanBodyBones.Hips); spine = B(HumanBodyBones.Spine); head = B(HumanBodyBones.Head);
            armLS = B(HumanBodyBones.LeftShoulder); armRS = B(HumanBodyBones.RightShoulder);
            armLU = B(HumanBodyBones.LeftUpperArm); armLL = B(HumanBodyBones.LeftLowerArm); armLH = B(HumanBodyBones.LeftHand);
            armRU = B(HumanBodyBones.RightUpperArm); armRL = B(HumanBodyBones.RightLowerArm); armRH = B(HumanBodyBones.RightHand);
            legLU = B(HumanBodyBones.LeftUpperLeg); legLL = B(HumanBodyBones.LeftLowerLeg); legLF = B(HumanBodyBones.LeftFoot);
            legRU = B(HumanBodyBones.RightUpperLeg); legRL = B(HumanBodyBones.RightLowerLeg); legRF = B(HumanBodyBones.RightFoot);
            fingerL = B(HumanBodyBones.LeftMiddleProximal); fingerR = B(HumanBodyBones.RightMiddleProximal);
            if (armLU != null) shoulderY = armLU.position.y - a.transform.position.y; // in the bind pose, standing
        }

        // ---------- locomotion ----------

        // Every gait clip shares one stride phase (0..1 from the left heel strike), so walk, jog, run and sprint blend at
        // any speed without the legs scissoring, and the phase advances by ground speed over the blended stride length,
        // so a planted foot stays put. Speeds and heel strikes are measured from the clips on this rig (MeasureGaits).
        readonly List<Slot> gaits = new();   // the gaits this skin has, slowest first
        readonly List<float> gaitSpeed = new(), gaitStrike = new();
        bool measuredGaits;
        float phase, speedS, airT, landT = -1;
        Slot last;

        public void Tick(Slot want, float speed)
        {
            if (!graph.IsValid()) return;
            if (!measuredGaits) MeasureGaits();
            float dt = Time.deltaTime;
            speedS = Mathf.Lerp(speedS, speed, 1 - Mathf.Exp(-12f * dt)); // speed from position deltas is noisy (remote peers more so)
            bool onFoot = want == Slot.Idle || IsGait(want);
            if (!onFoot && !plays[(int)want].IsValid()) { want = Slot.Idle; onFoot = true; } // e.g. no climbing clip
            // Landing after a real fall (not a hop or a step down): the landing clip, cut short if we run straight on.
            airT = want == Slot.Jump ? airT + dt : onFoot ? airT : 0;
            if (onFoot && airT > 0.35f && plays[(int)Slot.Land].IsValid()) { landT = 0; plays[(int)Slot.Land].SetTime(0); }
            if (onFoot) airT = 0;
            float land = 0;
            if (landT >= 0)
            {
                float len = Mathf.Min(Len(Slot.Land), 0.6f);
                landT += dt;
                land = (1 - Mathf.Clamp01(speedS / 3.5f)) * Mathf.Clamp01((len - landT) / (len * 0.4f));
                if (!onFoot || landT >= len) { landT = -1; land = 0; }
            }
            var target = new float[w.Length];
            if (onFoot) GaitWeights(speedS, target); else target[(int)want] = 1;
            if (land > 0) { for (int i = 0; i < target.Length; i++) target[i] *= 1 - land; target[(int)Slot.Land] = land; }
            // Into the water: no lingering in the air pose, straight to treading.
            bool wet = want == Slot.Swim || want == Slot.SwimIdle;
            float k = 1 - Mathf.Exp(-(wet && last != Slot.Swim && last != Slot.SwimIdle ? 22f : land > 0 ? 25f : 10f) * dt);
            last = want;
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = Mathf.Lerp(w[i], target[i], k);
                if (plays[i].IsValid()) mixer.SetInputWeight(i, w[i]);
            }
            // Advance the shared stride over the blended stride length (m per cycle) of the gaits now playing.
            float stride = 0, sw = 0;
            for (int g = 0; g < gaits.Count; g++) { float gw = w[(int)gaits[g]]; stride += gw * gaitSpeed[g] * Len(gaits[g]); sw += gw; }
            if (sw > 0.001f) phase = Mathf.Repeat(phase + dt * speedS / Mathf.Max(0.05f, stride / sw), 1f);
            for (int g = 0; g < gaits.Count; g++)
            {
                var p = plays[(int)gaits[g]];
                p.SetSpeed(0); p.SetTime(Mathf.Repeat(phase + gaitStrike[g], 1f) * Len(gaits[g]));
            }
            Rate(Slot.Push, speedS / 1.5f, 0.4f, 1.8f);
            Rate(Slot.Swim, speedS / 2f, 0.7f, 1.5f);
            // Climbing clip: one cycle per two rungs climbed (up or down), whatever the speed.
            if (Ladder != null && plays[(int)Slot.Climb].IsValid())
            {
                var p = plays[(int)Slot.Climb];
                p.SetSpeed(0); p.SetTime(Mathf.Repeat((transform.position.y - Ladder.Value.FirstRung) / (2 * World.Ladder.RungStep), 1f) * Len(Slot.Climb));
            }
            if (layers.GetInputCount() > 1 && layers.GetInput(1).IsValid()) layers.SetInputWeight(1, boxW);
        }

        // Target weights for the on-foot clips at this speed: idle into the slowest gait, then each gait into the next,
        // crossing over between their own speeds (a gait only ever plays near the speed it was made for).
        void GaitWeights(float v, float[] t)
        {
            if (gaits.Count == 0) { t[(int)Slot.Idle] = 1; return; }
            if (v < gaitSpeed[0])
            {
                float m = Mathf.SmoothStep(0, 1, (v - 0.12f) / (0.45f * gaitSpeed[0]));
                t[(int)Slot.Idle] = 1 - m; t[(int)gaits[0]] = m;
                return;
            }
            for (int g = 0; g < gaits.Count - 1; g++)
                if (v < gaitSpeed[g + 1])
                {
                    float m = (v - gaitSpeed[g]) / (gaitSpeed[g + 1] - gaitSpeed[g]);
                    t[(int)gaits[g]] = 1 - m; t[(int)gaits[g + 1]] = m;
                    return;
                }
            t[(int)gaits[gaits.Count - 1]] = 1;
        }

        float Len(Slot s) => plays[(int)s].IsValid() ? Mathf.Max(0.1f, plays[(int)s].GetAnimationClip().length) : 1f;
        static bool IsGait(Slot s) => s == Slot.Walk || s == Slot.Jog || s == Slot.Run || s == Slot.Sprint;
        void Rate(Slot s, float r, float lo, float hi) { if (plays[(int)s].IsValid()) plays[(int)s].SetSpeed(Mathf.Clamp(r, lo, hi)); }

        // Ground speed and left heel strike of each gait clip on this rig, sampled once per (clip, avatar): while a foot
        // is planted it slides back under the hips at exactly the ground speed the clip was made for. Gaits that measure
        // no faster than the one below (the same clip twice, say) are dropped.
        static readonly Dictionary<(AnimationClip, Avatar), (float speed, float strike)> measured = new();
        void MeasureGaits()
        {
            measuredGaits = true;
            var saved = (float[])w.Clone();
            float scale = animator.transform.lossyScale.y;
            var found = new List<(Slot slot, float speed, float strike)>();
            foreach (var (slot, fallback) in new[] { (Slot.Walk, 1.1f), (Slot.Jog, 3.4f), (Slot.Run, 3.8f), (Slot.Sprint, 6.2f) })
            {
                var p = plays[(int)slot];
                if (!p.IsValid()) continue;
                var key = (p.GetAnimationClip(), animator.avatar);
                if (!measured.TryGetValue(key, out var m) && legLF != null)
                {
                    for (int i = 0; i < w.Length; i++) if (plays[i].IsValid()) mixer.SetInputWeight(i, i == (int)slot ? 1 : 0);
                    measured[key] = m = Sample(p);
                    Debug.Log($"[PB] gait {slot} = {p.GetAnimationClip().name}: {m.speed * scale:F2} m/s, left heel strike at {m.strike:F2}");
                }
                found.Add((slot, m.speed > 0.2f ? m.speed * scale : fallback * Scale, m.strike));
            }
            if (legLF != null && armLU != null) Debug.Log($"[PB] rig {animator.avatar?.name}: arm {ArmLen:F2} m, leg {LegLen:F2} m, shoulder {ShoulderY:F2} m");
            found.Sort((a, b) => a.speed.CompareTo(b.speed));
            foreach (var f in found)
            {
                if (gaitSpeed.Count > 0 && f.speed < gaitSpeed[gaitSpeed.Count - 1] * 1.15f) continue;
                gaits.Add(f.slot); gaitSpeed.Add(f.speed); gaitStrike.Add(f.strike);
            }
            for (int i = 0; i < w.Length; i++) if (plays[i].IsValid()) mixer.SetInputWeight(i, saved[i]);
        }

        (float speed, float strike) Sample(AnimationClipPlayable p)
        {
            const int N = 48;
            float len = p.GetAnimationClip().length;
            var root = animator.transform;
            var lf = new Vector3[N]; var rf = new Vector3[N];
            for (int s = 0; s < N; s++)
            {
                p.SetTime(len * s / N);
                graph.Evaluate(0);
                lf[s] = root.InverseTransformPoint(legLF.position); rf[s] = root.InverseTransformPoint(legRF.position);
            }
            Vector3 slide = Vector3.zero; int n = 0;
            foreach (var f in new[] { lf, rf })
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var q in f) { lo = Mathf.Min(lo, q.y); hi = Mathf.Max(hi, q.y); }
                for (int s = 0; s < N; s++)
                {
                    var a = f[s]; var b = f[(s + 1) % N];
                    if (a.y < lo + 0.15f * (hi - lo) && b.y < lo + 0.15f * (hi - lo)) { var d = b - a; d.y = 0; slide += d; n++; }
                }
            }
            if (n == 0) return (0, 0);
            slide /= n;
            float speed = slide.magnitude / (len / N);
            var fwd = -slide.normalized; // planted feet slide backwards
            int strike = 0; float best = float.MinValue;
            for (int s = 0; s < N; s++) { float f = Vector3.Dot(lf[s], fwd); if (f > best) { best = f; strike = s; } }
            p.SetTime(0);
            return (speed, strike / (float)N);
        }

        // ---------- after the clips: IK and overrides ----------

        float boxW, gripW, ladderW, waterW, crouch, bend, lastWaterY;
        World.Ladder lastLadder; // kept while the weights ease out after Ladder / WaterY are switched off

        void PostPose()
        {
            var t = Facing != null ? Facing : transform;
            float dt = Time.deltaTime, ease = 1 - Mathf.Exp(-14f * dt);
            boxW = Mathf.Lerp(boxW, Box != null ? BoxW : 0f, ease);
            gripW = Mathf.Lerp(gripW, Grip != null ? 1f : 0f, ease);
            ladderW = Mathf.Lerp(ladderW, Ladder != null ? LadderW : 0f, 1 - Mathf.Exp(-20f * dt));
            waterW = Mathf.Lerp(waterW, float.IsNaN(WaterY) ? 0f : 1f, 1 - Mathf.Exp(-20f * dt));
            if (Ladder != null) lastLadder = Ladder.Value;
            if (!float.IsNaN(WaterY)) lastWaterY = WaterY;

            if (Flail)
            {
                float f = Mathf.Sin(Time.time * 14f);
                Aim(armLU, armLL, t.up - t.right * 0.6f + t.forward * 0.35f * f);
                Aim(armLL, armLH, t.up - t.right * 0.3f * f);
                Aim(armRU, armRL, t.up + t.right * 0.6f - t.forward * 0.35f * f);
                Aim(armRL, armRH, t.up + t.right * 0.3f * f);
                return;
            }

            // Hands holding a crate: where the grips are decides how far to squat and bend (a crate on the ground
            // means down on the haunches, back bent; one on the stack is just held).
            Vector3 boxL = default, boxR = default;
            float wantCrouch = 0, wantBend = 0;
            if (Box != null && boxW > 0.01f)
            {
                BoxGrips(t, out boxL, out boxR);
                float handY = Mathf.Min(boxL.y, boxR.y) - t.position.y, lowest = ShoulderY - ArmLen * 0.9f;
                float need = Mathf.Max(0, lowest - handY) * boxW;
                wantCrouch = Mathf.Min(need, LegLen * 0.42f);
                wantBend = Mathf.Clamp((need - wantCrouch) / (ShoulderY * 0.45f), 0, 0.8f) * Mathf.Rad2Deg * 0.9f;
            }
            crouch = Mathf.Lerp(crouch, wantCrouch, ease); bend = Mathf.Lerp(bend, wantBend, ease);
            if (crouch > 0.005f) Squat(t, crouch);

            if (ladderW > 0.01f) LadderLimbs(t, lastLadder, ladderW);
            if (spine != null)
            {
                if (bend > 0.1f) spine.rotation = Quaternion.AngleAxis(bend, t.right) * spine.rotation;
                // Lean back against a load held high.
                if (Box != null && boxW > 0.01f && wantCrouch < 0.01f) spine.rotation = Quaternion.AngleAxis(-6f * boxW, t.right) * spine.rotation;
            }
            if (boxW > 0.01f && Box != null)
            {
                Hands(t, boxL, boxR, boxW, t.forward * 0.25f - t.up * 0.55f, Box.up * 0.4f + t.forward);
            }
            if (gripW > 0.01f && Grip != null)
                Hands(t, Grip.TransformPoint(GripL), Grip.TransformPoint(GripR), gripW, -t.up * 0.7f - t.forward * 0.1f, t.forward - t.up * 0.2f);
            if (waterW > 0.01f && armLU != null)
            {
                // Treading water: hands low out in front, sculling in small circles, always under the surface.
                float s = Time.time * 5f, below = lastWaterY - 0.22f;
                Vector3 Scull(Transform shoulder, float side, float ph)
                {
                    var p = shoulder.position + t.forward * 0.32f + t.right * side * 0.22f - t.up * 0.3f
                        + (t.right * side * Mathf.Cos(s + ph) + t.forward * Mathf.Sin(s + ph)) * 0.09f;
                    p.y = Mathf.Min(p.y, below);
                    return p;
                }
                Hands(t, Scull(armLU, -1, 0), Scull(armRU, 1, Mathf.PI), waterW, -t.up * 0.4f - t.forward * 0.2f, t.forward);
            }

            if (Throw01 >= 0)
            {
                float a = Throw01 < 0.35f ? Mathf.Lerp(-30f, 200f, Throw01 / 0.35f) : Mathf.Lerp(200f, 60f, (Throw01 - 0.35f) / 0.65f);
                var dir = Quaternion.AngleAxis(-a, t.right) * t.forward; // swings up over the head and down in front
                Aim(armRU, armRL, dir + t.right * 0.2f);
                Aim(armRL, armRH, dir);
            }
            if (Laugh > 0.01f)
            {
                // Belly laugh: rocking at the waist, one hand on the belly, the other pointing (finger wagging) at you.
                if (spine != null) spine.rotation = Quaternion.AngleAxis((Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 14f - 4f) * Laugh, t.right) * spine.rotation;
                var at = (PointAt - armRU.position).normalized;
                var wag = Quaternion.AngleAxis(Mathf.Sin(Time.time * 11f) * 8f, t.up);
                Aim(armRU, armRL, wag * at, Laugh);
                Aim(armRL, armRH, wag * at, Laugh);
                Aim(armLU, armLL, -t.up * 0.7f + t.forward * 0.4f - t.right * 0.3f, Laugh);
                Aim(armLL, armLH, t.right * 0.8f + t.forward * 0.3f, Laugh);
            }
        }

        float ArmLen => Dist(armLU, armLL) + Dist(armLL, armLH);
        float LegLen => legLU != null ? Dist(legLU, legLL) + Dist(legLL, legLF) : 0.9f;
        float ShoulderY => shoulderY > 0.3f ? shoulderY : 1.4f;
        float shoulderY;
        static float Dist(Transform a, Transform b) => Vector3.Distance(a.position, b.position);

        // Grip points on a crate: on the pair of sides facing our left and right, a little in from its back edge (the
        // side nearer us) and just above its bottom, so the fingers curl under it.
        void BoxGrips(Transform t, out Vector3 l, out Vector3 r)
        {
            var b = Box;
            var ax = Mathf.Abs(Vector3.Dot(b.right, t.right)) >= Mathf.Abs(Vector3.Dot(b.forward, t.right)) ? b.right : b.forward;
            if (Vector3.Dot(ax, t.right) < 0) ax = -ax;
            var back = Vector3.ProjectOnPlane(t.forward, b.up).normalized;
            var c = b.position - b.up * (BoxHalf - 0.07f) - back * (BoxHalf * 0.3f);
            l = c - ax * (BoxHalf + 0.035f); r = c + ax * (BoxHalf + 0.035f);
        }

        // Lower the hips by d with both feet left where they were: the knees bend (a squat, not sinking into the ground).
        void Squat(Transform t, float d)
        {
            if (hips == null || legLF == null || legRF == null) return;
            Vector3 lf = legLF.position, rf = legRF.position; Quaternion lr = legLF.rotation, rr = legRF.rotation;
            hips.position -= t.up * d;
            Limb(legLU, legLL, legLF, lf, t.forward * 0.6f - t.right * 0.15f, 1f); legLF.rotation = lr;
            Limb(legRU, legRL, legRF, rf, t.forward * 0.6f + t.right * 0.15f, 1f); legRF.rotation = rr;
        }

        // On a ladder: hands on the rungs, feet on the rungs, moving up two rungs at a time in diagonal pairs (left hand
        // with right foot, then right hand with left foot), all driven by how high the body is, so it matches any climb
        // speed, up or down, on every peer.
        void LadderLimbs(Transform t, World.Ladder l, float wgt)
        {
            float u = (t.position.y - l.FirstRung) / World.Ladder.RungStep;
            int handOff = Mathf.CeilToInt((ShoulderY + 0.3f) / World.Ladder.RungStep);
            var toUs = -l.In;
            Vector3 Rung(float side, int limbPhase, int above, float outward)
            {
                float s = (u - limbPhase) / 2f, k = Mathf.Floor(s), f = s - k;
                float m = Mathf.SmoothStep(0, 1, Mathf.Clamp01((f - 0.55f) / 0.45f));
                float rung = 2 * k + limbPhase + above + 2 * m;
                float y = Mathf.Clamp(l.FirstRung + rung * World.Ladder.RungStep, l.FirstRung, l.TopY);
                return new Vector3(l.Edge.x, y, l.Edge.z) + t.right * side * 0.15f + toUs * (outward + Mathf.Sin(m * Mathf.PI) * 0.12f);
            }
            if (legLF != null)
            {
                Limb(legLU, legLL, legLF, Rung(-1, 1, 1, 0.1f), t.forward + t.up * 0.3f - t.right * 0.2f, wgt);
                Limb(legRU, legRL, legRF, Rung(1, 0, 1, 0.1f), t.forward + t.up * 0.3f + t.right * 0.2f, wgt);
            }
            Hands(t, Rung(-1, 0, handOff, 0.05f), Rung(1, 1, handOff, 0.05f), wgt, -t.up * 0.8f, t.up);
        }

        // Both hands to these points, elbows bent towards `elbow` (from the shoulder) and out to the side, turned so the
        // fingers point along `fingers`.
        void Hands(Transform t, Vector3 l, Vector3 r, float wgt, Vector3 elbow, Vector3 fingers)
        {
            ReachShoulder(armLS, armLU, l, wgt); ReachShoulder(armRS, armRU, r, wgt);
            Limb(armLU, armLL, armLH, l, elbow - t.right * 0.35f, wgt);
            Limb(armRU, armRL, armRH, r, elbow + t.right * 0.35f, wgt);
            Point(armLH, fingerL, fingers, wgt); Point(armRH, fingerR, fingers, wgt);
        }

        // A target out of reach pulls the collarbone a little towards it, as people do.
        void ReachShoulder(Transform clav, Transform upper, Vector3 target, float wgt)
        {
            if (clav == null) return;
            float over = Vector3.Distance(upper.position, target) - ArmLen * 0.97f;
            if (over <= 0) return;
            var to = Quaternion.FromToRotation(upper.position - clav.position, target - clav.position);
            clav.rotation = Quaternion.Slerp(Quaternion.identity, to, Mathf.Clamp01(over / 0.25f) * 0.35f * wgt) * clav.rotation;
        }

        static void Point(Transform hand, Transform finger, Vector3 dir, float wgt)
        {
            if (hand == null || finger == null) return;
            hand.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(finger.position - hand.position, dir), wgt * 0.8f) * hand.rotation;
        }

        // Two-bone IK: turn a (shoulder / hip) and b (elbow / knee) so c (hand / foot) lands on the target, bending
        // towards hint (a direction from a). weight < 1 lands part of the way.
        static void Limb(Transform a, Transform b, Transform c, Vector3 target, Vector3 hint, float weight)
        {
            if (a == null || b == null || c == null || weight <= 0.001f) return;
            target = Vector3.Lerp(c.position, target, weight);
            Vector3 ap = a.position;
            float la = Vector3.Distance(ap, b.position), lb = Vector3.Distance(b.position, c.position);
            var to = target - ap;
            float d = Mathf.Clamp(to.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
            var dir = to.normalized;
            var bendDir = Vector3.ProjectOnPlane(hint, dir);
            if (bendDir.sqrMagnitude < 1e-6f) bendDir = Vector3.ProjectOnPlane(b.position - ap, dir);
            bendDir.Normalize();
            float cos = Mathf.Clamp((la * la + d * d - lb * lb) / (2 * la * d), -1f, 1f);
            var knee = ap + dir * (cos * la) + bendDir * (Mathf.Sqrt(1 - cos * cos) * la);
            a.rotation = Quaternion.FromToRotation(b.position - ap, knee - ap) * a.rotation;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, ap + dir * d - b.position) * b.rotation;
        }

        // Rotate a bone so it points from itself towards its child along dir (world space), whatever its local axes;
        // weight < 1 turns it only part of the way there.
        static void Aim(Transform bone, Transform child, Vector3 dir, float weight = 1f)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position;
            bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, dir.normalized), weight) * bone.rotation;
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
