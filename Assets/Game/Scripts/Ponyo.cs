using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PortBoss.M1
{
    // Ponyo Coast lighting, fixed for M1 at late afternoon in the approved sheet's palette (artdir §4; the day cycle is M3).
    // Values from the art direction (#4) blended with the systems §4.4 "Golden" state. Every number is a knob.
    public static class Ponyo
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        // Palette (gdd systems §2.3 tokens)
        public static readonly Color Paper = Hex("FFFBF2"), Paper2 = Hex("F8EFDD"), Paper3 = Hex("EEDFC4"), Line = Hex("805C52"),
            Ink = Hex("3F2C27"), InkSoft = Hex("7A625A"), Shade = Hex("98A6EA"), ShadeDeep = Hex("5B5E9E"), SeaC = Hex("46D6C8"),
            SeaDeep = Hex("2160B8"), Sky = Hex("3A86E0"), Night = Hex("1D2748"), Sunflower = Hex("F7C548"), Poppy = Hex("E2553F"),
            Leaf = Hex("7DBE6A"), Peach = Hex("F6A27A");
        // Two grades (Manan 1 Nov): the one stop in the green = Rare, anything else = Basic. Rare sells for x2 (Manan, confirmed).
        public static readonly Color[] GradeCol = { Hex("CDBFA8"), Hex("F7C548") };
        public static readonly string[] GradeName = { "Basic", "Rare" };
        public static readonly float[] GradeMul = { 1.0f, 2.0f };

        public static Light Sun;
        public static Material SkyMat, Outline;

        public static void Apply(M1Assets a)
        {
            if (a.Pipeline != null) { GraphicsSettings.defaultRenderPipeline = a.Pipeline; QualitySettings.renderPipeline = a.Pipeline; }

            // sun: low late-afternoon light from behind-left of the pier (long soft shadows over the deck)
            var sg = new GameObject("Sun");
            Sun = sg.AddComponent<Light>();
            // Ponyo Coast sheet (artdir §4): late-afternoon sun, warm white (#FFF3DC), still low enough for long shadows
            Sun.type = LightType.Directional; Sun.color = Hex("FFEACB"); Sun.intensity = 1.8f;
            Sun.shadows = LightShadows.Soft; Sun.shadowStrength = 0.85f; Sun.shadowBias = 0.04f; Sun.shadowNormalBias = 0.3f;
            sg.transform.rotation = Quaternion.Euler(30f, -125f, 0);
            RenderSettings.sun = Sun;

            Color ambSky = Hex("98A6EA"), ambEq = Hex("C4C2D2"), ambGround = Hex("6C6A80"); // the painted blue-violet shade
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGround;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("DCE6EE"); RenderSettings.fogStartDistance = 260; RenderSettings.fogEndDistance = 1900;

            SkyMat = new Material(a.Sky);
            // towering cumulus round the horizon, sunlit cream tops, blue-violet shade (#98A6EA); thin cloud overhead
            SkyMat.SetColor("_CloudColor", Hex("FFF8EC")); SkyMat.SetColor("_CloudShade", Hex("98A6EA"));
            SkyMat.SetFloat("_CloudCover", 0.28f); SkyMat.SetFloat("_CloudSharp", 0.1f); SkyMat.SetFloat("_CloudScale", 0.9f);
            SkyMat.SetFloat("_CloudBank", 1f); SkyMat.SetFloat("_CloudBankHeight", 1f);
            RenderSettings.skybox = SkyMat;

            Shader.SetGlobalColor("_LDSkyTop", Hex("3A86E0")); Shader.SetGlobalColor("_LDSkyHorizon", Hex("D6E8F2")); Shader.SetGlobalColor("_LDSkyBottom", Hex("CFE6F5"));
            var mid = Hex("7DB4EC"); mid.a = 1; Shader.SetGlobalColor("_LDSkyMid", mid);
            var sunSky = Hex("FFF3DC"); sunSky.a = 0.0025f; Shader.SetGlobalColor("_LDSunSky", sunSky);
            var sd = -sg.transform.forward; Shader.SetGlobalVector("_LDSunDir", new Vector4(sd.x, sd.y, sd.z, 10));
            Shader.SetGlobalColor("_LDAmbSky", ambSky); Shader.SetGlobalColor("_LDAmbEq", ambEq); Shader.SetGlobalColor("_LDAmbGround", ambGround);

            // post (Ponyo: neutral, +exposure, +saturation, Ghibli bloom, paper grain, light vignette)
            var vg = new GameObject("Post");
            var vol = vg.AddComponent<Volume>(); vol.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            p.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
            var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.1f); ca.contrast.Override(0); ca.saturation.Override(14);
            var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(6);
            var bl = p.Add<Bloom>(true); bl.intensity.Override(0.5f); bl.threshold.Override(1.15f); bl.scatter.Override(0.8f); bl.tint.Override(Hex("FFE6C4")); bl.highQualityFiltering.Override(true);
            var vi = p.Add<Vignette>(true); vi.intensity.Override(0.16f); vi.smoothness.Override(0.45f);
            var fg = p.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium1); fg.intensity.Override(0.12f);
            vol.sharedProfile = p;
        }

        // The sea, Ponyo Coast (luminous turquoise-cobalt by depth, soft glint, crisp white foam)
        public static Material SeaMaterial(M1Assets a)
        {
            var w = new Material(a.Water);
            // luminous turquoise shallows (#46D6C8) to deep cobalt (#2160B8) by the water's real depth; the sky's
            // reflection without the sun disc; a soft sheen + small crisp sparkles; crisp white contact foam only
            // where hull, pilings and shore meet the water; crest foam only on waves steeper than a calm sea's
            w.SetColor("_Shallow", Hex("46D6C8")); w.SetColor("_Deep", Hex("2160B8")); w.SetFloat("_DepthDist", 3.2f); w.SetFloat("_MinOpacity", 0.3f);
            w.SetFloat("_Bands", 0); w.SetFloat("_Refraction", 0.03f); w.SetFloat("_Reflect", 0.34f); w.SetColor("_ReflTint", Hex("F4F8FF"));
            w.SetFloat("_FresnelPow", 4); w.SetFloat("_SunSpec", 1.1f); w.SetFloat("_SunPow", 140); w.SetFloat("_SpecToon", 0f); w.SetFloat("_Sheen", 0.1f); // glitter lobe width, sheen
            w.SetColor("_Foam", Color.white); w.SetFloat("_FoamDist", 0.5f); w.SetFloat("_FoamScale", 1.4f); w.SetFloat("_FoamSharp", 0.04f);
            w.SetFloat("_CrestFoam", 1f); w.SetFloat("_CrestSteep", 0.2f); // whitecaps only in rough weather (a Clear sea peaks ~0.1)
            var args = System.Environment.GetCommandLineArgs(); // debug: -pb-nocrest / -pb-nocontact (find what draws a foam patch)
            if (System.Array.IndexOf(args, "-pb-nocrest") >= 0) w.SetFloat("_CrestFoam", 0);
            if (System.Array.IndexOf(args, "-pb-nocontact") >= 0) w.SetFloat("_FoamDist", 0.0001f); w.SetFloat("_DetailNormal", 0.14f); w.SetFloat("_DetailScale", 0.35f); w.SetFloat("_ShadowAmt", 0.5f); w.SetFloat("_Lift", 0.35f);
            return w;
        }

        public static Material Toon(M1Assets a, Color c)
        {
            var m = new Material(a.Toon); m.SetColor("_BaseColor", c); return m;
        }
    }
}
