Shader "Unlit/sm"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _PixelSize ("Pixel Size", Float) = 160
        _CelSteps ("Cel Steps", Range(1,6)) = 4

        _HatchTex ("Hatching Texture", 2D) = "gray" {}
        _HatchStrength ("Hatch Strength", Range(0,1)) = 0.6

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0,3)) = 1

        // --- Comic lighting controls ---
        _ViewLightBias ("View Light Bias", Range(0,1)) = 0.3
        _LightInfluence ("Light Influence", Range(0,1)) = 0.4

        // --- Anti-Glow Controls ---
        _LightClamp ("Max Light Intensity", Range(0,1)) = 0.85
        _LightContrast ("Light Contrast", Range(0.5,2)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // =========================
        // OUTLINE PASS
        // =========================
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineThickness;
            float4 _OutlineColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                float3 normalWS   = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);

                positionWS += normalWS * (_OutlineThickness * 0.01);

                o.positionHCS = TransformWorldToHClip(positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // =========================
        // MAIN PASS
        // =========================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_HatchTex);
            SAMPLER(sampler_HatchTex);

            float4 _Color;
            float _PixelSize;
            float _CelSteps;
            float _HatchStrength;

            float _ViewLightBias;
            float _LightInfluence;
            float _LightClamp;
            float _LightContrast;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.uv         = v.uv;

                float4 clipPos = TransformWorldToHClip(o.positionWS);

                float2 snap = _ScreenParams.xy / _PixelSize;
                clipPos.xy = floor(clipPos.xy * snap) / snap;

                o.positionHCS = clipPos;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                Light light = GetMainLight();
                float3 lightDir = normalize(-light.direction);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.positionWS);

                float lightTerm = saturate(dot(i.normalWS, lightDir));
                float viewTerm  = saturate(dot(i.normalWS, viewDir));

                float combined = lerp(viewTerm, lightTerm, _LightInfluence);
                combined += _ViewLightBias;

                combined = pow(saturate(combined), _LightContrast);
                combined = min(combined, _LightClamp);

                float cel = floor(combined * _CelSteps) / _CelSteps;

                float hatch = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, i.uv * 6).r;
                float hatchMask = (1 - cel) * hatch * _HatchStrength;

                float lighting = saturate(cel - hatchMask);

                return float4(albedo * lighting, 1);
            }
            ENDHLSL
        }
    }
}