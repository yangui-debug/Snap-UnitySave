// Made with Amplify Shader Editor v1.9.4.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SceneCommon"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_MainTexture("MainTexture(主贴图)", 2D) = "white" {}
		[HDR]_Maincolor("Maincolor（主贴图颜色）", Color) = (1,1,1,1)
		[Toggle]_IsGird("IsGird", Float) = 0
		_GirdSize("GirdSize", Float) = 1
		_FlatMaskRange("FlatMaskRange", Range( 0 , 1)) = 0.5
		_Render02_Sample("Render02_Sample", 2D) = "white" {}
		_ShadowColor("投影颜色", Color) = (0.5992157,0.7490196,0.7427121,1)
		[HDR]_Render02_HighLightColor("高光颜色", Color) = (1,1,1,1)
		_Render02_DarkLerpColor("暗部替换颜色", Color) = (1,1,1,0)
		_Render02_DarkColor("暗部颜色", Color) = (0,0,0,1)
		[HDR]_Render02_RefColor("反射颜色", Color) = (0.3113208,0.3113208,0.3113208,1)
		[Toggle(_WIND_ON)] _Wind("Wind（晃动开关）", Float) = 0
		_GlobalWindIntensity("WindIntensity（风强度）", Float) = 4
		_GlobalWindSpeed("WindSpeed（风速）", Float) = 1
		_GlobalWindDirection("WindDirection（风方向）", Vector) = (1,0,0,0)
		_LeavesWind("LeavesWind（次级风强度）", Float) = 1
		[Toggle(_WATER_ON)] _Water("Water（水开关）", Float) = 1
		_WaterHeight("WaterHeight", Float) = -2.5
		_NearColor("NearColor", Color) = (0.1803922,0.3019608,0.6039216,1)
		_MidColor("MidColor", Color) = (0.1176471,0.6156863,0.8196079,1)
		_FarColor("FarColor", Color) = (0.4039216,0.8274511,0.8666667,1)
		_FarDis("FarDis", Range( 0 , 0.32)) = 1
		_NearDis("NearDis", Range( 0 , 0.4)) = 0
		_UnderWaterColor("UnderWaterColor", Color) = (0.1843137,0.3176471,0.5215687,1)
		_DeepSmooth("DeepSmooth", Float) = 75
		_DeepRange("DeepRange", Float) = -0.5
		_FoamNoiseTex("FoamNoiseTex", 2D) = "white" {}
		_FoamColor("FoamColor", Color) = (0.854902,0.9215687,1,1)
		_FoamTilling("FoamTilling", Vector) = (20,20,0,0)
		_FoamSpeed("FoamSpeed", Vector) = (3.7,1,0,0)
		_FoamWidth("FoamWidth", Float) = 2.5
		_FoamBlend("FoamBlend", Float) = 0.26
		[Toggle]_Specullar("Specullar", Float) = 0
		_SpecullarRange("SpecullarRange", Range( 0 , 1)) = 0.0675128
		_SpecullarSmooth("SpecullarSmooth", Range( 0.01 , 1)) = 0.01
		[HDR]_SpecullarColor("SpecullarColor", Color) = (1,1,1,1)
		_UnlockScale("UnlockScale", Float) = 0
		_UnlockColor("UnlockColor", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		[HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }

		Cull Back
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		#pragma exclude_renderers xboxone xboxseries playstation ps4 ps5 switch // ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend Off
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			

			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#define ASE_SRP_VERSION 140010


			

			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			
			#if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			
			#if ASE_SRP_VERSION >=140010
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#endif
		

			

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_SHADOWCOORDS
			#pragma shader_feature_local _WIND_ON
			#pragma shader_feature_local _WATER_ON
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _FORWARD_PLUS


			struct VertexInput
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
					float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _SpecullarColor;
			half4 _ShadowColor;
			half4 _MidColor;
			half4 _UnderWaterColor;
			half4 _Render02_DarkLerpColor;
			half4 _UnlockColor;
			half4 _Maincolor;
			half4 _FarColor;
			half4 _Render02_HighLightColor;
			half4 _MainTexture_ST;
			half4 _Render02_DarkColor;
			half4 _FoamColor;
			half4 _Render02_RefColor;
			half4 _NearColor;
			half3 _GlobalWindDirection;
			half2 _FoamTilling;
			half2 _FoamSpeed;
			half _WaterHeight;
			half _DeepRange;
			half _DeepSmooth;
			half _FarDis;
			half _NearDis;
			half _GlobalWindSpeed;
			half _SpecullarRange;
			half _FoamWidth;
			half _FlatMaskRange;
			half _GirdSize;
			half _Specullar;
			half _IsGird;
			half _UnlockScale;
			half _LeavesWind;
			half _GlobalWindIntensity;
			half _SpecullarSmooth;
			half _FoamBlend;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _MainTexture;
			sampler2D _Render02_Sample;
			sampler2D _FoamNoiseTex;


			half3 HSVToRGB( half3 c )
			{
				half4 K = half4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				half3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			
			half3 RGBToHSV(half3 c)
			{
				half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				half4 p = lerp( half4( c.bg, K.wz ), half4( c.gb, K.xy ), step( c.b, c.g ) );
				half4 q = lerp( half4( p.xyw, c.r ), half4( c.r, p.yzx ), step( p.x, c.r ) );
				half d = q.x - min( q.w, q.y );
				half e = 1.0e-10;
				return half3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				half3 temp_cast_0 = (0.0).xxx;
				half temp_output_621_0 = ( ( ( _GlobalWindSpeed * _TimeParameters.x ) * PI ) % ( 2.0 * PI ) );
				half temp_output_316_0 = ( _GlobalWindIntensity * 0.1 );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				half3 worldToObj488 = mul( GetWorldToObjectMatrix(), float4( ( _GlobalWindDirection + ase_objectPosition ), 1 ) ).xyz;
				half temp_output_955_0 = ( 1.0 - v.ase_color.b );
				#ifdef _WIND_ON
				half3 staticSwitch850 = ( ( ( ( cos( temp_output_621_0 ) * temp_output_316_0 ) * worldToObj488 ) * ( v.ase_color.r * temp_output_955_0 ) ) + ( ( temp_output_955_0 * v.ase_color.g ) * ( ( worldToObj488 * ( sin( temp_output_621_0 ) * temp_output_316_0 ) ) * _LeavesWind ) ) );
				#else
				half3 staticSwitch850 = temp_cast_0;
				#endif
				half3 Wind351 = staticSwitch850;
				half3 vertexToFrag436 = Wind351;
				half2 texCoord1111 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				half UVMask1141 = step( texCoord1111.y , (( _IsGird )?( 0.75 ):( 1.0 )) );
				
				half3 ase_worldNormal = TransformObjectToWorldNormal(v.normalOS);
				o.ase_texcoord4.xyz = ase_worldNormal;
				float4 ase_clipPos = TransformObjectToHClip((v.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;
				
				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				o.ase_texcoord4.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = ( vertexToFrag436 + ( ( v.normalOS * _UnlockScale ) * ( 1.0 - UVMask1141 ) ) );

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS.xyz = vertexValue;
				#else
					v.positionOS.xyz += vertexValue;
				#endif

				v.normalOS = v.normalOS;

				float3 positionWS = TransformObjectToWorld( v.positionOS.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.positionWS = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				#ifdef ASE_FOG
					o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif

				o.positionCS = positionCS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.positionOS;
				o.normalOS = v.normalOS;
				o.ase_color = v.ase_color;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.positionOS.xyz - patch[i].normalOS * (dot(o.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				o.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN
				#ifdef _WRITE_RENDERING_LAYERS
				, out float4 outRenderingLayers : SV_Target1
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				half2 texCoord1106 = IN.ase_texcoord3.xy * float2( 1,1 ) + half2( 0,-0.125 );
				float2 uv_MainTexture = IN.ase_texcoord3.xy * _MainTexture_ST.xy + _MainTexture_ST.zw;
				half4 tex2DNode188 = tex2D( _MainTexture, uv_MainTexture );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				half3 break1094 = floor( ( ase_objectPosition / _GirdSize ) );
				half4 lerpResult1107 = lerp( tex2D( _MainTexture, texCoord1106 ) , tex2DNode188 , step( fmod( abs( ( break1094.x + break1094.z ) ) , 2.0 ) , 0.5 ));
				half2 texCoord1111 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				half UVMask1141 = step( texCoord1111.y , (( _IsGird )?( 0.75 ):( 1.0 )) );
				half4 lerpResult1119 = lerp( lerpResult1107 , tex2DNode188 , UVMask1141);
				half4 temp_output_519_0 = ( lerpResult1119 * _Maincolor );
				half3 hsvTorgb1152 = RGBToHSV( _UnlockColor.rgb );
				half3 hsvTorgb1150 = RGBToHSV( temp_output_519_0.rgb );
				half3 hsvTorgb1149 = HSVToRGB( half3(hsvTorgb1152.x,hsvTorgb1150.y,hsvTorgb1150.z) );
				half4 lerpResult1146 = lerp( temp_output_519_0 , half4( hsvTorgb1149 , 0.0 ) , ( ( 1.0 - UVMask1141 ) * _UnlockColor.a ));
				half4 Main889 = lerpResult1146;
				half4 blendOpSrc865 = _Render02_HighLightColor;
				half4 blendOpDest865 = Main889;
				half3 ase_worldNormal = IN.ase_texcoord4.xyz;
				half dotResult877 = dot( SafeNormalize(_MainLightPosition.xyz) , ase_worldNormal );
				half2 appendResult879 = (half2(0.5 , ( ( dotResult877 * 0.5 ) + 0.5 )));
				half2 clampResult880 = clamp( appendResult879 , float2( 0,0 ) , float2( 0,1 ) );
				half4 tex2DNode882 = tex2D( _Render02_Sample, clampResult880 );
				half4 lerpBlendMode865 = lerp(blendOpDest865,2.0f*blendOpDest865*blendOpSrc865 + blendOpDest865*blendOpDest865*(1.0f - 2.0f*blendOpSrc865),( _Render02_HighLightColor.a * tex2DNode882.r ));
				half4 blendOpSrc867 = _Render02_RefColor;
				half4 blendOpDest867 = ( saturate( lerpBlendMode865 ));
				half smoothstepResult864 = smoothstep( ( 1.0 - _Render02_RefColor.a ) , 1.0 , tex2DNode882.b);
				half4 lerpBlendMode867 = lerp(blendOpDest867,2.0f*blendOpDest867*blendOpSrc867 + blendOpDest867*blendOpDest867*(1.0f - 2.0f*blendOpSrc867),smoothstepResult864);
				half4 blendOpSrc870 = _Render02_DarkColor;
				half4 blendOpDest870 = ( saturate( lerpBlendMode867 ));
				half temp_output_866_0 = ( tex2DNode882.g - smoothstepResult864 );
				half4 lerpBlendMode870 = lerp(blendOpDest870,( blendOpSrc870 * blendOpDest870 ),( _Render02_DarkColor.a * temp_output_866_0 ));
				half4 lerpResult872 = lerp( ( saturate( lerpBlendMode870 )) , _Render02_DarkLerpColor , ( _Render02_DarkLerpColor.a * temp_output_866_0 ));
				half2 texCoord895 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				half4 lerpResult897 = lerp( lerpResult872 , Main889 , step( texCoord895.y , _FlatMaskRange ));
				half4 Render02873 = lerpResult897;
				half4 blendOpSrc1088 = _SpecullarColor;
				half4 blendOpDest1088 = Render02873;
				half3 normalizedWorldNormal = normalize( ase_worldNormal );
				float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				ase_worldViewDir = SafeNormalize( ase_worldViewDir );
				half3 normalizeResult1066 = normalize( ( ase_worldViewDir + SafeNormalize(_MainLightPosition.xyz) ) );
				half dotResult1064 = dot( normalizedWorldNormal , normalizeResult1066 );
				half clampResult1077 = clamp( dotResult1064 , 0.0 , 1.0 );
				half clampResult1084 = clamp( ( ( clampResult1077 + ( _SpecullarRange - 1.0 ) ) / _SpecullarSmooth ) , 0.0 , 1.0 );
				half Specullar1083 = clampResult1084;
				float ase_lightAtten = 0;
				Light ase_mainLight = GetMainLight( ShadowCoords );
				ase_lightAtten = ase_mainLight.distanceAttenuation * ase_mainLight.shadowAttenuation;
				half Dark962 = tex2DNode882.g;
				half clampResult617 = clamp( ( ( 1.0 - ase_lightAtten ) - Dark962 ) , 0.0 , 1.0 );
				half ase_lightIntensity = max( max( _MainLightColor.r, _MainLightColor.g ), _MainLightColor.b );
				half4 ase_lightColor = float4( _MainLightColor.rgb / ase_lightIntensity, ase_lightIntensity );
				half temp_output_505_0 = ( _ShadowColor.a * clampResult617 * ( ase_lightColor.a * 2.0 ) );
				half ShadowMask1131 = temp_output_505_0;
				half4 lerpBlendMode1088 = lerp(blendOpDest1088,(( blendOpSrc1088 > 0.5 )? ( blendOpDest1088 + 2.0 * blendOpSrc1088 - 1.0 ) : ( blendOpDest1088 + 2.0 * ( blendOpSrc1088 - 0.5 ) ) ),saturate( ( ( _SpecullarColor.a * Specullar1083 ) * ( 1.0 - ShadowMask1131 ) ) ));
				half4 lerpResult481 = lerp( float4( 1,1,1,0 ) , _ShadowColor , temp_output_505_0);
				half4 Shadow891 = lerpResult481;
				half4 OutColor710 = ( (( _Specullar )?( ( saturate( lerpBlendMode1088 )) ):( Render02873 )) * Shadow891 );
				float4 screenPos = IN.ase_texcoord5;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				half clampResult969 = clamp( ase_screenPosNorm.y , 0.0 , 1.0 );
				half clampResult977 = clamp( saturate( ( clampResult969 / _NearDis ) ) , 0.0 , 1.0 );
				half4 lerpResult979 = lerp( _NearColor , _MidColor , clampResult977);
				half clampResult981 = clamp( ( ( 1.0 - clampResult969 ) / _FarDis ) , 0.0 , 1.0 );
				half4 lerpResult982 = lerp( _FarColor , lerpResult979 , clampResult981);
				half4 WaterPlanColor1034 = lerpResult982;
				half ifLocalVar1052 = 0;
				if( WorldPosition.y < _WaterHeight )
				ifLocalVar1052 = ( 1.0 - ( WorldPosition.y - _WaterHeight ) );
				half WaterDepth998 = ifLocalVar1052;
				half clampResult983 = clamp( exp( ( ( -WaterDepth998 + _DeepRange ) / ( _DeepSmooth * 0.1 ) ) ) , 0.0 , 1.0 );
				half4 lerpResult984 = lerp( WaterPlanColor1034 , _UnderWaterColor , clampResult983);
				half temp_output_1027_0 = (0.8 + (sin( _TimeParameters.x ) - -1.0) * (1.2 - 0.8) / (1.0 - -1.0));
				half FormaMask1033 = step( tex2D( _FoamNoiseTex, ( ( (WorldPosition).xz / _FoamTilling ) + ( ( _FoamSpeed * float2( 0.01,0.01 ) ) * _TimeParameters.x ) ) ).r , ( exp( ( ( -WaterDepth998 + ( _FoamWidth * temp_output_1027_0 ) ) / ( _FoamBlend * temp_output_1027_0 ) ) ) * ( 1.0 - ase_screenPosNorm.y ) ) );
				half temp_output_991_0 = ( _FoamColor.a * FormaMask1033 );
				half4 lerpResult967 = lerp( lerpResult984 , _FoamColor , temp_output_991_0);
				half clampResult1047 = clamp( WaterDepth998 , 0.0 , 1.0 );
				half4 lerpResult1042 = lerp( OutColor710 , lerpResult967 , ( clampResult1047 * saturate( ( ( 1.0 - ( ( 1.0 - _UnderWaterColor.a ) * clampResult983 ) ) + temp_output_991_0 ) ) ));
				#ifdef _WATER_ON
				half4 staticSwitch1055 = lerpResult1042;
				#else
				half4 staticSwitch1055 = OutColor710;
				#endif
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = staticSwitch1055.rgb;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(IN.positionCS, Color);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( IN.positionCS );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM

			

			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 140010


			

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_SHADOWCASTER

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _WIND_ON


			struct VertexInput
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _SpecullarColor;
			half4 _ShadowColor;
			half4 _MidColor;
			half4 _UnderWaterColor;
			half4 _Render02_DarkLerpColor;
			half4 _UnlockColor;
			half4 _Maincolor;
			half4 _FarColor;
			half4 _Render02_HighLightColor;
			half4 _MainTexture_ST;
			half4 _Render02_DarkColor;
			half4 _FoamColor;
			half4 _Render02_RefColor;
			half4 _NearColor;
			half3 _GlobalWindDirection;
			half2 _FoamTilling;
			half2 _FoamSpeed;
			half _WaterHeight;
			half _DeepRange;
			half _DeepSmooth;
			half _FarDis;
			half _NearDis;
			half _GlobalWindSpeed;
			half _SpecullarRange;
			half _FoamWidth;
			half _FlatMaskRange;
			half _GirdSize;
			half _Specullar;
			half _IsGird;
			half _UnlockScale;
			half _LeavesWind;
			half _GlobalWindIntensity;
			half _SpecullarSmooth;
			half _FoamBlend;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			float3 _LightDirection;
			float3 _LightPosition;

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				half3 temp_cast_0 = (0.0).xxx;
				half temp_output_621_0 = ( ( ( _GlobalWindSpeed * _TimeParameters.x ) * PI ) % ( 2.0 * PI ) );
				half temp_output_316_0 = ( _GlobalWindIntensity * 0.1 );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				half3 worldToObj488 = mul( GetWorldToObjectMatrix(), float4( ( _GlobalWindDirection + ase_objectPosition ), 1 ) ).xyz;
				half temp_output_955_0 = ( 1.0 - v.ase_color.b );
				#ifdef _WIND_ON
				half3 staticSwitch850 = ( ( ( ( cos( temp_output_621_0 ) * temp_output_316_0 ) * worldToObj488 ) * ( v.ase_color.r * temp_output_955_0 ) ) + ( ( temp_output_955_0 * v.ase_color.g ) * ( ( worldToObj488 * ( sin( temp_output_621_0 ) * temp_output_316_0 ) ) * _LeavesWind ) ) );
				#else
				half3 staticSwitch850 = temp_cast_0;
				#endif
				half3 Wind351 = staticSwitch850;
				half3 vertexToFrag436 = Wind351;
				half2 texCoord1111 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				half UVMask1141 = step( texCoord1111.y , (( _IsGird )?( 0.75 ):( 1.0 )) );
				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = ( vertexToFrag436 + ( ( v.normalOS * _UnlockScale ) * ( 1.0 - UVMask1141 ) ) );

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS.xyz = vertexValue;
				#else
					v.positionOS.xyz += vertexValue;
				#endif

				v.normalOS = v.normalOS;

				float3 positionWS = TransformObjectToWorld( v.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.positionWS = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir( v.normalOS );

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				o.positionCS = positionCS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.positionOS;
				o.normalOS = v.normalOS;
				o.ase_color = v.ase_color;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.positionOS.xyz - patch[i].normalOS * (dot(o.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				o.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( IN.positionCS );
				#endif

				return 0;
			}
			ENDHLSL
		}

	
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19402
Node;AmplifyShaderEditor.CommentaryNode;352;-3344,6048;Inherit;False;5580.968;1767.427;Wind;55;346;351;327;326;325;324;323;322;350;321;320;318;488;347;348;487;316;527;489;315;340;528;328;349;338;339;345;337;336;341;342;335;331;332;334;329;584;589;590;591;592;594;595;596;597;599;600;620;621;622;849;850;954;955;956;Wind;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;342;-2704,6112;Inherit;False;Property;_GlobalWindSpeed;WindSpeed（风速）;13;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;346;-2480,6368;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;339;-2032,6368;Inherit;True;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;345;-2160,6144;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;340;-1776,6128;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;622;-1808,6384;Inherit;True;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;349;-592,6304;Inherit;False;Property;_GlobalWindDirection;WindDirection（风方向）;14;0;Create;False;0;0;0;False;0;False;1,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ObjectPositionNode;489;-336,6624;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleRemainderNode;621;-1504,6192;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;328;-704,6992;Inherit;False;Property;_GlobalWindIntensity;WindIntensity（风强度）;12;0;Create;False;0;0;0;False;0;False;4;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;348;-1104,6624;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;487;-80,6496;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;316;-368,6960;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;347;-1104,6080;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;488;80,6480;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;318;48,6816;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;324;320,6448;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;320;16,6288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;321;432,6704;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;350;368,6896;Inherit;False;Property;_LeavesWind;LeavesWind（次级风强度）;15;0;Create;False;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;955;528,6496;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;963;-4320,3440;Inherit;False;1361.803;606.9717;Comment;10;188;1107;187;1119;519;1141;1114;1130;1111;1115;Main;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;322;432,6288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;323;656,6736;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;954;704,6384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;956;704,6576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1115;-4272,3920;Inherit;False;Constant;_GirdRange;GirdRange;41;0;Create;True;0;0;0;False;0;False;0.75;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;325;848,6288;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;326;848,6672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1111;-4112,3760;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;1130;-4096,3888;Inherit;False;Property;_IsGird;IsGird;2;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;327;1008,6464;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;620;1072,6320;Inherit;False;Constant;_Float0;Float 0;23;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1114;-3840,3776;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;850;1344,6432;Inherit;False;Property;_Wind;Wind（晃动开关）;11;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1141;-3568,3712;Inherit;False;UVMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;1038;9824,3968;Inherit;False;564;483;Comment;3;354;436;844;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;351;1680,6336;Inherit;False;Wind;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;1139;10672,5120;Inherit;False;Property;_UnlockScale;UnlockScale;40;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;1137;10672,4960;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;1140;10896,5200;Inherit;False;1141;UVMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;354;9840,4304;Inherit;False;351;Wind;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1138;10896,5008;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;1143;11056,5120;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;957;2128,4704;Inherit;False;1732;571;Comment;12;505;617;502;501;481;891;479;478;598;958;959;1131;Shadow;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;862;-3264,4192;Inherit;False;5048.791;1585.925;Render02;41;899;895;896;962;866;883;881;869;874;898;897;893;873;871;885;888;887;884;882;880;879;878;877;876;875;872;870;868;867;865;864;863;1085;1086;1087;1088;1118;1132;1133;1134;1135;Render02;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;964;5456,5632;Inherit;False;2580;1123;Comment;27;1033;1032;1031;1030;1029;1028;1027;1026;1025;1024;1023;1022;1021;1020;1019;1018;1017;1016;1015;1014;1013;1012;1011;1010;1009;1008;1007;Foam;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;965;5104,4496;Inherit;False;2607.976;863.7517;Comment;16;1034;982;981;980;979;978;977;976;975;974;973;972;971;970;969;968;3WaterColor;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;966;6112,4128;Inherit;False;1498.981;308.6877;depth;6;997;996;995;994;993;992;Depth;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1142;11232,4976;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexToFragmentNode;436;10064,4304;Inherit;False;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ObjectPositionNode;329;-2448,7328;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;334;-2192,7264;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;600;-1968,7248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;332;-1760,7216;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;331;-1552,7216;Inherit;False;Radom;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;335;-2160,7472;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;336;-1968,7472;Inherit;True;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;150;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;528;-1168,6528;Inherit;False;Property;_AddWind;GlobalAddWind( 加强风强度系数);27;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;337;-1680,7472;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;592;-656,6528;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;25;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;338;-1488,7472;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;596;-464,6528;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;590;-1104,6848;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;589;-1104,6304;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;863;-1136,4832;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;864;-944,4768;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;865;-1088,4240;Inherit;True;SoftLight;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendOpsNode;867;-640,4592;Inherit;True;SoftLight;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;868;-400,5024;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;870;-112,4928;Inherit;True;Multiply;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;872;288,4960;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;875;-3200,4688;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;876;-3168,4848;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DotProductOpNode;877;-2928,4768;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;878;-2704,4768;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;879;-2240,4768;Inherit;True;FLOAT2;4;0;FLOAT;0.5;False;1;FLOAT;0.5;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;880;-2016,4768;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;882;-1856,4752;Inherit;True;Property;_Render02_Sample;Render02_Sample;5;0;Create;True;0;0;0;False;0;False;-1;2e0ad166a6f904a4cbe5c1a77fccce86;2e0ad166a6f904a4cbe5c1a77fccce86;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;884;-1328,4528;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;887;-2480,4784;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;871;112,5264;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;897;688,5152;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;874;-672,4912;Inherit;False;Property;_Render02_DarkColor;暗部颜色;9;0;Create;False;0;0;0;False;0;False;0,0,0,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;869;-176,5152;Inherit;False;Property;_Render02_DarkLerpColor;暗部替换颜色;8;0;Create;False;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;881;-1456,4656;Inherit;False;Property;_Render02_RefColor;反射颜色;10;1;[HDR];Create;False;0;0;0;False;0;False;0.3113208,0.3113208,0.3113208,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;883;-2048,4256;Inherit;False;Property;_Render02_HighLightColor;高光颜色;7;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;849;-2720,6256;Inherit;False;Property;_WindRadom;WindRadom;26;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;341;-2928,6304;Inherit;False;331;Radom;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;594;-1632,6608;Inherit;False;Property;_StrongWindSpeed;StrongWindSpeed;33;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;315;-848,7472;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;584;-784,6240;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;597;-1392,6512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;591;-720,6816;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;595;-832,6624;Inherit;False;Property;_StrongWindIntensity;StrongWindIntensity;29;0;Create;True;0;0;0;False;0;False;25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;527;-320,6496;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;617;2736,4960;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;502;2656,5120;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;501;2864,5152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;481;3344,4832;Inherit;True;3;0;COLOR;1,1,1,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;479;2688,4768;Inherit;False;Property;_ShadowColor;投影颜色;6;0;Create;False;0;0;0;False;0;False;0.5992157,0.7490196,0.7427121,1;0.5992157,0.7490196,0.7427121,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;958;2544,5024;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;598;2352,4896;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;866;-800,5152;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;962;-1328,5136;Inherit;False;Dark;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;959;2304,5152;Inherit;False;962;Dark;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;599;-2480,6208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;896;176,5488;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;895;-320,5440;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;899;-240,5600;Inherit;False;Property;_FlatMaskRange;FlatMaskRange;4;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;968;5136,4848;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;1018;7648,5968;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1033;7792,5968;Inherit;False;FormaMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;970;5760,4912;Inherit;False;Property;_NearDis;NearDis;22;0;Create;True;0;0;0;False;0;False;0;0;0;0.4;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;971;6064,4832;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;972;6144,5248;Inherit;False;Property;_FarDis;FarDis;21;0;Create;True;0;0;0;False;0;False;1;1;0;0.32;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;973;5952,5104;Inherit;True;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;974;6336,4848;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;975;6656,4544;Inherit;False;Property;_NearColor;NearColor;18;0;Create;True;0;0;0;False;0;False;0.1803922,0.3019608,0.6039216,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;976;6592,5104;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;977;6544,4848;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;978;6688,4736;Inherit;False;Property;_MidColor;MidColor;19;0;Create;True;0;0;0;False;0;False;0.1176471,0.6156863,0.8196079,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;992;6144,4336;Inherit;False;Reconstruct World Position From Depth;-1;;1;e7094bcbcc80eb140b2a3dbe6a861de8;0;0;1;FLOAT4;0
Node;AmplifyShaderEditor.SwizzleNode;993;6496,4336;Inherit;False;FLOAT3;0;1;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;995;6912,4336;Inherit;False;FLOAT;1;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;996;6816,4176;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;997;7072,4256;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1007;6560,6544;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1008;6384,6608;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.01,0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1009;6576,5776;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ExpOpNode;1010;6736,5776;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1011;6432,5776;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1012;6480,5968;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1013;6848,6432;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode;1014;6144,6304;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SwizzleNode;1015;6400,6352;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1016;6608,6384;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1017;7072,6400;Inherit;True;Property;_FoamNoiseTex;FoamNoiseTex;28;0;Create;True;0;0;0;False;0;False;-1;263d5c613c779d342a61d59e263c2209;d4065fa198330b34ea429abc2824bcbb;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;1019;5504,6288;Inherit;True;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;1020;6256,6448;Inherit;False;Property;_FoamTilling;FoamTilling;31;0;Create;True;0;0;0;False;0;False;20,20;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;1021;6192,6608;Inherit;False;Property;_FoamSpeed;FoamSpeed;32;0;Create;True;0;0;0;False;0;False;3.7,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;1022;6304,5968;Inherit;False;Property;_FoamBlend;FoamBlend;35;0;Create;True;0;0;0;False;0;False;0.26;7.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1023;6160,5904;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;1025;6272,5696;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;1029;5744,6608;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1030;6912,5840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1031;6800,6048;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;1032;6272,6112;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;979;7008,4800;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;980;6976,4560;Inherit;False;Property;_FarColor;FarColor;20;0;Create;True;0;0;0;False;0;False;0.4039216,0.8274511,0.8666667,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;981;6912,5072;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;982;7248,4816;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;994;6672,4336;Inherit;False;PositonFormDepth;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1034;7456,4816;Inherit;False;WaterPlanColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1024;5968,5696;Inherit;True;998;WaterDepth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;969;5456,4864;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;983;9824,5136;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ExpOpNode;999;9664,5168;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;1000;9184,5056;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1001;9120,5248;Inherit;False;Property;_DeepSmooth;DeepSmooth;24;0;Create;True;0;0;0;False;0;False;75;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1002;9120,5152;Inherit;False;Property;_DeepRange;DeepRange;25;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1003;9344,5248;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1004;9376,5104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1005;9536,5168;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;991;9760,5536;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1035;8960,5056;Inherit;False;998;WaterDepth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1027;5920,6048;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0.8;False;4;FLOAT;1.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;1028;5728,6048;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1026;5936,5904;Inherit;False;Property;_FoamWidth;FoamWidth;34;0;Create;True;0;0;0;False;0;False;2.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1041;9008,4192;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;998;8896,4304;Inherit;True;WaterDepth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;1052;8432,4256;Inherit;True;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;1037;7808,4192;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;1040;7776,4384;Inherit;False;Property;_WaterHeight;WaterHeight;17;0;Create;True;0;0;0;False;0;False;-2.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1054;8080,4448;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1053;8256,4448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;989;9536,5344;Inherit;False;Property;_FoamColor;FoamColor;30;0;Create;True;0;0;0;False;0;False;0.854902,0.9215687,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;990;9536,5568;Inherit;False;1033;FormaMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1047;10016,4528;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1044;9792,4496;Inherit;False;998;WaterDepth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;984;10032,4688;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;1006;9584,4736;Inherit;False;Property;_UnderWaterColor;UnderWaterColor;23;0;Create;True;0;0;0;False;0;False;0.1843137,0.3176471,0.5215687,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;1036;9648,4640;Inherit;False;1034;WaterPlanColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;1059;9824,4864;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1057;10176,4816;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1056;10000,4896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1042;10704,4512;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1058;10512,4576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;967;10496,4880;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1062;10352,4800;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1063;10426.28,4687.097;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;1055;10880,4336;Inherit;False;Property;_Water;Water（水开关）;16;0;Create;False;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;1064;-224,3264;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1065;-752,3456;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;1066;-480,3408;Inherit;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;1067;-1104,3648;Inherit;False;True;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;1068;-1728,3296;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;1069;-1760,3488;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1070;-1424,3376;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;1071;-1136,3232;Inherit;True;World;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;1072;-1200,3440;Inherit;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector;1074;-704,3232;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ClampOpNode;1077;80,3200;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1078;880,3216;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1079;576,3168;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1080;576,3424;Inherit;False;Property;_SpecullarSmooth;SpecullarSmooth;38;0;Create;True;0;0;0;False;0;False;0.01;0.01;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1081;336,3424;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1082;16,3488;Inherit;False;Property;_SpecullarRange;SpecullarRange;37;0;Create;True;0;0;0;False;0;False;0.0675128;0.0675128;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1083;1392,3216;Inherit;True;Specullar;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1084;1152,3216;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;885;-2352,4480;Inherit;True;Property;_MainTexture1;MainTexture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;gray;Auto;False;Instance;188;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;888;-2000,4448;Inherit;False;889;Main;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1117;-2672,3152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightAttenuation;478;2128,4896;Inherit;True;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;891;3632,4928;Inherit;False;Shadow;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;1118;1552,4944;Inherit;False;Property;_Specullar;Specullar;36;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;894;1760,4960;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;710;1936,4960;Inherit;False;OutColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;893;1472,5200;Inherit;False;891;Shadow;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;1087;448,4480;Inherit;False;Property;_SpecullarColor;SpecullarColor;39;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1085;752,4704;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;505;3040,4944;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1131;3232,5056;Inherit;False;ShadowMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;1088;1216,4800;Inherit;True;LinearLight;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1086;464,4720;Inherit;True;1083;Specullar;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1132;624,4944;Inherit;True;1131;ShadowMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1135;1088,4848;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;1136;11440,4496;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;844;10096,4160;Inherit;False;710;OutColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1133;960,4832;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1134;816,4944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;873;944,5088;Inherit;False;Render02;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;898;384,5216;Inherit;False;889;Main;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;889;-1328,3712;Inherit;False;Main;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;1119;-3344,3584;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;187;-3440,3792;Inherit;False;Property;_Maincolor;Maincolor（主贴图颜色）;1;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;1107;-3744,3488;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;188;-4128,3552;Inherit;True;Property;_MainTexture;MainTexture(主贴图);0;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;519;-3120,3632;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FmodOpNode;1090;-4224,3152;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1097;-5168,3248;Inherit;False;Property;_GirdSize;GirdSize;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectPositionNode;1098;-5200,3072;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;1093;-4544,3152;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;1094;-4672,3136;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.FloorOpNode;1095;-4816,3136;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;1096;-4976,3136;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.AbsOpNode;1089;-4336,3152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;1092;-4032,3168;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1105;-4256,2944;Inherit;True;Property;_MainTexture2;MainTexture(主贴图);0;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;188;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;1106;-4496,2944;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;1108;-4720,2992;Inherit;False;Constant;_Vector0;Vector 0;41;0;Create;True;0;0;0;False;0;False;0,-0.125;0.001953125,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;1151;-2240,3616;Inherit;False;Property;_Float1;Float 1;42;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1146;-1632,3728;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;1148;-2224,4032;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;1153;-2480,3792;Inherit;False;Property;_Float2;Float 2;43;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1145;-2400,4016;Inherit;False;1141;UVMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1147;-2032,4096;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1144;-2624,3936;Inherit;False;Property;_UnlockColor;UnlockColor;41;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RGBToHSVNode;1152;-2288,3872;Half;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.HSVToRGBNode;1149;-2000,3792;Half;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;1154;-2208,3712;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;1155;-2048,3952;Inherit;False;ColorBurn;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.RGBToHSVNode;1150;-2816,3568;Half;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;256;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;257;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;True;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;258;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;259;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;260;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;261;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;262;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;263;1536,-1360;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;254;2720,-160;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;True;True;6;2;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;True;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;True;True;True;0;False;;255;False;;255;False;;5;False;;7;False;;1;False;;1;False;;5;False;;7;False;;1;False;;1;False;;True;True;2;False;;True;3;False;;True;True;-1;False;;0;False;;True;1;LightMode=;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;255;11584,4416;Half;False;True;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;SceneCommon;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;6;d3d11;glcore;gles;gles3;metal;vulkan;0;True;True;0;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;;0;0;Standard;22;Surface;0;0;  Blend;0;0;Two Sided;1;0;Forward Only;0;0;Cast Shadows;1;638784025872682783;  Use Shadow Threshold;0;0;Receive Shadows;0;638644948248460716;GPU Instancing;1;638784025957154694;LOD CrossFade;0;638644948270387164;Built-in Fog;0;638852238597649686;Meta Pass;0;638663259646609848;Extra Pre Pass;0;638784025916513883;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;10;False;True;True;False;False;False;False;False;False;False;False;;False;0
WireConnection;345;0;342;0
WireConnection;345;1;346;0
WireConnection;340;0;345;0
WireConnection;340;1;339;0
WireConnection;621;0;340;0
WireConnection;621;1;622;0
WireConnection;348;0;621;0
WireConnection;487;0;349;0
WireConnection;487;1;489;0
WireConnection;316;0;328;0
WireConnection;347;0;621;0
WireConnection;488;0;487;0
WireConnection;318;0;348;0
WireConnection;318;1;316;0
WireConnection;320;0;347;0
WireConnection;320;1;316;0
WireConnection;321;0;488;0
WireConnection;321;1;318;0
WireConnection;955;0;324;3
WireConnection;322;0;320;0
WireConnection;322;1;488;0
WireConnection;323;0;321;0
WireConnection;323;1;350;0
WireConnection;954;0;324;1
WireConnection;954;1;955;0
WireConnection;956;0;955;0
WireConnection;956;1;324;2
WireConnection;325;0;322;0
WireConnection;325;1;954;0
WireConnection;326;0;956;0
WireConnection;326;1;323;0
WireConnection;1130;1;1115;0
WireConnection;327;0;325;0
WireConnection;327;1;326;0
WireConnection;1114;0;1111;2
WireConnection;1114;1;1130;0
WireConnection;850;1;620;0
WireConnection;850;0;327;0
WireConnection;1141;0;1114;0
WireConnection;351;0;850;0
WireConnection;1138;0;1137;0
WireConnection;1138;1;1139;0
WireConnection;1143;0;1140;0
WireConnection;1142;0;1138;0
WireConnection;1142;1;1143;0
WireConnection;436;0;354;0
WireConnection;334;0;329;1
WireConnection;334;1;329;2
WireConnection;334;2;329;3
WireConnection;600;0;334;0
WireConnection;332;0;600;0
WireConnection;331;0;332;0
WireConnection;335;0;329;1
WireConnection;335;1;329;3
WireConnection;336;0;335;0
WireConnection;337;0;336;0
WireConnection;592;0;528;0
WireConnection;592;1;595;0
WireConnection;338;0;337;0
WireConnection;596;0;592;0
WireConnection;590;0;597;0
WireConnection;589;0;597;0
WireConnection;863;0;881;4
WireConnection;864;0;882;3
WireConnection;864;1;863;0
WireConnection;865;0;883;0
WireConnection;865;1;888;0
WireConnection;865;2;884;0
WireConnection;867;0;881;0
WireConnection;867;1;865;0
WireConnection;867;2;864;0
WireConnection;868;0;874;4
WireConnection;868;1;866;0
WireConnection;870;0;874;0
WireConnection;870;1;867;0
WireConnection;870;2;868;0
WireConnection;872;0;870;0
WireConnection;872;1;869;0
WireConnection;872;2;871;0
WireConnection;877;0;875;0
WireConnection;877;1;876;0
WireConnection;878;0;877;0
WireConnection;879;1;887;0
WireConnection;880;0;879;0
WireConnection;882;1;880;0
WireConnection;884;0;883;4
WireConnection;884;1;882;1
WireConnection;887;0;878;0
WireConnection;871;0;869;4
WireConnection;871;1;866;0
WireConnection;897;0;872;0
WireConnection;897;1;898;0
WireConnection;897;2;896;0
WireConnection;849;1;341;0
WireConnection;315;1;338;0
WireConnection;584;1;589;0
WireConnection;584;2;528;0
WireConnection;597;0;621;0
WireConnection;597;1;594;0
WireConnection;591;1;590;0
WireConnection;591;2;528;0
WireConnection;527;1;596;0
WireConnection;617;0;958;0
WireConnection;501;0;502;2
WireConnection;481;1;479;0
WireConnection;481;2;505;0
WireConnection;958;0;598;0
WireConnection;958;1;959;0
WireConnection;598;0;478;0
WireConnection;866;0;882;2
WireConnection;866;1;864;0
WireConnection;962;0;882;2
WireConnection;599;1;849;0
WireConnection;896;0;895;2
WireConnection;896;1;899;0
WireConnection;1018;0;1017;1
WireConnection;1018;1;1030;0
WireConnection;1033;0;1018;0
WireConnection;971;0;969;0
WireConnection;971;1;970;0
WireConnection;973;1;969;0
WireConnection;974;0;971;0
WireConnection;976;0;973;0
WireConnection;976;1;972;0
WireConnection;977;0;974;0
WireConnection;993;0;992;0
WireConnection;995;0;994;0
WireConnection;997;0;996;2
WireConnection;997;1;995;0
WireConnection;1007;0;1008;0
WireConnection;1007;1;1029;0
WireConnection;1008;0;1021;0
WireConnection;1009;0;1011;0
WireConnection;1009;1;1012;0
WireConnection;1010;0;1009;0
WireConnection;1011;0;1025;0
WireConnection;1011;1;1023;0
WireConnection;1012;0;1022;0
WireConnection;1012;1;1027;0
WireConnection;1013;0;1016;0
WireConnection;1013;1;1007;0
WireConnection;1015;0;1014;0
WireConnection;1016;0;1015;0
WireConnection;1016;1;1020;0
WireConnection;1017;1;1013;0
WireConnection;1023;0;1026;0
WireConnection;1023;1;1027;0
WireConnection;1025;0;1024;0
WireConnection;1029;0;1019;0
WireConnection;1030;0;1010;0
WireConnection;1030;1;1031;0
WireConnection;1031;0;1032;2
WireConnection;979;0;975;0
WireConnection;979;1;978;0
WireConnection;979;2;977;0
WireConnection;981;0;976;0
WireConnection;982;0;980;0
WireConnection;982;1;979;0
WireConnection;982;2;981;0
WireConnection;994;0;993;0
WireConnection;1034;0;982;0
WireConnection;969;0;968;2
WireConnection;983;0;999;0
WireConnection;999;0;1005;0
WireConnection;1000;0;1035;0
WireConnection;1003;0;1001;0
WireConnection;1004;0;1000;0
WireConnection;1004;1;1002;0
WireConnection;1005;0;1004;0
WireConnection;1005;1;1003;0
WireConnection;991;0;989;4
WireConnection;991;1;990;0
WireConnection;1027;0;1028;0
WireConnection;1028;0;1019;0
WireConnection;998;0;1052;0
WireConnection;1052;0;1037;2
WireConnection;1052;1;1040;0
WireConnection;1052;4;1053;0
WireConnection;1054;0;1037;2
WireConnection;1054;1;1040;0
WireConnection;1053;0;1054;0
WireConnection;1047;0;1044;0
WireConnection;984;0;1036;0
WireConnection;984;1;1006;0
WireConnection;984;2;983;0
WireConnection;1059;0;1006;4
WireConnection;1057;0;1056;0
WireConnection;1056;0;1059;0
WireConnection;1056;1;983;0
WireConnection;1042;0;844;0
WireConnection;1042;1;967;0
WireConnection;1042;2;1058;0
WireConnection;1058;0;1047;0
WireConnection;1058;1;1063;0
WireConnection;967;0;984;0
WireConnection;967;1;989;0
WireConnection;967;2;991;0
WireConnection;1062;0;1057;0
WireConnection;1062;1;991;0
WireConnection;1063;0;1062;0
WireConnection;1055;1;844;0
WireConnection;1055;0;1042;0
WireConnection;1064;0;1074;0
WireConnection;1064;1;1066;0
WireConnection;1065;0;1071;0
WireConnection;1065;1;1067;0
WireConnection;1066;0;1065;0
WireConnection;1070;0;1068;0
WireConnection;1070;1;1069;0
WireConnection;1072;0;1070;0
WireConnection;1077;0;1064;0
WireConnection;1078;0;1079;0
WireConnection;1078;1;1080;0
WireConnection;1079;0;1077;0
WireConnection;1079;1;1081;0
WireConnection;1081;0;1082;0
WireConnection;1083;0;1084;0
WireConnection;1084;0;1078;0
WireConnection;891;0;481;0
WireConnection;1118;0;873;0
WireConnection;1118;1;1088;0
WireConnection;894;0;1118;0
WireConnection;894;1;893;0
WireConnection;710;0;894;0
WireConnection;1085;0;1087;4
WireConnection;1085;1;1086;0
WireConnection;505;0;479;4
WireConnection;505;1;617;0
WireConnection;505;2;501;0
WireConnection;1131;0;505;0
WireConnection;1088;0;1087;0
WireConnection;1088;1;873;0
WireConnection;1088;2;1135;0
WireConnection;1135;0;1133;0
WireConnection;1136;0;436;0
WireConnection;1136;1;1142;0
WireConnection;1133;0;1085;0
WireConnection;1133;1;1134;0
WireConnection;1134;0;1132;0
WireConnection;873;0;897;0
WireConnection;889;0;1146;0
WireConnection;1119;0;1107;0
WireConnection;1119;1;188;0
WireConnection;1119;2;1141;0
WireConnection;1107;0;1105;0
WireConnection;1107;1;188;0
WireConnection;1107;2;1092;0
WireConnection;519;0;1119;0
WireConnection;519;1;187;0
WireConnection;1090;0;1089;0
WireConnection;1093;0;1094;0
WireConnection;1093;1;1094;2
WireConnection;1094;0;1095;0
WireConnection;1095;0;1096;0
WireConnection;1096;0;1098;0
WireConnection;1096;1;1097;0
WireConnection;1089;0;1093;0
WireConnection;1092;0;1090;0
WireConnection;1105;1;1106;0
WireConnection;1106;1;1108;0
WireConnection;1146;0;519;0
WireConnection;1146;1;1149;0
WireConnection;1146;2;1147;0
WireConnection;1148;0;1145;0
WireConnection;1147;0;1148;0
WireConnection;1147;1;1144;4
WireConnection;1152;0;1144;0
WireConnection;1149;0;1152;1
WireConnection;1149;1;1150;2
WireConnection;1149;2;1150;3
WireConnection;1154;0;1150;1
WireConnection;1154;1;1153;0
WireConnection;1150;0;519;0
WireConnection;255;2;1055;0
WireConnection;255;5;1136;0
ASEEND*/
//CHKSM=ED1102A8ADE7AA2AB1591CC9822C77D8A3430C05