// Stylised gradient sky with procedural clouds (soft or toon-stepped) and an optional towering cloud bank on the horizon.
Shader "PortBoss/M1/Sky"
{
    Properties
    {
        _CloudColor ("Cloud lit", Color) = (1,1,1,1)
        _CloudShade ("Cloud shade", Color) = (0.7,0.75,0.85,1)
        _CloudCover ("Cloud cover", Range(0,1)) = 0.45
        _CloudSharp ("Cloud edge (small = toon)", Range(0.01,0.5)) = 0.15
        _CloudScale ("Cloud scale", Float) = 1.2
        _CloudBank ("Horizon cloud bank", Range(0,1)) = 0
        _CloudBankHeight ("Bank height", Float) = 0.25
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

            float Cloud(float2 p) { return LDFbm(p); }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d);
                float3 col = LDSkyGradient(d);
                float3 sun = normalize(_LDSunDir.xyz);
                if (d.y > 0.0)
                {
                    float2 uv = d.xz / (d.y + 0.12) * _CloudScale * 0.35 + _LDTime * 0.002;
                    float c = Cloud(uv);
                    float m = smoothstep(1 - _CloudCover - _CloudSharp, 1 - _CloudCover + _CloudSharp, c);
                    float lit = saturate((c - Cloud(uv + sun.xz * 0.08)) * 6 + 0.5);
                    lit = _CloudSharp < 0.08 ? step(0.45, lit) : lit;
                    float3 cc = lerp(_CloudShade.rgb, _CloudColor.rgb, lit);
                    m *= smoothstep(0.0, 0.12, d.y);
                    col = lerp(col, cc, m);
                }
                if (_CloudBank > 0)
                {
                    float az = atan2(d.z, d.x);
                    float hgt = _CloudBankHeight * (0.35 + 0.65 * LDFbm(float2(az * 2.2, 3.1)));
                    float puff = LDFbm(float2(az * 9, d.y * 14));
                    float m = smoothstep(hgt + 0.02, hgt - 0.02, d.y - (puff - 0.5) * 0.08) * smoothstep(-0.02, 0.02, d.y) * _CloudBank;
                    float lit = saturate(0.5 + dot(normalize(float3(d.x, 0, d.z)), sun) * 0.5);
                    lit = saturate(lit * 0.6 + (d.y / max(hgt, 1e-3)) * 0.6);
                    lit = _CloudSharp < 0.08 ? step(0.5, lit) * 0.6 + 0.4 : lit;
                    col = lerp(col, lerp(_CloudShade.rgb, _CloudColor.rgb, lit), m);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
