Shader "URP/GrassTintFlatBalanced"
{
    Properties
    {
        _BaseMap ("Atlas Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.45, 0.63, 0.28, 1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.55

        _SaturationThreshold ("Max Saturation to Tint", Range(0,1)) = 0.25
        _MinBrightness ("Min Brightness", Range(0,1)) = 0.12
        _MaxBrightness ("Max Brightness", Range(0,1)) = 0.95
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 100

        Pass
        {
            Name "GrassTint"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float  fogFactor   : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _TintColor;
            float _TintStrength;
            float _SaturationThreshold;
            float _MinBrightness;
            float _MaxBrightness;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // 🔒 Forzar opacidad total
                col.a = 1.0;

                float3 hsv = RGBtoHSV(col.rgb);
                float brightness = hsv.z;
                float saturation = hsv.y;

                bool isGray =
                    saturation < _SaturationThreshold &&
                    brightness > _MinBrightness &&
                    brightness < _MaxBrightness;

                if (isGray)
                {
                    float3 tinted =
                        pow(
                            lerp(pow(col.rgb, 2.2), pow(_TintColor.rgb, 2.2), _TintStrength),
                            1.0 / 2.2
                        );

                    col.rgb = lerp(col.rgb, tinted, 0.8);
                }

                // Flat look
                col.rgb *= 0.95;

                // Fog
                col.rgb = MixFog(col.rgb, IN.fogFactor);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}