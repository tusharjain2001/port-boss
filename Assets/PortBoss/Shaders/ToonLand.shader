// Faceted toon landscape (Scenery.cs): vertex colours (grass, forest, rock, sand, snow), two-band sun shading on the
// flat facets plus the ambient sky, and the scene fog, so far hills fade into the haze in layers.
Shader "PortBoss/ToonLand"
{
    Properties
    {
        _ShadeLevel ("Shaded side brightness", Range(0, 1)) = 0.62
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Land"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _ShadeLevel;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; half4 color : COLOR; float fogFactor : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                Light L = GetMainLight();
                float band = lerp(_ShadeLevel, 1, smoothstep(0.05, 0.25, dot(n, L.direction)));
                half3 col = i.color.rgb * (L.color * band * 0.85 + SampleSH(n) * 0.5);
                return half4(MixFog(col, i.fogFactor), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
