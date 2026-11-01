// Água dos canais: duas camadas da mesma textura de ondas deslizando em direções diferentes.
// Sem script e sem luz (barato pra celular). A velocidade é por material.
Shader "Bilixins/AguaCorrente"
{
    Properties
    {
        [MainTexture] _BaseMap ("Ondas", 2D) = "white" {}
        [MainColor] _BaseColor ("Cor", Color) = (1, 1, 1, 1)
        _Velocidades ("Velocidade camada 1 (xy) e camada 2 (zw), em ciclos por segundo", Vector) = (0.12, 0.05, -0.08, 0.1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Agua"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _Velocidades;
            CBUFFER_END

            struct Entrada { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Saida { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; };

            Saida Vert(Entrada i)
            {
                Saida o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 Frag(Saida i) : SV_Target
            {
                float t = _Time.y;
                half3 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv + _Velocidades.xy * t).rgb;
                half3 b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * 0.73 + _Velocidades.zw * t).rgb;
                half3 cor = lerp(a, b, 0.4) * _BaseColor.rgb;
                cor = MixFog(cor, i.fog);
                return half4(cor, 1);
            }
            ENDHLSL
        }
    }
}
