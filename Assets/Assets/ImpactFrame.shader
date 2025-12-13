Shader "Hidden/BlackWhiteThreshold"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Threshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample screen
                fixed4 col = tex2D(_MainTex, i.uv);

                // Convert to grayscale brightness
                float gray = dot(col.rgb, float3(0.3, 0.59, 0.11));

                // Threshold to black or white
                float bw = step(_Threshold, gray);

                return float4(bw, bw, bw, 1);
            }
            ENDCG
        }
    }
}
