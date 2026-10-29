using UnityEngine;
using UnityEngine.Rendering;

namespace PortBoss
{
    // The art set the game is dressed in. The build fills it from the Toon Harbor Pack when the pack is in the
    // project (Assets/ThirdParty/ToonHarbor, paid, never committed); with an empty slot the game builds its own piece.
    public class Skin : ScriptableObject
    {
        public static Skin I;
        public static bool On => I != null && I.Toon != null;

        [Header("Look")]
        public Material Toon;          // the pack's toon material (atlas + ramp); our solid colours reuse its ramp
        public Material ToonWater;
        public Material Sky;
        public VolumeProfile Post;
        [Header("People")]
        public GameObject[] Players;   // one per player slot
        public GameObject Buyer;
        public AnimationClip Idle, Walk, Run, Jump, Swim, SwimIdle, Drive, Push, Sprint;
        [Header("Props")]
        public GameObject Crate;
        public GameObject[] Barrels, Pallets, BigCrates, Dinghies;
        [Header("Buildings and boats")]
        public GameObject Shed, Stall, Speedboat;
        public GameObject[] Containers, Cranes, Freighters, Warehouses;

        [Header("Harbour map library (name → prefab)")]
        public string[] LibNames;
        public GameObject[] Lib;

        public static GameObject Pick(GameObject[] set, int i) => set == null || set.Length == 0 ? null : set[Mathf.Abs(i) % set.Length];
        public static GameObject Get(string name)
        {
            if (I == null || I.LibNames == null) return null;
            int i = System.Array.IndexOf(I.LibNames, name);
            return i >= 0 ? I.Lib[i] : null;
        }
    }
}
