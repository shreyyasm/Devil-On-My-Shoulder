Shader "DrawingDeath/Tutorial Button Polygon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Flat Polygon Color", Color) = (0, 0, 0, 1)
        _VertexDriftSpeed ("Vertex Drift Speed", Range(0, 3)) = 0.65
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.05)) = 0.012
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float _VertexDriftSpeed;
            float _EdgeSoftness;
            float4 _ClipRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 world : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.world = v.vertex;
                return o;
            }

            float Cross2D(float2 first, float2 second)
            {
                return first.x * second.y - first.y * second.x;
            }

            float2 DriftingCorner(float2 basePosition, float phase, float time)
            {
                float2 drift = float2(sin(time + phase), cos(time * .77 + phase * 1.63));
                return basePosition + drift * .045;
            }

            float SignedEdgeDistance(float2 edgeStart, float2 edgeEnd, float2 position)
            {
                return Cross2D(edgeEnd - edgeStart, position - edgeStart) /
                       max(length(edgeEnd - edgeStart), .0001);
            }

            float DynamicPolygonMask(float2 uv)
            {
                float time = _Time.y * _VertexDriftSpeed;
                float2 bottomLeft = DriftingCorner(float2(-.48, -.48), .0, time);
                float2 bottomRight = DriftingCorner(float2(.48, -.48), 1.7, time);
                float2 topRight = DriftingCorner(float2(.48, .48), 3.4, time);
                float2 topLeft = DriftingCorner(float2(-.48, .48), 5.1, time);
                float2 polygonPosition = uv - .5;

                float nearestEdge = min(
                    min(SignedEdgeDistance(bottomLeft, bottomRight, polygonPosition),
                        SignedEdgeDistance(bottomRight, topRight, polygonPosition)),
                    min(SignedEdgeDistance(topRight, topLeft, polygonPosition),
                        SignedEdgeDistance(topLeft, bottomLeft, polygonPosition)));
                return smoothstep(0.0, _EdgeSoftness, nearestEdge);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float polygon = DynamicPolygonMask(i.uv);
                fixed4 colour = _Color;
                colour.a *= tex2D(_MainTex, i.uv).a * polygon *
                            UnityGet2DClipping(i.world.xy, _ClipRect);
                return colour * i.color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
