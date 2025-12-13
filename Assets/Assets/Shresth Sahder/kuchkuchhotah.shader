Shader "Unlit/kuchkuchhotah"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Amplitude ("Wave Amplitude", Float) = 0.03
        _Frequency ("Wave Frequency", Float) = 8.0
        _Speed ("Wave Speed", Float) = 1.5
        _PixelSteps ("Pixelation Steps", Float) = 16
        _GlowThreshold ("Glow Threshold (0-1)", Range(0,1)) = 0.6
        _GlowIntensity ("Glow Intensity", Float) = 8.0
        _GlowColor ("Glow Tint", Color) = (1,1,1,1)
        _Color ("Base Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _PixelSteps;
            float _GlowThreshold;
            float _GlowIntensity;
            float4 _GlowColor;
            float4 _Color;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float Lum(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }
            float Quantize(float v, float steps) { return floor(v * steps) / steps; }

            float4 Frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;

                float2 uv = IN.uv;

                // Pixelated sine distortion
                float waveX = sin((uv.y * _Frequency) + (t * _Speed)) * _Amplitude;
                float waveY = cos((uv.x * _Frequency) + (t * _Speed)) * _Amplitude;

                waveX = Quantize(waveX, _PixelSteps);
                waveY = Quantize(waveY, _PixelSteps);

                uv.x += waveX;
                uv.y += waveY;

                // Sample texture
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                col.rgb *= _Color.rgb;

                // Glow mask from luminance
                float lum = Lum(col.rgb);
                float glowMask = smoothstep(_GlowThreshold, 1.0, lum);

                // Strong HDR emission
                float3 emission = col.rgb * glowMask * _GlowIntensity;
                emission = pow(emission, 2.2) * _GlowColor.rgb * 5.0; // boost & tint

                float3 finalColor = col.rgb + emission;

                return float4(finalColor, col.a);
            }
            ENDHLSL
        }
    }
}
