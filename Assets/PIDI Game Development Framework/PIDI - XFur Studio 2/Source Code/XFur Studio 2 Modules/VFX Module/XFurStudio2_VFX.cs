namespace XFurStudio2 {

    using UnityEngine;

    [System.Serializable]
    public class XFurStudio2_VFX : XFurStudioModule {


        #region SHADER PROPERTIES

        readonly static int _xfurVFXMask = Shader.PropertyToID( "_XFurVFXMask" );
        readonly static int _xfurVFX1Color = Shader.PropertyToID( "_XFurVFX1Color" );
        readonly static int _xfurVFX2Color = Shader.PropertyToID( "_XFurVFX2Color" );
        readonly static int _xfurVFX1Penetration = Shader.PropertyToID( "_XFurVFX1Penetration" );
        readonly static int _xfurVFX2Penetration = Shader.PropertyToID( "_XFurVFX2Penetration" );
        readonly static int _xfurVFX3Penetration = Shader.PropertyToID( "_XFurVFX3Penetration" );
        readonly static int _xfurVFX1Smoothness = Shader.PropertyToID( "_XFurVFX1Smoothness" );
        readonly static int _xfurVFX2Smoothness = Shader.PropertyToID( "_XFurVFX2Smoothness" );
        readonly static int _xfurVFX3Smoothness = Shader.PropertyToID( "_XFurVFX3Smoothness" );



        readonly static int _xfurVFXBasicMode = Shader.PropertyToID( "_XFurBasicMode" );
        readonly static int _xfurVFXObjectMatrix = Shader.PropertyToID( "_XFurObjectMatrix" );
        readonly static int _xfurVFXInputMap = Shader.PropertyToID( "_InputMap" );
        readonly static int _xfurVFXMask0 = Shader.PropertyToID( "_VFXMask0" );
        readonly static int _xfurVFXTiling0 = Shader.PropertyToID( "_VFXTiling0" );
        readonly static int _xfurVFXAdd0 = Shader.PropertyToID( "_FXAdd0" );
        readonly static int _xfurVFXMelt0 = Shader.PropertyToID( "_FXMelt0" );
        readonly static int _xfurVFXFalloff0 = Shader.PropertyToID( "_FXFalloff0" );
        readonly static int _xfurVFXMask1 = Shader.PropertyToID( "_VFXMask1" );
        readonly static int _xfurVFXTiling1 = Shader.PropertyToID( "_VFXTiling1" );
        readonly static int _xfurVFXAdd1 = Shader.PropertyToID( "_FXAdd1" );
        readonly static int _xfurVFXMelt1 = Shader.PropertyToID( "_FXMelt1" );
        readonly static int _xfurVFXFalloff1 = Shader.PropertyToID( "_FXFalloff1" );
        readonly static int _xfurVFXMask2 = Shader.PropertyToID( "_VFXMask2" );
        readonly static int _xfurVFXTiling2 = Shader.PropertyToID( "_VFXTiling2" );
        readonly static int _xfurVFXAdd2 = Shader.PropertyToID( "_FXAdd2" );
        readonly static int _xfurVFXMelt2 = Shader.PropertyToID( "_FXMelt2" );
        readonly static int _xfurVFXFalloff2 = Shader.PropertyToID( "_FXFalloff2" );
        readonly static int _xfurVFXDirection1 = Shader.PropertyToID( "_XFurVFX1Direction" );
        readonly static int _xfurVFXDirection2 = Shader.PropertyToID( "_XFurVFX2Direction" );
        readonly static int _xfurVFXGlobalAdd1 = Shader.PropertyToID( "_XFurVFX1GlobalAdd" );
        readonly static int _xfurVFXGlobalAdd2 = Shader.PropertyToID( "_XFurVFX2GlobalAdd" );

        readonly static int _xfurVFXGlobalMask = Shader.PropertyToID( "_XFurVFXGlobalMask" );

        #endregion

        [System.Serializable]
        public class VFXEffect {

            public string name = "New Effect";

            public Color color;
            public Color specular;
            public float penetration;
            public float smoothness;
            public float intensity = 2.0f;
            public float fadeoutTime = 5f;
            public float normalFalloff = 0.15f;
            public Texture vfxMask;
            public float vfxTiling = 2;
            public bool updated;
            public bool ignoreWeatherComponent;

        }


        [SerializeField] protected Shader VFXProgressiveShader;

        static Material vfxMat;

        private RenderTexture[] vfxTexture;

        private int internalRes;

        public bool disableOnLOD;

        protected float updateTime = 0.05f;

        private float timer;

        public RenderTexture[] VFXTexture { get { return vfxTexture; } }


        [SerializeField] protected ModuleQuality quality = ModuleQuality.Normal;

        public VFXEffect Snow = new VFXEffect { name = "SnowFX", color = new Color( 0.85f, 0.95f, 1f, 1f ), specular = new Color( 0.45f, 0.5f, 0.6f ), smoothness = 0.5f, intensity = 1.25f, fadeoutTime = 4.0f, vfxTiling = 2, normalFalloff = 0.75f, penetration = 0.65f };

        public VFXEffect Rain = new VFXEffect { name = "RainFX", color = Color.white, specular = new Color( 0.45f, 0.5f, 0.6f ), smoothness = 0.75f, intensity = 1.25f, fadeoutTime = 4.0f, vfxTiling = 2, normalFalloff = 0.75f };

        public VFXEffect Blood = new VFXEffect { name = "BloodFX", color = new Color( 0.75f, 0.05f, 0.05f ), specular = new Color( 0.15f, 0.15f, 0.15f ), smoothness = 0.2f, intensity = 1.25f, fadeoutTime = 10.0f, vfxTiling = 2, normalFalloff = 0.75f };

        public override void Setup( XFurStudioInstance xfurOwner, bool update = false ) {
            if ( !update ) {
                moduleName = "VFX & Weather";
                version = "3.1";
                moduleStatus = 3;
                experimentalFeatures = false;
                isEnabled = true;
                hasMobileMode = true;
                hasSRPMode = true;
            }
            xfurInstance = xfurOwner;

        }


#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.SubsystemRegistration )]
        static void DestroyMaterial() {
            if ( vfxMat )
                Object.DestroyImmediate( vfxMat );
        }
#endif


        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetQuality"></param>
        public virtual void SetQuality( ModuleQuality targetQuality ) {

            quality = targetQuality;

            switch ( quality ) {
                case ModuleQuality.VeryLow:
                    internalRes = Owner.FurDatabase.MobileMode ? 16 : 32;
                    break;
                case ModuleQuality.Low:
                    internalRes = Owner.FurDatabase.MobileMode ? 32 : 64;
                    break;
                case ModuleQuality.Normal:
                    internalRes = Owner.FurDatabase.MobileMode ? 64 : 128;
                    break;
                case ModuleQuality.High:
                    internalRes = Owner.FurDatabase.MobileMode ? 128 : 256;
                    break;
            }

        }





        public override void Load() {

            updateTime = Random.Range( updateTime * 0.9f, updateTime * 1.1f );

            SetQuality( quality );

#if UNITY_2019_3_OR_NEWER
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGB32, 0, 2 );
#else
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGB32, 0 );
#endif

            vfxTexture = new RenderTexture[Owner.MainRenderer.furProfiles.Length];

            for ( int i = 0; i < vfxTexture.Length; i++ ) {

                if ( Owner.MainRenderer.isFurMaterial[i] ) {
                    vfxTexture[i] = RenderTexture.GetTemporary( rd );
                    vfxTexture[i].filterMode = FilterMode.Bilinear;
                    XFurStudioAPI.LoadPaintResources();
                    XFurStudioAPI.FillTexture( Owner, Color.clear, vfxTexture[i], Color.clear );
                    vfxTexture[i].name = "XFUR2_VFX" + i + "_" + Owner.name;
                }
            }


            if ( !vfxMat ) {
                vfxMat = new Material( VFXProgressiveShader );
            }

        }


        public override void MainLoop() {

            if ( criticalError ) {
                return;
            }

            if ( Application.isPlaying ) {
                if ( Time.timeSinceLevelLoad > timer ) {
                    VFXPass();
                    timer = Time.timeSinceLevelLoad + updateTime;
                }
            }

        }


        public override void MainRenderLoop( MaterialPropertyBlock block, int furProfileIndex ) {

            if ( criticalError ) {
                return;
            }


            if ( furProfileIndex > -1 && furProfileIndex < vfxTexture.Length ) {
                block.SetTexture( _xfurVFXMask, vfxTexture[furProfileIndex] );
                block.SetColor( _xfurVFX1Color, Blood.color );
                block.SetColor( _xfurVFX2Color, Snow.color );
                block.SetFloat( _xfurVFX1Penetration, 1 - Blood.penetration );
                block.SetFloat( _xfurVFX2Penetration, 1 - Snow.penetration );
                block.SetFloat( _xfurVFX3Penetration, 1 - Rain.penetration );
                block.SetFloat( _xfurVFX1Smoothness, Blood.smoothness );
                block.SetFloat( _xfurVFX2Smoothness, Snow.smoothness );
                block.SetFloat( _xfurVFX3Smoothness, Rain.smoothness );
            }

        }


        public override void Unload() {
            if ( vfxTexture != null ) {
                for ( int i = 0; i < vfxTexture.Length; i++ ) {
                    if ( vfxTexture[i] )
                        RenderTexture.ReleaseTemporary( vfxTexture[i] );
                }
            }
        }


        protected void VFXPass() {

            if ( internalRes < 16 ) {
                Unload();
                Load();
            }

            if ( !vfxMat ) {
                vfxMat = new Material( VFXProgressiveShader );
            }

            var targetMatrix = Owner.CurrentFurRenderer.renderer.transform.localToWorldMatrix;
            var targetMesh = Owner.CurrentMesh;

#if UNITY_2019_3_OR_NEWER
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGB32, 0, 2 );
#else
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGB32, 0 );
#endif

            for ( int i = 0; i < vfxTexture.Length; i++ ) {

                if ( vfxTexture[i] && Owner.MainRenderer.isFurMaterial[i] ) {
                    var tempRT1 = RenderTexture.GetTemporary( rd );


                    vfxMat.SetVector( _xfurVFXGlobalMask, new Vector4( Snow.ignoreWeatherComponent ? 0 : 1, Rain.ignoreWeatherComponent ? 0 : 1, 1 ) );

                    vfxMat.SetFloat( _xfurVFXBasicMode, Owner.RenderingMode == XFurStudioInstance.XFurRenderingMode.BasicShells ? 1 : 0 );
                    vfxMat.SetMatrix( _xfurVFXObjectMatrix, Owner.transform.localToWorldMatrix );
                    vfxMat.SetTexture( _xfurVFXInputMap, vfxTexture[i] );
                    vfxMat.SetTexture( _xfurVFXMask0, Blood.vfxMask ? Blood.vfxMask : Texture2D.whiteTexture );
                    vfxMat.SetFloat( _xfurVFXTiling0, Blood.vfxTiling );
                    vfxMat.SetFloat( _xfurVFXAdd0, 0 );

                    if ( Blood.fadeoutTime <= 0 ) {
                        vfxMat.SetFloat( _xfurVFXMelt0, 0 );
                    }
                    else {
                        vfxMat.SetFloat( _xfurVFXMelt0, ( 1.0f / Blood.fadeoutTime ) * updateTime );
                    }

                    vfxMat.SetTexture( _xfurVFXMask1, Snow.vfxMask ? Snow.vfxMask : Texture2D.whiteTexture );
                    vfxMat.SetFloat( _xfurVFXTiling1, Snow.vfxTiling );
                    vfxMat.SetFloat( _xfurVFXAdd1, Snow.intensity * updateTime );

                    if ( Snow.fadeoutTime <= 0 ) {
                        vfxMat.SetFloat( _xfurVFXMelt1, 0 );
                    }
                    else {
                        vfxMat.SetFloat( _xfurVFXMelt1, ( 1.0f / Snow.fadeoutTime ) * updateTime );
                    }

                    vfxMat.SetFloat( _xfurVFXFalloff1, Snow.normalFalloff );
                    vfxMat.SetTexture( _xfurVFXMask2, Rain.vfxMask ? Rain.vfxMask : Texture2D.whiteTexture );
                    vfxMat.SetFloat( _xfurVFXTiling2, Rain.vfxTiling );
                    vfxMat.SetFloat( _xfurVFXAdd2, Rain.intensity * updateTime );

                    if ( Rain.fadeoutTime <= 0 ) {
                        vfxMat.SetFloat( _xfurVFXMelt2, 0 );
                    }
                    else {
                        vfxMat.SetFloat( _xfurVFXMelt2, ( 1.0f / Rain.fadeoutTime ) * updateTime );
                    }

                    vfxMat.SetFloat( _xfurVFXFalloff2, Rain.normalFalloff );

                    if ( XFurStudioInstance.WindZone ) {
                        vfxMat.SetVector( _xfurVFXDirection1, XFurStudioInstance.WindZone.SnowDirection );
                        vfxMat.SetVector( _xfurVFXDirection2, XFurStudioInstance.WindZone.RainDirection );
                        vfxMat.SetFloat( _xfurVFXGlobalAdd1, XFurStudioInstance.WindZone.SnowIntensity );
                        vfxMat.SetFloat( _xfurVFXGlobalAdd2, XFurStudioInstance.WindZone.RainIntensity );
                    }


                    var currentActive = RenderTexture.active;
                    RenderTexture.active = tempRT1;
                    GL.Clear( true, true, new Color( 0, 0, 0, 0 ) );
                    vfxMat.SetPass( 0 );
                    Graphics.DrawMeshNow( targetMesh, targetMatrix, i );
                    RenderTexture.active = currentActive;

                    Graphics.Blit( tempRT1, vfxTexture[i] );

                    RenderTexture.ReleaseTemporary( tempRT1 );

                    XFurStudioAPI.LoadPaintResources();
                    XFurStudioAPI.FillUVCracks( xfurInstance, vfxTexture[i], i );

                }

            }

        }




#if UNITY_EDITOR


        private bool[] fxFolds = new bool[8];

        public override void UpdateModule() {

            if ( Snow.penetration < 0.1f ) {
                Snow.penetration = 0.65f;
            }

            if ( Blood.penetration < 0.1f ) {
                Blood.penetration = 0.75f;
            }

            if ( Rain.penetration < 0.1f ) {
                Rain.penetration = 0.75f;
            }

            moduleName = "VFX & Weather";
            version = "3.0";
            moduleStatus = 3;
            experimentalFeatures = false;
            hasMobileMode = true;
            hasSRPMode = true;


            if ( !VFXProgressiveShader ) {
                VFXProgressiveShader = Shader.Find( "Hidden/XFur Studio 2/VFX/VFXProgressive" );

                if ( !VFXProgressiveShader ) {
                    criticalError = true;
                    Debug.LogError( "Critical Error on the VFX Module : The GPU accelerated VFX shader has not been found. Please re-import the asset in order to restore the missing files" );
                }
            }

            if ( VFXProgressiveShader ) {
                criticalError = false;
            }

        }


        public override void ModuleUI() {

            base.ModuleUI();

            GUILayout.Space( 16 );

            quality = (ModuleQuality)StandardEnumField( new GUIContent( "FX Mask Quality", "The overall quality of the mask used to paint different FX over the fur" ), quality );

            if ( !Application.isPlaying )
                updateTime = FloatField( new GUIContent( "Update Frequency (s)", "The update frequency (in seconds) of this module" ), Mathf.Clamp( updateTime, 0.01f, 1 ) );

            GUILayout.Space( 16 );

            if ( Owner.LODModule.enabled ) {
                disableOnLOD = EnableDisableToggle( new GUIContent( "Disable with LOD", "Disables this module when the character is far from the camera" ), disableOnLOD );
            }
            else {
                disableOnLOD = false;
            }

            GUILayout.Space( 16 );

            if ( BeginCenteredGroup( "Snow FX", ref fxFolds[0] ) ) {
                GUILayout.Space( 8 );
                Snow.ignoreWeatherComponent = EnableDisableToggle( new GUIContent( "Ignore Weather Component" ), Snow.ignoreWeatherComponent, true );
                Snow.color = ColorField( new GUIContent( "Snow Color" ), Snow.color );
                Snow.intensity = SliderField( new GUIContent( "Intensity" ), Snow.intensity, 0, 24 );
                Snow.penetration = SliderField( new GUIContent( "Penetration" ), Snow.penetration, 0.25f, 1.0f );
                Snow.fadeoutTime = FloatField( new GUIContent( "Fade Time" ), Snow.fadeoutTime );
                Snow.normalFalloff = SliderField( new GUIContent( "Falloff" ), Snow.normalFalloff, 0.05f, 2.0f );

                GUILayout.Space( 8 );

                Snow.vfxMask = ObjectField<Texture>( new GUIContent( "VFX Mask Texture" ), Snow.vfxMask );
                Snow.vfxTiling = SliderField( new GUIContent( "VFX Tiling" ), Snow.vfxTiling, 0, 32 );

                GUILayout.Space( 8 );
            }
            EndCenteredGroup();

            if ( BeginCenteredGroup( "Rain FX", ref fxFolds[1] ) ) {
                GUILayout.Space( 8 );
                Rain.ignoreWeatherComponent = EnableDisableToggle( new GUIContent( "Ignore Weather Component" ), Rain.ignoreWeatherComponent, true );
                Rain.intensity = SliderField( new GUIContent( "Intensity" ), Rain.intensity, 0, 24 );
                Rain.penetration = SliderField( new GUIContent( "Penetration" ), Rain.penetration, 0.25f, 1.0f );
                Rain.smoothness = SliderField( new GUIContent( "Smoothness" ), Rain.smoothness, 0.25f, 1.0f );
                Rain.fadeoutTime = FloatField( new GUIContent( "Fade Time" ), Rain.fadeoutTime );
                Rain.normalFalloff = SliderField( new GUIContent( "Falloff" ), Rain.normalFalloff, 0.05f, 2.0f );

                GUILayout.Space( 8 );

                Rain.vfxMask = ObjectField<Texture>( new GUIContent( "VFX Mask Texture" ), Rain.vfxMask );
                Rain.vfxTiling = SliderField( new GUIContent( "VFX Tiling" ), Rain.vfxTiling, 0, 32 );

                GUILayout.Space( 8 );
            }
            EndCenteredGroup();

            if ( BeginCenteredGroup( "Blood FX", ref fxFolds[2] ) ) {
                GUILayout.Space( 8 );

                Blood.color = ColorField( new GUIContent( "Blood Color" ), Blood.color );
                Blood.fadeoutTime = FloatField( new GUIContent( "Fade Time" ), Blood.fadeoutTime );
                Blood.penetration = SliderField( new GUIContent( "Penetration" ), Blood.penetration, 0.25f, 1.0f );
                Blood.smoothness = SliderField( new GUIContent( "Smoothness" ), Blood.smoothness, 0.25f, 1.0f );
                GUILayout.Space( 8 );

                Blood.vfxMask = ObjectField<Texture>( new GUIContent( "VFX Mask Texture" ), Blood.vfxMask );
                Blood.vfxTiling = SliderField( new GUIContent( "VFX Tiling" ), Blood.vfxTiling, 0, 32 );

                GUILayout.Space( 8 );
            }
            EndCenteredGroup();

            GUILayout.Space( 16 );


        }

#endif

    }
}