Shader "Unlit/NewCelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.normal = mul((float3x3)UNITY_MATRIX_M, v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float intensity = max(0, dot(i.normal, lightDirection));
                float4 col = tex2D(_MainTex, i.uv);
                col.rgb = col.rgb * intensity;
                // we make the value have the range [1-5], and then take the floor operation 
                col.rgb = floor(col.rgb * 4.0 + 1.0);
                // we divide by 4, so our values are .25, .5, .75 and 1. 
                col.rgb = col.rgb * 0.25;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                // values above 1 are clamped to 1
                return col;
            }
            ENDCG
        }
    }
}
