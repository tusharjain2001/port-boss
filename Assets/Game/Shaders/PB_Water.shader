// Ponyo Coast sea (from the look-dev water). The 3 Gerstner waves here are mirrored on the CPU by Sea.cs (same
// numbers, same time, same distance fade) so the boat, swimmers and the bobber float on exactly what you see.
// Stylised water for the look-dev: Gerstner waves, depth colour, refraction, sky reflection, sun glint,
// contact foam and crest foam. Smooth or banded (toon) by parameters. No Voronoi ripple lines (Manan disliked them).
Shader "PortBoss/M1/Water"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.3,0.8,0.8,1)
        _Deep ("Deep", Color) = (0.02,0.2,0.35,1)
        _DepthDist ("Depth distance", Float) = 6
        _MinOpacity ("Min opacity", Range(0,1)) = 0.25
        _Bands ("Colour bands (0 = smooth)", Float) = 0
        _Refraction ("Refraction", Float) = 0.03
        _Reflect ("Sky reflection", Range(0,1)) = 0.6
        _ReflTint ("Reflection tint", Color) = (1,1,1,1)
        _FresnelPow ("Fresnel power", Float) = 4
        _SunSpec ("Sun glint strength", Float) = 2
        _SunPow ("Sun glint power", Float) = 256
        _SpecToon ("Glint toon (0 = smooth)", Range(0,0.5)) = 0
        _Foam ("Foam colour", Color) = (1,1,1,1)
        _FoamDist ("Contact foam width", Float) = 1.2
        _FoamScale ("Foam noise scale", Float) = 0.6
        _FoamSharp ("Foam edge sharpness", Range(0.01,0.5)) = 0.1
        _CrestFoam ("Crest foam", Range(0,1)) = 0.2
        _CrestSteep ("Crest foam from steepness (1 - normal.y)", Range(0,0.5)) = 0.08
        _Sheen ("Soft sun sheen", Float) = 0.15
        _WaveAmp ("Wave height", Float) = 0.25
        _WaveLen ("Wave length", Float) = 14
        _WaveSteep ("Wave steepness", Range(0,1)) = 0.4
        _DetailNormal ("Detail normal", Float) = 0.25
        _DetailScale ("Detail scale", Float) = 0.35
        _ShadowAmt ("Shadow on water", Range(0,1)) = 0.5
        _Lift ("Lit by sun", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "PB_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Shallow, _Deep, _ReflTint, _Foam;
                float _DepthDist, _MinOpacity, _Bands, _Refraction, _Reflect, _FresnelPow, _SunSpec, _SunPow, _SpecToon;
                float _CrestSteep, _Sheen;
                float _FoamDist, _FoamScale, _FoamSharp, _CrestFoam, _WaveAmp, _WaveLen, _WaveSteep, _DetailNormal, _DetailScale, _ShadowAmt, _Lift;
            CBUFFER_END
            float4 _PBSeaCam; // the camera the CPU wave sampler uses (Sea.cs sets it each frame)
            float4x4 _PBHullW2L; float4 _PBHull; // Biscuit's world->local and hull half size (x, z) + gunwale y (Skiff sets them)

            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float3 n : TEXCOORD1; float4 sp : TEXCOORD2; float h : TEXCOORD3; float fog : TEXCOORD4; };

            void Gerstner(float2 dir, float len, float amp, float steep, float2 p, inout float3 off, inout float3 tan, inout float3 bin)
            {
                float k = 2 * PI / len, c = sqrt(9.8 / k), f = k * (dot(dir, p) - c * _LDTime);
                float a = amp, q = steep / (k * amp * 3 + 1e-4);
                off += float3(q * a * dir.x * cos(f), a * sin(f), q * a * dir.y * cos(f));
                tan += float3(-q * dir.x * dir.x * k * a * sin(f), dir.x * k * a * cos(f), -q * dir.x * dir.y * k * a * sin(f));
                bin += float3(-q * dir.x * dir.y * k * a * sin(f), dir.y * k * a * cos(f), -q * dir.y * dir.y * k * a * sin(f));
            }

            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                float3 off = 0, tan = float3(1,0,0), bin = float3(0,0,1);
                float fade = saturate(1 - (length(ws.xz - _PBSeaCam.xz) - 300) / 600); // calm towards the horizon (Sea.Fade)
                Gerstner(normalize(float2(1, 0.6)), _WaveLen, _WaveAmp * fade, _WaveSteep, ws.xz, off, tan, bin);
                Gerstner(normalize(float2(-0.4, 1)), _WaveLen * 0.61, _WaveAmp * 0.55 * fade, _WaveSteep, ws.xz, off, tan, bin);
                Gerstner(normalize(float2(0.9, -0.7)), _WaveLen * 0.37, _WaveAmp * 0.3 * fade, _WaveSteep, ws.xz, off, tan, bin);
                ws += off;
                o.ws = ws; o.h = off.y / max(_WaveAmp * 1.85, 1e-3);
                o.n = normalize(cross(bin, tan));
                o.cs = TransformWorldToHClip(ws);
                o.sp = ComputeScreenPos(o.cs);
                o.fog = ComputeFogFactor(o.cs.z);
                return o;
            }

            float Band(float x) { return _Bands < 0.5 ? x : floor(x * _Bands + 0.5) / _Bands; }

            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                // no sea inside the boat: a wave crest must not show through Biscuit's floor
                if (_PBHull.x > 0)
                {
                    float3 hl = mul(_PBHullW2L, float4(i.ws, 1)).xyz;
                    float2 e = hl.xz / _PBHull.xy;
                    if (dot(e, e) < 1 && hl.y < _PBHull.z) discard; // an ellipse sits inside the skiff's plan everywhere
                }
                float2 uv = i.sp.xy / i.sp.w;
                float surf = i.sp.w;

                // detail normal from two drifting noise layers (finite differences)
                float2 p = i.ws.xz * _DetailScale;
                float2 q1 = p + _LDTime * float2(0.05, 0.03), q2 = p * 1.7 - _LDTime * float2(0.04, -0.02);
                float e = 0.15;
                float n0 = LDNoise(q1) + LDNoise(q2) * 0.6;
                float nx = LDNoise(q1 + float2(e, 0)) + LDNoise(q2 + float2(e, 0)) * 0.6;
                float nz = LDNoise(q1 + float2(0, e)) + LDNoise(q2 + float2(0, e)) * 0.6;
                float3 n = normalize(i.n + float3(n0 - nx, 0, n0 - nz) * _DetailNormal / e * 0.15);

                float raw = SampleSceneDepth(uv);
                float sceneEye = LinearEyeDepth(raw, _ZBufferParams);
                float depth = sceneEye - surf;
                float2 ruv = uv + n.xz * _Refraction * saturate(depth * 0.5);
                float rdepth = LinearEyeDepth(SampleSceneDepth(ruv), _ZBufferParams) - surf;
                if (rdepth < 0) { ruv = uv; rdepth = depth; }
                float3 scene = SampleSceneColor(ruv);
                // the water's VERTICAL depth here (the scene point under this pixel, rebuilt from the depth buffer):
                // colour and contact foam follow how deep the water really is, not how long the view ray is in it, so
                // a grazing look over deep water is never "shallow" and foam only rings what pierces the surface
                float3 scenePos = _WorldSpaceCameraPos + (i.ws - _WorldSpaceCameraPos) * (sceneEye / max(surf, 1e-3));
                float vdepth = raw <= 0.0001 || raw >= 0.9999 ? 1000 : max(i.ws.y - scenePos.y, 0);

                float absorb = Band(saturate(1 - exp(-vdepth / _DepthDist)));
                float3 body = lerp(_Shallow.rgb, _Deep.rgb, absorb);
                float3 col = lerp(scene * _Shallow.rgb, body, saturate(absorb + _MinOpacity));

                Light L = GetMainLight(TransformWorldToShadowCoord(i.ws));
                float shadow = lerp(1, L.shadowAttenuation, _ShadowAmt);
                col *= lerp(1, saturate(dot(n, L.direction)) * 0.5 + 0.5, _Lift) * lerp(float3(1,1,1), L.color, 0.25);
                col *= shadow;

                float3 v = GetWorldSpaceNormalizeViewDir(i.ws);
                float fres = pow(1 - saturate(dot(n, v)), _FresnelPow);
                fres = _Bands < 0.5 ? fres : smoothstep(0.35, 0.45, fres) * 0.6 + fres * 0.4;
                float3 r = reflect(-v, n); r.y = abs(r.y);
                col = lerp(col, LDSkyNoDisc(r) * _ReflTint.rgb, saturate(fres * _Reflect));

                // glint: small crisp sparkles on the facets that catch the sun, over a soft broad sheen
                float rl = saturate(dot(reflect(-L.direction, n), v));
                float g = pow(rl, _SunPow);
                if (_SpecToon > 0) g = smoothstep(0.5 - _SpecToon, 0.5 + _SpecToon, g);
                col += L.color * (g * _SunSpec + pow(rl, 48) * _Sheen) * L.shadowAttenuation;

                // contact foam: a crisp, broken white band where the hull, pilings and shore meet the water
                float fn = LDNoise(i.ws.xz * _FoamScale + _LDTime * 0.2) * 0.6 + LDNoise(i.ws.xz * _FoamScale * 2.3) * 0.4;
                float contact = 1 - saturate(vdepth / _FoamDist);
                float foam = smoothstep(fn - _FoamSharp, fn + _FoamSharp, contact * 1.1);
                // crest foam only where a wave is steeper than the threshold (none on a calm Clear sea)
                float steep = 1 - i.n.y;
                foam = max(foam, smoothstep(_CrestSteep, _CrestSteep + 0.03, steep + (fn - 0.5) * 0.04) * step(0.2, i.h) * _CrestFoam);
                col = lerp(col, _Foam.rgb * lerp(0.6, 1, shadow) * (LDAmbient(float3(0,1,0)) + L.color * 0.8), foam * _Foam.a);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
