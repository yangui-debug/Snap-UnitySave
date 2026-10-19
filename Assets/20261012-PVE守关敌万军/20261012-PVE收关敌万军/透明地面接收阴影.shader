// ====================================================================================
//  透明地面接收阴影.shader  （URP / Universal Render Pipeline 专用）
//
//  特性：
//    1. 地面透明度设为 0 时，地面完全透明隐形，但上方物体投下的阴影依然清晰可见；
//    2. 地面透明度 > 0 时，呈现半透明地面并接收压暗阴影；
//    3. 支持自定义阴影颜色与阴影浓度。
// ====================================================================================

Shader "自定义/透明地面接收阴影"
{
    Properties
    {
        // 地面颜色：RGB 为地面颜色，A 为基准透明度
        _GroundColor ("地面颜色", Color) = (1, 1, 1, 1)

        // 地面透明度：设为 0 时地面完全隐形，仅保留影子
        _Transparency ("地面透明度", Range(0, 1)) = 0.0

        // 阴影颜色：投射在地面的影子颜色（默认为纯黑）
        _ShadowColor ("阴影颜色", Color) = (0, 0, 0, 1)

        // 阴影强度：控制影子的深浅透明度
        _ShadowStrength ("阴影强度", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 传统半透明混合
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 主光源阴影变体支持
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GroundColor;
                float4 _ShadowColor;
                float  _Transparency;
                float  _ShadowStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 获取主光源阴影衰减（1 = 完全受光，0 = 完全处于阴影中）
                Light mainLight = GetMainLight(input.shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                // 计算阴影贡献的 Alpha（阴影越深，值越大）
                half shadowAlpha = (1.0 - shadow) * _ShadowStrength;

                // 地面材质本身的基础 Alpha
                half groundAlpha = _GroundColor.a * _Transparency;

                // 核心：最终 Alpha 由地面基础透明度和阴影透明度叠加
                // 当 _Transparency 为 0 时，无阴影处 finalAlpha 为 0（完全透明），阴影处则大于 0（显现）
                half finalAlpha = saturate(groundAlpha + shadowAlpha);

                // 计算地面受光暗化的基色
                half3 groundRgb = _GroundColor.rgb * (1.0 - shadowAlpha);

                // 按透明度权重进行颜色平滑插值，避免地面透明时地面底色反渗
                half groundWeight = (finalAlpha > 0.0001) ? (groundAlpha / finalAlpha) : 0.0;
                half3 finalColor = lerp(_ShadowColor.rgb, groundRgb, groundWeight);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
