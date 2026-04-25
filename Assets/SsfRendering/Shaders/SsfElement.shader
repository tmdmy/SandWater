Shader "SampleSsf/SsfElement"
{
    Properties
    {
        _Radius("Sphere Radius", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "SsfBillboardSphereDepth"
            Tags { "LightMode" = "SsfBillboardSphereDepth" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex PassVertex
            #pragma fragment PassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half _Radius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionVS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings PassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Camera-facing billboard at object origin in view space
                float3 originVS = mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1)).xyz;
                float3 offsetVS = float3(input.positionOS.x, input.positionOS.y, 0) * (_Radius * 2.0);
                output.positionVS = originVS + offsetVS;
                output.positionCS = mul(UNITY_MATRIX_P, float4(output.positionVS, 1));
                output.uv = input.uv;
                return output;
            }

            float4 PassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 st = input.uv * 2.0 - 1.0;
                half d2 = dot(st, st);
                clip(d2 > 1 ? -1 : 1);

                // Reconstruct sphere VS position from billboard
                float3 sphereOffsetVS = float3(st.xy, sqrt(1 - d2));
                float3 spherePosVS = input.positionVS + sphereOffsetVS * _Radius;
                float4 spherePosCS = mul(UNITY_MATRIX_P, float4(spherePosVS, 1));

                float depth = spherePosCS.z / spherePosCS.w;
                return float4(depth, depth, depth, 1);
            }
            ENDHLSL
        }
    }
}
