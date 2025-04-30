Shader "Hidden/ContrastPP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass // FXAA pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            V2F Vert (Appdata v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct LuminanceData
            {
                // center, north, east, south, west
                float m, n, e, s, w;
                float highest, lowest, contrast;
            };

            float4 SampleLuminance(float2 uv)
            {
                return tex2D(_MainTex, uv);
            }

            float4 SampleLuminance(float2 uv, float uOffset, float vOffset)
            {
                uv += _MainTex_TexelSize * float2(uOffset, vOffset);
                return SampleLuminance(uv);
            }

            LuminanceData SampleLuminanceNeighborhood(float2 uv)
            {
                LuminanceData l;
                l.m = SampleLuminance(uv);
                // get luminance in cardinal directions
                l.n = SampleLuminance(uv, 0, 1);
                l.e = SampleLuminance(uv, 1, 0);
                l.s = SampleLuminance(uv, 0, -1);
                l.w = SampleLuminance(uv, -1, 0);

                // get highest, lowest, and contrast values
                l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
                l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
                l.contrast = l.highest - l.lowest;

                return l;
            }

            float4 Frag (V2F i) : SV_Target
            {
                LuminanceData l = SampleLuminanceNeighborhood(i.uv);
                return l.contrast;
            }

            ENDCG
        }
    }
}