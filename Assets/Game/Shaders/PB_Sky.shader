// Stylised gradient sky, Ponyo Coast: towering cumulus heads standing on the horizon (stacked domes, sunlit tops, a
// painted blue-violet shade side and base, a soft cel terminator), thin fair-weather cloud overhead, the sun's glow.
Shader "PortBoss/M1/Sky"
{
    Properties
    {
        _CloudColor ("Cloud lit", Color) = (1,1,1,1)
        _CloudShade ("Cloud shade (blue-violet)", Color) = (0.6,0.65,0.92,1)
        _CloudCover ("Overhead cloud cover", Range(0,1)) = 0.3
        _CloudSharp ("Cloud edge (small = toon)", Range(0.01,0.5)) = 0.12
        _CloudScale ("Overhead cloud scale", Float) = 1.2
        _CloudBank ("Cumulus amount", Range(0,1)) = 1
        _CloudBankHeight ("Cumulus height scale", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PB_Common.hlsl"
            float4 _CloudColor, _CloudShade; float _CloudCover, _CloudSharp, _CloudScale, _CloudBank, _CloudBankHeight;
            struct V { float4 cs : SV_POSITION; float3 d : TEXCOORD0; };
            V vert(float4 pos : POSITION) { V o; o.cs = TransformObjectToHClip(pos.xyz); o.d = pos.xyz; return o; }

            float WrapPi(float a) { return a - 6.2831853 * floor((a + 3.14159265) / 6.2831853); }

            // One cumulus head in its own unit space (x: -1..1 across, y: 0 at the flat base, up to ~1.5): a union of
            // domes, big and low at the base, smaller up the tower. Returns the signed distance (in units) and the
            // nearest dome's normal for shading.
            static const float3 Puffs[12] = {
                float3( 0.00, 0.30, 0.46), float3(-0.48, 0.22, 0.34), float3( 0.50, 0.20, 0.33), float3(-0.82, 0.10, 0.22),
                float3( 0.84, 0.10, 0.21), float3(-0.22, 0.68, 0.36), float3( 0.26, 0.72, 0.33), float3( 0.02, 1.02, 0.31),
                float3(-0.36, 0.98, 0.22), float3( 0.34, 1.08, 0.20), float3( 0.06, 1.32, 0.22), float3(-0.12, 1.50, 0.13) };

            float SMin(float a, float b, float k) { float h = saturate(0.5 + 0.5 * (b - a) / k); return lerp(b, a, h) - k * h * (1 - h); }

            // the head's smooth (soft-union) distance: neighbouring domes melt into one billow
            float HeadD(float2 q, float tall)
            {
                float d = 1e3;
                [unroll] for (int k = 0; k < 12; k++)
                {
                    float3 pf = Puffs[k];
                    float r = pf.z * lerp(1, 0.85, saturate(pf.y - 0.5));
                    d = SMin(d, length(q - float2(pf.x, pf.y * tall)) - r, 0.09);
                }
                return d;
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d);
                float3 col = LDSkyGradient(d);
                float3 sun = normalize(_LDSunDir.xyz);
                float sunAz = atan2(sun.z, sun.x);
                float el = asin(clamp(d.y, -1, 1));
                float az = atan2(d.z, d.x);

                // stars at night (M3): a sparse hashed field, twinkling, fading to the horizon; the clouds below cover them
                if (_LDStars > 0.001 && d.y > 0.02)
                {
                    float2 g = d.xz / (d.y + 0.35) * 70;
                    float2 c = floor(g);
                    float h = LDHash(c);
                    float2 f = frac(g) - 0.5 - (float2(LDHash(c + 3.1), LDHash(c + 7.7)) - 0.5) * 0.6;
                    float s = step(0.955, h) * smoothstep(0.11, 0.0, length(f)) * (0.55 + 0.45 * sin(_LDTime * 1.7 + h * 60));
                    col += float3(0.9, 0.93, 1.0) * s * _LDStars * smoothstep(0.02, 0.35, d.y) * (0.6 + 0.8 * frac(h * 37));
                }

                // overhead: thin fair-weather cloud, sun side lit, blue-violet shade
                if (d.y > 0.0)
                {
                    float2 uv = d.xz / (d.y + 0.12) * _CloudScale * 0.35 + _LDTime * 0.002;
                    float c = LDFbm(uv);
                    float m = smoothstep(1 - _CloudCover - _CloudSharp, 1 - _CloudCover + _CloudSharp, c);
                    float lit = saturate((c - LDFbm(uv + sun.xz * 0.08)) * 6 + 0.5);
                    lit = smoothstep(0.38, 0.52, lit);
                    float3 cc = lerp(_CloudShade.rgb, _CloudColor.rgb, lit);
                    m *= smoothstep(0.08, 0.3, d.y) * 0.85;
                    col = lerp(col, cc, m);
                }

                // the cumulus heads standing on the horizon all round (two rings: near and tall, far and low)
                if (_CloudBank > 0 && el > -0.02 && el < 0.6)
                {
                    float3 horizon = _LDSkyHorizon.rgb;
                    [unroll] for (int c = 0; c < 11; c++)
                    {
                        float h1 = LDHash(float2(c * 1.37, 4.1)), h2 = LDHash(float2(c * 2.11, 9.3)), h3 = LDHash(float2(c * 0.77, 1.9));
                        float azc = c * 0.571 + h1 * 0.35;                         // spread round the horizon
                        float w = lerp(0.10, 0.22, h2) * _CloudBankHeight;          // half-width in radians
                        float tall = lerp(0.7, 1.35, h3);
                        float far = c % 2 == 0 ? 0.25 : 0.6;                        // aerial perspective
                        float2 q = float2(WrapPi(az - azc), el + 0.012) / w;
                        if (abs(q.x) > 1.3 || q.y > 1.9) continue;
                        float dist = HeadD(q, tall);
                        if (dist > 0.08) continue;
                        float m = smoothstep(0.02, -0.02, dist + (LDNoise(q * 9 + c * 3.1) - 0.5) * 0.03) * smoothstep(-0.02, 0.05, q.y) * _CloudBank;
                        if (m <= 0.001) continue;
                        // rounded like a ball of cotton: the outline turns away, the middle faces us
                        float e = 0.01;
                        float2 g = float2(HeadD(q + float2(e, 0), tall) - dist, HeadD(q + float2(0, e), tall) - dist) / e;
                        float3 n3 = normalize(float3(g, saturate(-dist * 4) * 1.6 + 0.05));
                        // lit from the sun's side of this head and from above
                        float side = clamp(WrapPi(sunAz - azc) * 1.5, -1, 1);
                        float3 L3 = normalize(float3(side * 0.85, 0.8, 0.45));
                        float lit = dot(n3, L3) * 0.5 + 0.5;
                        lit -= 0.35 * (1 - saturate(q.y / 0.5));                   // the flat base sits in shade
                        float t = smoothstep(0.46, 0.58, lit);                       // soft cel terminator
                        float3 cc = lerp(_CloudShade.rgb, _CloudColor.rgb, t);
                        cc += _CloudColor.rgb * pow(saturate(dot(n3, L3)), 8) * 0.1; // bright sunlit tops
                        cc = lerp(cc, horizon, far * (1 - saturate(q.y * 0.6)) * 0.6);
                        col = lerp(col, cc, m);
                    }
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
