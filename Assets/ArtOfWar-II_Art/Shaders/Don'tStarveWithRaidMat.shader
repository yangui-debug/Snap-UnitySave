Shader "Raid/DontStarveWithRaidMat"
{
    Properties
    {
        _DailyTexture("DailyTexture", 2D) = "white" {}
        _NightTexture("NightTexture", 2D) = "white" {}
        [HDR]_BaseColor ("Color", Color) = (1,1,1,1)

        _LineTexture("LineTexture", 2D) = "black" {}
        _LineTilling("LineTilling", Float) = 16.66
        _LineRange("LineRange", Range(-0.05, 0.05)) = 0.01
        [HDR]_DarkColor("DarkColor", Color) = (1,1,1,0)
        _DarkValue("DarkValue", Range(0, 1)) = 0
        _Emissive("Emissive", 2D) = "white" {}
        [HDR]_EmissiveColor("EmissiveColor", Color) = (1,1,1,0)
        [HideInInspector] _texcoord("", 2D) = "white" {}

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineSize("Outline Size", Range(.001, 0.5)) = 0.1
        _OutlineMultiplier("Outline Multiplier", Range(0, 1)) = .5
        [Toggle] _ApplyScale("Apply Scale", Float) = 0
    }

    HLSLINCLUDE
    uniform float _DayToNight; // 定义全局变量
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend Off
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 worldPos : TEXCOORD3;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DailyTexture_ST;
                float4 _NightTexture_ST;
                float4 _BaseColor;
                float4 _Emissive_ST;
                float _LineTilling;
                float _LineRange;
                float4 _DarkColor;
                float _DarkValue;
                float4 _EmissiveColor;
            CBUFFER_END

            TEXTURE2D(_DailyTexture);
            TEXTURE2D(_NightTexture);
            TEXTURE2D(_LineTexture);
            TEXTURE2D(_Emissive);
            SAMPLER(sampler_DailyTexture);
            SAMPLER(sampler_NightTexture);
            SAMPLER(sampler_LineTexture);
            SAMPLER(sampler_Emissive);

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalWS = normalWS;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldPos = worldPos;

                output.positionCS = TransformWorldToHClip(worldPos);

                float4 screenPos = ComputeScreenPos(output.positionCS);
                output.screenPos = screenPos;

                output.uv = input.texcoord.xy;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv_DailyTexture = input.uv * _DailyTexture_ST.xy + _DailyTexture_ST.zw;
                float2 uv_NightTexture = input.uv * _NightTexture_ST.xy + _NightTexture_ST.zw;
                float4 dailyColor = SAMPLE_TEXTURE2D(_DailyTexture, sampler_DailyTexture, uv_DailyTexture);
                float4 nightColor = SAMPLE_TEXTURE2D(_NightTexture, sampler_NightTexture, uv_NightTexture);
                float4 lerpColor = lerp(dailyColor, nightColor, _DayToNight) * _BaseColor;

                float4 screenPos = input.screenPos / input.screenPos.w;
                screenPos.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPos.z : screenPos.z * 0.5 + 0.5;
                float2 lineUV = screenPos.xy * _LineTilling;
                float4 lineColor = SAMPLE_TEXTURE2D(_LineTexture, sampler_LineTexture, lineUV);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float dotResult = dot(input.normalWS, lightDir);
                float smoothStep = smoothstep(0.0, 1.0, step(dotResult / -31.2, _LineRange));
                float4 lerpLineColor = lerp(float4(1, 1, 1, 0), lineColor, 1.0 - smoothStep);

                float4 blendOpSrc71 = _DarkColor;
                float4 blendOpDest71 = float4(1, 1, 1, 0);
                float smoothstepResult14 = smoothstep(0.0, 1.0, (1.0 - (dotResult / 0.15)));
                float4 lerpBlendMode71 = lerp(blendOpDest71, (blendOpSrc71 * blendOpDest71), smoothstepResult14);
                float4 lerpResult51 = lerp(((saturate(lerpLineColor)) * (saturate(lerpBlendMode71))), float4(1, 1, 1, 0), _DarkValue);

                float4 finalColor = lerpColor * lerpResult51;

                float2 uv_Emissive = input.uv * _Emissive_ST.xy + _Emissive_ST.zw;
                float4 emissiveColor = SAMPLE_TEXTURE2D(_Emissive, sampler_Emissive, uv_Emissive) * _EmissiveColor;

                return finalColor * emissiveColor;
            }
            ENDHLSL
        }

        // Outline
        Pass
        {
            Name "OUTLINE"
            Tags {}
            Cull Front

            HLSLPROGRAM
            #pragma multi_compile_instancing

            #pragma vertex vertex_outline
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata_outline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                float4 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 texcoord3 : TEXCOORD3;
                float4 vertexColor : COLOR;
                float4 tangent : TANGENT;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_outline
            {
                float4 pos : SV_POSITION;
                float4 vcolor : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _OutlineSize;
            float _OutlineGrowth;
            float4 _OutlineColor;
            float _OutlineMultiplier;
            float _OutlineAutoShrink;
            float _OutlineShrinkFactor;
            float _OutlineInitialSize;
            float _OutlineGrowthShrinkFactor;
            float _OutlineGrowthShrinkSensitivity;
            float _OutlineMaxDistance;
            float _ApplyScale;

            v2f_outline vertex_outline(appdata_outline v)
            {
                v2f_outline o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = TransformObjectToHClip(v.vertex.xyz);

                float3 scale = float3(1, 1, 1);
                float averageScale = 1;
                if (_ApplyScale)
                {
                    scale = float3(
                        length(unity_ObjectToWorld._m00_m10_m20),
                        length(unity_ObjectToWorld._m01_m11_m21),
                        length(unity_ObjectToWorld._m02_m12_m22)
                    );
                    averageScale = (scale.x + scale.y + scale.z) / 3;
                }

                float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal)) * scale;
                float3 vert = normalize(mul(UNITY_MATRIX_IT_MV, v.vertex)) * scale;
                const float2 offset = mul((float2x2)UNITY_MATRIX_P, float2(lerp(norm.x, vert.x, 0), lerp(norm.y, vert.y, 0)));
                const float distance = length(unity_ObjectToWorld._m03_m13_m23 - _WorldSpaceCameraPos);

                // Calculate shrink factor based on distance
                float shrinkFactor = 1.0;
                if (_OutlineAutoShrink > 0.5)
                {
                    const float normalizedDistance = distance * _OutlineShrinkFactor;
                    shrinkFactor = 1.0 / (1.0 + exp(-normalizedDistance)) * _OutlineInitialSize;
                }
                o.pos.xy += offset * _OutlineSize * averageScale * _OutlineMultiplier * shrinkFactor;

                // Calculate growth shrink factor based on distance
                if (_OutlineAutoShrink > 0.5)
                {
                    if (distance <= _OutlineMaxDistance)
                    {
                        const float growthShrinkFactor = 1.0 / (1.0 + exp(-_OutlineGrowthShrinkSensitivity * (distance - _OutlineGrowthShrinkFactor)));
                        o.pos.z += _OutlineGrowth * growthShrinkFactor;
                    }
                    else
                    {
                        o.pos.z = 0; //wont render lines based on max distance slider
                    }
                }
                else
                {
                    o.pos.z += _OutlineGrowth;
                }


                // Output color
                o.color = _OutlineColor;
                return o;
            }

            float4 fragment_outline(v2f_outline input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Shader Properties Sampling
                const float4 __outlineColor = float4(1, 1, 1, 1);
                half4 outlineColor = __outlineColor * input.vcolor.xyzw;
                return outlineColor;
            }

            float4 frag(v2f_outline i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
    CustomEditor "ASEMaterialInspector"
    Fallback Off
}