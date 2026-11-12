Shader "CosmoForge/AtmosphericScattering"
{
    Properties
    {
        _BaseColor ("Base Surface Color", Color) = (0.2, 0.4, 0.8, 1.0)
        _AtmosphereColor ("Atmosphere Rim Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _RimPower ("Rim Exponent", Range(1.0, 8.0)) = 3.5
        _AtmosphereIntensity ("Atmosphere Intensity", Range(0.5, 4.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _AtmosphereColor;
                float _RimPower;
                float _AtmosphereIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // N dot V for rim scattering
                float NdotV = saturate(dot(normal, viewDir));
                float rim = pow(1.0 - NdotV, _RimPower) * _AtmosphereIntensity;

                float3 surface = _BaseColor.rgb * (NdotV * 0.8 + 0.2);
                float3 atmosphere = _AtmosphereColor.rgb * rim;

                return float4(surface + atmosphere, 1.0);
            }
            ENDHLSL
        }
    }
}
