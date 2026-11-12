Shader "CosmoForge/GasGiantBands"
{
    Properties
    {
        _BandColorA ("Equatorial Band Color", Color) = (0.85, 0.65, 0.45, 1.0)
        _BandColorB ("Temperate Band Color", Color) = (0.6, 0.4, 0.25, 1.0)
        _StormColor ("Great Red Spot Color", Color) = (0.9, 0.25, 0.15, 1.0)
        _BandFrequency ("Band Frequency", Range(4.0, 32.0)) = 16.0
        _ZonalSpeed ("Zonal Wind Speed", Range(0.01, 0.2)) = 0.05
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
                float4 _BandColorA;
                float4 _BandColorB;
                float4 _StormColor;
                float _BandFrequency;
                float _ZonalSpeed;
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

                // Alternating zonal bands sheared by latitude and time
                float timeOffset = _Time.y * _ZonalSpeed;
                float latitude = input.uv.y;
                float band = sin(latitude * _BandFrequency + sin(input.uv.x * 6.28 + timeOffset) * 0.2) * 0.5 + 0.5;

                float3 surfaceColor = lerp(_BandColorA.rgb, _BandColorB.rgb, band);

                // Great Red Spot cyclonic vortex centered around UV (0.6, 0.35)
                float2 stormCenter = float2(frac(0.6 + timeOffset * 0.3), 0.35);
                float2 stormDelta = input.uv - stormCenter;
                stormDelta.x *= 2.0; // Elliptical storm
                float stormDist = length(stormDelta);
                float stormMask = smoothstep(0.12, 0.04, stormDist);

                surfaceColor = lerp(surfaceColor, _StormColor.rgb, stormMask * 0.85);

                float NdotV = saturate(dot(normal, viewDir));
                float limbDarkening = pow(NdotV, 0.6);

                return float4(surfaceColor * (limbDarkening * 0.8 + 0.2), 1.0);
            }
            ENDHLSL
        }
    }
}
