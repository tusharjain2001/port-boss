// Toon sea for URP, in the Toon Harbor style. The wave shape comes from the CPU mesh (Water.cs, shared with boat
// bob + buoyancy); this colours it: shallow turquoise to deep blue by water depth, lighter wave crests, soft
// slow tone patches, a broad sun glint, white foam where the water meets quays, hulls and posts, and the horizon
// haze. Nothing is drawn thinner than a couple of pixels (the old cel ripple lines broke up into glittering specks),
// and detail fades out with distance, so the sea reads clean and calm. Sky colours and the storm come from globals
// (GameUI, Storm).
Shader "PortBoss/Water"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.26, 0.72, 0.76, 1)
        _Deep ("Deep", Color) = (0.07, 0.36, 0.57, 1)
        _Crest ("Crest", Color) = (0.42, 0.86, 0.88, 1)
        _FoamColor ("Foam", Color) = (0.97, 0.99, 1.0, 1)
        _DepthFade ("Depth fade (m)", Float) = 7
        _FoamWidth ("Contact foam width (m)", Float) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow, _Deep, _Crest, _FoamColor;
                float _DepthFade, _FoamWidth;
            CBUFFER_END
            float4 _PB_SkyZenith, _PB_SkyHorizon;
            float _PB_Storm, _PB_WaveScale; // 0 calm .. 1 storm; Water.Storm (wave height multiplier)

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                float a = frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453), b = frac(sin(dot(i + float2(1, 0), float2(12.9898, 78.233))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(12.9898, 78.233))) * 43758.5453), d = frac(sin(dot(i + 1, float2(12.9898, 78.233))) * 43758.5453);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y, storm = saturate(_PB_Storm), waves = max(1, _PB_WaveScale);
                float3 pos = i.positionWS;
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(pos);
                float dist = distance(_WorldSpaceCameraPos, pos);

                // Water depth below the surface along the view ray (the quay, hulls, the sea bed).
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surfZ = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float depth = max(0, sceneZ - surfZ);
                float d01 = saturate(depth / _DepthFade);

                Light L = GetMainLight(TransformWorldToShadowCoord(pos));
                float shadow = lerp(0.7, 1.0, L.shadowAttenuation);
                half3 light = L.color * shadow * 0.8 + SampleSH(float3(0, 1, 0)) * 0.45;

                // Base: shallow → deep; the wave crests a shade lighter (a soft gradient, not patches).
                half3 col = lerp(_Shallow.rgb, _Deep.rgb, sqrt(d01));
                float crestH = (pos.y + 1.2) / waves;   // height over the mean, in calm-sea units
                float aaH = fwidth(crestH) + 1e-4;
                col = lerp(col, _Crest.rgb, smoothstep(0.02, 0.3, crestH) * 0.28 * saturate(1 - dist / 260));

                // Soft, slow tone patches (large and low-contrast: no lines, nothing thinner than a pixel to shimmer).
                float tone = VNoise(pos.xz * 0.035 + float2(t * 0.015, -t * 0.01)) * 0.6 + VNoise(pos.xz * 0.09 - float2(t * 0.02, t * 0.012)) * 0.4;
                col = lerp(col, _Crest.rgb, smoothstep(0.45, 0.85, tone) * 0.16 * saturate(1 - dist / 200));

                col *= light;

                // Sun glint: one broad, soft path towards the sun, kept under the bloom threshold (a sharp band glittered).
                float3 r = reflect(-v, n);
                float glint = smoothstep(0.94, 1.0, dot(r, L.direction));
                col += glint * glint * 0.3 * L.color * shadow * (1 - storm);

                // Storm: grey-green and heavy.
                col = lerp(col, half3(0.22, 0.30, 0.33) * light, storm * 0.55);

                // Foam: a wobbly white band where the water touches things, and whitecaps on storm crests.
                float wob = 0.65 + 0.35 * sin(pos.x * 2.3 + t * 1.6) * sin(pos.z * 1.9 - t * 1.3);
                float edge = depth - _FoamWidth * wob;
                float fwD = fwidth(depth) + 1e-4;
                float aaD = min(fwD, 0.08) + 1e-3; // capped: at a pile's silhouette the depth jumps metres per pixel (it drew white outlines)
                // Where the band would be under ~2 pixels wide (grazing views along a quay, far off) it fades out instead of
                // breaking into dashes and specks that crawl as the waves move.
                float bandPx = _FoamWidth * wob / fwD;
                float foam = (1 - smoothstep(-aaD, aaD, edge)) * saturate((bandPx - 1) / 2) * saturate(1 - dist / 160);
                foam = max(foam, smoothstep(0.32 - aaH, 0.32 + aaH, crestH) * storm * 0.9 * saturate(1 - dist / 250));
                col = lerp(col, _FoamColor.rgb * (light * 0.85 + 0.15), foam);

                // Out towards the horizon: flat colour that melts into the haze.
                float far = saturate((dist - 120) / 600);
                col = lerp(col, lerp(_Deep.rgb * light, _PB_SkyHorizon.rgb, 0.35), far * 0.5);
                col = MixFog(col, i.fogFactor);
                float alpha = saturate(0.82 + d01 * 0.18 + foam);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
