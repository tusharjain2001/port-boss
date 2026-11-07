// 3D sign words (TextMesh) that sit in the world: depth-tested, so a wall hides them (Tushar 7 Nov: the town's "Closed for
// now" boards showed through the office walls; the font's own GUI/Text Shader draws on top of everything in URP), the
// vertex colour at full strength (the GUI shader read light from afar), and the scene's fog.
Shader "PortBoss/M1/Text"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half4 _Color;
            CBUFFER_END
            Varyings vert(Attributes i)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.color = i.color * _Color;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex); o.fog = ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 c = i.color; c.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                c.rgb = MixFog(c.rgb, i.fog); return c;
            }
            ENDHLSL
        }
    }
}
