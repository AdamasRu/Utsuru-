// Multi-pass separable Gaussian blur shader for Utsuru UI modal backgrounds (URP 17+).
Shader "Hidden/Utsuru/UIBackgroundBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            float _BlurSize;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }

        half4 SampleBlur(float2 uv, float2 dir)
        {
            // 9-tap Gaussian weights (sigma ~ 2.0)
            static const float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
            half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * weights[0];
            float2 stepUv = dir * _MainTex_TexelSize.xy * _BlurSize;

            [unroll]
            for (int i = 1; i < 5; i++)
            {
                float2 offset = stepUv * (float)i;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset) * weights[i];
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset) * weights[i];
            }
            return col;
        }
        ENDHLSL

        // Pass 0: Horizontal Gaussian Blur
        Pass
        {
            Name "BlurHorizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragH

            half4 FragH(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(1.0, 0.0));
            }
            ENDHLSL
        }

        // Pass 1: Vertical Gaussian Blur
        Pass
        {
            Name "BlurVertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragV

            half4 FragV(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(0.0, 1.0));
            }
            ENDHLSL
        }
    }
}
