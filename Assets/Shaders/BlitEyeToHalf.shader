﻿Shader "Hidden/BlitEyeToHalf"
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _eyeIndex;

            fixed4 frag(v2f i) : SV_Target
            {
                if (_eyeIndex == 0) // left eye
                {
                    // skip right half
                    clip(0.5 - i.uv.x);
                    i.uv.x *= 2;
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                else // right eye
                {
                    // skip left half
                    clip(i.uv.x - 0.5);
                    i.uv.x *= 2;
                    i.uv.x -= 1;
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
            }
            ENDCG
        }
    }
}