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

            Tags
            {
                "LightMode"="UniversalForward"
            }

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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 atlasUV : TEXCOORD0;
                float2 tiledUV : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _Metallic;

            CBUFFER_END

            Varyings vert(
                Attributes input)
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

                return output;
            }

            half4 frag(
                Varyings input) : SV_Target
            {
                const int AtlasWidth = 512;
                const int AtlasHeight = 512;
                const int TileSize = 16;

                /*
                 * atlasUV contiene la esquina inferior
                 * izquierda del tile.
                 *
                 * La convertimos directamente a píxeles.
                 */
                int2 tileOrigin =
                    int2(
                        round(
                            input.atlasUV.x *
                            AtlasWidth
                        ),
                        round(
                            input.atlasUV.y *
                            AtlasHeight
                        )
                    );

                /*
                 * tiledUV representa la posición
                 * dentro del greedy quad.
                 *
                 * Cada unidad equivale a un bloque.
                 *
                 * Convertimos directamente a uno de
                 * los 16x16 píxeles del tile.
                 */
                float2 localUV =
                    frac(input.tiledUV);

                int2 localPixel =
                    int2(
                        floor(
                            localUV.x *
                            TileSize
                        ),
                        floor(
                            localUV.y *
                            TileSize
                        )
                    );

                /*
                 * Protección contra cualquier
                 * posible error de precisión.
                 */
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

                /*
                 * Lectura exacta de un píxel.
                 *
                 * No existe filtrado bilineal,
                 * mipmap ni mezcla con el tile vecino.
                 */
                half4 albedo =
                    _BaseMap.Load(
                        int3(
                            atlasPixel,
                            0
                        )
                    ) * _BaseColor;

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

            Tags
            {
                "LightMode"="ShadowCaster"
            }

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