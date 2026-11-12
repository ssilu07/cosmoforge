Shader "CosmoForge/GravitationalLensing"
{
    Properties
    {
        _SchwarzschildRadius ("Schwarzschild Radius", Float) = 2.0
        _LensingStrength ("Lensing Deflection Strength", Range(0.1, 5.0)) = 1.5
        _RingColor ("Einstein Ring Glow Color", Color) = (0.7, 0.85, 1.0, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
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
                float _SchwarzschildRadius;
                float _LensingStrength;
                float4 _RingColor;
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
                // Centered radial coordinates [-1, 1]
                float2 coord = (input.uv - 0.5) * 2.0;
                float r = length(coord);

                // Black hole event horizon shadow: completely black inside Rs
                if (r < 0.25)
                {
                    return float4(0.0, 0.0, 0.0, 1.0);
                }

                // Einstein Ring: maximum deflection at the photon sphere (r ~ 1.5 * Rs)
                // Light rays bend around the photon sphere creating a sharp luminous ring
                float photonSphere = 0.38;
                float ringIntensity = exp(-pow(r - photonSphere, 2.0) / 0.003) * _LensingStrength;

                // Secondary diffuse deflection fringe
                float fringe = smoothstep(0.25, 0.8, r) * (1.0 - smoothstep(0.8, 1.0, r)) * 0.3;

                float alpha = saturate(ringIntensity + fringe);
                return float4(_RingColor.rgb * (ringIntensity * 2.0 + fringe), alpha);
            }
            ENDHLSL
        }
    }
}
