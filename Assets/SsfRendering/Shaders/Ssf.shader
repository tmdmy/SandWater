Shader "SampleSsf/Hidden/Ssf"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        float4 PackDepthNormal(float depth, float3 normal)
        {
            float4 packed;
            packed.xy = PackNormalOctQuadEncode(normal);
            packed.zw = PackFloatToR8G8(depth);
            return packed;
        }

        void UnpackDepthNormal(float4 packed, out float depth, out float3 normal)
        {
            normal = UnpackNormalOctQuadEncode(packed.xy);
            depth = UnpackFloatFromR8G8(packed.zw);
        }

        half3 SampleBox(float2 uv, float delta)
        {
            float4 offset = _BlitTexture_TexelSize.xyxy * float2(-delta, delta).xxyy;
            half3 s =
                SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset.xy, 0).rgb +
                SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset.zy, 0).rgb +
                SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset.xw, 0).rgb +
                SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset.zw, 0).rgb;
            return s * 0.25f;
        }
        ENDHLSL

        Pass
        {
            Name "DownSampling"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DownSamplingFrag

            half4 DownSamplingFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(SampleBox(input.texcoord, 1), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UpSampling"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment UpSamplingFrag

            half4 UpSamplingFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(SampleBox(input.texcoord, 0.5), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthNormalFrag

            uniform float4x4 _MatrixClipToView;
            uniform half _DepthThreshold;

            float3 ReconstructPosition(float2 uv, float depth)
            {
                return ComputeViewSpacePosition(uv, depth, _MatrixClipToView);
            }

            float4 DepthNormalFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float depth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.texcoord, 0).r;
                float depth01 = Linear01Depth(depth, _ZBufferParams);
                half enabled = (1 - depth01) > _DepthThreshold ? 1 : 0;

                float3 pos = ReconstructPosition(input.texcoord, depth);
                float3 n = cross(ddy(pos.xyz), ddx(pos.xyz));
                n = normalize(mul(transpose(UNITY_MATRIX_V), float4(n, 0)).rgb);
                n *= enabled;
                depth *= enabled;
                return PackDepthNormal(depth, n);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SsfLit"

            HLSLPROGRAM
            #pragma vertex SsfLitVert
            #pragma fragment SsfLitFrag

            uniform half _DistortionStrength;
            uniform half4 _Tint;
            uniform half4 _AmbientColor;
            uniform half4 _SpecColor;
            uniform half _Gloss;

            uniform half4 _EdgeColor;
            uniform half _EdgeScaleFactor;
            uniform half _EdgeDepthThreshold;
            uniform half _EdgeNormalThreshold;

            TEXTURE2D_X(_SsfDepthNormalTexture);
            SAMPLER(sampler_SsfDepthNormalTexture);

            struct LitVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float2 uv4 : TEXCOORD4;
                float3 viewDirVS : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            LitVaryings SsfLitVert(Attributes input)
            {
                LitVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
                uv = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);

                output.positionCS = pos;
                output.uv0 = uv;

                float2 texelOffset = _BlitTexture_TexelSize.xy * _EdgeScaleFactor;
                output.uv1 = uv + texelOffset * float2( 1,  1);
                output.uv2 = uv + texelOffset * float2(-1, -1);
                output.uv3 = uv + texelOffset * float2(-1,  1);
                output.uv4 = uv + texelOffset * float2( 1, -1);

                output.viewDirVS = float3(0, 0, 1);
                return output;
            }

            half EdgeDetection(LitVaryings input)
            {
                float3 n1, n2, n3, n4;
                float d1, d2, d3, d4;

                float4 dn1 = SAMPLE_TEXTURE2D_X_LOD(_SsfDepthNormalTexture, sampler_SsfDepthNormalTexture, input.uv1, 0);
                float4 dn2 = SAMPLE_TEXTURE2D_X_LOD(_SsfDepthNormalTexture, sampler_SsfDepthNormalTexture, input.uv2, 0);
                float4 dn3 = SAMPLE_TEXTURE2D_X_LOD(_SsfDepthNormalTexture, sampler_SsfDepthNormalTexture, input.uv3, 0);
                float4 dn4 = SAMPLE_TEXTURE2D_X_LOD(_SsfDepthNormalTexture, sampler_SsfDepthNormalTexture, input.uv4, 0);

                UnpackDepthNormal(dn1, d1, n1);
                UnpackDepthNormal(dn2, d2, n2);
                UnpackDepthNormal(dn3, d3, n3);
                UnpackDepthNormal(dn4, d4, n4);

                float nDotV = dot(n1, input.viewDirVS);

                float depthDifference1 = d2 - d1;
                float depthDifference2 = d4 - d3;
                float edgeDepth = sqrt(dot(depthDifference1, depthDifference1) + dot(depthDifference2, depthDifference2)) * 100;
                edgeDepth = edgeDepth > _EdgeDepthThreshold ? 1 : 0;

                float3 normalDifference1 = n2 - n1;
                float3 normalDifference2 = n4 - n3;
                float edgeNormal = sqrt(dot(normalDifference1, normalDifference1) + dot(normalDifference2, normalDifference2));
                edgeNormal = edgeNormal > _EdgeNormalThreshold ? 1 : 0;

                return edgeNormal * edgeDepth * (nDotV > 0.4 ? 1 : 0);
            }

            half4 SsfLitFrag(LitVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv0;
                float4 depthNormal = SAMPLE_TEXTURE2D_X_LOD(_SsfDepthNormalTexture, sampler_SsfDepthNormalTexture, uv, 0);
                float depth;
                float3 n;
                UnpackDepthNormal(depthNormal, depth, n);

                half enabled = depth > 0 ? 1 : 0;

                half nDotL = dot(_MainLightPosition.xyz, n);
                float lightIntensity = smoothstep(0, 0.01, nDotL);
                float4 light = lightIntensity * _MainLightColor;

                float3 halfVector = normalize(_MainLightPosition.xyz + input.viewDirVS);
                float nDotH = dot(halfVector, n);
                float specularIntensity = pow(abs(nDotH * lightIntensity), _Gloss * _Gloss);
                specularIntensity = smoothstep(0.005, 0.01, specularIntensity);

                half edge = EdgeDetection(input);

                float2 uvScreenOffset = n.xy * _DistortionStrength * enabled;
                float2 uvDist = uv + uvScreenOffset;
                half4 screen = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uvDist, 0);

                half4 color = lerp(_Tint * (_AmbientColor + light), _SpecColor, specularIntensity);
                color = lerp(screen, color, color.a * enabled);
                color = lerp(color, _EdgeColor, edge);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
