using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PortBoss
{
    // Blends a humanoid's locomotion clips with the Playables API (no Animator Controller asset), and bends the
    // arms on top for carrying and throwing. Clips come from the skin (CC0 UAL1, retargeted onto Toon Harbor workers).
    public class CharAnim : MonoBehaviour
    {
        public enum Slot { Idle, Walk, Run, Jump, Swim, SwimIdle, Drive, Push }

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] plays;
        float[] w;
        Animator animator;
        Transform armLU, armLL, armLH, armRU, armRL, armRH;
        public bool Carry;            // set by Player each frame
        public Transform Facing;      // the player (the model itself is turned to face the pack's way)
        public float Throw01 = -1;
        public bool Flail;            // just fell in the water

        void LateUpdate() => Arms(Carry, Throw01);

        public void Init(Animator a, Skin k)
        {
            animator = a;
            a.runtimeAnimatorController = null; a.applyRootMotion = false; a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var clips = new[] { k.Idle, k.Walk, k.Run, k.Jump, k.Swim, k.SwimIdle, k.Drive, k.Push };
            for (int i = 0; i < clips.Length; i++) if (clips[i] == null) clips[i] = clips[i == (int)Slot.Run ? (int)Slot.Walk : i == (int)Slot.SwimIdle ? (int)Slot.Swim : 0];
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
        }

        // speed: horizontal m/s; the clips are authored around 1.4 m/s walk and 3.5 m/s jog.
        public void Tick(Slot want, float speed)
        {
            if (!graph.IsValid()) return;
            float k = 1 - Mathf.Exp(-10f * Time.deltaTime);
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = Mathf.Lerp(w[i], i == (int)want ? 1f : 0f, k);
                if (plays[i].IsValid()) mixer.SetInputWeight(i, w[i]);
            }
            if (plays[(int)Slot.Walk].IsValid()) plays[(int)Slot.Walk].SetSpeed(Mathf.Clamp(speed / 1.6f, 0.7f, 2.2f));
            if (plays[(int)Slot.Run].IsValid()) plays[(int)Slot.Run].SetSpeed(Mathf.Clamp(speed / 4f, 0.8f, 1.8f));
            if (plays[(int)Slot.Push].IsValid()) plays[(int)Slot.Push].SetSpeed(Mathf.Clamp(speed / 1.5f, 0.5f, 1.8f));
        }

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
                // Hands up under the bottom crate at shoulder height.
                Aim(armLU, armLL, t.forward * 0.85f - t.up * 0.2f - t.right * 0.15f);
                Aim(armLL, armLH, t.forward * 0.7f + t.up * 0.5f + t.right * 0.1f);
                Aim(armRU, armRL, t.forward * 0.85f - t.up * 0.2f + t.right * 0.15f);
                Aim(armRL, armRH, t.forward * 0.7f + t.up * 0.5f - t.right * 0.1f);
            }
            if (throw01 >= 0)
            {
                float a = throw01 < 0.35f ? Mathf.Lerp(-30f, 200f, throw01 / 0.35f) : Mathf.Lerp(200f, 60f, (throw01 - 0.35f) / 0.65f);
                var dir = Quaternion.AngleAxis(-a, t.right) * t.forward; // swings up over the head and down in front
                Aim(armRU, armRL, dir + t.right * 0.2f);
                Aim(armRL, armRH, dir);
            }
        }

        // Rotate a bone so it points from itself towards its child along dir (world space), whatever its local axes.
        static void Aim(Transform bone, Transform child, Vector3 dir)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position;
            bone.rotation = Quaternion.FromToRotation(cur, dir.normalized) * bone.rotation;
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
