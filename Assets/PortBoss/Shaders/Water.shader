// Stylised sea for URP. The wave shape comes from the CPU mesh (Water.cs, shared with boat bob + buoyancy);
// this adds ripples (procedural normals), fresnel sky reflection, sun glint, depth tint/transparency,
// contact foam (depth texture) and crest foam. Sky colours come from globals set by GameUI.
Shader "PortBoss/Water"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.18, 0.45, 0.52, 1)
        _Deep ("Deep", Color) = (0.07, 0.22, 0.33, 1)
        _FoamColor ("Foam", Color) = (0.95, 0.95, 0.9, 1)
        _DepthFade ("Depth fade (m)", Float) = 5
        _FoamWidth ("Contact foam width (m)", Float) = 0.7
        _RippleScale ("Ripple scale", Float) = 0.45
        _RippleStrength ("Ripple strength", Float) = 0.1
        _Gloss ("Sun glint sharpness", Float) = 350
        _CrestHeight ("Crest foam above (y)", Float) = -0.55
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
                half4 _Shallow, _Deep, _FoamColor;
                float _DepthFade, _FoamWidth, _RippleScale, _RippleStrength, _Gloss, _CrestHeight;
            CBUFFER_END
            float4 _PB_SkyZenith, _PB_SkyHorizon;

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

            // Slope of a few crossing sine trains: cheap stand-in for a scrolling normal map.
            float2 Ripple(float2 p, float t)
            {
                float2 g = float2(cos(p.x * 1.7 + t * 1.3), cos(p.y * 1.3 - t * 1.1)) * 0.5;
                g += float2(cos((p.x + p.y) * 2.9 + t * 2.1), cos((p.x - p.y) * 3.3 - t * 1.7)) * 0.3;
                g += float2(cos(p.y * 6.1 + p.x * 0.7 + t * 3.0), cos(p.x * 5.3 - p.y * 0.9 + t * 2.6)) * 0.15;
                return g;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 p = i.positionWS.xz * _RippleScale;
                float2 g = Ripple(p, t) + Ripple(p * 2.3 + 7.0, t * 1.3) * 0.5;
                float3 n = normalize(normalize(i.normalWS) + float3(g.x, 0, g.y) * _RippleStrength);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // Water depth below the surface along the view ray.
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surfZ = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float depth = max(0, sceneZ - surfZ);
                float d01 = saturate(depth / _DepthFade);

                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float lit = L.shadowAttenuation;
                half3 water = lerp(_Shallow.rgb, _Deep.rgb, d01) * (0.35 + 0.65 * L.color * lit) + _PB_SkyZenith.rgb * 0.15;

                float fres = pow(1 - saturate(dot(n, v)), 5) * 0.55 + 0.03;
                float3 r = reflect(-v, n);
                half3 sky = lerp(_PB_SkyHorizon.rgb, _PB_SkyZenith.rgb, saturate(r.y * 1.6));
                float3 h = normalize(L.direction + v);
                float spec = pow(saturate(dot(n, h)), _Gloss) * 5;
                half3 col = lerp(water, sky, fres) + spec * L.color * lit;

                // Foam: a band where the water touches posts, hulls and the quay, plus wave crests; noise breaks it up.
                float fn = 0.5 + 0.5 * sin(i.positionWS.x * 3.1 + t * 1.5) * sin(i.positionWS.z * 2.7 - t * 1.2);
                float contact = saturate(1 - depth / _FoamWidth);
                float foam = contact * step(0.3, fn + contact * 0.7);
                foam = max(foam, saturate((i.positionWS.y - _CrestHeight) * 3) * 0.3 * fn);
                col = lerp(col, _FoamColor.rgb * (0.55 + 0.45 * L.color * lit), foam);

                col = MixFog(col, i.fogFactor);
                float alpha = saturate(0.6 + d01 * 0.5 + foam + fres);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
