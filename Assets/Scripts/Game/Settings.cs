using UnityEngine;

namespace PortBoss
{
    // Player settings, kept in PlayerPrefs.
    public static class Settings
    {
        public static float Music { get => PlayerPrefs.GetFloat("music", 0.6f); set => PlayerPrefs.SetFloat("music", value); }
        public static float Sfx { get => PlayerPrefs.GetFloat("sfx", 0.9f); set => PlayerPrefs.SetFloat("sfx", value); }
        public static float Sensitivity { get => PlayerPrefs.GetFloat("sens", 1f); set => PlayerPrefs.SetFloat("sens", value); }
        public static bool InvertY { get => PlayerPrefs.GetInt("invertY", 0) == 1; set => PlayerPrefs.SetInt("invertY", value ? 1 : 0); }
    }
}
