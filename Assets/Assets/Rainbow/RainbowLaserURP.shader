Shader "Custom/URP/RainbowLaser"
{
    Properties
    {
        _MainColor ("Tint", Color) = (1,1,1,1)

        _Brightness ("Brightness", Range(0,3)) = 1.0
        _Saturation ("Saturation", Range(0,2)) = 1.0
        _Contrast ("Contrast", Range(0,3)) = 1.0

        _FlowSpeed ("Flow Speed", Float) = 0.0
        _RainbowScale ("Rainbow Scale", Float) = 1.0

        _CoreStrength ("Core Strength", Range(0,3)) = 1.0
        _GlowStrength ("Glow Strength", Range(0,3)) = 0.35
        _EdgeSoftness ("Edge Softness", Range(0.2,8)) = 1.5
        _Alpha ("Alpha", Range(0,1)) = 1.0

        _PulseSpeed ("Pulse Speed", Float) = 4.0
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.05

        // NEW — for the proper rainbow stripe look
        _RainbowLines ("Rainbow Lines", Range(2,12)) = 7
        _LineSeparation ("Line Separation", Range(0.0,0.2)) = 0.02
        _WaveAmplitude ("Wave Amplitude", Range(0,0.2)) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 8.0
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveOffset ("Wave Offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float _Brightness;
                float _Saturation;
                float _Contrast;
                float _FlowSpeed;
                float _RainbowScale;
                float _CoreStrength;
                float _GlowStrength;
                float _EdgeSoftness;
                float _Alpha;
                float _PulseSpeed;
                float _PulseAmount;

                float _RainbowLines;
                float _LineSeparation;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _WaveOffset;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float3 HueToRGB(float h)
            {
                float3 rgb = saturate(abs(frac(h + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
                rgb = rgb * rgb * (3.0 - 2.0 * rgb); // smoother transitions
                return rgb;
            }

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(luma.xxx, color, saturation);
            }

            float3 ApplyContrast(float3 color, float contrast)
            {
                return (color - 0.5) * contrast + 0.5;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // =========================================================
                // 1) MAKE THE WHOLE RAINBOW WAVE UP/DOWN
                // =========================================================
                float wave = sin(uv.x * _WaveFrequency - _WaveOffset) * _WaveAmplitude;

                // Optional second softer wave for nicer motion
                wave += sin(uv.x * (_WaveFrequency * 0.45) - (_WaveOffset * 0.65)) * (_WaveAmplitude * 0.35);

                // Shift rainbow bands up/down together
                float rainbowY = uv.y + wave;

                // Keep within 0..1
                rainbowY = saturate(rainbowY);

                // =========================================================
                // 2) CREATE 7 HORIZONTAL RAINBOW STRIPES ACROSS THE WIDTH
                // =========================================================
                float lines = max(_RainbowLines, 2.0);

                // Convert width into line index space
                float linePos = rainbowY * lines;

                // Which stripe are we in?
                float stripeIndex = floor(linePos);

                // 0..1 inside that stripe
                float stripeFrac = frac(linePos);

                // Thin dark gap between stripes if wanted
                float gapMask = smoothstep(0.0, _LineSeparation, stripeFrac) *
                                (1.0 - smoothstep(1.0 - _LineSeparation, 1.0, stripeFrac));

                // Hue based on stripe index, not on beam length
                float hue = stripeIndex / max(lines - 1.0, 1.0);

                // tiny flow inside the colors if you still want subtle movement
                hue = frac(hue + _Time.y * _FlowSpeed * 0.05);

                float3 rainbow = HueToRGB(hue);

                // =========================================================
                // 3) COLOR CONTROLS
                // =========================================================
                rainbow = ApplySaturation(rainbow, _Saturation);
                rainbow = ApplyContrast(rainbow, _Contrast);
                rainbow *= _Brightness;
                rainbow = saturate(rainbow);

                // =========================================================
                // 4) BEAM SHAPE MASK
                // =========================================================
                float dist = abs(uv.y - 0.5) * 2.0;

                float bodyMask = pow(saturate(1.0 - dist), max(_EdgeSoftness, 0.001));
                float coreMask = pow(saturate(1.0 - dist), max(_EdgeSoftness * 2.2, 0.001));

                float pulse = 1.0 + sin(uv.x * 10.0 - _Time.y * _PulseSpeed) * _PulseAmount;

                // Keep stripes separated a little
                float stripeMask = lerp(0.35, 1.0, gapMask);

                float3 bodyCol = rainbow * bodyMask * stripeMask;
                float3 coreCol = rainbow * coreMask * _CoreStrength * stripeMask;
                float3 glowCol = rainbow * bodyMask * _GlowStrength * 0.4;

                float3 finalCol = (bodyCol + coreCol + glowCol) * _MainColor.rgb * pulse;

                float alpha = bodyMask * _Alpha * _MainColor.a;

                return half4(finalCol, alpha);
            }
            ENDHLSL
        }
    }
}
