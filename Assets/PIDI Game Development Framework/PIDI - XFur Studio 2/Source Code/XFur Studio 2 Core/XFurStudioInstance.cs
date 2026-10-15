/*

XFur Studio™ 2, by Irreverent Software™
Copyright© 2018-2020, Jorge Pinal Negrete. All Rights Reserved.

*/


namespace XFurStudio2
{

    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [System.Serializable]
    public struct XFurRendererData
    {

        public Renderer renderer;

        public Material[] materials;

        public Mesh originalMesh;

        public bool brokenData;

        public bool[] isFurMaterial;

        public bool isLOD;

        public int[] furProfiles;

        public void AssignRenderer(Renderer rend) {

            isLOD = false;
            renderer = rend;

            if (!rend) {
                materials = new Material[0];
                originalMesh = null;
                return;
            }

            materials = rend.sharedMaterials;

            var tempBools = new bool[materials.Length];

            if (isFurMaterial != null) {
                for (int i = 0; i < tempBools.Length; ++i) {
                    if (isFurMaterial.Length > i)
                        tempBools[i] = isFurMaterial[i];
                }
            }

            isFurMaterial = tempBools;

            var tempInts = new int[materials.Length];

            if (furProfiles != null) {
                for (int i = 0; i < tempInts.Length; ++i) {
                    if (furProfiles.Length > i)
                        tempInts[i] = furProfiles[i];
                }
            }
            else {
                furProfiles = new int[rend.sharedMaterials.Length];
            }

            if (!isLOD) {
                for (int i = 0; i < furProfiles.Length; ++i) {
                    furProfiles[i] = -1;
                }
            }

            furProfiles = tempInts;

            if (rend.GetType() == typeof(SkinnedMeshRenderer)) {
                originalMesh = ((SkinnedMeshRenderer)rend).sharedMesh;
            }
            else if (rend.GetType() == typeof(MeshRenderer)) {
                originalMesh = rend.GetComponent<MeshFilter>().sharedMesh;
            }

        }

    }




    [ExecuteAlways]
    [DefaultExecutionOrder(99000)]
    public class XFurStudioInstance : MonoBehaviour
    {

        public enum XFurRenderingMode { XFShells, BasicShells }

        /// <summary> The rendering method to be used on this XFur Instance </summary>
        public XFurRenderingMode RenderingMode = XFurRenderingMode.XFShells;


        public bool UseLossyScale;

        public bool BetaMode = false;

#if UNITY_EDITOR
        public bool ShowAdvancedProperties = false;
#endif

        //public bool FurProfilesBlending = true;

        /// <summary> Whether this XFur instance will be rendered in a way compatible with full forward lighting (point lights)</summary>
        public bool FullForwardMode = false;


        /// <summary>The current version of this asset. Used internally for the auto-update routines and UI displays</summary>
        private string AssetVersion = "v2.5.5";

        public string Version { get { return AssetVersion; } }

        private float timedUpdate;

        /// <summary>
        /// Whether the fur properties will be updated and applied automatically
        /// </summary>
        public bool AutoUpdateMaterials = true;

        /// <summary>
        /// Whether the properties will be adjusted to take into account the model's scale
        /// </summary>
        public bool AutoAdjustForScale = false;


        /// <summary>
        /// The interval of time between each automatic materials update
        /// </summary>
        public float UpdateFrequency = 0.1f;

        public XFurStudioDatabase FurDatabase {
            get { return furDatabase; }
#if UNITY_EDITOR
            set { furDatabase = value; }
#endif
        }

        /// <summary> The database asset to be used by this XFur Instance</summary>
        [SerializeField] protected XFurStudioDatabase furDatabase;

        /// <summary> The Fur Template profiles to be assigned to each material</summary>
        [SerializeField] protected XFurTemplate[] furDataProfiles = new XFurTemplate[0];

        /// <summary> The Fur Template profiles to be assigned to each material</summary>
        public XFurTemplate[] FurDataProfiles { get { return furDataProfiles; } }

        /// <summary>The main renderer from which bones, materials, profiles, normals etc. will be referenced </summary>
        [SerializeField] protected XFurRendererData mainRenderer = new XFurRendererData();

        /// <summary>Randomization module (built in)</summary>
        [SerializeField] protected XFurStudio2_Random randomModule;

        /// <summary>LOD module (built in)</summary>
        [SerializeField] protected XFurStudio2_LOD lodModule;

        /// <summary>Physics module (built in)</summary>
        [SerializeField] protected XFurStudio2_Physics physicsModule;

        /// <summary>VFX module (built in)</summary>
        [SerializeField] protected XFurStudio2_VFX vfxModule;

        /// <summary>Decals module (built in)</summary>
        [SerializeField] protected XFurStudio2_Decals decalsModule;

        private Mesh fMesh;

        [SerializeField] private MaterialPropertyBlock mBlock;

        /// <summary> Gives access to the internal Material blocks that represent each fur material, allowing you to modify their data right before it is passed to the renderer. Useful for modules such as physics, randomization, weather etc. where the fur appearance should be modified on the fly and, in cases, independently from the regular fur updates</summary>
        private MaterialPropertyBlock[] furBlocks = new MaterialPropertyBlock[0];

        /// <summary>The randomization module assigned to this instance</summary>
        public XFurStudio2_Random RandomizationModule { get { return randomModule; } }

        /// <summary>The Dynamic LOD module assigned to this instance</summary>
        public XFurStudio2_LOD LODModule { get { return lodModule; } }

        /// <summary>The Physics Module assigned to this instance</summary>
        public XFurStudio2_Physics PhysicsModule { get { return physicsModule; } }

        /// <summary>The VFX Module assigned to this instance</summary>
        public XFurStudio2_VFX VFXModule { get { return vfxModule; } }

        /// <summary>The Decals Module assigned to this instance</summary>
        public XFurStudio2_Decals DecalsModule { get { return decalsModule; } }

        /// <summary>Currently unused</summary>
        [SerializeField] protected List<XFurStudioModule> customModules = new List<XFurStudioModule>();

        static List<float> passes;

        private Vector3 tPos;
        private Vector3 tScale;
        private Quaternion tRot;

        private bool isSkinned;

        /// <summary>The current, active mesh being rendered by this XFur Instance</summary>
        public Mesh CurrentMesh { get { return fMesh ? fMesh : CurrentFurRenderer.originalMesh; } }

        private Matrix4x4[] tMatrices;

        /// <summary>The current, active renderer used by this XFur Instance</summary>
        public XFurRendererData CurrentFurRenderer;

        [System.NonSerialized] public bool skipRenderFrame;

        [SerializeField] private XFurRenderingMode currentRenderingMode;

        public static XFurStudio2_WindZone WindZone;

        private Plane[] frustumPlanes = new Plane[6];

        private Camera[] allCameras = new Camera[0];

        private Material[] temps = new Material[0];

        private Material[] currs = new Material[0];

        private XFurRenderingMode buffMode;

        #region SHADER PROPERTIES

        //XFur MODULES properties
        readonly static int _xfurVFXMap = Shader.PropertyToID("_XFurVFXMask");
        readonly static int _xfurPhysicsMap = Shader.PropertyToID("_XFurPhysics");

        //INTERNAL DATA        
        readonly static int _xfurTotalPasses = Shader.PropertyToID("_XFurTotalPasses");
        readonly static int _xfurInstancedCurrentPass = Shader.PropertyToID("_XFurInstancedCurrentPass");
        readonly static int _xfurForceNonInstanced = Shader.PropertyToID("_XFurForceNonInstanced");
        readonly static int _xfurCurrentPass = Shader.PropertyToID("_XFurCurrentPass");

        //HDRP Helpers
        readonly static int _xfurHDWorld2Local = Shader.PropertyToID("_XFurHDWorld2LocalMatrix");
        readonly static int _xfurHDLocal2World = Shader.PropertyToID("_XFurHDLocal2WorldMatrix");


        //SKIN RENDERING DATA
        readonly static int _xfurSkinColor = Shader.PropertyToID("_XFurSkinColor");
        readonly static int _xfurSkinSmoothness = Shader.PropertyToID("_XFurSkinSmoothness");
        readonly static int _xfurSkinColorMap = Shader.PropertyToID("_XFurSkinMap");
        readonly static int _xfurSkinNormalMap = Shader.PropertyToID("_XFurSkinNormal");


        //FUR PROPERTIES
        readonly static int _xfurSelfStrandsPattern = Shader.PropertyToID("_XFurSelfStrandsPattern");
        readonly static int _xfurHasGroomData = Shader.PropertyToID("_XFurHasGroomData");
        readonly static int _xfurGroomData = Shader.PropertyToID("_XFurGroomData");
        readonly static int _xfurParamData = Shader.PropertyToID("_XFurParamData");
        readonly static int _xfurSelfGroomStrength = Shader.PropertyToID("_XFurSelfGroomStrength");

        readonly static int _xfurMainColorMap = Shader.PropertyToID("_XFurMainColorMap");
        readonly static int _xfurNormalmap = Shader.PropertyToID( "_XFurNormalmap" );
        readonly static int _xfurSelfBaseTiling = Shader.PropertyToID("_XFurSelfBaseTiling");
        readonly static int _xfurEmissionMap = Shader.PropertyToID("_XFurEmissionMap");
        readonly static int _xfurEmissionColor = Shader.PropertyToID("_XFurEmissionColor");
        readonly static int _xfurSelfColor = Shader.PropertyToID("_XFurSelfColor");
        readonly static int _xfurSelfSpecularTint = Shader.PropertyToID("_XFurSelfSpecularTint");
        readonly static int _xfurSelfTransmission = Shader.PropertyToID("_XFurSelfTransmission");
        readonly static int _xfurSelfRimPower = Shader.PropertyToID("_XFurSelfRimPower");
        readonly static int _xfurSelfRimBoost = Shader.PropertyToID("_XFurSelfRimBoost");
        readonly static int _xfurSelfRimColor = Shader.PropertyToID("_XFurSelfRimColor");

        readonly static int _xfurSelfColorVariation = Shader.PropertyToID("_XFurSelfColorVariation");
        readonly static int _xfurSelfUnderColorMod = Shader.PropertyToID("_XFurSelfUnderColorMod");
        readonly static int _xfurSelfOverColorMod = Shader.PropertyToID("_XFurSelfOverColorMod");

        readonly static int _xfurSelfLength = Shader.PropertyToID("_XFurSelfLength");
        readonly static int _xfurSelfSmoothness = Shader.PropertyToID("_XFurSelfSmoothness");
        readonly static int _xfurSelfThickness = Shader.PropertyToID("_XFurSelfThickness");
        readonly static int _xfurSelfThicknessCurve = Shader.PropertyToID("_XFurSelfThicknessCurve");
        readonly static int _xfurSelfShadowsTint = Shader.PropertyToID("_XFurSelfShadowsTint");
        readonly static int _xfurSelfOcclusion = Shader.PropertyToID("_XFurSelfOcclusion");
        readonly static int _xfurSelfOcclusionCurve = Shader.PropertyToID("_XFurSelfOcclusionCurve");

        //COLOR BLENDING
        readonly static int _xfurSelfColorA = Shader.PropertyToID("_XFurSelfColorA");
        readonly static int _xfurSelfColorB = Shader.PropertyToID("_XFurSelfColorB");
        readonly static int _xfurSelfColorC = Shader.PropertyToID("_XFurSelfColorC");
        readonly static int _xfurSelfColorD = Shader.PropertyToID("_XFurSelfColorD");

        //FUR BETA PROPERTIES

        readonly static int _xfurSelfCurlAmountX = Shader.PropertyToID("_XFurSelfCurlAmountX");
        readonly static int _xfurSelfCurlAmountY = Shader.PropertyToID("_XFurSelfCurlAmountY");
        readonly static int _xfurSelfCurlSizeX = Shader.PropertyToID("_XFurSelfCurlSizeX");
        readonly static int _xfurSelfCurlSizeY = Shader.PropertyToID("_XFurSelfCurlSizeY");

        readonly static int _xfurSelfWindStrength = Shader.PropertyToID("_XFurSelfWindStrength");
        readonly static int _xfurUVTiling = Shader.PropertyToID("_XFurUVTiling");


        #endregion

        void OnEnable() {

            InitialSetup();

            ForceUpdateAllCameras();

            buffMode = RenderingMode;

            if (!WindZone) {
                WindZone = GameObject.FindObjectOfType<XFurStudio2_WindZone>();
            }

            if (!Application.isPlaying) {
                if (randomModule) {
                    randomModule.Unload();
                }

                if (lodModule) {
                    lodModule.Unload();
                }

                if (vfxModule) {
                    vfxModule.Unload();
                }

                if (physicsModule) {
                    physicsModule.Unload();
                }


                if ( !AutoUpdateMaterials ) {
                    AutoUpdateMaterials = true;
                    RenderFur();
                    AutoUpdateMaterials = false;
                }
                else {
                    AutoUpdateMaterials = false;
                    RenderFur();
                    AutoUpdateMaterials = true;
                    RenderFur();
                }
            }



        }


        private IEnumerator Start() {


            if (Application.isPlaying) {
                if (randomModule && randomModule.enabled) {
                    randomModule.Load();
                }

                if (lodModule && lodModule.enabled) {
                    lodModule.Load();
                }

                if (vfxModule && vfxModule.enabled) {
                    vfxModule.Load();
                }

                if (physicsModule && physicsModule.enabled) {
                    physicsModule.Load();
                }


                if (!AutoUpdateMaterials) {
                    AutoUpdateMaterials = true;
                    RenderFur();
                    AutoUpdateMaterials = false;
                }

            }

            yield return new WaitForSeconds(0.5f);


            if (!AutoUpdateMaterials) {
                AutoUpdateMaterials = true;
                RenderFur();
                AutoUpdateMaterials = false;
            }


            yield break;

        }


#if XFURDESKTOP_LEGACY

        public void LoadLegacyXFurProfile( int index, XFurStudio.XFur_CoatingProfile furTemplate ) {

            furDataProfiles[index].FurColorMap = furTemplate.profile.furColorMap;
            furDataProfiles[index].FurData0 = furTemplate.profile.furDataMap;
            furDataProfiles[index].FurLength = furTemplate.profile.furLength * 0.5f;
            furDataProfiles[index].FurThickness = furTemplate.profile.furThickness;
            furDataProfiles[index].FurOcclusion = furTemplate.profile.furOcclusion * 0.5f;
            furDataProfiles[index].FurMainTint = furTemplate.profile.furColorA;
            furDataProfiles[index].FurRim = furTemplate.profile.furRimColor;
            furDataProfiles[index].FurRimPower = furTemplate.profile.furRimPower * 4.5f;
            furDataProfiles[index].FurSamples = Mathf.Clamp( furTemplate.profile.furmatSamples * 8, 4, 32 );
            furDataProfiles[index].FurUVTiling = furTemplate.profile.furUVScale * 2;
            furDataProfiles[index].SkinColor = furTemplate.profile.skinColor;
            furDataProfiles[index].SkinColorMap = furTemplate.profile.skinTexture;
            furDataProfiles[index].SkinNormalMap = furTemplate.profile.skinNormalmap;

        }

#endif



#if XFurStudioMobile_LEGACY

        public void LoadLegacyXFurProfile( int index, XFurStudioMobile.XFur_CoatingProfile furTemplate ) {

            furDataProfiles[index].FurColorMap = furTemplate.profile.furColorMap;
            furDataProfiles[index].FurData0 = furTemplate.profile.furDataMap;
            furDataProfiles[index].FurLength = furTemplate.profile.furLength * 0.5f;
            furDataProfiles[index].FurThickness = furTemplate.profile.furThickness;
            furDataProfiles[index].FurOcclusion = furTemplate.profile.furOcclusion * 0.5f;
            furDataProfiles[index].FurMainTint = furTemplate.profile.furColorA;
            furDataProfiles[index].FurRim = furTemplate.profile.furRimColor;
            furDataProfiles[index].FurRimPower = furTemplate.profile.furRimPower * 4.5f;
            furDataProfiles[index].FurSamples = Mathf.Clamp( furTemplate.profile.furmatSamples * 8, 4, 32 );
            furDataProfiles[index].FurUVTiling = furTemplate.profile.furUVScale * 2;
            furDataProfiles[index].SkinColor = furTemplate.profile.skinColor;
            furDataProfiles[index].SkinColorMap = furTemplate.profile.skinTexture;
            furDataProfiles[index].SkinNormalMap = furTemplate.profile.skinNormalmap;

        }

#endif

        /// <summary>
        /// Forces the XFur Studio Instance to update all the cameras in the scene. Useful when you add or remove cameras at runtime
        /// </summary>
        public void ForceUpdateAllCameras()
        {
            allCameras = Camera.allCameras;
        }

        /// <summary>
        /// Experimental. Allows you to set the Main Renderer of this XFur Studio Instance at runtime
        /// </summary>
        /// <param name="renderer"></param>
        public void SetMainRenderer(Renderer renderer) {
            mainRenderer.AssignRenderer(renderer);
        }


        /// <summary>
        /// Retrieves the settings and properties of a fur material in use by this instance as an XFur Template
        /// </summary>
        /// <param name="index">The index of the material from which we will retrieve the properties</param>
        /// <param name="furTemplate">The output XFurTemplate with the copy of the properties</param>
        public void GetFurData(int index, out XFurTemplate furTemplate) {

            if (index < furDataProfiles.Length) {
                furTemplate = furDataProfiles[index];
                return;
            }

            furTemplate = new XFurTemplate(true);

        }

        /// <summary>
        /// Assigns an XFur Template with fur properties to the given material in this instance
        /// </summary>
        /// <param name="index">The index of the material to set</param>
        /// <param name="furTemplate">The template from where the properties will be copied</param>
        public void SetFurDataFull(int index, XFurTemplate furTemplate) {

            furTemplate.Update(Version);

            if (index < furDataProfiles.Length) {
                furDataProfiles[index] = furTemplate;
                UpdateFurMaterials(true);
            }

        }


        /// <summary>
        /// Assigns an XFur Template with fur properties to the given material in this instance
        /// </summary>
        /// <param name="index">The index of the material to set</param>
        /// <param name="furTemplate">The template from where the properties will be copied</param>
        /// <param name="replaceColorMap">Whether the fur color map should be set or left unchanged</param>
        /// <param name="replaceDataMaps">Whether the fur data maps should be set or left unchanged</param>
        /// <param name="replaceColorMix">Whether the fur color variation map should be set or left unchanged</param>
        /// <param name="replaceStrandsMap">Whether the fur strands asset should be set or left unchanged</param>
        public void SetFurData(int index, XFurTemplate furTemplate, bool replaceColorMap = false, bool replaceDataMaps = false, bool replaceColorMix = false, bool replaceStrandsMap = false) {

            furTemplate.Update(Version);

            if (index < furDataProfiles.Length) {
                furDataProfiles[index].FurMainTint = furTemplate.FurMainTint;
                furDataProfiles[index].FurLength = furTemplate.FurLength;
                furDataProfiles[index].FurThickness = furTemplate.FurThickness;
                furDataProfiles[index].FurThicknessCurve = furTemplate.FurThicknessCurve;
                furDataProfiles[index].FurOcclusion = furTemplate.FurOcclusionCurve;
                furDataProfiles[index].FurShadowsTint = furTemplate.FurShadowsTint;

                if (replaceColorMap) {
                    furDataProfiles[index].FurColorMap = furTemplate.FurColorMap;
                }

                if (replaceDataMaps) {
                    furDataProfiles[index].FurData0 = furTemplate.FurData0;
                    furDataProfiles[index].FurData1 = furTemplate.FurData1;
                }

                if (replaceColorMix) {
                    furDataProfiles[index].FurColorVariation = furTemplate.FurColorVariation;
                    furDataProfiles[index].FurColorA = furTemplate.FurColorA;
                    furDataProfiles[index].FurColorB = furTemplate.FurColorB;
                    furDataProfiles[index].FurColorC = furTemplate.FurColorC;
                    furDataProfiles[index].FurColorD = furTemplate.FurColorD;
                }

                if (replaceStrandsMap) {
                    furDataProfiles[index].FurStrandsAsset = furTemplate.FurStrandsAsset;
                }

            }

        }


        /// <summary>
        /// Assigns an XFur Template with fur properties to the given material in this instance
        /// </summary>
        /// <param name="index">The index of the material to set</param>
        /// <param name="furProfileAsset">The fur profile asset that contains the template from where the properties will be copied</param>
        public void SetFurProfileAsset(int index, XFurStudioFurProfile furProfileAsset) {

            if (index < furDataProfiles.Length) {
                XFurTemplate furTemplate = furProfileAsset.FurTemplate;
                furTemplate.Update(Version);
                SetFurDataFull(index, furTemplate);
            }

        }


        /// <summary>
        /// Assigns an XFur Template with fur properties to the given material in this instance
        /// </summary>
        /// <param name="index">The index of the material to set</param>
        /// <param name="furProfileAsset">The fur profile asset that contains the template from where the properties will be copied</param>
        /// <param name="replaceColorMap">Whether the fur color map should be set or left unchanged</param>
        /// <param name="replaceDataMaps">Whether the fur data maps should be set or left unchanged</param>
        /// <param name="replaceColorMix">Whether the fur color variation map should be set or left unchanged</param>
        /// <param name="replaceStrandsMap">Whether the fur strands asset should be set or left unchanged</param>
        public void SetFurProfileAssetExtended(int index, XFurStudioFurProfile furProfileAsset, bool replaceColorMap = false, bool replaceDataMaps = false, bool replaceColorMix = false, bool replaceStrandsMap = false) {

            if (index < furDataProfiles.Length) {
                XFurTemplate furTemplate = furProfileAsset.FurTemplate;
                SetFurData(index, furTemplate, replaceColorMap, replaceDataMaps, replaceColorMix, replaceStrandsMap);
            }

        }



        /// <summary>
        /// Applies all changes to the fur properties across all materials and forcefully updates any modules that work on the render loop
        /// </summary>
        /// <param name="forceUpdate">Whether to force the update regardless of the Auto-update settings</param>
        public void UpdateFurMaterials(bool forceUpdate = false) {

            if (passes == null || passes.Count < 128) {
                passes = new List<float>();
                for (int p = 0; p < 128; ++p) {
                    passes.Add(p);
                }
            }



            for (int i = 0; i < FurDataProfiles.Length; ++i) {
                if (mainRenderer.isFurMaterial[i]) {

                    int current = 0;
                    for (int p = FurDataProfiles[i].FurSamples - 1; p > 0; p--) {
                        passes[p] = current;
                        current++;
                    }


#if UNITY_EDITOR
                    if (!Application.isPlaying) {
                        furDataProfiles[i].Update(Version);
                    }
#endif


#if UNITY_2020_1_OR_NEWER

                    if ( furDatabase.RenderingMode == XFurStudioDatabase.XFurRenderingMode.HighDefinition ) {

                        int flags = 0xff; // enable all light layers
                        float flagsFloat = System.BitConverter.ToSingle( System.BitConverter.GetBytes( flags ), 0 );
                        float[] renderFlags = new float[128];
                        for ( int x = 0; x < 128; x++ ) {
                            renderFlags[x] = flagsFloat;
                        }


                        furBlocks[i].SetFloatArray( "unity_RenderingLayer", renderFlags );
                    }

#endif

                    if (vfxModule.enabled && Application.isPlaying) {
                        vfxModule.MainRenderLoop(furBlocks[i], i);
                    }
                    else {
                        furBlocks[i].SetTexture(_xfurVFXMap, Texture2D.blackTexture);
                    }

                    if (lodModule) {
                        lodModule.MainRenderLoop(furBlocks[i], i);
                    }

                    if (physicsModule.enabled && Application.isPlaying) {
                        physicsModule.MainRenderLoop(furBlocks[i], i);
                    }
                    else {
                        furBlocks[i].SetTexture(_xfurPhysicsMap, Texture2D.blackTexture);
                    }

                    furBlocks[i].SetFloat(_xfurTotalPasses, RenderingMode == XFurRenderingMode.BasicShells ? (FurDataProfiles[i].FurSamples >= 8 ? 8 : 4) : furDataProfiles[i].FurSamples);

                    if (RenderingMode == XFurRenderingMode.XFShells) {
                        furBlocks[i].SetFloatArray(_xfurInstancedCurrentPass, passes);
                    }
                    else {
                        furBlocks[i].SetColor(_xfurSkinColor, furDataProfiles[i].SkinColor);
                        furBlocks[i].SetFloat(_xfurSkinSmoothness, furDataProfiles[i].SkinSmoothness);
                        furBlocks[i].SetTexture(_xfurSkinColorMap, furDataProfiles[i].SkinColorMap ? furDataProfiles[i].SkinColorMap : Texture2D.whiteTexture);
                        if (furDataProfiles[i].SkinNormalMap)
                            furBlocks[i].SetTexture(_xfurSkinNormalMap, furDataProfiles[i].SkinNormalMap);
                    }


                    if (FurDatabase.RenderingMode == XFurStudioDatabase.XFurRenderingMode.HighDefinition) {
                        furBlocks[i].SetMatrix(_xfurHDWorld2Local, MainRenderer.renderer.transform.worldToLocalMatrix);
                        furBlocks[i].SetMatrix(_xfurHDLocal2World, MainRenderer.renderer.transform.localToWorldMatrix);
                    }

                    if (!Application.isPlaying || forceUpdate || (AutoUpdateMaterials && Time.realtimeSinceStartup > timedUpdate)) {

                        if (BetaMode) {
                            furBlocks[i].SetFloat(_xfurSelfGroomStrength, furDataProfiles[i].FurGroomStrength * Mathf.Clamp01(AutoAdjustForScale ? CurrentFurRenderer.renderer.transform.lossyScale.magnitude : 1));
                        }
                        else {
                            furBlocks[i].SetFloat(_xfurSelfGroomStrength, 1);
                        }


                        if (RenderingMode == XFurRenderingMode.XFShells) {
                            furBlocks[i].SetFloatArray(_xfurInstancedCurrentPass, passes);
                        }
                        else {
                            furBlocks[i].SetColor(_xfurSkinColor, furDataProfiles[i].SkinColor);
                            furBlocks[i].SetFloat(_xfurSkinSmoothness, furDataProfiles[i].SkinSmoothness);
                            furBlocks[i].SetTexture(_xfurSkinColorMap, furDataProfiles[i].SkinColorMap ? furDataProfiles[i].SkinColorMap : Texture2D.whiteTexture);
                            if (furDataProfiles[i].SkinNormalMap)
                                furBlocks[i].SetTexture(_xfurSkinNormalMap, furDataProfiles[i].SkinNormalMap);
                        }

                        furBlocks[i].SetTexture(_xfurSelfStrandsPattern, furDataProfiles[i].FurStrandsAsset ? furDataProfiles[i].FurStrandsAsset.FurStrands : Texture2D.whiteTexture);

                        if (furDataProfiles[i].FurData1) {
                            furBlocks[i].SetFloat(_xfurHasGroomData, 1);
                            furBlocks[i].SetTexture(_xfurGroomData, furDataProfiles[i].FurData1);
                        }
                        else {
                            furBlocks[i].SetFloat(_xfurHasGroomData, 0);
                            furBlocks[i].SetTexture(_xfurGroomData, Texture2D.blackTexture);
                        }

                        furBlocks[i].SetVector(_xfurSelfColor, furDataProfiles[i].FurMainTint);
                        furBlocks[i].SetVector(_xfurSelfSpecularTint, furDataProfiles[i].FurSpecularTint);
                        furBlocks[i].SetFloat(_xfurSelfTransmission, furDataProfiles[i].FurTransmission);

                        furBlocks[i].SetFloat(_xfurSelfRimPower, furDataProfiles[i].FurRimPower);
                        furBlocks[i].SetColor(_xfurSelfRimColor, furDataProfiles[i].FurRim);
                        furBlocks[i].SetFloat(_xfurSelfRimBoost, furDataProfiles[i].FurRimBoost);

                        furBlocks[i].SetTexture(_xfurParamData, furDataProfiles[i].FurData0 ? furDataProfiles[i].FurData0 : Texture2D.whiteTexture);

                        furBlocks[i].SetTexture(_xfurMainColorMap, furDataProfiles[i].FurColorMap ? furDataProfiles[i].FurColorMap : Texture2D.whiteTexture);
                        if ( furDatabase.UseNormalmaps && furDataProfiles[i].FurNormalmap ) {
                            furBlocks[i].SetTexture( _xfurNormalmap, furDataProfiles[i].FurNormalmap ); 
                        }


                        if ( furDataProfiles[i].FurColorTiling == Vector4.zero ) {
                            furDataProfiles[i].FurColorTiling = new Vector4( furDataProfiles[i].FurBaseTiling, furDataProfiles[i].FurBaseTiling, 0, 0 );
                        }

                        furBlocks[i].SetVector(_xfurSelfBaseTiling, furDataProfiles[i].FurColorTiling);

                        furBlocks[i].SetTexture(_xfurEmissionMap, BetaMode ? (furDataProfiles[i].FurEmissionMap ? furDataProfiles[i].FurEmissionMap : Texture2D.whiteTexture) : Texture2D.blackTexture);
                        furBlocks[i].SetVector(_xfurEmissionColor, (Vector4)furDataProfiles[i].FurEmissionColor);

                        furBlocks[i].SetFloat(_xfurSelfSmoothness, furDataProfiles[i].FurSmoothness);
                        furBlocks[i].SetFloat(_xfurSelfLength, furDataProfiles[i].FurLength * (AutoAdjustForScale ? CurrentFurRenderer.renderer.transform.lossyScale.magnitude : 1));
                        furBlocks[i].SetFloat(_xfurSelfThickness, furDataProfiles[i].FurThickness);
                        furBlocks[i].SetFloat(_xfurSelfThicknessCurve, furDataProfiles[i].FurThicknessCurve);
                        furBlocks[i].SetColor(_xfurSelfShadowsTint, furDataProfiles[i].FurShadowsTint);
                        furBlocks[i].SetFloat(_xfurSelfOcclusion, furDataProfiles[i].FurOcclusion);
                        furBlocks[i].SetFloat(_xfurSelfOcclusionCurve, furDataProfiles[i].FurOcclusionCurve);

                        furBlocks[i].SetTexture(_xfurSelfColorVariation, furDataProfiles[i].FurColorVariation ? furDataProfiles[i].FurColorVariation : Texture2D.blackTexture);


                        furBlocks[i].SetFloat(_xfurSelfUnderColorMod, furDataProfiles[i].FurUnderColorMod);
                        furBlocks[i].SetFloat(_xfurSelfOverColorMod, furDataProfiles[i].FurOverColorMod);



                        if (furDataProfiles[i].UseCurlyFur) {
                            furBlocks[i].SetFloat(_xfurSelfCurlAmountX, furDataProfiles[i].FurCurlAmountX);
                            furBlocks[i].SetFloat(_xfurSelfCurlAmountY, furDataProfiles[i].FurCurlAmountY);
                            furBlocks[i].SetFloat(_xfurSelfCurlSizeX, furDataProfiles[i].FurCurlSizeX);
                            furBlocks[i].SetFloat(_xfurSelfCurlSizeY, furDataProfiles[i].FurCurlSizeY);
                        }
                        else {
                            furBlocks[i].SetFloat(_xfurSelfCurlAmountX, 0f);
                            furBlocks[i].SetFloat(_xfurSelfCurlAmountY, 0f);
                            furBlocks[i].SetFloat(_xfurSelfCurlSizeX, 0f);
                            furBlocks[i].SetFloat(_xfurSelfCurlSizeY, 0f);
                        }


                        /*
                        if ( furDatabase.MultiProfileBlending ) {
                            if ( furDataProfiles[i].FurBlendSplatmap ) {
                                furBlocks[i].SetTexture( "_XFurProfilesSplat", furDataProfiles[i].FurBlendSplatmap );
                                float[] furLengthArray = new float[4];
                                float[] furThicknessArray = new float[4];
                                float[] furThicknessCurveArray = new float[4];
                                float[] furOcclusionArray = new float[4];
                                float[] furOcclusionCurveArray = new float[4];
                                Vector4[] furOcclusionTintsArray = new Vector4[4];

                                if ( furDatabase.Profiles.Count > 0 && furDataProfiles[i].BlendedProfiles[0] < furDatabase.Profiles.Count && furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[0]] ) {
                                    for ( int p = 0; p < 4; p++ ) {
                                        if ( furDatabase.Profiles.Count > p && furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]] ) {
                                            furLengthArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurLength;
                                            furThicknessArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurThickness;
                                            furThicknessCurveArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurThicknessCurve;
                                            furOcclusionArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurOcclusion;
                                            furOcclusionCurveArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurOcclusionCurve;
                                            furOcclusionTintsArray[p] = furDatabase.Profiles[furDataProfiles[i].BlendedProfiles[p]].FurTemplate.FurShadowsTint;
                                        }
                                    }
                                }

                                furBlocks[i].SetVectorArray( "_XFurShadowsTint", furOcclusionTintsArray );
                                furBlocks[i].SetFloatArray( "_XFurLength", furLengthArray );
                                furBlocks[i].SetFloatArray( "_XFurThickness", furThicknessArray );
                                furBlocks[i].SetFloatArray( "_XFurThicknessCurve", furThicknessCurveArray );
                                furBlocks[i].SetFloatArray( "_XFurOcclusion", furOcclusionArray );
                                furBlocks[i].SetFloatArray( "_XFurOcclusionCurve", furOcclusionCurveArray );

                                if ( furDatabase.ProfileStrands )
                                    furBlocks[i].SetTexture( "_XFurStrandsPattern", furDatabase.ProfileStrands );
                                else if ( furDataProfiles[i].FurStrandsAsset.FurStrands ) {
                                    furBlocks[i].SetTexture( "_XFurStrandsPattern", furDataProfiles[i].FurStrandsAsset.FurStrands );
                                }

                            }
                            else {
                                furBlocks[i].SetTexture( "_XFurProfilesSplat", Texture2D.blackTexture );
                            }
                        }
                        else {
                            furBlocks[i].SetTexture( "_XFurProfilesSplat", Texture2D.blackTexture );
                        }
                        */

                        if (furDataProfiles[i].FurColorVariation) {
                            furBlocks[i].SetColor(_xfurSelfColorA, furDataProfiles[i].FurColorA);
                            furBlocks[i].SetColor(_xfurSelfColorB, furDataProfiles[i].FurColorB);
                            furBlocks[i].SetColor(_xfurSelfColorC, furDataProfiles[i].FurColorC);
                            furBlocks[i].SetColor(_xfurSelfColorD, furDataProfiles[i].FurColorD);
                        }
                        else {
                            furBlocks[i].SetColor(_xfurSelfColorA, Color.white);
                        }

                        furBlocks[i].SetFloat(_xfurSelfWindStrength, furDataProfiles[i].SelfWindStrength);

                        furBlocks[i].SetFloat(_xfurUVTiling, furDataProfiles[i].FurUVTiling);

                    }

                    if (decalsModule && decalsModule.enabled) {
                        decalsModule.MainRenderLoop(furBlocks[i], i);
                    }


                }
            }

            if (!Application.isPlaying || forceUpdate || (AutoUpdateMaterials && Time.realtimeSinceStartup > timedUpdate))
                timedUpdate = Time.realtimeSinceStartup + UpdateFrequency;

        }


#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (!Application.isPlaying) {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
        }
#endif




        private void RenderFur() {

            CurrentFurRenderer = mainRenderer;

            if (CurrentFurRenderer.materials == null || CurrentFurRenderer.materials.Length < 1 || !furDatabase) {
                return;
            }

            if (!CurrentFurRenderer.renderer) {
                return;
            }

            if (currentRenderingMode != RenderingMode) {
                CurrentFurRenderer.renderer.sharedMaterials = CurrentFurRenderer.materials;
                currentRenderingMode = RenderingMode;
            }

            if (furBlocks.Length != mainRenderer.materials.Length) {
                furBlocks = new MaterialPropertyBlock[mainRenderer.materials.Length];

                for (int i = 0; i < furBlocks.Length; ++i) {
                    furBlocks[i] = new MaterialPropertyBlock();
                }
            }





            if (lodModule && lodModule.enabled)
                lodModule.MainLoop();

            if (lodModule.IsFarLOD) {
                if (physicsModule.disableOnLOD) {
                    physicsModule.Disable();
                }

                if (vfxModule.disableOnLOD) {
                    vfxModule.Disable();
                }

            }
            else {
                if (physicsModule.disableOnLOD) {
                    physicsModule.Enable();
                }

                if (vfxModule.disableOnLOD) {
                    vfxModule.Enable();
                }
            }

            if (physicsModule && physicsModule.enabled)
                physicsModule.MainLoop();

            if (vfxModule.enabled) {
                vfxModule.MainLoop();
            }

            UpdateFurMaterials();



            var forceVisible = false;

            if (allCameras.Length >= Camera.allCamerasCount)
            {
                Camera.GetAllCameras( allCameras );
            }

            foreach ( Camera cam in allCameras ) {
                if ( cam ) {
                    GeometryUtility.CalculateFrustumPlanes( cam, frustumPlanes );
                    if ( GeometryUtility.TestPlanesAABB( frustumPlanes, CurrentFurRenderer.renderer.bounds ) ) {
                        forceVisible = true;
                    }
                }
            }

#if UNITY_EDITOR
            if ( !Application.isPlaying ) {
                if ( SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.camera ) {
                    GeometryUtility.CalculateFrustumPlanes( SceneView.lastActiveSceneView.camera, frustumPlanes );
                    if ( GeometryUtility.TestPlanesAABB( frustumPlanes, CurrentFurRenderer.renderer.bounds ) ) {
                        forceVisible = true;
                    }
                }
            }
#endif

            if ( skipRenderFrame || ( !forceVisible ) || !CurrentFurRenderer.renderer.enabled ) {
                skipRenderFrame = false;
                return;
            }

            if (RenderingMode == XFurRenderingMode.XFShells) {

                if (!fMesh) {
                    fMesh = new Mesh();
                }

                if (isSkinned || CurrentFurRenderer.renderer.GetType() == typeof(SkinnedMeshRenderer)) {
                    isSkinned = true;
                    ((SkinnedMeshRenderer)CurrentFurRenderer.renderer).BakeMesh(fMesh);
                }
                else {
                    fMesh = CurrentFurRenderer.renderer.GetComponent<MeshFilter>().sharedMesh;
                }


                if (buffMode != RenderingMode) {
                    CurrentFurRenderer.materials = CurrentFurRenderer.renderer.sharedMaterials;
                }


                if (passes == null || passes.Count < 128) {
                    passes = new List<float>();
                    for (int p = 0; p < 128; ++p) {
                        passes.Add(p);
                    }
                }

                if (tMatrices == null || tMatrices.Length != 128) {
                    tMatrices = new Matrix4x4[128];
                }

                if (CurrentFurRenderer.renderer.transform.position != tPos || CurrentFurRenderer.renderer.transform.rotation != tRot || CurrentFurRenderer.renderer.transform.lossyScale != tScale) {
                    tPos = CurrentFurRenderer.renderer.transform.position;
                    tRot = CurrentFurRenderer.renderer.transform.rotation;
                    tScale = UseLossyScale? CurrentFurRenderer.renderer.transform.lossyScale : CurrentFurRenderer.renderer.transform.localScale;

                    for (int m = 0; m < 128; ++m) {
                        tMatrices[m] = Matrix4x4.TRS(tPos, tRot, tScale);
                    }
                }

                if (furDataProfiles == null || furDataProfiles.Length != mainRenderer.materials.Length) {

                    List<XFurTemplate> furProf = new List<XFurTemplate>(); ;


                    for (int i = furProf.Count; i < mainRenderer.materials.Length; i++) {

                        if (furDataProfiles != null && furDataProfiles.Length > i) {
                            furProf.Add(furDataProfiles[i]);
                        }
                        else {
                            furProf.Add(new XFurTemplate(true));
                        }

                    }

                    furDataProfiles = furProf.ToArray();
                }



                for (int i = 0; i < CurrentFurRenderer.materials.Length; ++i) {


                    if (mainRenderer.isFurMaterial[i] && furDataProfiles[i].FurStrandsAsset) {
                        furBlocks[i].SetFloat(_xfurForceNonInstanced, FullForwardMode ? 1 : 0);

                        if (furDatabase.RenderingMode == XFurStudioDatabase.XFurRenderingMode.HighDefinition && furDataProfiles[i].UseEmissiveFur) {
                            if (!FullForwardMode) {
                                Graphics.DrawMeshInstanced(fMesh, i, furDataProfiles[i].DoubleSided ? furDatabase.HDRPEmitDouble : furDatabase.HDRPEmit, tMatrices, furDataProfiles[i].FurSamples, furBlocks[i], furDataProfiles[i].CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, furDataProfiles[i].ReceiveShadows, gameObject.layer, null, furDataProfiles[i].ProbeUse ? UnityEngine.Rendering.LightProbeUsage.BlendProbes : UnityEngine.Rendering.LightProbeUsage.Off);
                            }
                            else {
                                for (int shell = 0; shell < furDataProfiles[i].FurSamples; ++shell) {
                                    furBlocks[i].SetFloat(_xfurCurrentPass, shell);
                                    Graphics.DrawMesh(fMesh, tMatrices[i], furDataProfiles[i].DoubleSided ? furDatabase.HDRPEmitDouble : furDatabase.HDRPEmit, gameObject.layer, null, i, furBlocks[i], furDataProfiles[i].CastShadows, furDataProfiles[i].ReceiveShadows, furDataProfiles[i].ProbeUse);
                                }
                            }
                        }
                        else {
                            if (!FullForwardMode) {
                                Graphics.DrawMeshInstanced(fMesh, i, furDataProfiles[i].DoubleSided ? furDatabase.XFShellDoubleI : furDatabase.XFShellI, tMatrices, furDataProfiles[i].FurSamples, furBlocks[i], furDataProfiles[i].CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, furDataProfiles[i].ReceiveShadows, gameObject.layer, null, furDataProfiles[i].ProbeUse ? UnityEngine.Rendering.LightProbeUsage.BlendProbes : UnityEngine.Rendering.LightProbeUsage.Off);
                            }
                            else {
                                for (int shell = 0; shell < furDataProfiles[i].FurSamples; ++shell) {
                                    furBlocks[i].SetFloat(_xfurCurrentPass, shell);
                                    Graphics.DrawMesh(fMesh, tMatrices[i], furDataProfiles[i].DoubleSided ? furDatabase.XFShellDoubleF : furDatabase.XFShellF, gameObject.layer, null, i, furBlocks[i], furDataProfiles[i].CastShadows, furDataProfiles[i].ReceiveShadows, furDataProfiles[i].ProbeUse);
                                }
                            }
                        }
                    }

                }
            }
            else if (furDatabase.BasicReady) {

                if (temps.Length != CurrentFurRenderer.materials.Length)
                    temps = new Material[CurrentFurRenderer.materials.Length];


                if (buffMode != RenderingMode) {
                    currs = CurrentFurRenderer.renderer.sharedMaterials;
                }
                else {
                    currs = CurrentFurRenderer.materials;
                }

                for (int i = 0; i < temps.Length; i++) {
                    temps[i] = CurrentFurRenderer.materials[i];
                }

                for (int i = 0; i < CurrentFurRenderer.materials.Length; ++i) {

                    if (mainRenderer.isFurMaterial[i]) {
                        temps[i] = furDataProfiles[i].FurSamples == 4 ? furDatabase.BasicShellsFour : furDatabase.BasicShellsEight;
                        CurrentFurRenderer.renderer.SetPropertyBlock(furBlocks[i], i);
                    }
                    else {
                        temps[i] = CurrentFurRenderer.materials[i] = currs[i];
                    }

                }

                CurrentFurRenderer.renderer.sharedMaterials = temps;

            }

            buffMode = RenderingMode;

        }



        private void LateUpdate() {
            RenderFur();
        }

        public void InitialSetup() {

            CleanupXFurData();

#if UNITY_EDITOR

            if (furDatabase) {
                if (!furDatabase.IsCreated) {
                    furDatabase.LoadResources();
                }
            }

#endif

            if (mainRenderer.renderer && furDataProfiles.Length != mainRenderer.materials.Length) {
                furDataProfiles = new XFurTemplate[mainRenderer.materials.Length];

                for (int i = 0; i < furDataProfiles.Length; ++i) {
                    furDataProfiles[i] = new XFurTemplate(true);
                }

            }

            if (!randomModule) {
                randomModule = new XFurStudio2_Random();
                randomModule.Setup(this);
            }

            if (randomModule.Owner != this) {
                randomModule.Setup(this, true);
            }

            if (!lodModule) {
                lodModule = new XFurStudio2_LOD();
                lodModule.Setup(this);
            }

            if (lodModule.Owner != this) {
                lodModule.Setup(this, true);
            }

            if (!physicsModule) {
                physicsModule = new XFurStudio2_Physics();
                physicsModule.Setup(this);
            }

            if (physicsModule.Owner != this) {
                physicsModule.Setup(this, true);
            }

            if (!vfxModule) {
                vfxModule = new XFurStudio2_VFX();
                vfxModule.Setup(this);
            }

            if (vfxModule.Owner != this) {
                vfxModule.Setup(this, true);
            }


            if (!decalsModule) {
                decalsModule = new XFurStudio2_Decals();
                decalsModule.Setup(this);
            }

            if (decalsModule.Owner != this) {
                decalsModule.Setup(this, true);
            }

#if UNITY_EDITOR
            randomModule.UpdateModule();
            lodModule.UpdateModule();
            physicsModule.UpdateModule();
            vfxModule.UpdateModule();
            decalsModule.UpdateModule();


            if (folds.Length != 16 + customModules.Count) {
                folds = new bool[16 + customModules.Count];
            }

#endif

        }



        private void OnDisable() {

            if (!Application.isPlaying)
                CleanupXFurData();

            if (randomModule) {
                randomModule.Unload();
            }

            if (lodModule) {
                lodModule.Unload();
            }

            if (vfxModule) {
                vfxModule.Unload();
            }

            if (physicsModule) {
                physicsModule.Unload();
            }

            if (decalsModule) {
                decalsModule.Unload();
            }

        }


        private void OnDestroy() {
            CleanupXFurData();

            if (randomModule) {
                randomModule.Unload();
            }

            if (lodModule) {
                lodModule.Unload();
            }

            if (vfxModule) {
                vfxModule.Unload();
            }

            if (physicsModule) {
                physicsModule.Unload();
            }

            if (decalsModule) {
                decalsModule.Unload();
            }

        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private void UnloadStaticResources() {
            if (randomModule && randomModule.enabled) {
                randomModule.UnloadResources();
            }

            if (lodModule && lodModule.enabled) {
                lodModule.UnloadResources();
            }

            if (vfxModule && vfxModule.enabled) {
                vfxModule.UnloadResources();
            }

            if (physicsModule) {
                physicsModule.UnloadResources();
            }

            if (decalsModule) {
                decalsModule.UnloadResources();
            }

        }
#endif

        private void CleanupXFurData() {


            for (int i = 0; i < FurDataProfiles.Length; i++) {

                if (FurDataProfiles[i].FurColorMap is RenderTexture) {
                    RenderTexture.ReleaseTemporary((RenderTexture)FurDataProfiles[i].FurColorMap);
                }

                if (FurDataProfiles[i].FurColorVariation is RenderTexture) {
                    RenderTexture.ReleaseTemporary((RenderTexture)FurDataProfiles[i].FurColorVariation);
                }

                if (FurDataProfiles[i].FurData0 is RenderTexture) {
                    RenderTexture.ReleaseTemporary((RenderTexture)FurDataProfiles[i].FurData0);
                }

                if (FurDataProfiles[i].FurData1 is RenderTexture) {
                    RenderTexture.ReleaseTemporary((RenderTexture)FurDataProfiles[i].FurData1);
                }
                /*
                if (FurDataProfiles[i].FurBlendSplatmap is RenderTexture ) {
                    RenderTexture.ReleaseTemporary( (RenderTexture)FurDataProfiles[i].FurBlendSplatmap );
                }
                */

            }


        }


        /// <summary>
        /// Set the XFur Database to be used by this instance. May cause errors, use carefully.
        /// </summary>
        /// <param name="targetFurDatabase"></param>
        public void SetFurDatabase(XFurStudioDatabase targetFurDatabase) {
            furDatabase = targetFurDatabase;
        }



        #region EDITOR VARIABLES


#if UNITY_EDITOR

        public bool[] folds = new bool[16];

        public bool[] profileFolds = new bool[32];

        /// <summary> INTERNAL. DO NOT USE</summary>
        public XFurRendererData MainRenderer { get { return mainRenderer; } set { mainRenderer = value; } }
#else
        public XFurRendererData MainRenderer { get { return mainRenderer; } }
#endif

        #endregion


    }

}