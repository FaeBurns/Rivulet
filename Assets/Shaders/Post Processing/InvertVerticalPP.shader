Shader "Hidden/InvertVerticalPP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct V2F
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            V2F vert (Appdata v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // invert vertically
                o.uv = float2(v.uv.x, 1 - v.uv.y);
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (V2F i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
