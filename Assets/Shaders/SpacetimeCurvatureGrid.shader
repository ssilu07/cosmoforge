Shader "CosmoForge/SpacetimeCurvatureGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.0, 0.94, 1.0, 0.7) // Pulsar Cyan
        _DepthColor ("Deep Gravity Color", Color) = (0.55, 0.36, 0.96, 0.9) // Event Horizon Violet
        _LineWidth ("Line Width", Range(0.01, 0.1)) = 0.03
        _GlowIntensity ("Glow Intensity", Range(1.0, 5.0)) = 2.0
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
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _DepthColor;
                float _LineWidth;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Procedural grid line pattern
                float2 grid = abs(frac(input.uv * 40.0 - 0.5) - 0.5) / fwidth(input.uv * 40.0);
                float lineFactor = 1.0 - min(min(grid.x, grid.y), 1.0);

                // Depth coloring based on depression depth (y coordinate)
                float depthFactor = saturate(-input.positionWS.y * 0.05);
                float4 col = lerp(_GridColor, _DepthColor, depthFactor);

                float alpha = saturate(lineFactor * col.a * _GlowIntensity);
                return float4(col.rgb * _GlowIntensity, alpha);
            }
            ENDHLSL
        }
    }
}
