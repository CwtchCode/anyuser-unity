Shader "AnyUser/ColorblindCompensation"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _FilterMode ("Filter Mode (0=None, 1=Protanopia, 2=Deuteranopia, 3=Tritanopia, 4=Achromatopsia)", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            int _FilterMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv);
                float3 rgb = original.rgb;

                if (_FilterMode == 1)
                {
                    // Protanopia (Red-blind compensation matrix)
                    float3x3 protan = float3x3(
                        0.56667, 0.43333, 0.0,
                        0.55833, 0.44167, 0.0,
                        0.0,     0.24167, 0.75833
                    );
                    rgb = mul(protan, rgb);
                }
                else if (_FilterMode == 2)
                {
                    // Deuteranopia (Green-blind compensation matrix)
                    float3x3 deut = float3x3(
                        0.625, 0.375, 0.0,
                        0.700, 0.300, 0.0,
                        0.0,   0.300, 0.700
                    );
                    rgb = mul(deut, rgb);
                }
                else if (_FilterMode == 3)
                {
                    // Tritanopia (Blue-blind compensation matrix)
                    float3x3 trit = float3x3(
                        0.95, 0.05, 0.0,
                        0.0,  0.43333, 0.56667,
                        0.0,  0.475,   0.525
                    );
                    rgb = mul(trit, rgb);
                }
                else if (_FilterMode == 4)
                {
                    // Achromatopsia (Monochromacy / Greyscale)
                    float lum = dot(rgb, float3(0.299, 0.587, 0.114));
                    rgb = float3(lum, lum, lum);
                }

                return fixed4(rgb, original.a);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
