Shader "CosmoForge/AccretionDisk"
{
    Properties
    {
        _BlueShiftColor ("Approaching Blue-Shift Color", Color) = (0.3, 0.7, 1.0, 1.0)
        _RedShiftColor ("Receding Red-Shift Color", Color) = (1.0, 0.35, 0.1, 1.0)
        _CoreDiskColor ("Hot Plasma Core", Color) = (1.0, 0.95, 0.8, 1.0)
        _DopplerFactor ("Doppler Beaming Factor", Range(1.0, 4.0)) = 2.0
        _RotationSpeed ("Swirl Speed", Range(0.1, 2.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BlueShiftColor;
                float4 _RedShiftColor;
                float4 _CoreDiskColor;
                float _DopplerFactor;
                float _RotationSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 centerCoord = (input.uv - 0.5) * 2.0;
                float r = length(centerCoord);

                // Annular disk bounds (from ISCO ~0.25 to outer edge ~0.95)
                float innerEdge = 0.25;
                float outerEdge = 0.95;
                if (r < innerEdge || r > outerEdge)
                {
                    discard;
                }

                // Swirl noise / spiral arms
                float angle = atan2(centerCoord.y, centerCoord.x);
                float spiral = sin(angle * 4.0 - r * 15.0 + _Time.y * _RotationSpeed * 6.28) * 0.5 + 0.5;

                // Relativistic Doppler beaming:
                // Approaching side (x < 0 or sin(angle) < 0 depending on rotation direction)
                // is amplified by relativistic beaming factor D^4, receding side is dimmed.
                float dopplerTerm = -cos(angle); // Positive on approaching side, negative on receding
                float beamingIntensity = pow(saturate(1.0 + dopplerTerm * (_DopplerFactor - 1.0) * 0.5), 3.0);

                // Radial temperature falloff (hottest at innermost circular orbit)
                float radialHotness = 1.0 - smoothstep(innerEdge, outerEdge, r);

                // Color gradient from Doppler blue to Doppler red
                float3 baseTone = lerp(_RedShiftColor.rgb, _BlueShiftColor.rgb, saturate(dopplerTerm * 0.5 + 0.5));
                float3 diskColor = lerp(baseTone, _CoreDiskColor.rgb, radialHotness * 0.6);

                float alpha = saturate((1.0 - smoothstep(outerEdge - 0.1, outerEdge, r)) * smoothstep(innerEdge, innerEdge + 0.05, r) * (0.6 + 0.4 * spiral) * beamingIntensity);

                return float4(diskColor * beamingIntensity * 1.5, alpha);
            }
            ENDHLSL
        }
    }
}
