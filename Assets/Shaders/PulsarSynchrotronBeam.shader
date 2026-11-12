Shader "CosmoForge/PulsarSynchrotronBeam"
{
    Properties
    {
        _BeamColor ("Synchrotron Beam Color", Color) = (0.0, 0.94, 1.0, 1.0) // Pulsar Cyan
        _CoreColor ("High-Energy Core", Color) = (1.0, 1.0, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(1.0, 10.0)) = 5.0
        _PulseFrequency ("Pulse Frequency", Range(1.0, 30.0)) = 12.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

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
                float4 _BeamColor;
                float4 _CoreColor;
                float _GlowIntensity;
                float _PulseFrequency;
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
                // Center-to-edge falloff across the beam diameter
                float distFromCenter = abs(input.uv.x - 0.5) * 2.0;
                float beamFalloff = 1.0 - smoothstep(0.0, 1.0, distFromCenter);
                float core = 1.0 - smoothstep(0.0, 0.3, distFromCenter);

                // Longitudinal decay along beam length
                float lengthFalloff = 1.0 - input.uv.y;

                // Pulsing modulation
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseFrequency * 6.28);

                float3 col = lerp(_BeamColor.rgb, _CoreColor.rgb, core);
                float alpha = saturate((beamFalloff * 0.7 + core * 0.3) * lengthFalloff * pulse);

                return float4(col * _GlowIntensity, alpha);
            }
            ENDHLSL
        }
    }
}
