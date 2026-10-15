Shader "Hidden/XFur Studio 2/XF Shells"
{
    Properties {
		[PerRendererData] _XFurSelfStrandsPattern("Strands Pattern", 2D) = "white"{}
		[PerRendererData] _XFurParamData("XFur Params Data", 2D) = "white"{}
		[PerRendererData] _XFurSelfUnderColorMod("XFur Fur Layer R Intensity", Range(0,0.65)) = 0.25
		[PerRendererData] _XFurSelfOverColorMod("XFur Fur Layer G Intensity", Range(0,0.75)) = 0.5
		[PerRendererData] _XFurSelfColorVariation("XFur Color Variation Map", 2D) = "black"{}
		[PerRendererData] _XFurSelfColor("XFur Main Tint", Color) = (1,1,1,1)
		[PerRendererData] _XFurSelfColorA("XFur Color Variation A", Color) = (1,1,1,1)
		[PerRendererData] _XFurSelfColorB("XFur Color Variation B", Color) = (1,1,1,1)
		[PerRendererData] _XFurSelfColorC("XFur Color Variation C", Color) = (1,1,1,1)
		[PerRendererData] _XFurSelfColorD("XFur Color Variation D", Color) = (1,1,1,1)
		[PerRendererData] _XFurUVTiling("XFur Strands Tiling", Float) = 2
		[PerRendererData] _XFurProfilesSplat("Profiles Splat Map", 2D ) = "black"{}
		[Enum(UnityEngine.Rendering.CullMode)] _XFurCullFur("Fur Cull Mode", Int) = 2
		[PerRendererData] _XFurHasGroomData("Has Groom Data", Float ) = 0
		[PerRendererData] _XFurVFXMask( "VFX Mask", 2D ) = "black"{}
		[PerRendererData] _XFurVFX1Color("VFX1 Color", Color) = (0.75, 0.05, 0.05, 1)
		[PerRendererData] _XFurVFX2Color("VFX2 Color", Color) = (0.85, 0.95, 1, 1)
		[PerRendererData] _XFurPhysics("XFur Physics", 2D) = "black"{}
		[PerRendererData] _XFurSelfRimPower("XFur Rim Power", Range(1,8)) = 2
		[PerRendererData] _XFurRimColor("XFur Rim Color", Color) = (1,1,1,1)
		[PerRendererData] _XFurEmissionMap("Fur Emission Map", 2D) = "black"{}
		[PerRendererData] _XFurNormalmap("XFur Normalmap", 2D) = "bump"{}
	
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			Cull[_XFurCullFur]


			CGPROGRAM

			#pragma target 3.0
			#pragma surface XFShellSurfacePass Standard fullforwardshadows vertex:XFShellVert addshadow
			#pragma shader_feature_local FEATURESET_MOBILE
			#pragma shader_feature_local FURPROFILES_BLENDED
			#pragma shader_feature_local FURDATA_BAKED
			#pragma shader_feature_local USE_NORMALMAP

			float _XFurAnisoOffset;

#define FUR_PASS 0

		#include "CGIncludes/XFurStudio2_StandardPasses.cginc"

        ENDCG
    }
    FallBack "Diffuse"
}
