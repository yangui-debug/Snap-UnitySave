

namespace XFurStudio2 {

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    [System.Serializable]
    public class XFurStudio2_Physics : XFurStudioModule {

        public ModuleQuality quality = ModuleQuality.Normal;

        public bool disableOnLOD = false;

        public float gravityStrength = 0.5f;

        public float physicsSensitivity = 0.35f;

        private int toPass = 0;

        [SerializeField] protected bool[] perMatPhysics;

        private int internalRes;

        private struct PhysicsSimulationData {

            public RenderTexture internalPass0, internalPass1, physicsPass;

        }

        private PhysicsSimulationData[] perProfilePhysicsData = new PhysicsSimulationData[0];

        [SerializeField] protected Shader PhysicsShader;

        private Material physicsMat;
        private Vector3 prevPos;

        public override void Setup( XFurStudioInstance xfurOwner, bool update = false ) {

            if ( !update ) {
                moduleName = "Fur Physics";
                version = "2.0";
                moduleStatus = 2;
                experimentalFeatures = false;
                isEnabled = true;
                hasMobileMode = true;
                hasSRPMode = true;
            }
            xfurInstance = xfurOwner;

        }


#if UNITY_EDITOR
        
        public override void UpdateModule() {
            moduleName = "Fur Physics";
            version = "3.0";
            moduleStatus = 3;
            experimentalFeatures = false;
            hasMobileMode = true;
            hasSRPMode = true;

            if ( Owner && Owner.MainRenderer.renderer && ( perMatPhysics == null || perMatPhysics.Length != Owner.MainRenderer.furProfiles.Length ) ) {
                perMatPhysics = new bool[Owner.MainRenderer.furProfiles.Length];
                for ( int i = 0; i < perMatPhysics.Length; i++ ) {
                    perMatPhysics[i] = true;
                }
            }


            if ( !PhysicsShader ) {
                PhysicsShader = Shader.Find( "Hidden/XFur Studio 2/Physics/GPU Physics" );
                if ( !PhysicsShader ) {
                    criticalError = true;
                    Debug.LogError( "Critical Error on the Physics Module : The GPU accelerated physics shader has not been found. Please re-import the asset in order to restore the missing files" );
                }
                else {
                    criticalError = false;
                }
            }
        }


        public override void ModuleUI() {

            //UnityEditor.Undo.RecordObject( this, xfurInstance.GetInstanceID() + "_" + this.GetInstanceID() );
            base.ModuleUI();


            if ( Owner && Owner.MainRenderer.renderer && ( perMatPhysics == null || perMatPhysics.Length != Owner.MainRenderer.furProfiles.Length ) ) {
                perMatPhysics = new bool[Owner.MainRenderer.furProfiles.Length];
                for ( int i = 0; i < perMatPhysics.Length; i++ ) {
                    perMatPhysics[i] = true;
                }
            }


            GUILayout.Space( 16 );

            if ( Application.isPlaying ) {
                StandardEnumField( new GUIContent( "Physics Quality", "The overall quality of the physics simulation" ), quality );
            }
            else {
                quality = (ModuleQuality)StandardEnumField( new GUIContent( "Physics Quality", "The overall quality of the physics simulation" ), quality );
            }

            GUILayout.Space( 16 );

            if ( Owner.LODModule.enabled ) {
                disableOnLOD = EnableDisableToggle( new GUIContent( "Disable with LOD", "Disables this module when the character is far from the camera" ), disableOnLOD );
            }
            else {
                disableOnLOD = false;
            }

            GUILayout.Space( 16 );

            for (int i = 0; i < perMatPhysics.Length; i++ ) {
                if ( Owner.MainRenderer.isFurMaterial[i] ) {
                    perMatPhysics[i] = EnableDisableToggle( new GUIContent( "Simulate for material " + i ), perMatPhysics[i], true );
                }
            }

            GUILayout.Space( 16 );

            gravityStrength = SliderField( new GUIContent( "Gravity Strength" ), gravityStrength, 0, 0.75f );
            physicsSensitivity = SliderField( new GUIContent( "Physics Sensitivity" ), physicsSensitivity, 0, 1 );

            GUILayout.Space( 16 );
        }


#endif


        public virtual void SetQuality( ModuleQuality targetQuality ) {

            quality = targetQuality;

            switch ( quality ) {
                case ModuleQuality.VeryLow:
                    internalRes = 16;
                    break;
                case ModuleQuality.Low:
                    internalRes = 32;
                    break;
                case ModuleQuality.Normal:
                    internalRes = 64;
                    break;
                case ModuleQuality.High:
                    internalRes = 128;
                    break;
            }

        }



        protected void PhysicsPass( int pass = 0 ) {

            if ( internalRes < 16 ) {
                SetQuality( quality );
            }

            if ( pass == 0 ) {
                prevPos = Owner.transform.position;
            }

#if UNITY_2019_3_OR_NEWER
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGBHalf, 0, 0 );
#else
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGBHalf, 0 );
#endif

            var targetMatrix = Owner.CurrentFurRenderer.renderer.transform.localToWorldMatrix;
            var targetMesh = Owner.CurrentMesh;

            if ( perProfilePhysicsData != null ) {

                for ( int i = 0; i < perProfilePhysicsData.Length; i++ ) {
                    if ( perMatPhysics[i] && Owner.MainRenderer.isFurMaterial[i] ) {
                        var tempRT1 = RenderTexture.GetTemporary( rd );

                        var currentActive = RenderTexture.active;
                        RenderTexture.active = tempRT1;

                        physicsMat.SetFloat( "_XFurBasicMode", Owner.RenderingMode == XFurStudioInstance.XFurRenderingMode.BasicShells ? 1 : 0 );
                        physicsMat.SetMatrix( "_XFurObjectMatrix", Owner.transform.localToWorldMatrix );

                        switch ( pass ) {
                            case 0:
                                GL.Clear( true, true, new Color( 0, 0, 0, 0 ) );
                                physicsMat.SetVector( "_WorldPosition", Owner.transform.position );
                                physicsMat.SetPass( 0 );
                                Graphics.DrawMeshNow( targetMesh, targetMatrix, i );
                                Graphics.Blit( tempRT1, perProfilePhysicsData[i].internalPass0 );
                                break;

                            case 1:
                                GL.Clear( true, true, new Color( 0, 0, 0, 0 ) );
                                physicsMat.SetFloat( "_XFurPhysicsSensitivity", physicsSensitivity * 150 );
                                physicsMat.SetFloat( "_XFurGravityStrength", gravityStrength );
                                physicsMat.SetVector( "_WorldPosition", Owner.transform.position );
                                physicsMat.SetVector( "_WorldDirection", ( prevPos - Owner.transform.position ) );
                                physicsMat.SetTexture( "_InputMap", perProfilePhysicsData[i].internalPass0 );
                                physicsMat.SetPass( 1 );
                                Graphics.DrawMeshNow( targetMesh, targetMatrix, i );
                                Graphics.Blit( tempRT1, perProfilePhysicsData[i].internalPass1 );
                                break;

                            case 2:
                                GL.Clear( true, true, new Color( 0, 0, 0, 0 ) );
                                physicsMat.SetTexture( "_InputMap", perProfilePhysicsData[i].internalPass1 );
                                physicsMat.SetTexture( "_PhysicsMap", perProfilePhysicsData[i].physicsPass );
                                physicsMat.SetPass( 2 );
                                Graphics.DrawMeshNow( targetMesh, targetMatrix, i );
                                Graphics.Blit( tempRT1, perProfilePhysicsData[i].physicsPass );
                                break;
                        }

                        RenderTexture.active = currentActive;

                        RenderTexture.ReleaseTemporary( tempRT1 );
                    }
                }
            }
        }




        public override void Load() {

            if ( perMatPhysics == null || perMatPhysics.Length != Owner.MainRenderer.furProfiles.Length ) {
                perMatPhysics = new bool[Owner.MainRenderer.furProfiles.Length];
                for (int i = 0; i < perMatPhysics.Length; i++ ) {
                    perMatPhysics[i] = true;
                }
            }

            if ( !Application.isPlaying ) {
                return;
            }


            SetQuality( quality );

#if UNITY_2019_3_OR_NEWER
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGBHalf, 0, 0 );
#else
            var rd = new RenderTextureDescriptor( internalRes, internalRes, RenderTextureFormat.ARGBHalf, 0 );
#endif

            perProfilePhysicsData = new PhysicsSimulationData[perMatPhysics.Length];

            for (int i = 0; i < perProfilePhysicsData.Length; i++ ) {
                if ( perMatPhysics[i] && Owner.MainRenderer.isFurMaterial[i] ) {
                    perProfilePhysicsData[i] = new PhysicsSimulationData { physicsPass = RenderTexture.GetTemporary( rd ), internalPass0 = RenderTexture.GetTemporary( rd ), internalPass1 = RenderTexture.GetTemporary( rd ) };                    XFurStudioAPI.LoadPaintResources();
                    XFurStudioAPI.FillTexture( Owner, new Color( 0, 0, 0 ), perProfilePhysicsData[i].internalPass0, new Color( 0, 0, 0 ) );
                    XFurStudioAPI.FillTexture( Owner, new Color( 0, 0, 0 ), perProfilePhysicsData[i].internalPass1, new Color( 0, 0, 0 ) );
                    XFurStudioAPI.FillTexture( Owner, new Color( 1, 1, 1 ), perProfilePhysicsData[i].physicsPass, new Color( 1, 1, 1 ) );
                    perProfilePhysicsData[i].internalPass0.name = "XFUR2_PHYSPASS_A" + i +"_"+ Owner.name;
                    perProfilePhysicsData[i].internalPass1.name = "XFUR2_PHYSPASS_B" + i +"_"+ Owner.name;
                    perProfilePhysicsData[i].physicsPass.name = "XFUR2_PHYSPASS_C" + i +"_"+ Owner.name;
                }
            }

            
            if ( !physicsMat ) {
                physicsMat = new Material( PhysicsShader );
            }


            

        }

        public override void MainLoop() {
            if ( Application.isPlaying ) {
                PhysicsPass( toPass );
                toPass = toPass == 0 ? 1 : 0;
                PhysicsPass( 2 );
            }
        }


        public override void MainRenderLoop( MaterialPropertyBlock block, int furProfileIndex ) {
            if ( furProfileIndex > -1 && furProfileIndex < perMatPhysics.Length && perProfilePhysicsData != null && furProfileIndex < perProfilePhysicsData.Length ) {
                if ( perMatPhysics[furProfileIndex] && perProfilePhysicsData[furProfileIndex].physicsPass ) {
                    block.SetTexture( "_XFurPhysics", perProfilePhysicsData[furProfileIndex].physicsPass );
                }
            }
        }


        public override void Unload() { 
            if ( perProfilePhysicsData != null ) {
                for ( int i = 0; i < perProfilePhysicsData.Length; i++ ) {
                    if ( perMatPhysics[i] ) {
                        RenderTexture.ReleaseTemporary( perProfilePhysicsData[i].internalPass0 );
                        RenderTexture.ReleaseTemporary( perProfilePhysicsData[i].internalPass1 );
                        RenderTexture.ReleaseTemporary( perProfilePhysicsData[i].physicsPass );
                    }
                }
            }
        }


        public override void UnloadResources() {
            base.UnloadResources();
            if ( physicsMat ) {
                Object.DestroyImmediate( physicsMat );
            }
        }


    }


}