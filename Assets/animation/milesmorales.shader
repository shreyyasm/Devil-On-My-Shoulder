Shader "Unlit/milesmorales"
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

        _ViewLightBias ("View Light Bias", Range(0,1)) = 0.3
        _LightInfluence ("Light Influence", Range(0,1)) = 0.4

        _LightClamp ("Max Light Intensity", Range(0,1)) = 0.85
        _LightContrast ("Light Contrast", Range(0.5,2)) = 1.2

        _SnapFPS ("Vertex Snap FPS", Float) = 12
        _SnapStrength ("Snap Strength", Range(0,1)) = 1
        _SnapAmplitude ("Snap Amplitude", Range(0,0.01)) = 0.002

        // 🔥 SMEAR SETTINGS
        _SmearColorA ("Smear Color A", Color) = (1,0.3,0,1)
        _SmearColorB ("Smear Color B", Color) = (1,1,0,1)
        _SmearIntensity ("Smear Intensity", Range(0,3)) = 1.5
        _SmearWidth ("Smear Width", Range(0,1)) = 0.5
        _SmearNoiseScale ("Smear Noise Scale", Float) = 5
        _SmearSpeed ("Smear Speed", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        // =========================
        // OUTLINE PASS
        // =========================
        Pass
        {
            Name "Outline"
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineThickness;
            float4 _OutlineColor;

            float _SnapFPS;
            float _SnapStrength;
            float _SnapAmplitude;

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

                float steppedTime = floor(_Time.y * _SnapFPS) / _SnapFPS;
                float t = lerp(_Time.y, steppedTime, _SnapStrength);

                float3 posOS = v.positionOS.xyz;
                posOS += sin(t * 10 + posOS.yzx * 2) * _SnapAmplitude;

                float3 normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 positionWS = TransformObjectToWorld(posOS);

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

            float _SnapFPS;
            float _SnapStrength;
            float _SnapAmplitude;

            // 🔥 SMEAR
            float4 _SmearColorA;
            float4 _SmearColorB;
            float _SmearIntensity;
            float _SmearWidth;
            float _SmearNoiseScale;
            float _SmearSpeed;

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

                float steppedTime = floor(_Time.y * _SnapFPS) / _SnapFPS;
                float t = lerp(_Time.y, steppedTime, _SnapStrength);

                float3 posOS = v.positionOS.xyz;
                posOS += sin(t * 10 + posOS.yzx * 2) * _SnapAmplitude;

                o.positionWS = TransformObjectToWorld(posOS);
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

                // =======================
                // 🔥 SMEAR FLAMES
                // =======================

                float3 viewDirN = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = 1.0 - saturate(dot(i.normalWS, viewDirN));

                fresnel = smoothstep(1 - _SmearWidth, 1, fresnel);

                float steppedTime = floor(_Time.y * _SnapFPS) / _SnapFPS;
                float t = steppedTime * _SmearSpeed;

                float noise =
                    sin(i.positionWS.y * _SmearNoiseScale + t) *
                    sin(i.positionWS.x * _SmearNoiseScale * 0.7 + t * 1.3);

                noise = noise * 0.5 + 0.5;

                float smearMask = fresnel * noise * _SmearIntensity;

                float3 smearColor = lerp(_SmearColorA.rgb, _SmearColorB.rgb, noise);

                float3 smear = smearColor * smearMask;

                float3 finalColor = albedo * lighting + smear;

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}