Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Reveal ("Reveal", Range(0,1)) = 0
        _Softness ("Reveal Softness", Range(0,0.1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // --------------------------------------------------
            // STRUCTS
            // --------------------------------------------------

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


            // --------------------------------------------------
            // TEXTURE
            // --------------------------------------------------

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            // --------------------------------------------------
            // MATERIAL PROPERTIES
            // --------------------------------------------------

            CBUFFER_START(UnityPerMaterial)

                float4 _Color;
                float _Reveal;
                float _Softness;

            CBUFFER_END


            // --------------------------------------------------
            // VERTEX
            // --------------------------------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                OUT.color =
                    IN.color * _Color;

                return OUT;
            }


            // --------------------------------------------------
            // FRAGMENT
            // --------------------------------------------------

            half4 frag(Varyings IN) : SV_Target
            {
                // Get the sprite texture
                half4 col = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );


                // --------------------------------------------------
                // TOP → BOTTOM REVEAL
                // --------------------------------------------------
                //
                // Unity UV:
                //
                //       UV Y = 1
                //       ┌──────────────┐
                //       │     TOP      │
                //       │              │
                //       │              │
                //       │   BOTTOM     │
                //       └──────────────┘
                //       UV Y = 0
                //
                // We invert UV Y so that:
                //
                // revealY = 0  → TOP
                // revealY = 1  → BOTTOM
                //

                float revealY = 1.0 - IN.uv.y;


                // --------------------------------------------------
                // HARD REVEAL
                // --------------------------------------------------

                float alpha;

                if (_Softness <= 0.0001)
                {
                    alpha = step(
                        revealY,
                        _Reveal
                    );
                }

                // --------------------------------------------------
                // SOFT REVEAL EDGE
                // --------------------------------------------------

                else
                {
                    alpha = smoothstep(
                        _Reveal - _Softness,
                        _Reveal + _Softness,
                        revealY
                    );
                }


                // Apply reveal to sprite alpha
                col.a *= alpha;


                // Apply SpriteRenderer color
                col *= IN.color;


                return col;
            }

            ENDHLSL
        }
    }
}
