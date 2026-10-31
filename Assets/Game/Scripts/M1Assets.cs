using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PortBoss.M1
{
    // Everything the M1 game builds itself from, filled by the editor build (M1Build) and loaded from Resources.
    public class M1Assets : ScriptableObject
    {
        public UniversalRenderPipelineAsset Pipeline;     // copy of the PC pipeline + the Ponyo outline pass
        public Shader Toon, Water, Sky, Particle;
        public Material SoftParticle;   // a transparent particle material ASSET, so the build keeps its shader variant
        public Font Display, DisplayBold, Ui, UiBold, UiBlack; // Fraunces 600 / 700, Zen Maru Gothic 500 / 700 / 900
        public GameObject Boat, Landing, Worker;
        public string[] FishIds; public GameObject[] Fish;
        public TextAsset FishCsv;
        public AnimationClip Idle, Walk, Run, Sprint, Jump, Land, SwimIdle, Swim, Climb, Drive;
        public AudioClip Waves, Splash, Gull, Thud, Kaching, Click;
        public AudioClip[] Steps;
        public Texture2D WorkerPalette;

        static M1Assets i;
        public static M1Assets I => i != null ? i : i = Resources.Load<M1Assets>("M1Assets");

        public GameObject FishPrefab(string id)
        {
            if (FishIds == null) return null;
            int k = System.Array.IndexOf(FishIds, id);
            return k >= 0 ? Fish[k] : null;
        }
    }
}
