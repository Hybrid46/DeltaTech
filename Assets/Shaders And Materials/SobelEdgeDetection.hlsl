            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            /*
            #ifndef UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
            #define UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     
            TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);
     
            float3 SampleSceneNormals(float2 uv)
            {
                return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, UnityStereoTransformScreenSpaceTex(uv)).xy) * float3(1.0, 1.0, -1.0);
            }
     
            float3 LoadSceneNormals(uint2 uv)
            {
                return UnpackNormalOctRectEncode(LOAD_TEXTURE2D_X(_CameraNormalsTexture, uv).xy) * float3(1.0, 1.0, -1.0);
            }
            #endif
            */

            void SobelFilter_float(float4 screenUV, float2 texelSize, out float sobelEdge)
            {
                float3 gx = float3(0, 0, 0);
                float3 gy = float3(0, 0, 0);

                float2x2 mask = float2x2(-1, 1, -2, 2);

                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        float2 offset = float2(k, j) * texelSize;
                        float3 neighborNormal = SampleSceneNormals(screenUV.xy + offset).rgb * 2 - 1;

                        float weight = mask[j + 1, k + 1];
                        gx += neighborNormal * weight;
                        gy += neighborNormal * weight;
                    }
                }

                float g = length(gx) + length(gy);
s
                sobelEdge = g;
            }