Shader "Devil On My Shoulder/Shop Intro Speed Lines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0, 0.6470588, 1, 1)
        _Color ("Scratch Color", Color) = (1, 1, 1, 1)
        _BlueScratchTint ("Blue Scratch Tint", Color) = (0.05, 0.48, 1, 1)
        _Speed ("Travel Speed", Range(0, 4)) = 1.35
        _LineCount ("Scratch Count", Range(3, 12)) = 8
        _Intensity ("Intensity", Range(0, 3)) = 1.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpeedLines"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BlueScratchTint;
                float4 _BackgroundColor;
                float _Speed;
                float _LineCount;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float Hash(float value)
            {
                return frac(sin(value * 127.1) * 43758.5453);
            }

            // Interpolated noise provides an uneven, worn edge for every claw mark.
            float ValueNoise(float value)
            {
                float cell = floor(value);
                float fraction = frac(value);
                fraction = fraction * fraction * (3.0 - 2.0 * fraction);
                return lerp(Hash(cell), Hash(cell + 1.0), fraction);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 scratches = 0.0;
                float time = _Time.y * _Speed;

                // Independent streak blocks are packed into separate vertical
                // lanes. Each one has a different size, position, rough edge,
                // and colour, instead of reading as repeated claw groups.
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    if (i >= (int)_LineCount)
                        break;

                    float seed = (float)i + 1.0;
                    // The lane is fixed for the current streak, which prevents
                    // long pieces from crossing or overlapping each other.
                    float laneY = ((float)i + .5) / max(_LineCount, 1.0);
                    float laneOffset = lerp(-.025, .025, Hash(seed * 2.17));
                    float speed = lerp(.35, 1.35, Hash(seed * 7.71));
                    float localX = frac(input.uv.x - time * speed + Hash(seed * 11.19));
                    float length = lerp(.12, .68, Hash(seed * 5.93));
                    float body = smoothstep(0.0, .018, localX) *
                                 (1.0 - smoothstep(length, length + .018, localX));
                    float angle = lerp(-.11, .11, Hash(seed * 1.91));
                    float y = laneY + laneOffset + (localX - length * .5) * angle;
                    float thickness = lerp(.009, .043, Hash(seed * 4.31));
                    float edgeWear = ValueNoise(localX * 90.0 + seed * 9.7);
                    float raggedThickness = thickness * lerp(.65, 1.22, edgeWear);
                    float vertical = 1.0 - smoothstep(raggedThickness, raggedThickness * 1.34,
                                                      abs(input.uv.y - y));
                    float chipped = lerp(.62, 1.0, ValueNoise(localX * 230.0 + seed * 3.1));
                    float tintPick = Hash(seed * 11.3);
                    float3 tint = lerp(_BlueScratchTint.rgb, _Color.rgb,
                                        smoothstep(.25, .82, tintPick));
                    scratches += body * vertical * chipped * tint;
                }

                float mask = sprite.a * input.color.a;
                float3 background = _BackgroundColor.rgb * input.color.rgb;
                float3 speedLines = saturate(scratches) * input.color.rgb * _Intensity;
                float alpha = mask * _BackgroundColor.a;
                float3 colour = background + speedLines;
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
