Shader "TripoRide/Kart/MiniBoostHemisphere"
{
    Properties
    {
        _SpeedLineMap ("Speed Line Map", 2D) = "black" {}
        _NoiseMap ("Noise Map", 2D) = "white" {}
        _EmissionBoost ("Emission Boost", Color) = (0, 0.38, 0.52, 1)
        _FlowSpeed ("Reverse V Flow Speed", Float) = 2.8
        _NoiseFlowSpeed ("Noise Reverse V Flow Speed", Float) = 1.0
        _NoiseTiling ("Noise Tiling", Vector) = (2.45, 1.65, 0.23, 0)
        _SpeedAlphaMin ("Speed Alpha Min", Range(0, 1)) = 0.075
        _SpeedAlphaMax ("Speed Alpha Max", Range(0, 1)) = 0.46
        _NoiseAlphaMin ("Noise Alpha Min", Range(0, 1)) = 0.15
        _NoiseAlphaMax ("Noise Alpha Max", Range(0, 1)) = 0.84
        _VFadeInStart ("V Fade In Start", Range(0, 1)) = 0.16
        _VFadeInEnd ("V Fade In End", Range(0, 1)) = 0.34
        _VFadeOutStart ("V Fade Out Start", Range(0, 1)) = 0.72
        _VFadeOutEnd ("V Fade Out End", Range(0, 1)) = 0.94
        _AlphaPower ("Alpha Power", Range(0.2, 2)) = 0.82
        _AlphaScale ("Alpha Scale", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_SpeedLineMap);
            SAMPLER(sampler_SpeedLineMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionBoost;
                half _FlowSpeed;
                half _NoiseFlowSpeed;
                half4 _NoiseTiling;
                half _SpeedAlphaMin;
                half _SpeedAlphaMax;
                half _NoiseAlphaMin;
                half _NoiseAlphaMax;
                half _VFadeInStart;
                half _VFadeInEnd;
                half _VFadeOutStart;
                half _VFadeOutEnd;
                half _AlphaPower;
                half _AlphaScale;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                float2 speedUv = uv + float2(0.0, -time * _FlowSpeed);
                float2 noiseUv = uv * _NoiseTiling.xy + float2(_NoiseTiling.z, -time * _NoiseFlowSpeed);

                half4 speed = SAMPLE_TEXTURE2D(_SpeedLineMap, sampler_SpeedLineMap, speedUv);
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUv).r;
                half speedAlpha = smoothstep(_SpeedAlphaMin, _SpeedAlphaMax, speed.a);
                half noiseAlpha = lerp(0.16, 1.0, smoothstep(_NoiseAlphaMin, _NoiseAlphaMax, noise));
                half fadeIn = smoothstep(_VFadeInStart, _VFadeInEnd, uv.y);
                half fadeOut = 1.0 - smoothstep(_VFadeOutStart, _VFadeOutEnd, uv.y);
                half vMask = saturate(fadeIn * fadeOut);
                half alpha = pow(saturate(speedAlpha * noiseAlpha * vMask), _AlphaPower) * _AlphaScale * input.color.a;
                half3 color = (speed.rgb * 1.8 + _EmissionBoost.rgb * 0.18) * input.color.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}