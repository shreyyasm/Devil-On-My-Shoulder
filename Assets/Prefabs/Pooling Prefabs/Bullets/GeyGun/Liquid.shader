Shader "Custom/LiquidWobbleNew"
{
    Properties
    {
        // Original fill settings
        _Fill("Fill", Range(0,1)) = 0.5
        _FillMin("Fill Min", Float) = -0.5
        _FillMax("Fill Max", Float) = 0.5

        // Colors
        _Color("Color", Color) = (1, 0.85, 0.45, 1) // base beer tint
        _EmissionColor("Emission Color", Color) = (1, 0.75, 0.25, 1)

        // Beer gradient tints
        _BeerTopTint("Top Tint", Color) = (1.0, 0.9, 0.5, 1)
        _BeerBottomTint("Bottom Tint", Color) = (0.9, 0.5, 0.1, 1)

        // Foam
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamSoftness("Foam Softness", Range(0.01, 0.2)) = 0.05
        _FoamHeight("Foam Height", Range(0.001, 0.2)) = 0.05

        // Bubbles
        _BubbleColor("Bubble Color", Color) = (1, 1, 1, 1)
        _BubbleSize("Bubble Size", Range(0.005, 0.05)) = 0.02
        _BubbleSpeed("Bubble Speed", Range(0.1, 3)) = 0.5
        _BubbleDensity("Bubble Density", Range(1, 20)) = 8

        // Wobble
        _WobbleX("WobbleX", Float) = 0
        _WobbleY("WobbleY", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Fill;
            float _FillMin;
            float _FillMax;
            float4 _Color;
            float4 _EmissionColor;

            float4 _BeerTopTint;
            float4 _BeerBottomTint;

            float4 _FoamColor;
            float _FoamSoftness;
            float _FoamHeight;

            float4 _BubbleColor;
            float _BubbleSize;
            float _BubbleSpeed;
            int _BubbleDensity;

            float _WobbleX;
            float _WobbleY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float worldY : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;

                // Keep your wobble
                float wobbleOffset = sin(_Time.y * 3 + pos.x * 5) * _WobbleX +
                                     cos(_Time.y * 2 + pos.y * 4) * _WobbleY;
                pos.y += wobbleOffset * 0.02;

                o.vertex = UnityObjectToClipPos(float4(pos, 1));
                o.uv = v.uv;
                o.worldY = pos.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fill mask
                float fillY = lerp(_FillMin, _FillMax, _Fill);
                float insideLiquid = step(i.worldY, fillY); // 1 if inside, 0 if above

                // Height factor for beer gradient
                float heightNorm = saturate((i.worldY - _FillMin) / (_FillMax - _FillMin));
                float3 beerTint = lerp(_BeerBottomTint.rgb, _BeerTopTint.rgb, heightNorm);

                // Base beer color
                float3 baseBeer = _Color.rgb * beerTint;

                // Foam near surface
                float foamMask = smoothstep(fillY + _FoamHeight, fillY, i.worldY + _FoamSoftness);
                float3 foamCol = _FoamColor.rgb * foamMask;

                // Procedural bubbles
                float bubbleSum = 0;
                for (int b = 0; b < _BubbleDensity; b++)
                {
                    float2 seed = float2(frac(sin(b * 12.9898) * 43758.5453),
                                         frac(sin((b + 10) * 78.233) * 12345.6789));
                    float2 bubbleUV = frac(i.uv * float2(3, 6) + seed);
                    bubbleUV.y = frac(bubbleUV.y + _Time.y * _BubbleSpeed);
                    float2 d = bubbleUV - 0.5;
                    float dist = length(d);
                    bubbleSum += smoothstep(_BubbleSize, 0.0, dist);
                }
                bubbleSum /= _BubbleDensity;
                float3 bubbleCol = _BubbleColor.rgb * bubbleSum * insideLiquid;

                // Combine beer + bubbles inside liquid
                float3 finalCol = baseBeer * insideLiquid + bubbleCol + foamCol;

                // Emission inside liquid
                finalCol += _EmissionColor.rgb * insideLiquid * 0.2;

                // Alpha: inside liquid or foam
                float alpha = saturate(insideLiquid + foamMask);

                return float4(finalCol, alpha);
            }
            ENDCG
        }
    }
}