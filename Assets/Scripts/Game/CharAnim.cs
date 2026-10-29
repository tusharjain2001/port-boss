using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PortBoss
{
    // Blends a humanoid's locomotion clips with the Playables API (no Animator Controller asset), and bends the
    // arms on top for carrying and throwing. Clips come from the skin (CC0 UAL1, retargeted onto Toon Harbor workers).
    public class CharAnim : MonoBehaviour
    {
        public enum Slot { Idle, Walk, Run, Jump, Swim, SwimIdle, Drive, Push, Sprint }

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] plays;
        float[] w;
        Animator animator;
        Transform armLU, armLL, armLH, armRU, armRL, armRH, spine;
        Slot last;
        public float Scale = 1f;      // the model's scale: a bigger worker takes longer strides
        public bool Carry;            // set by Player each frame
        public Transform Facing;      // the player (the model itself is turned to face the pack's way)
        public float Throw01 = -1;
        public bool Flail;            // just fell in the water
        public float Reach;           // 0..1: arms down to a crate (picking up / putting down)

        void LateUpdate() => Arms(Carry, Throw01);

        public void Init(Animator a, Skin k)
        {
            animator = a;
            a.runtimeAnimatorController = null; a.applyRootMotion = false; a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var clips = new[] { k.Idle, k.Walk, k.Run, k.Jump, k.Swim, k.SwimIdle, k.Drive, k.Push, k.Sprint };
            for (int i = 0; i < clips.Length; i++) if (clips[i] == null) clips[i] = clips[i == (int)Slot.Run || i == (int)Slot.Sprint ? (int)Slot.Walk : i == (int)Slot.SwimIdle ? (int)Slot.Swim : 0];
            graph = PlayableGraph.Create("char");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput.Create(graph, "anim", a).SetSourcePlayable(mixer = AnimationMixerPlayable.Create(graph, clips.Length));
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
            armLU = a.GetBoneTransform(HumanBodyBones.LeftUpperArm); armLL = a.GetBoneTransform(HumanBodyBones.LeftLowerArm); armLH = a.GetBoneTransform(HumanBodyBones.LeftHand);
            armRU = a.GetBoneTransform(HumanBodyBones.RightUpperArm); armRL = a.GetBoneTransform(HumanBodyBones.RightLowerArm); armRH = a.GetBoneTransform(HumanBodyBones.RightHand);
            spine = a.GetBoneTransform(HumanBodyBones.Spine);
        }

        // Ground speeds the UAL1 clips are authored at (measured from the planted foot): playing a clip at any other
        // speed makes the feet slide, so each gait only covers the speeds near its own and plays at the matching rate.
        const float WalkSpeed = 1.1f, JogSpeed = 3.8f, SprintSpeed = 6.2f;
        public static Slot Gait(float speed) => speed < 0.3f ? Slot.Idle : speed < 2.2f ? Slot.Walk : speed < 5.9f ? Slot.Run : Slot.Sprint;

        public void Tick(Slot want, float speed)
        {
            if (!graph.IsValid()) return;
            // Changing gait: start the new clip at the same point of the stride, so the feet don't skip.
            if (want != last && IsGait(want) && IsGait(last) && plays[(int)last].IsValid() && plays[(int)want].IsValid())
            {
                var from = plays[(int)last]; var to = plays[(int)want];
                float phase = Mathf.Repeat((float)from.GetTime() / from.GetAnimationClip().length, 1f);
                to.SetTime(phase * to.GetAnimationClip().length);
            }
            last = want;
            float k = 1 - Mathf.Exp(-10f * Time.deltaTime);
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = Mathf.Lerp(w[i], i == (int)want ? 1f : 0f, k);
                if (plays[i].IsValid()) mixer.SetInputWeight(i, w[i]);
            }
            Rate(Slot.Walk, speed / (WalkSpeed * Scale), 0.6f, 1.9f);
            Rate(Slot.Run, speed / (JogSpeed * Scale), 0.7f, 1.5f);
            Rate(Slot.Sprint, speed / (SprintSpeed * Scale), 0.85f, 1.4f);
            Rate(Slot.Push, speed / 1.5f, 0.5f, 1.8f);
        }

        static bool IsGait(Slot s) => s == Slot.Walk || s == Slot.Run || s == Slot.Sprint;
        void Rate(Slot s, float r, float lo, float hi) { if (plays[(int)s].IsValid()) plays[(int)s].SetSpeed(Mathf.Clamp(r, lo, hi)); }

        // Called after the clips have posed the body: carry = both forearms out front under the stack,
        // throw01 >= 0 = right arm mid-throw (0 wound back over the shoulder, 1 followed through).
        public void Arms(bool carry, float throw01)
        {
            if (armLU == null || armRU == null) return;
            var t = Facing != null ? Facing : transform;
            if (Flail)
            {
                float w = Mathf.Sin(Time.time * 14f);
                Aim(armLU, armLL, t.up - t.right * 0.6f + t.forward * 0.35f * w);
                Aim(armLL, armLH, t.up - t.right * 0.3f * w);
                Aim(armRU, armRL, t.up + t.right * 0.6f - t.forward * 0.35f * w);
                Aim(armRL, armRH, t.up + t.right * 0.3f * w);
                return;
            }
            if (carry)
            {
                // Lean back against the load, elbows bent, hands gripping the bottom crate low on its sides
                // (it rides at the belly, so the head stays above a single crate).
                if (spine != null) spine.rotation = Quaternion.AngleAxis(-7f, t.right) * spine.rotation;
                Aim(armLU, armLL, t.forward * 0.42f - t.up * 0.62f - t.right * 0.2f);
                Aim(armLL, armLH, t.forward * 0.95f - t.up * 0.08f + t.right * 0.04f);
                Aim(armRU, armRL, t.forward * 0.42f - t.up * 0.62f + t.right * 0.2f);
                Aim(armRL, armRH, t.forward * 0.95f - t.up * 0.08f - t.right * 0.04f);
            }
            if (throw01 >= 0)
            {
                float a = throw01 < 0.35f ? Mathf.Lerp(-30f, 200f, throw01 / 0.35f) : Mathf.Lerp(200f, 60f, (throw01 - 0.35f) / 0.65f);
                var dir = Quaternion.AngleAxis(-a, t.right) * t.forward; // swings up over the head and down in front
                Aim(armRU, armRL, dir + t.right * 0.2f);
                Aim(armRL, armRH, dir);
            }
            if (Reach > 0.01f)
            {
                // Both hands down and out to where the crate is, from whatever pose we were in.
                Aim(armLU, armLL, t.forward * 0.7f - t.up * 0.8f - t.right * 0.25f, Reach);
                Aim(armLL, armLH, t.forward * 0.9f - t.up * 0.4f, Reach);
                Aim(armRU, armRL, t.forward * 0.7f - t.up * 0.8f + t.right * 0.25f, Reach);
                Aim(armRL, armRH, t.forward * 0.9f - t.up * 0.4f, Reach);
            }
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
