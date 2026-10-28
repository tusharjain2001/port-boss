using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PortBoss
{
    // Blends a rigged character's clips with the Playables API (no Animator Controller asset needed).
    // Slots are filled by the editor build script by matching clip names; missing slots fall back.
    public class CharAnim : MonoBehaviour
    {
        public enum Slot { Idle, Walk, Run, Jump, CarryIdle, CarryWalk, Throw }

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] plays;
        AnimationClip[] clips;
        float[] w;
        float throwUntil;
        int count;

        // clips indexed by Slot; nulls allowed.
        public void Init(Animator animator, AnimationClip[] bySlot)
        {
            count = System.Enum.GetValues(typeof(Slot)).Length;
            clips = new AnimationClip[count];
            for (int i = 0; i < count && i < bySlot.Length; i++) clips[i] = bySlot[i];
            // Fallbacks: carry → plain, run → walk, throw/jump → idle.
            clips[(int)Slot.Walk] ??= clips[(int)Slot.Idle];
            clips[(int)Slot.Run] ??= clips[(int)Slot.Walk];
            clips[(int)Slot.CarryIdle] ??= clips[(int)Slot.Idle];
            clips[(int)Slot.CarryWalk] ??= clips[(int)Slot.Walk];
            clips[(int)Slot.Jump] ??= clips[(int)Slot.Idle];
            clips[(int)Slot.Throw] ??= clips[(int)Slot.Idle];

            graph = PlayableGraph.Create("char");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "anim", animator);
            mixer = AnimationMixerPlayable.Create(graph, count);
            output.SetSourcePlayable(mixer);
            plays = new AnimationClipPlayable[count];
            w = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (clips[i] == null) continue;
                plays[i] = AnimationClipPlayable.Create(graph, clips[i]);
                graph.Connect(plays[i], 0, mixer, i);
            }
            w[0] = 1;
            graph.Play();
        }

        public void Throw()
        {
            throwUntil = Time.time + (clips != null && clips[(int)Slot.Throw] != null ? Mathf.Min(clips[(int)Slot.Throw].length, 0.8f) : 0.4f);
            if (plays != null && plays[(int)Slot.Throw].IsValid()) plays[(int)Slot.Throw].SetTime(0);
        }

        // speed: horizontal m/s.
        public void Tick(float speed, bool grounded, bool carrying)
        {
            if (!graph.IsValid()) return;
            Slot want;
            if (Time.time < throwUntil) want = Slot.Throw;
            else if (!grounded) want = Slot.Jump;
            else if (carrying) want = speed > 0.4f ? Slot.CarryWalk : Slot.CarryIdle;
            else want = speed < 0.4f ? Slot.Idle : speed < 5.5f ? Slot.Walk : Slot.Run;

            float k = 1 - Mathf.Exp(-12f * Time.deltaTime);
            for (int i = 0; i < count; i++)
            {
                w[i] = Mathf.Lerp(w[i], i == (int)want ? 1f : 0f, k);
                if (plays[i].IsValid()) mixer.SetInputWeight(i, w[i]);
            }
            // Match stride to speed (clips are authored around 1.5 m/s walk / 5 m/s run).
            if (plays[(int)Slot.Walk].IsValid()) plays[(int)Slot.Walk].SetSpeed(Mathf.Clamp(speed / 3.2f, 0.6f, 1.8f));
            if (plays[(int)Slot.Run].IsValid()) plays[(int)Slot.Run].SetSpeed(Mathf.Clamp(speed / 6f, 0.7f, 1.5f));
            if (plays[(int)Slot.CarryWalk].IsValid()) plays[(int)Slot.CarryWalk].SetSpeed(Mathf.Clamp(speed / 3f, 0.6f, 1.6f));
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
