Shader "Unlit/newshader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // --- Painterly lighting ---
        _LightRamp ("Light Ramp", 2D) = "gray" {}
        _ViewBias ("View Bias", Range(0,1)) = 0.65
        _LightInfluence ("Light Influence", Range(0,1)) = 0.2

        // --- Shadow color control ---
        _ShadowTint ("Shadow Tint", Color) = (0.85,0.85,0.9,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.45

        // --- Brush breakup ---
        _BrushTex ("Brush Texture", 2D) = "gray" {}
        _BrushScale ("Brush Scale", Range(1,10)) = 4
        _BrushStrength ("Brush Strength", Range(0,1)) = 0.35

        // --- Outline ---
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0,3)) = 0.8
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
        // OUTLINE PASS (soft, depth-safe)
        // =========================
        Pass
        {
            Name "Outline"
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
                float3 normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);
                posWS += normalWS * (_OutlineThickness * 0.01);
                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // =========================
        // MAIN PASS (hand-drawn lighting)
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_LightRamp); SAMPLER(sampler_LightRamp);
            TEXTURE2D(_BrushTex);  SAMPLER(sampler_BrushTex);

            float4 _Color;
            float4 _ShadowTint;

            float _ViewBias;
            float _LightInfluence;
            float _ShadowStrength;

            float _BrushScale;
            float _BrushStrength;

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
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                Light light = GetMainLight();
                float3 lightDir = normalize(-light.direction);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.positionWS);

                // --- Dispatch-style lighting ---
                float lightTerm = saturate(dot(i.normalWS, lightDir));
                float viewTerm  = saturate(dot(i.normalWS, viewDir));

                float lighting = lerp(viewTerm, lightTerm, _LightInfluence);
                lighting = saturate(lighting + _ViewBias);

                // Painterly ramp (never goes to black)
                float ramp = SAMPLE_TEXTURE2D(_LightRamp, sampler_LightRamp, float2(lighting, 0.5)).r;
                ramp = lerp(0.35, 1.0, ramp);

                // Brush breakup (value only)
                float brush = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, i.uv * _BrushScale).r;
                ramp = lerp(ramp, ramp * brush, _BrushStrength);

                // Shadow tint instead of darkening
                float3 shadowColor = baseColor * _ShadowTint.rgb;
                float3 litColor    = baseColor;

                float3 finalColor = lerp(
                    shadowColor,
                    litColor,
                    lerp(ramp, 1.0, _ShadowStrength)
                );

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}