// Ponyo Coast toon lit (from the look-dev's LD_Stylized, art direction #4): one soft cel step, painted blue-violet
// shade, paint-grain noise, rim. Colour = _BaseColor x vertex colour (our Blender assets are vertex-coloured, sRGB);
// vertex alpha 0 marks a glowing part (the anglerfish bulb). Fish swim/flop by bending along their long axis (local
// +Z = head) in the vertex shader, in every pass so the outline and shadows follow the body. _Fade (0..1, set per
// renderer by the camera) dithers a wall away between the camera and the player: the colour pass drops that share of its
// pixels in a 4x4 pattern, and the depth passes drop it whole, so the outline and the SSAO never draw the holes and the
// player behind still draws (depth priming is off). Shadows stay.
// M5 paint/dye channels (systems 4.8.2: no extra materials, a MaterialPropertyBlock per renderer): vertex alpha
// 255 - 16*ch tags a face's channel (boats 1 Hull, 2 Stripe, 3 Trim, 4 Deck; clothes 1 Hat .. 6 Gloves). _ChN.rgb is the
// new colour (linear) and _ChN.a the channel's own luminance (0 = channel untouched), so painted faces keep their
// light/dark shading; _PtN is a pattern (a = id) mixing in a second colour (rgb). _Sweep paints channel w from old
// (_Ch(c+4)/_Pt(c+4)) to new along _SweepAxis, x = progress. _ChHide (bitmask) folds a channel's vertices away in every
// pass (a hat swapped for another). Patterns read object space, or the bind pose in TEXCOORD1 when _PatUV = 1 (skins).
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
        _Fade ("See-through (camera)", Range(0,1)) = 0
        _Ch1 ("Ch1", Vector) = (0,0,0,0)
        _Ch2 ("Ch2", Vector) = (0,0,0,0)
        _Ch3 ("Ch3", Vector) = (0,0,0,0)
        _Ch4 ("Ch4", Vector) = (0,0,0,0)
        _Ch5 ("Ch5", Vector) = (0,0,0,0)
        _Ch6 ("Ch6", Vector) = (0,0,0,0)
        _Ch7 ("Ch7", Vector) = (0,0,0,0)
        _Ch8 ("Ch8", Vector) = (0,0,0,0)
        _Pt1 ("Pt1", Vector) = (0,0,0,0)
        _Pt2 ("Pt2", Vector) = (0,0,0,0)
        _Pt3 ("Pt3", Vector) = (0,0,0,0)
        _Pt4 ("Pt4", Vector) = (0,0,0,0)
        _Pt5 ("Pt5", Vector) = (0,0,0,0)
        _Pt6 ("Pt6", Vector) = (0,0,0,0)
        _Pt7 ("Pt7", Vector) = (0,0,0,0)
        _Pt8 ("Pt8", Vector) = (0,0,0,0)
        _Sweep ("Paint sweep (progress, from, to, channel)", Vector) = (0,0,1,0)
        _SweepAxis ("Paint sweep axis (object)", Vector) = (0,0,1,0)
        _PatK ("Pattern heights (knee, sock, toe, shoe)", Vector) = (0.5,0.15,0.1,0.1)
        _ChHide ("Hidden channels (bitmask)", Float) = 0
        _PatUV ("Pattern coords from TEXCOORD1", Float) = 0
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
            float _RimPower, _PaintNoise, _PaintScale, _Emit, _GlowStrength, _Swim, _SwimFreq, _SwimPhase, _Wet, _Cull, _Fade;
            float4 _Ch1, _Ch2, _Ch3, _Ch4, _Ch5, _Ch6, _Ch7, _Ch8, _Pt1, _Pt2, _Pt3, _Pt4, _Pt5, _Pt6, _Pt7, _Pt8, _Sweep, _SweepAxis, _PatK;
            float _ChHide, _PatUV;
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

        // ---- M5 channels
        int Chan(float a) { return (int)round((1 - a) * 255.0 / 16.0); }
        float4 ChCol(int c) { return c == 1 ? _Ch1 : c == 2 ? _Ch2 : c == 3 ? _Ch3 : c == 4 ? _Ch4 : c == 5 ? _Ch5 : c == 6 ? _Ch6 : c == 7 ? _Ch7 : _Ch8; }
        float4 ChPat(int c) { return c == 1 ? _Pt1 : c == 2 ? _Pt2 : c == 3 ? _Pt3 : c == 4 ? _Pt4 : c == 5 ? _Pt5 : c == 6 ? _Pt6 : c == 7 ? _Pt7 : _Pt8; }
        // a hidden channel's vertices fold to the origin (zero-area triangles draw nothing, in every pass)
        float3 Fold(float3 p, float a)
        {
            if (_ChHide < 0.5) return p;
            int c = Chan(a);
            return (c >= 1 && c <= 8 && fmod(floor(_ChHide / exp2(c)), 2) > 0.5) ? float3(0, 0, 0) : p;
        }
        // 0 = the channel's colour, 1 = the pattern's second colour
        float PatMask(int id, float3 q)
        {
            float x = q.x, y = q.y, z = q.z;
            if (id == 2) return step(0.3, frac(y * 14));                                    // twin pinstripe
            if (id == 3) return step(0.55, frac(y * 7));                                    // racing double
            if (id == 4) return step(0.5, frac(y * 6 + sin(z * 3.1) * 0.35));               // wave
            if (id == 5) return fmod(abs(floor(z * 5) + floor(y * 5)), 2);                  // checker
            if (id == 6) return step(0.3, length(frac(float2(z, y) * 7) - 0.5));            // dots (the dots are the colour)
            if (id == 7) return step(0.5, frac((z + y * 1.6) * 2.5));                       // sun rays
            if (id == 8) return step(0.5, frac(z * 3.5 + abs(frac(y * 4) - 0.5) * 2));      // chevron
            if (id == 9) return max(step(0.72, frac(x * 11)), step(0.72, frac(y * 11)));    // plaid
            if (id == 10) return step(0.5, frac(y * 15));                                   // breton stripe
            if (id == 11) return step(0.24, length(frac(float2(x + y * 0.4, y) * 6.5) - 0.5)); // hibiscus dots
            if (id == 12) return step(0.68, frac(x * 26 + sin(y * 38) * 0.18));             // cable knit
            if (id == 13) return 1 - step(0.075, abs(frac(y * 4.5) - 0.5));                 // hi-vis bands
            if (id == 14) return step(0.86, frac(y * 13));                                  // quilting
            if (id == 15) return (abs(x) < 0.022 && z > 0 && frac(y * 9) < 0.32) ? 1 : 0;   // brass buttons
            if (id == 16) return y < _PatK.x ? 1 : 0;                                       // shorts: skin below the knee
            if (id == 17) return z > _PatK.z ? 1 : 0;                                       // steel toe caps
            if (id == 18) return y > _PatK.w ? 1 : 0;                                       // deck shoes: sock above the shoe
            return 0;
        }
        // linear albedo in, painted albedo out. q = pattern coords, os = object space (the sweep)
        float3 Painted(float3 alb, float a, float3 q, float3 os, out float wet)
        {
            wet = 0;
            int c = Chan(a); if (c < 1 || c > 8) return alb;
            float4 cc = ChCol(c), pt = ChPat(c);
            if (c == (int)round(_Sweep.w) && c <= 4)
            {
                float t = saturate((dot(os, _SweepAxis.xyz) - _Sweep.y) / max(_Sweep.z - _Sweep.y, 0.001));
                if (t > _Sweep.x) { cc = ChCol(c + 4); pt = ChPat(c + 4); }
                else wet = smoothstep(_Sweep.x - 0.12, _Sweep.x, t) * step(_Sweep.x, 0.999);
            }
            if (cc.a <= 0) return alb;
            float k = clamp(dot(alb, float3(0.299, 0.587, 0.114)) / cc.a, 0.6, 1.3);
            float3 o = cc.rgb * k;
            int id = (int)round(pt.a);
            if (id > 0) o = lerp(o, pt.rgb * (id >= 16 ? 1 : k), PatMask(id, q));
            return o;
        }

        // 4x4 ordered dither threshold at a pixel (0..1)
        float Bayer4(float2 px)
        {
            const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            uint2 q = uint2(px) & 3;
            return (m[q.y * 4 + q.x] + 0.5) / 16;
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

            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 col : COLOR; float2 uv : TEXCOORD0; float3 bind : TEXCOORD1; };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; float3 ws : TEXCOORD1; float3 n : TEXCOORD2; float fog : TEXCOORD3; float3 os : TEXCOORD4; float3 q : TEXCOORD5; };

            V vert(A i)
            {
                V o; o.ws = TransformObjectToWorld(Fold(SwimBend(i.pos.xyz), i.col.a)); o.cs = TransformWorldToHClip(o.ws);
                o.os = i.pos.xyz; o.q = _PatUV > 0.5 ? i.bind : i.pos.xyz;
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
                if (_Fade > 0.001) clip(Bayer4(i.cs.xy) - _Fade);
                float wetPaint;
                float3 alb = Painted(i.col.rgb, i.col.a, i.q, i.os, wetPaint) * _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float lum = dot(alb, float3(0.299, 0.587, 0.114));
                alb = lerp(lum.xxx, alb, _Saturation) * _Brightness * _Tint.rgb;
                if (_PaintNoise > 0)
                {
                    float2 p = (i.ws.xz + i.ws.y * 0.7) * _PaintScale;
                    alb *= 1 + (LDFbm(p) - 0.5) * _PaintNoise;
                }

                float3 n = SafeNormalize(front ? i.n : -i.n);
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
                col *= lerp(_LDShadeTint.a > 0 ? _LDShadeTint.rgb : _ShadowTint.rgb, 1, saturate(lit * 2)); // colour-shifted shade (the hour's, M3), never plain grey

                float rim = pow(1 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * _RimColor.a * rim * lerp(0.35, 1, lit);

                float wetAll = max(_Wet, wetPaint);
                if (wetAll > 0)
                {
                    float3 h = normalize(L.direction + v);
                    float sp = smoothstep(0.82, 0.9, pow(saturate(dot(n, h)), 48));
                    col += L.color * sp * wetAll * 0.6 * L.shadowAttenuation;
                    col += alb * wetPaint * 0.12;                                     // fresh paint glistens at the brush line
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
            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 col : COLOR; };
            float4 vert(A i) : SV_POSITION
            {
                float3 ws = TransformObjectToWorld(Fold(SwimBend(i.pos.xyz), i.col.a)); float3 n = TransformObjectToWorldNormal(i.n);
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
            float4 vert(float4 pos : POSITION, float4 col : COLOR) : SV_POSITION { return TransformObjectToHClip(Fold(SwimBend(pos.xyz), col.a)); }
            half4 frag() : SV_Target { clip(0.001 - _Fade); return 0; }
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
            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 col : COLOR; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(Fold(SwimBend(i.pos.xyz), i.col.a)); o.n = TransformObjectToWorldNormal(i.n); return o; }
            half4 frag(V i) : SV_Target { clip(0.001 - _Fade); return half4(SafeNormalize(i.n), 0); }
            ENDHLSL
        }
    }
}
