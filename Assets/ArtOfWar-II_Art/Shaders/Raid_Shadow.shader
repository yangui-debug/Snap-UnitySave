// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

Shader "Raid/ShadowMat"
{
    Properties
    {
        [TCP2HeaderHelp(Base)]
        _BaseColor ("Color", Color) = (1,1,1,1)
        [TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
        [TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        _ColorGlitter ("Colo Glitter", Color) = (0,0,0,0)
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [TCP2Separator]

        [TCP2Header(Ramp Shading)]

        _RampThreshold ("Threshold", Range(0.01,1)) = 0.5
        _RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
        [TCP2Separator]

        [TCP2HeaderHelp(Emission)]
        [NoScaleOffset] _Emission ("Emission Map", 2D) = "black" {}
        [TCP2ColorNoAlpha] [HDR] _Emissioncolor ("Emission Color", Color) = (1,1,1,1)
        [TCP2Separator]

        [TCP2HeaderHelp(Normal Mapping)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Enable Normal Mapping", Float) = 0
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        [TCP2Separator]

        [Header(Alpha)]
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 1
        _Cutoff("_Cutoff (Alpha Cutoff)", Range(0.0, 1.0)) = 0.5 // alpha clip threshold

        [Header(Shadow)]
        _GroundHeight("_GroundHeight", Float) = 0
        _ShadowColor("_ShadowColor", Color) = (0,0,0,1)
        _ShadowFalloff("_ShadowFalloff", Range(0,1)) = 0.05

        // Blending state
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
            "Queue" = "Transparent"
        }

        HLSLINCLUDE
        #define fixed half
        #define fixed2 half2
        #define fixed3 half3
        #define fixed4 half4

        #if UNITY_VERSION >= 202020
			#define URP_10_OR_NEWER
        #endif
        #if UNITY_VERSION >= 202120
			#define URP_12_OR_NEWER
        #endif
        #if UNITY_VERSION >= 202220
			#define URP_14_OR_NEWER
        #endif

        // Texture/Sampler abstraction
        #define TCP2_TEX2D_WITH_SAMPLER(tex)						TEXTURE2D(tex); SAMPLER(sampler##tex)
        #define TCP2_TEX2D_NO_SAMPLER(tex)							TEXTURE2D(tex)
        #define TCP2_TEX2D_SAMPLE(tex, samplertex, coord)			SAMPLE_TEXTURE2D(tex, sampler##samplertex, coord)
        #define TCP2_TEX2D_SAMPLE_LOD(tex, samplertex, coord, lod)	SAMPLE_TEXTURE2D_LOD(tex, sampler##samplertex, coord, lod)

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // Uniforms

        // Shader Properties
        TCP2_TEX2D_WITH_SAMPLER(_BumpMap);
        TCP2_TEX2D_WITH_SAMPLER(_BaseMap);
        TCP2_TEX2D_WITH_SAMPLER(_Emission);

        CBUFFER_START(UnityPerMaterial)
            // Shader Properties
            float _OutlineWidth;
            fixed4 _OutlineColorVertex;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;
            half4 _Emissioncolor;
            float _RampThreshold;
            float _RampSmoothing;
            fixed4 _SColor;
            fixed4 _HColor;
            half4 _ColorGlitter;
        CBUFFER_END

        // Built-in renderer (CG) to SRP (HLSL) bindings
        #define UnityObjectToClipPos TransformObjectToHClip
        #define _WorldSpaceLightPos0 _MainLightPosition
        ENDHLSL

        Pass
        {
            Name "Main"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // -------------------------------------
            // Material keywords
            #pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Render Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // -------------------------------------

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options nolightmap nolightprobe

            #pragma vertex Vertex
            #pragma fragment Fragment

            //--------------------------------------
            // Toony Colors Pro 2 keywords
            #pragma shader_feature_local _NORMALMAP

            // vertex input
            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 texcoord0 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // vertex output / fragment input
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normal : NORMAL;
                float4 worldPosAndFog : TEXCOORD0;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord    : TEXCOORD1; // compute shadow coord per-vertex for the main light
                #endif
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
				half3 vertexLights : TEXCOORD2;
                #endif
                float3 pack0 : TEXCOORD3; /* pack0.xyz = tangent */
                float3 pack1 : TEXCOORD4; /* pack1.xyz = bitangent */
                float2 pack2 : TEXCOORD5; /* pack2.xy = texcoord0 */
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Texture Coordinates
                output.pack2.xy.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal, input.tangent);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
				// Vertex lighting
				output.vertexLights = VertexLighting(vertexInput.positionWS, vertexNormalInput.normalWS);
                #endif

                // world position
                output.worldPosAndFog = float4(vertexInput.positionWS.xyz, 0);

                // normal
                output.normal = normalize(vertexNormalInput.normalWS);

                // tangent
                output.pack0.xyz = vertexNormalInput.tangentWS;
                output.pack1.xyz = vertexNormalInput.bitangentWS;

                // clip position
                output.positionCS = vertexInput.positionCS;

                return output;
            }

            half4 Fragment(Varyings input
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS = input.worldPosAndFog.xyz;
                float3 normalWS = normalize(input.normal);
                half3 tangentWS = input.pack0.xyz;
                half3 bitangentWS = input.pack1.xyz;
                #if defined(_NORMALMAP)
				half3x3 tangentToWorldMatrix = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
                #endif

                // Shader Properties Sampling
                float4 __normalMap = (TCP2_TEX2D_SAMPLE(_BumpMap, _BumpMap, input.pack2.xy).rgba);
                float4 __albedo = (TCP2_TEX2D_SAMPLE(_BaseMap, _BaseMap, input.pack2.xy).rgba);
                float4 __mainColor = (_BaseColor.rgba);
                float __alpha = (__albedo.a * __mainColor.a);
                float __ambientIntensity = (1.0);
                float3 __emission = (TCP2_TEX2D_SAMPLE(_Emission, _Emission, input.pack2.xy).rgb * _Emissioncolor.rgb);
                float __rampThreshold = (_RampThreshold);
                float __rampSmoothing = (_RampSmoothing);
                float3 __shadowColor = (_SColor.rgb);
                float3 __highlightColor = (_HColor.rgb);

                #if defined(_NORMALMAP)
				half4 normalMap = __normalMap;
				half3 normalTS = UnpackNormal(normalMap);
                #if defined(_NORMALMAP)
				normalWS = normalize( mul(normalTS, tangentToWorldMatrix) );
                #endif
                #endif

                // main texture
                half3 albedo = __albedo.rgb;
                half alpha = __alpha;

                half3 emission = half3(0, 0, 0);

                albedo *= __mainColor.rgb;

                // main light: direction, color, distanceAttenuation, shadowAttenuation
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                #else
                float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                #if defined(URP_10_OR_NEWER)
                #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
					half4 shadowMask = SAMPLE_SHADOWMASK(input.uvLM);
                #elif !defined (LIGHTMAP_ON)
					half4 shadowMask = unity_ProbesOcclusion;
                #else
					half4 shadowMask = half4(1, 1, 1, 1);
                #endif

				Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);
                #else
                Light mainLight = GetMainLight(shadowCoord);
                #endif

                // ambient or lightmap
                // Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
                // are also defined in case you want to sample some terms per-vertex.
                half3 bakedGI = SampleSH(normalWS);
                half occlusion = 1;

                half3 indirectDiffuse = bakedGI;
                indirectDiffuse *= occlusion * albedo * __ambientIntensity;
                emission += __emission;

                half3 lightDir = mainLight.direction;
                half3 lightColor = mainLight.color.rgb;

                half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                half ndl = dot(normalWS, lightDir);
                half3 ramp;

                half rampThreshold = __rampThreshold;
                half rampSmooth = __rampSmoothing * 0.5;
                ndl = saturate(ndl);
                ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndl);

                // apply attenuation
                ramp *= atten;

                // highlight/shadow colors
                ramp = lerp(__shadowColor, __highlightColor, ramp);

                // output color
                half3 color = half3(0, 0, 0);
                color += albedo * lightColor.rgb * ramp;

                // Additional lights loop
                #ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();

				LIGHT_LOOP_BEGIN(pixelLightCount)
				{
                #if defined(URP_10_OR_NEWER)
						Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                #else
						Light light = GetAdditionalLight(lightIndex, positionWS);
                #endif
					half atten = light.shadowAttenuation * light.distanceAttenuation;

                #if defined(_LIGHT_LAYERS)
						half3 lightDir = half3(0, 1, 0);
						half3 lightColor = half3(0, 0, 0);
						if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
						{
							lightColor = light.color.rgb;
							lightDir = light.direction;
						}
                #else
						half3 lightColor = light.color.rgb;
						half3 lightDir = light.direction;
                #endif

					half ndl = dot(normalWS, lightDir);
					half3 ramp;
					
					ndl = saturate(ndl);
					ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndl);

					// apply attenuation (shadowmaps & point/spot lights attenuation)
					ramp *= atten;

					// apply highlight color
					ramp = lerp(half3(0,0,0), __highlightColor, ramp);
					
					// output color
					color += albedo * lightColor.rgb * ramp;

				}
				LIGHT_LOOP_END
                #endif
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
				color += input.vertexLights * albedo;
                #endif

                // apply ambient
                color += indirectDiffuse;

                color += emission;

                color += _ColorGlitter;

                return half4(color, alpha);
            }
            ENDHLSL
        }

        // Planar Shadows平面阴影
        Pass
        {
            Name "PlanarShadow"

            // 用使用模板测试以保证alpha显示正确
            Stencil
            {
                Ref 0
                Comp equal
                Pass incrWrap
                Fail keep
                ZFail keep
            }

            Cull Off

            // 透明混合模式
            Blend SrcAlpha OneMinusSrcAlpha

            // 关闭深度写入
            ZWrite off

            // 深度稍微偏移防止阴影与地面穿插
            Offset -1 , 0

            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            float _GroundHeight;
            float4 _ShadowColor;
            float _ShadowFalloff;
            float _Clipping;
            half _Cutoff;
            sampler2D Albedo;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowProjectPos(float4 vertPos)
            {
                // 得到顶点的世界空间坐标
                float4 worldPos = mul(UNITY_MATRIX_M, float4(vertPos.xyz, 1.0));

                // 灯光方向
                float3 lightDir = -_MainLightPosition.xyz;

                float4 _ShadowPlane = float4(0, 1, 0, _GroundHeight);
                const float distance = (_ShadowPlane.w - dot(_ShadowPlane.xyz, worldPos.xyz)) / dot(_ShadowPlane.xyz, lightDir.xyz);
                worldPos = worldPos + distance * float4(lightDir.xyz, 0.0);
                return worldPos;
            }

            float GetAlpha(v2f i)
            {
                float alpha = _BaseColor.a;
                alpha *= tex2D(Albedo, i.uv.xy).a;
                return alpha;
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // 得到阴影的世界空间坐标
                const float4 shadowPos = ShadowProjectPos(v.vertex);

                // 转换到裁切空间
                o.vertex = TransformWorldToHClip(shadowPos);

                // 得到中心点世界坐标
                const float3 center = float3(UNITY_MATRIX_M[0].w, _GroundHeight, UNITY_MATRIX_M[2].w);

                // 计算阴影衰减
                const float falloff = 1 - saturate(distance(shadowPos, center) * _ShadowFalloff);

                // 阴影颜色
                o.color = _ShadowColor;
                o.color.a *= falloff;

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                if (_Clipping)
                {
                    const float alpha = GetAlpha(i);
                    i.color.a *= step(_Cutoff, alpha);
                }
                return i.color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
    CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}

/* TCP_DATA u config(ver:"2.9.6";unity:"2022.3.14f1";tmplt:"SG2_Template_URP";features:list["UNITY_5_4","UNITY_5_5","UNITY_5_6","UNITY_2017_1","UNITY_2018_1","UNITY_2018_2","UNITY_2018_3","UNITY_2019_1","UNITY_2019_2","UNITY_2019_3","UNITY_2019_4","UNITY_2020_1","UNITY_2021_1","UNITY_2021_2","UNITY_2022_2","OUTLINE","EMISSION","SHADOW_COLOR_MAIN_DIR","BUMP","BUMP_SHADER_FEATURE","OUTLINE_MAX_WIDTH","OUTLINE_MIN_WIDTH","OUTLINE_PIXEL_PERFECT","OUTLINE_CLIP_SPACE","TEMPLATE_LWRP"];flags:list[];flags_extra:dict[pragma_gpu_instancing=list["nolightmap","nolightprobe"]];keywords:dict[RENDER_TYPE="Opaque",RampTextureDrawer="[TCP2Gradient]",RampTextureLabel="Ramp Texture",SHADER_TARGET="3.0",BASEGEN_ALBEDO_DOWNSCALE="1",BASEGEN_MASKTEX_DOWNSCALE="1/2",BASEGEN_METALLIC_DOWNSCALE="1/4",BASEGEN_SPECULAR_DOWNSCALE="1/4",BASEGEN_DIFFUSEREMAPMIN_DOWNSCALE="1/4",BASEGEN_MASKMAPREMAPMIN_DOWNSCALE="1/4",RIM_LABEL="Rim Lighting"];shaderProperties:list[,,,,,,,,sp(name:"Emission";imps:list[imp_mp_texture(uto:False;tov:"";tov_lbl:"";gto:False;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:False;triplanar_local:False;def:"black";locked_uv:False;uv:0;cc:3;chan:"RGB";mip:-1;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Texcoord;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_Emission";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"2d076891-723b-476d-82f9-b0423e0f9d93";op:Multiply;lbl:"Emission Map";gpu_inst:False;locked:False;impl_index:-1),imp_mp_color(def:RGBA(1, 1, 1, 1);hdr:True;cc:3;chan:"RGB";prop:"_Emissioncolor";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"869cad9e-6850-4560-ae04-dc12e37a5181";op:Multiply;lbl:"Emission Color";gpu_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];clones:dict[];isClone:False),,sp(name:"Outline Width";imps:list[imp_mp_range(def:5;min:0;max:15;prop:"_OutlineWidth";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"fb4f3f11-5f3d-4b7a-81b0-d41bc88fa9aa";op:Multiply;lbl:"Width";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False),sp(name:"Outline Min Width";imps:list[imp_mp_range(def:4;min:0;max:20;prop:"_OutlineMinWidth";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"4bd514fb-f8bf-4c7f-984c-258f124b68ab";op:Multiply;lbl:"Min Width";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False),sp(name:"Outline Max Width";imps:list[imp_mp_range(def:5;min:0;max:20;prop:"_OutlineMaxWidth";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"6a462a44-bb86-4fea-8a86-7a28ee8bf12f";op:Multiply;lbl:"Max Width";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False)];customTextures:list[ct(cimp:imp_mp_texture(uto:True;tov:"";tov_lbl:"";gto:False;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:False;triplanar_local:False;def:"white";locked_uv:False;uv:0;cc:4;chan:"RGBA";mip:0;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Texcoord;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_DetailTex";md:"";gbv:False;custom:True;refs:"";pnlock:False;guid:"d9bdb63a-02a4-4d35-8e12-75d4d0d0152f";op:Multiply;lbl:"Detail Map";gpu_inst:False;locked:False;impl_index:-1);exp:False;uv_exp:False;imp_lbl:"Texture")];codeInjection:codeInjection(injectedFiles:list[];mark:False);matLayers:list[]) */
/* TCP_HASH 3e94d29d14b4b53b79e408c7fc115757 */