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
        public GameObject Boat, Landing, Worker, Pip;    // Pip: the character kit's first body (replaces the placeholder Worker when present)
        public GameObject[] Crew; public string[] CrewNames; // every kit body built so far (art-src/kit), Pip first: F9 / -pb-body
        public Vector2[] CrewBelly;                          // each body's belly: how far it sticks out in front, half its width (m)
        public string[] FishIds; public GameObject[] Fish;
        public GameObject[] Cells; public GameObject WorldProps; public TextAsset WorldJson; public Texture2D Seabed; // map B (Art/World, Map.cs)
        public TextAsset FishCsv;
        public AnimationClip Idle, Walk, Run, Sprint, Jump, Land, SwimIdle, Swim, Climb, Drive;
        public AnimationClip JumpUp, Throw, GetUpBack, GetUpFront, FallIn; // the feel pass's Mixamo clips (Jumping Up, Frisbee Throw, Getting Up, Standing Up, Falling Into Pool)
        // sampled on Pip at build (M1Build.Feel): how far a clip lifts the hips above standing (the game takes it back out:
        // the controller does the rising), and each get-up (one is picked at random)
        public AnimationCurve JumpLift, FallLift;
        public GetUpClip[] GetUps;

        // A get-up: the part of the clip from lying still to standing, how fast it plays, where it starts (hips, hips ->
        // head, measured from the body) and how it creeps along the ground (the game moves the body by that).
        [System.Serializable]
        public class GetUpClip
        {
            public AnimationClip Clip; public float From, To, Speed;
            public Vector3 Hips, Spine; public AnimationCurve MoveX, MoveZ;
        }
        public AnimationClip RodIdle, Wait, Cast, Strike, Fight, Reel, HoldUp, Helm, Repair; // keyed fishing clips (M1Anim, Assets/Game/Anim)
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
