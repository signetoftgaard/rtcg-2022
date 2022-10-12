Shader "Unlit/NewPhongShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // NEW we now have some custom properties
        _Ia("Ambient Color", Color) = (1,1,1,1)
        _Ka("Ambient reflection", Range(0,1)) = 0.1 // Ka
        _Kd("Diffuse reflection", Range(0,1)) = 0.5 // Kd
        // TODO add the 'Specular intensity' and the 'Specular exponent' properties 
        // use the properties above as a reference
        _Ks("Specular reflection", Range(0,5)) = 0.5 // Kd
        _exp("exponent", Range(1,50)) = 0.5 // Kd
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
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                // we need to get the normals for lighting
                float3 normal : NORMAL; 
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                // NEW we now compute the vertex position in world space, and send it to the fragment shader
                float4 vertexWorld : TEXCOORD1;
                // we need to pass the normals from the vertex to the fragment shader
                float3 normal : NORMAL; 
            };

            sampler2D _MainTex;
            // NEW we declare the properties as variables here
            float4 _Ia;
            float _Ka;
            float _Kd;
            // TODO add variables for 'Specular intensity' and the 'Specular exponent' properties 
            // remember that names must match, use the variables above as a reference
            float _Ks;
            float _exp;

            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                // we will need the normal in world space, so we multiply it with the model matrix
                o.normal = mul((float3x3)UNITY_MATRIX_M, v.normal);
                // NEW we now compute the vertex position in world space, and send it to the fragment shader
                o.vertexWorld = mul(UNITY_MATRIX_M, v.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // compute/retrieve the normal vector, the view vector and the light direction
                // they must all be in the same coordinate space, in this case that is the World space
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = normalize(i.normal);
                float3 view = normalize(_WorldSpaceCameraPos.xyz - i.vertexWorld.xyz);

                // sample the texture
                float4 color = tex2D(_MainTex, i.uv);
                
                // diffuse contribution of the light, 'max' ensures it is not negative
                float diffuse = _Kd * max(0, dot(normal, lightDirection));
                
                // TODO compute the specular contribution, 
                // you will need to compute the half vector
                // you will need the 'pow' (power), 'dot', and 'max' functions.
                // search the hlsl decomentation to learn how to use it if necessary
                float3 halfVector = normalize(view + lightDirection);
                float specular = _Ks * pow(max(0, dot(halfVector, normal)), _exp);


                // we create our return variable, the final fragment color
                fixed4 outColor = fixed4(0, 0, 0, 1);

                // combine color and intensity to create the lighting effect, currently we only have diffuse lighting
                // TODO add the ambient contribution
                // TODO add the specular contribution        
                outColor.rgb = 
                    _Ka * _Ia * color.rgb + 
                    diffuse * color.rgb * _LightColor0.rgb +
                    specular * _LightColor0.rgb;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, outColor);
                // return the color we draw
                return outColor;
            }
            ENDCG
        }
    }
}
