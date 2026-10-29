// Cartoon clouds (and other flat-lit things such as the sea bed): two-tone, sunlit tops and a cool shaded
// underside with a soft edge between, a bright rim against the sky, and only half the scene fog so they don't
// vanish into it. _BaseColor is the lit colour (GameUI tints it gold at dusk), _ShadeColor multiplies it underneath.
Shader "PortBoss/ToonCloud"
{
    Properties
    {
        _BaseColor ("Lit", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade (multiplier)", Color) = (0.72, 0.78, 0.9, 1)
        _FogAmount ("Fog amount", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Cloud"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _ShadeColor;
                float _FogAmount;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float fogFactor : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS), v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light L = GetMainLight();
                float k = saturate(dot(n, L.direction) * 0.35 + n.y * 0.65 + 0.25);
                float lit = smoothstep(0.42, 0.58, k);
                half3 col = _BaseColor.rgb * lerp(_ShadeColor.rgb, 1, lit);
                float rim = pow(1 - saturate(dot(n, v)), 3) * 0.25 * lit;
                col += rim * _BaseColor.rgb;
                col = lerp(col, MixFog(col, i.fogFactor), _FogAmount);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
