using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PortBoss.M1
{
    // Ponyo Coast lighting, fixed at golden hour for M1 (M1-BRIEF: "a nice fixed Golden-hour light"; the day cycle is M3).
    // Values from the art direction (#4) blended with the systems §4.4 "Golden" state. Every number is a knob.
    public static class Ponyo
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        // Palette (gdd systems §2.3 tokens)
        public static readonly Color Paper = Hex("FFFBF2"), Paper2 = Hex("F8EFDD"), Paper3 = Hex("EEDFC4"), Line = Hex("805C52"),
            Ink = Hex("3F2C27"), InkSoft = Hex("7A625A"), Shade = Hex("98A6EA"), ShadeDeep = Hex("5B5E9E"), SeaC = Hex("46D6C8"),
            SeaDeep = Hex("2160B8"), Sky = Hex("3A86E0"), Night = Hex("1D2748"), Sunflower = Hex("F7C548"), Poppy = Hex("E2553F"),
            Leaf = Hex("7DBE6A"), Peach = Hex("F6A27A");
        public static readonly Color[] GradeCol = { Hex("CDBFA8"), Hex("46D6C8"), Hex("3A86E0"), Hex("F7C548") };
        public static readonly string[] GradeName = { "Common", "Fine", "Rare", "Prized" };
        public static readonly float[] GradeMul = { 1.0f, 1.3f, 1.7f, 2.5f };

        public static Light Sun;
        public static Material SkyMat, Outline;

        public static void Apply(M1Assets a)
        {
            if (a.Pipeline != null) { GraphicsSettings.defaultRenderPipeline = a.Pipeline; QualitySettings.renderPipeline = a.Pipeline; }

            // sun: low warm golden-hour light from behind-left of the pier (long soft shadows over the deck)
            var sg = new GameObject("Sun");
            Sun = sg.AddComponent<Light>();
            Sun.type = LightType.Directional; Sun.color = Hex("FFD9A0"); Sun.intensity = 1.75f;
            Sun.shadows = LightShadows.Soft; Sun.shadowStrength = 0.85f; Sun.shadowBias = 0.04f; Sun.shadowNormalBias = 0.3f;
            sg.transform.rotation = Quaternion.Euler(24f, -125f, 0);
            RenderSettings.sun = Sun;

            Color ambSky = Hex("8E9EDC"), ambEq = Hex("C0A8A8"), ambGround = Hex("6C6070");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGround;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("F2DCC4"); RenderSettings.fogStartDistance = 180; RenderSettings.fogEndDistance = 1400;

            SkyMat = new Material(a.Sky);
            SkyMat.SetColor("_CloudColor", Hex("FFF4E6")); SkyMat.SetColor("_CloudShade", Hex("B4AEDA"));
            SkyMat.SetFloat("_CloudCover", 0.24f); SkyMat.SetFloat("_CloudSharp", 0.07f); SkyMat.SetFloat("_CloudScale", 1.1f);
            SkyMat.SetFloat("_CloudBank", 1f); SkyMat.SetFloat("_CloudBankHeight", 0.15f);
            RenderSettings.skybox = SkyMat;

            Shader.SetGlobalColor("_LDSkyTop", Hex("4F8FE2")); Shader.SetGlobalColor("_LDSkyHorizon", Hex("FFE2B8")); Shader.SetGlobalColor("_LDSkyBottom", Hex("E8C8A8"));
            var sunSky = Hex("FFD08A"); sunSky.a = 0.0025f; Shader.SetGlobalColor("_LDSunSky", sunSky);
            var sd = -sg.transform.forward; Shader.SetGlobalVector("_LDSunDir", new Vector4(sd.x, sd.y, sd.z, 10));
            Shader.SetGlobalColor("_LDAmbSky", ambSky); Shader.SetGlobalColor("_LDAmbEq", ambEq); Shader.SetGlobalColor("_LDAmbGround", ambGround);

            // post (Ponyo: neutral, +exposure, +saturation, Ghibli bloom, paper grain, light vignette)
            var vg = new GameObject("Post");
            var vol = vg.AddComponent<Volume>(); vol.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            p.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
            var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.2f); ca.contrast.Override(-4); ca.saturation.Override(10);
            var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(6);
            var bl = p.Add<Bloom>(true); bl.intensity.Override(0.5f); bl.threshold.Override(1.15f); bl.scatter.Override(0.8f); bl.tint.Override(Hex("FFE6C4")); bl.highQualityFiltering.Override(true);
            var vi = p.Add<Vignette>(true); vi.intensity.Override(0.16f); vi.smoothness.Override(0.45f);
            var fg = p.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium1); fg.intensity.Override(0.12f);
            vol.sharedProfile = p;
        }

        // The sea, golden-hour Ponyo (luminous turquoise-cobalt, soft glint, crisp white foam)
        public static Material SeaMaterial(M1Assets a)
        {
            var w = new Material(a.Water);
            w.SetColor("_Shallow", Hex("4FD8C8")); w.SetColor("_Deep", Hex("2468B8")); w.SetFloat("_DepthDist", 6); w.SetFloat("_MinOpacity", 0.35f);
            w.SetFloat("_Bands", 0); w.SetFloat("_Refraction", 0.03f); w.SetFloat("_Reflect", 0.5f); w.SetColor("_ReflTint", Hex("FFF2E0"));
            w.SetFloat("_FresnelPow", 4); w.SetFloat("_SunSpec", 2.2f); w.SetFloat("_SunPow", 500); w.SetFloat("_SpecToon", 0);
            w.SetColor("_Foam", Color.white); w.SetFloat("_FoamDist", 1.1f); w.SetFloat("_FoamScale", 0.6f); w.SetFloat("_FoamSharp", 0.05f);
            w.SetFloat("_CrestFoam", 0.12f); w.SetFloat("_DetailNormal", 0.2f); w.SetFloat("_DetailScale", 0.35f); w.SetFloat("_ShadowAmt", 0.5f); w.SetFloat("_Lift", 0.35f);
            return w;
        }

        public static Material Toon(M1Assets a, Color c)
        {
            var m = new Material(a.Toon); m.SetColor("_BaseColor", c); return m;
        }
    }
}
