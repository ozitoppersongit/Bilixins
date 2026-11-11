// Skybox panoramico: mistura duas texturas (dia e noite) e desenha o sol, a lua e o brilho do horizonte pela direcao da luz.
// Controlado pelo script CicloDiaNoite (_Mix: 0 = dia, 1 = noite). _Rotacao gira o ceu em graus.
Shader "Bilixins/SkyboxDiaNoite"
{
    Properties
    {
        _TexDia ("Dia (equirretangular)", 2D) = "white" {}
        _TexNoite ("Noite (equirretangular)", 2D) = "black" {}
        _Mix ("Noite (0 = dia, 1 = noite)", Range(0, 1)) = 0
        _Rotacao ("Rotacao", Range(0, 360)) = 0
        _Exposicao ("Exposicao", Range(0, 2)) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _SunDir ("Direcao do sol", Vector) = (0.3, 0.8, -0.5, 0)
        _MoonDir ("Direcao da lua", Vector) = (-0.3, -0.8, 0.5, 0)
        _CorHorizonte ("Brilho do horizonte (alfa = forca)", Color) = (1, 0.5, 0.2, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _TexDia;
            sampler2D _TexNoite;
            half _Mix;
            half _Rotacao;
            half _Exposicao;
            half4 _Tint;
            float4 _SunDir;
            float4 _MoonDir;
            half4 _CorHorizonte;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }
            half4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sol = normalize(_SunDir.xyz);
                float3 lua = normalize(_MoonDir.xyz);
                // textura (gira em torno do eixo vertical)
                float r = _Rotacao * 0.0174533;
                float s = sin(r), c = cos(r);
                float3 dr = float3(c * d.x - s * d.z, d.y, s * d.x + c * d.z);
                float u = atan2(dr.x, dr.z) / 6.2831853 + 0.5;
                float v = asin(clamp(dr.y, -1.0, 1.0)) / 3.14159265 + 0.5;
                half3 col = lerp(tex2D(_TexDia, float2(u, v)).rgb, tex2D(_TexNoite, float2(u, v)).rgb, _Mix);
                col *= _Tint.rgb * _Exposicao;
                // brilho quente no horizonte, do lado do sol (nascer e por do sol)
                float h = saturate(1.0 - abs(d.y) * 3.2);
                float2 dh = normalize(d.xz + 1e-4);
                float2 sh = normalize(sol.xz + 1e-4);
                float lado = saturate(dot(dh, sh) * 0.5 + 0.5);
                col += _CorHorizonte.rgb * _CorHorizonte.a * h * h * (0.25 + 0.75 * lado * lado);
                // sol: disco e halo, so acima do horizonte
                float ds = dot(d, sol);
                float vis = saturate((sol.y + 0.06) * 6.0) * step(-0.015, d.y);
                col += half3(1.0, 0.93, 0.75) * smoothstep(0.99935, 0.99975, ds) * vis * 3.0;
                col += half3(1.0, 0.82, 0.55) * pow(saturate(ds), 28.0) * 0.5 * vis * (1.0 - _Mix);
                // lua
                float dl = dot(d, lua);
                float visL = saturate((lua.y + 0.06) * 6.0) * step(-0.015, d.y);
                float disco = smoothstep(0.9988, 0.9992, dl);
                half3 corLua = lerp(half3(0.98, 0.97, 0.88), half3(0.78, 0.79, 0.76), saturate(frac(sin(dot(floor(d.xz * 220.0), float2(12.9898, 78.233))) * 43758.5453) * 1.4 - 0.3) * 0.5);
                col = lerp(col, corLua, disco * visL);
                col += half3(0.7, 0.78, 1.0) * pow(saturate(dl), 70.0) * 0.4 * visL * _Mix;
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
