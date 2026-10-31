// Full-screen outline from depth + normals, for URP's built-in Full Screen Pass Renderer Feature (free, no assets).
// Lines are a multiply of the scene colour (tinted ink) so they read as drawn, not as black wire.
Shader "PortBoss/M1/Outline"
{
    Properties
    {
        _OutlineColor ("Ink (rgb multiply, a = strength)", Color) = (0.1,0.08,0.12,1)
        _Thickness ("Thickness px", Float) = 1.5
        _DepthSens ("Depth sensitivity", Float) = 12
        _NormalSens ("Normal sensitivity", Float) = 2
        _FadeDist ("Fade distance", Float) = 160
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            float4 _OutlineColor; float _Thickness, _DepthSens, _NormalSens, _FadeDist;

            float Eye(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float2 px = _Thickness / _ScreenParams.xy;
                float d = Eye(uv);
                float3 nC = SampleSceneNormals(uv);
                float dE = 0, nE = 0;
                float2 o[4] = { float2(px.x, 0), float2(-px.x, 0), float2(0, px.y), float2(0, -px.y) };
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float dk = Eye(uv + o[k]);
                    dE += max(0, dk - d);
                    nE += 1 - saturate(dot(nC, SampleSceneNormals(uv + o[k])));
                }
                float edge = saturate(dE / d * _DepthSens - 0.25) ;
                edge = max(edge, saturate(nE * _NormalSens - 0.6));
                edge *= saturate(1 - d / _FadeDist);
                if (SampleSceneDepth(uv) == UNITY_RAW_FAR_CLIP_VALUE) edge = 0;
                col = lerp(col, col * _OutlineColor.rgb, edge * _OutlineColor.a);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
