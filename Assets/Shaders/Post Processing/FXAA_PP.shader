// based on https://catlikecoding.com/unity/tutorials/custom-srp/fxaa/

Shader "Hidden/FXAAPP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
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

    float4 Sample(float2 uv)
    {
        return tex2Dlod(_MainTex, float4(uv, 0, 0));
    }

    V2F Vert (Appdata v)
    {
        V2F o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }
    ENDCG

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass // Luminance pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Preparse_frag

            float4 Preparse_frag (V2F i) : SV_Target
            {
                // get color of pixel and use that to calculate luminance
                // store luminance in the alpha channel as it's needed in the next pass
                i.uv.y = 1 - i.uv.y;
                float4 col = tex2D(_MainTex, i.uv);
                col.a = LinearRgbToLuminance(col.rgb);
                return col;
            }

            ENDCG
        }

        Pass // FXAA pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ LOW_QUALITY

            float _ContrastThreshold;
            float _RelativeThreshold;
            float _SubpixelBlending;

            #if defined(LOW_QUALITY)
            #define EDGE_STEP_COUNT 4
            #define EDGE_STEPS 1, 1.5, 2, 4
            #define EDGE_GUESS 12
            #else
            #define EDGE_STEP_COUNT 10
            #define EDGE_STEPS 1, 1.5, 2, 2, 2, 2, 2, 2, 2, 4
            #define EDGE_GUESS 8
            #endif

            static const float edgeSteps[EDGE_STEP_COUNT] = { EDGE_STEPS };

            struct LuminanceData
            {
                // center, north, east, south, west
                float m, n, e, s, w;
                float ne, nw, se, sw;
                float highest, lowest, contrast;
            };

            struct EdgeData
            {
                bool isHorizontal;
                float pixelStep;
                float oppositeLuminance;
                float gradient;
            };

            float SampleLuminance(float2 uv)
            {
                return Sample(uv).a;
            }

            float SampleLuminance(float2 uv, float uOffset, float vOffset)
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

                // get luminance on diagonals
                l.ne = SampleLuminance(uv,  1,  1);
			    l.nw = SampleLuminance(uv, -1,  1);
			    l.se = SampleLuminance(uv,  1, -1);
			    l.sw = SampleLuminance(uv, -1, -1);

                // get highest, lowest, and contrast values
                l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
                l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
                l.contrast = l.highest - l.lowest;

                return l;
            }

            bool ShouldSkipPixel(LuminanceData l)
            {
                float threshold = max(_ContrastThreshold, _RelativeThreshold * l.highest);
                return l.contrast < threshold;
            }

            float4 DeterminePixelBlendFactor(LuminanceData l)
            {
                // accumulated cardinals
                // with double weight
                float filter = 2 * (l.n + l.e + l.s + l.w);
                // single weight on diagonals
                filter += l.ne + l.nw + l.se + l.sw;
                // scale to a 12th
                filter *= 1.0 / 12;
                // filter on high pass
                filter = abs(filter - l.m);
                // normalize
                filter = saturate(filter / l.contrast);
                // do a squared smoth to smooth out harsh edges
                float blendFactor = smoothstep(0, 1, filter);
                return blendFactor * blendFactor * _SubpixelBlending;
            }

            EdgeData DetermineEdge(LuminanceData l)
            {
                // calculate the edge by determine whether the horizontal or vertical edge has a higher contrast
                EdgeData e;
                float horizontal =
				    abs(l.n + l.s - 2 * l.m) * 2 +
				    abs(l.ne + l.se - 2 * l.e) +
				    abs(l.nw + l.sw - 2 * l.w);
			    float vertical =
				    abs(l.e + l.w - 2 * l.m) * 2 +
				    abs(l.ne + l.nw - 2 * l.n) +
				    abs(l.se + l.sw - 2 * l.s);
			    e.isHorizontal = horizontal >= vertical;

                // positive and negative luminance
                float pLuminance = e.isHorizontal ? l.n : l.e;
                float nLuminance = e.isHorizontal ? l.s : l.w;
                float pGradient = abs(pLuminance - l.m);
                float nGradient = abs(nLuminance - l.m);

                e.pixelStep = e.isHorizontal ? _MainTex_TexelSize.y : _MainTex_TexelSize.x;
                if (pGradient < nGradient)
                {
                    e.pixelStep = -e.pixelStep;
                    e.oppositeLuminance = nLuminance;
                    e.gradient = nGradient;
                }
                else
                {
                    e.oppositeLuminance = pLuminance;
                    e.gradient = pGradient;
                }

                return e;
            }

            float DetermineEdgeBlendFactor(LuminanceData l, EdgeData e, float2 uv)
            {
                float2 uvEdge = uv;
                float2 edgeStep;
                if (e.isHorizontal)
                {
                    uvEdge.y += e.pixelStep * 0.5f;
                    edgeStep = float2(_MainTex_TexelSize.x, 0);
                }
                else
                {
                    uvEdge.x += e.pixelStep * 0.5f;
                    edgeStep = float2(0, _MainTex_TexelSize.y);
                }

                // get luminance on the positive edge
                float edgeLuminance = (l.m + e.oppositeLuminance) * 0.5;
                float gradientThreshold = e.gradient * 0.25;
                float2 puv = uvEdge + edgeStep * edgeSteps[0];
                float pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                bool pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;

                // step 9 more pixels along the edge and test them
                UNITY_UNROLL
                for(int i = 1; i < EDGE_STEP_COUNT && !pAtEnd; i++)
                {
                    puv += edgeStep * edgeSteps[i];
                    pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                    pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                }
                if (!pAtEnd) // if the end of the edge was not found then it's garuanteed to be at least one pixel further
                    puv += edgeStep * EDGE_GUESS;

                // the same needs to be done with the negative edge
                float2 nuv = uvEdge - edgeStep * edgeSteps[0];
                float nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                bool nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;

                UNITY_UNROLL
                for (int i = 1; i < EDGE_STEP_COUNT && !nAtEnd; i++)
                {
                    nuv -= edgeStep * edgeSteps[0];
                    nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                    nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                }
                if (!nAtEnd)
                    nuv -= edgeStep * EDGE_GUESS;

                float pDistance, nDistance;
                if (e.isHorizontal)
                {
                    pDistance = puv.x - uv.x;
                    nDistance = uv.x - nuv.x;
                }
                else
                {
                    pDistance = puv.y - uv.y;
                    nDistance = uv.y - nuv.y;
                }

                // distance to the end of the nearest edge
                float shortestDistance;
                bool deltaSign;
                if (pDistance <= nDistance)
                {
                    shortestDistance = pDistance;
                    deltaSign = pLuminanceDelta >= 0;
                }
                else
                {
                    shortestDistance = nDistance;
                    deltaSign = nLuminanceDelta >= 0;
                }

                // only blend on one side of the edge
                if (deltaSign == (l.m - edgeLuminance >= 0))
                {
                    return 0;
                }

                return 0.5 - shortestDistance / (pDistance + nDistance);
            }

            float4 Frag (V2F i) : SV_Target
            {
                LuminanceData l = SampleLuminanceNeighborhood(i.uv);

                if (ShouldSkipPixel(l))
                {
                    return Sample(i.uv);
                }

                float pixelBlend = DeterminePixelBlendFactor(l);
                EdgeData e = DetermineEdge(l);
                float edgeBlend = DetermineEdgeBlendFactor(l, e, i.uv);
                float finalBlend = max(pixelBlend, edgeBlend);

                if (e.isHorizontal)
                {
                    i.uv.y += e.pixelStep * finalBlend;
                }
                else
                {
                    i.uv.x += e.pixelStep * finalBlend;
                }
                return float4(Sample(i.uv).rgb, l.m);
            }

            ENDCG
        }
    }
}
