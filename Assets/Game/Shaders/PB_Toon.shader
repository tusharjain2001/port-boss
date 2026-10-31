// Ponyo Coast toon lit (from the look-dev's LD_Stylized, art direction #4): one soft cel step, painted blue-violet
// shade, paint-grain noise, rim. Colour = _BaseColor x vertex colour (our Blender assets are vertex-coloured, sRGB);
// vertex alpha 0 marks a glowing part (the anglerfish bulb). Fish swim/flop by bending along their long axis (local
// +Z = head) in the vertex shader, in every pass so the outline and shadows follow the body.
Shader "PortBoss/M1/Toon"
{
    Properties
    {
        _BaseMap ("Albedo (optional)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Saturation ("Saturation", Float) = 1.1
        _Brightness ("Brightness", Float) = 1.08
        _Tint ("Tint", Color) = (1,0.984,0.949,1)
        _RampSoft ("Cel edge softness", Range(0.001, 0.5)) = 0.12
        _RampOffset ("Cel offset", Range(-0.5, 0.5)) = 0.05
        _Wrap ("Diffuse wrap", Range(0, 1)) = 0.3
        _ShadowTint ("Shadow tint", Color) = (0.596,0.651,0.918,1)
        _AmbientMul ("Ambient", Float) = 1
        _AOMul ("SSAO amount", Range(0, 1)) = 0.6
        _RimColor ("Rim (a = strength)", Color) = (1,1,0.95,0.2)
        _RimPower ("Rim power", Float) = 4
        _PaintNoise ("Paint noise", Float) = 0.3
        _PaintScale ("Paint noise scale", Float) = 0.6
        _Emit ("Emission", Float) = 0
        _GlowColor ("Glow (vertex alpha 0)", Color) = (1,0.9,0.6,1)
        _GlowStrength ("Glow strength", Float) = 0
        _Swim ("Swim bend amplitude (fraction of length)", Float) = 0
        _SwimFreq ("Swim beats per second", Float) = 2
        _SwimPhase ("Swim phase", Float) = 0
        _Wet ("Wet sheen", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float4 _BaseColor, _Tint, _ShadowTint, _RimColor, _GlowColor;
            float _Saturation, _Brightness, _RampSoft, _RampOffset, _Wrap, _AmbientMul, _AOMul;
            float _RimPower, _PaintNoise, _PaintScale, _Emit, _GlowStrength, _Swim, _SwimFreq, _SwimPhase, _Wet, _Cull;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        // Body bend: a travelling sine along local Z (fish are 1 unit long, head at +0.5), stronger toward the tail.
        float3 SwimBend(float3 p)
        {
            if (_Swim <= 0) return p;
            float along = saturate(0.5 - p.z);                 // 0 at the head .. 1 at the tail
            float w = along * along * 1.2 + 0.08;
            p.x += sin(p.z * 6.2 - _Time.y * _SwimFreq * 6.2832 + _SwimPhase) * _Swim * w;
            return p;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PB_Common.hlsl"

            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 col : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; float3 ws : TEXCOORD1; float3 n : TEXCOORD2; float fog : TEXCOORD3; };

            V vert(A i)
            {
                V o; o.ws = TransformObjectToWorld(SwimBend(i.pos.xyz)); o.cs = TransformWorldToHClip(o.ws);
                o.n = TransformObjectToWorldNormal(i.n);
                o.col = float4(SRGBToLinear(i.col.rgb), i.col.a); o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.cs.z); return o;
            }

            float Cel(float x)
            {
                x = saturate(x + _RampOffset);
                return smoothstep(0.5 - _RampSoft, 0.5 + _RampSoft, x);
            }

            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 alb = i.col.rgb * _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float lum = dot(alb, float3(0.299, 0.587, 0.114));
                alb = lerp(lum.xxx, alb, _Saturation) * _Brightness * _Tint.rgb;
                if (_PaintNoise > 0)
                {
                    float2 p = (i.ws.xz + i.ws.y * 0.7) * _PaintScale;
                    alb *= 1 + (LDFbm(p) - 0.5) * _PaintNoise;
                }

                float3 n = normalize(front ? i.n : -i.n);
                float3 v = GetWorldSpaceNormalizeViewDir(i.ws);
                float4 sc = TransformWorldToShadowCoord(i.ws);
                Light L = GetMainLight(sc, i.ws, half4(1,1,1,1));
                float2 suv = GetNormalizedScreenSpaceUV(i.cs);

                float aoD = 1, aoI = 1;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(suv);
                    aoD = lerp(1, ao.directAmbientOcclusion, _AOMul); aoI = lerp(1, ao.indirectAmbientOcclusion, _AOMul);
                #endif

                float ndl = dot(n, L.direction);
                float wrapped = saturate((ndl + _Wrap) / (1 + _Wrap));
                float lit = Cel(wrapped * L.shadowAttenuation) * aoD;

                float3 amb = LDAmbient(n) * _AmbientMul * aoI;
                float3 col = alb * (amb + L.color * lit);
                col *= lerp(_ShadowTint.rgb, 1, saturate(lit * 2)); // colour-shifted shade, never plain grey

                float rim = pow(1 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * _RimColor.a * rim * lerp(0.35, 1, lit);

                if (_Wet > 0)
                {
                    float3 h = normalize(L.direction + v);
                    float sp = smoothstep(0.82, 0.9, pow(saturate(dot(n, h)), 48));
                    col += L.color * sp * _Wet * 0.6 * L.shadowAttenuation;
                }

                #if defined(_ADDITIONAL_LIGHTS) || defined(_CLUSTER_LIGHT_LOOP)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.ws; inputData.normalizedScreenSpaceUV = suv;
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light al = GetAdditionalLight(lightIndex, i.ws, half4(1,1,1,1));
                        float at = al.distanceAttenuation * al.shadowAttenuation;
                        float d = Cel(saturate(dot(n, al.direction) * 0.8 + 0.2));
                        col += alb * al.color * d * at;
                    LIGHT_LOOP_END
                #endif

                col += alb * _Emit;
                col = lerp(col, _GlowColor.rgb * (1 + _GlowStrength), saturate(1 - i.col.a) * step(0.001, _GlowStrength));
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            float4 vert(A i) : SV_POSITION
            {
                float3 ws = TransformObjectToWorld(SwimBend(i.pos.xyz)); float3 n = TransformObjectToWorldNormal(i.n);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 ld = normalize(_LightPosition - ws);
                #else
                    float3 ld = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, n, ld));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 pos : POSITION) : SV_POSITION { return TransformObjectToHClip(SwimBend(pos.xyz)); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(SwimBend(i.pos.xyz)); o.n = TransformObjectToWorldNormal(i.n); return o; }
            half4 frag(V i) : SV_Target { return half4(normalize(i.n), 0); }
            ENDHLSL
        }
    }
}
