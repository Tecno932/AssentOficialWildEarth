Shader "WildEarth/VoxelAtlasBlock"
{
    Properties
    {
        _BaseMap ("Atlas", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 atlasUV : TEXCOORD0;
                float2 tiledUV : TEXCOORD1;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 atlasUV : TEXCOORD0;
                float2 tiledUV : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalInputs.normalWS;

                output.atlasUV =
                    input.atlasUV;

                output.tiledUV =
                    input.tiledUV;

                output.color =
                    input.color;

                return output;
            }

            // =========================================================
            // RGB -> HSV
            // =========================================================

            float3 RGBtoHSV(float3 c)
            {
                float4 K =
                    float4(
                        0.0,
                        -1.0 / 3.0,
                        2.0 / 3.0,
                        -1.0
                    );

                float4 p =
                    c.g < c.b
                        ? float4(c.bg, K.wz)
                        : float4(c.gb, K.xy);

                float4 q =
                    c.r < p.x
                        ? float4(p.xyw, c.r)
                        : float4(c.r, p.yzx);

                float d =
                    q.x -
                    min(q.w, q.y);

                float e =
                    1e-10;

                return float3(
                    abs(
                        q.z +
                        (q.w - q.y) /
                        (6.0 * d + e)
                    ),
                    d /
                    (q.x + e),
                    q.x
                );
            }

            // =========================================================
            // HSV -> RGB
            // =========================================================

            float3 HSVtoRGB(float3 hsv)
            {
                float3 p =
                    abs(
                        frac(
                            hsv.xxx +
                            float3(
                                0.0,
                                2.0 / 3.0,
                                1.0 / 3.0
                            )
                        ) * 6.0 -
                        3.0
                    );

                return hsv.z *
                    lerp(
                        float3(
                            1.0,
                            1.0,
                            1.0
                        ),
                        saturate(
                            p - 1.0
                        ),
                        hsv.y
                    );
            }

            half4 frag(Varyings input) : SV_Target
            {
                const int AtlasWidth = 512;
                const int AtlasHeight = 512;
                const int TileSize = 16;

                int2 tileOrigin = int2(
                    round(
                        input.atlasUV.x *
                        AtlasWidth
                    ),
                    round(
                        input.atlasUV.y *
                        AtlasHeight
                    )
                );

                float2 localUV =
                    frac(input.tiledUV);

                int2 localPixel = int2(
                    floor(
                        localUV.x *
                        TileSize
                    ),
                    floor(
                        localUV.y *
                        TileSize
                    )
                );

                localPixel =
                    clamp(
                        localPixel,
                        int2(0, 0),
                        int2(
                            TileSize - 1,
                            TileSize - 1
                        )
                    );

                int2 atlasPixel =
                    tileOrigin +
                    localPixel;

                // =====================================================
                // Textura original
                // =====================================================

                half4 albedo =
                    _BaseMap.Load(
                        int3(
                            atlasPixel,
                            0
                        )
                    ) *
                    _BaseColor;

                // =====================================================
                // SISTEMA DE GRISES
                // =====================================================

                float3 hsv =
                    RGBtoHSV(
                        albedo.rgb
                    );

                float brightness =
                    hsv.z;

                float saturation =
                    hsv.y;

                bool isGray =
                    saturation < 0.25 &&
                    brightness > 0.12 &&
                    brightness < 0.95;

                // =====================================================
                // COLOR DEL BIOMA
                //
                // Solamente se aplica a los píxeles grises.
                // Las partes marrones de la textura permanecen
                // con su color original.
                // =====================================================

                if (isGray)
                {
                    float3 biomeHSV =
                        RGBtoHSV(
                            input.color.rgb
                        );

                    // Conservamos el brillo original
                    // de la textura.
                    biomeHSV.z =
                        brightness;

                    // Aplicamos el tono y saturación
                    // del color del bioma.
                    albedo.rgb =
                        HSVtoRGB(
                            biomeHSV
                        );
                }

                // =====================================================
                // Lighting
                // =====================================================

                float3 normalWS =
                    normalize(
                        input.normalWS
                    );

                Light mainLight =
                    GetMainLight();

                float NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );

                half3 diffuse =
                    albedo.rgb *
                    mainLight.color *
                    NdotL;

                half3 ambient =
                    SampleSH(normalWS) *
                    albedo.rgb;

                return half4(
                    diffuse + ambient,
                    albedo.a
                );
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
            };

            ShadowVaryings vertShadow(
                ShadowAttributes input)
            {
                ShadowVaryings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                return output;
            }

            half4 fragShadow(
                ShadowVaryings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}