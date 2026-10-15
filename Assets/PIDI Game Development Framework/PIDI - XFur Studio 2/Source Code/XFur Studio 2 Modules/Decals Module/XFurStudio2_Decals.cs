namespace XFurStudio2 {

    using UnityEngine;
    using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    //This is a test comment

    [System.Serializable]
    public class XFurStudio2_Decals : XFurStudioModule {

        [SerializeField] protected Shader DecalsShader;

        static Material decalsMat;

        public enum MixingMode { Overlay, Add, Multiply }

        [System.Serializable]
        public class DecalDefinition {

            public Color color = Color.white;
            public Texture sourceDecal;
            public Vector2 offset;
            public Vector2 tiling = Vector2.one;
            public MixingMode mixingMode;

        }


        [System.Serializable]
        public class PerProfileDecals {

            public bool enabled;

            [System.NonSerialized] public bool[] folds = new bool[4];

            public RenderTexture finalOutput;

            public int outputMode;

            public List<DecalDefinition> decals = new List<DecalDefinition>();

        }

         

        public List<PerProfileDecals> ProfileDecals = new List<PerProfileDecals>();


#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.SubsystemRegistration )]
        public static void DestroyMaterial() {
            if ( decalsMat )
                Object.DestroyImmediate( decalsMat );
        }
#endif


        public override void Load() {

            if ( Owner.MainRenderer.renderer ) {
                if ( ProfileDecals.Count != Owner.MainRenderer.materials.Length ) {
                    ProfileDecals = new List<PerProfileDecals>();
                    for ( int i = 0; i < Owner.MainRenderer.materials.Length; i++ ) {
                        ProfileDecals.Add( new PerProfileDecals() );
                        ProfileDecals[i].enabled = Owner.MainRenderer.isFurMaterial[i];
                    }
                }
            }
            if ( !decalsMat && DecalsShader ) {
                decalsMat = new Material( DecalsShader );
            }

        }


        private void GenerateDecals() {

            for( int i = 0; i < ProfileDecals.Count; i++ ) {

                RenderTexture.ReleaseTemporary( ProfileDecals[i].finalOutput );

                RenderTexture tempRT0;
                RenderTexture tempRT1;

                if ( ProfileDecals[i].outputMode == 0 ) {
                    if ( Owner.FurDataProfiles[i].FurColorMap ) {
                        tempRT0 = RenderTexture.GetTemporary( Owner.FurDataProfiles[i].FurColorMap.width, Owner.FurDataProfiles[i].FurColorMap.height );
                        Graphics.Blit( Owner.FurDataProfiles[i].FurColorMap, tempRT0 );
                    }
                    else {
                        tempRT0 = RenderTexture.GetTemporary( 1024, 1024 );
                        var target = RenderTexture.active;
                        RenderTexture.active = tempRT0;
                        GL.Clear( true, true, Color.white );
                        RenderTexture.active = target;
                    }
                }
                else {
                    if ( Owner.FurDataProfiles[i].FurEmissionMap ) {
                        tempRT0 = RenderTexture.GetTemporary( Owner.FurDataProfiles[i].FurEmissionMap.width, Owner.FurDataProfiles[i].FurEmissionMap.height );
                        Graphics.Blit( Owner.FurDataProfiles[i].FurEmissionMap, tempRT0 );
                    }
                    else {
                        tempRT0 = RenderTexture.GetTemporary( 1024, 1024 );
                        var target = RenderTexture.active;
                        RenderTexture.active = tempRT0;
                        GL.Clear( true, true, Color.clear );
                        RenderTexture.active = target;
                    }
                }
                
                ProfileDecals[i].finalOutput = RenderTexture.GetTemporary( tempRT0.width, tempRT0.height );
                ProfileDecals[i].finalOutput.name = "XFUR DECAL";

                tempRT1 = RenderTexture.GetTemporary( tempRT0.width, tempRT0.height );
                
                for (int d = 0; d < ProfileDecals[i].decals.Count; d++ ) {
                    decalsMat.SetTexture( "_Decal", ProfileDecals[i].decals[d].sourceDecal ? ProfileDecals[i].decals[d].sourceDecal : Texture2D.blackTexture );
                    decalsMat.SetVector( "_DecalOffsetTiling", new Vector4( ProfileDecals[i].decals[d].offset.x, ProfileDecals[i].decals[d].offset.y, ProfileDecals[i].decals[d].tiling.x, ProfileDecals[i].decals[d].tiling.y ) );
                    decalsMat.SetFloat( "_MixMode", (int)ProfileDecals[i].decals[d].mixingMode );
                    decalsMat.SetColor( "_DecalColor", ProfileDecals[i].decals[d].color );
                    Graphics.Blit( tempRT0, ProfileDecals[i].finalOutput, decalsMat );
                    Graphics.Blit( ProfileDecals[i].finalOutput, tempRT0 );
                }
                
                RenderTexture.ReleaseTemporary( tempRT0 );
                RenderTexture.ReleaseTemporary( tempRT1 );

            }

        }


        public override void MainRenderLoop( MaterialPropertyBlock block, int furProfileIndex ) {
            
            if ( criticalError || !enabled ) {
                return;
            }

            if ( !decalsMat && DecalsShader ) {
                decalsMat = new Material( DecalsShader );
            }

            GenerateDecals();

            if ( ProfileDecals[furProfileIndex].enabled && ProfileDecals[furProfileIndex].finalOutput ) {
                if ( ProfileDecals[furProfileIndex].outputMode == 0 ) {
                    block.SetTexture( "_XFurMainColorMap", ProfileDecals[furProfileIndex].finalOutput );
                }
                else {
                    block.SetTexture( "_XFurEmissionMap", ProfileDecals[furProfileIndex].finalOutput );
                }
            }

        }


#if UNITY_EDITOR

        private bool[] folds = new bool[0];

        public override void UpdateModule() {

            moduleName = "UV Decals";
            moduleStatus = 3;
            version = "2.0";
            hasMobileMode = true;
            hasSRPMode = true;

            if ( Owner.MainRenderer.renderer ) {
                if ( ProfileDecals.Count != Owner.MainRenderer.materials.Length ) {
                    ProfileDecals = new List<PerProfileDecals>();
                    for ( int i = 0; i < Owner.MainRenderer.materials.Length; i++ ) {
                        ProfileDecals.Add( new PerProfileDecals() );
                        ProfileDecals[i].enabled = Owner.MainRenderer.isFurMaterial[i];
                    }
                }
            }

            if ( !DecalsShader ) {
                DecalsShader = Shader.Find( "Hidden/XFur Studio 2/DecalMixing" );

                if ( !DecalsShader ) {
                    criticalError = true;
                    Debug.LogError( "Critical Error on the Decals Module : The Decals Mixing shader has not been found. Please re-import the asset in order to restore the missing files" );
                }
            }

            if ( DecalsShader ) {
                criticalError = false;
                if ( !decalsMat ) {
                    decalsMat = new Material( DecalsShader );
                }
            }

        }

        public override void ModuleUI() {
            GUILayout.Space( 16 );

            if ( folds.Length != ProfileDecals.Count ) {
                folds = new bool[ProfileDecals.Count];
            }

            for ( int i = 0; i < ProfileDecals.Count; i++ ) {
                if ( Owner.MainRenderer.isFurMaterial[i] ) {
                    ProfileDecals[i].enabled = EnableDisableToggle( new GUIContent( "Material " + i + " Decals" ), ProfileDecals[i].enabled );
                    GUILayout.Space( 4 );
                }
                else {
                    ProfileDecals[i].enabled = false;
                }
            }

            GUILayout.Space( 16 );

            for ( int i = 0; i < ProfileDecals.Count; i++ ) {
                if ( ProfileDecals[i].enabled ) {

                    GUILayout.Space( 8 );

                    if ( xfurInstance.FurDataProfiles[i].UseEmissiveFur ) {
                        ProfileDecals[i].outputMode = PopupField( new GUIContent( "Decals Output" ), ProfileDecals[i].outputMode, new string[] { "Diffuse Channel", "Emission Channel" } );
                    }
                    else {
                        ProfileDecals[i].outputMode = 0;
                    }

                    GUILayout.Space( 8 );

                    if ( BeginCenteredGroup("Material "+i+" Decals", ref folds[i] ) ) {
                        GUILayout.Space( 16 );

                        for (int d = 0; d < ProfileDecals[i].decals.Count; d++ ) {
                            if ( BeginCenteredGroup("Decal "+d, ref ProfileDecals[i].folds[d] ) ) {
                                GUILayout.Space( 12 );
                                ProfileDecals[i].decals[d].mixingMode = (MixingMode)StandardEnumField( new GUIContent( "Decal Mix Mode", "The way in which the color of this decal will be mixed with the fur's color map" ), ProfileDecals[i].decals[d].mixingMode );
                                ProfileDecals[i].decals[d].sourceDecal = ObjectField<Texture>( new GUIContent( "Decal Texture" ), ProfileDecals[i].decals[d].sourceDecal );
                                ProfileDecals[i].decals[d].color = ColorField( new GUIContent( "Decal Tint" ), ProfileDecals[i].decals[d].color );
                                ProfileDecals[i].decals[d].offset = Vector2Field( new GUIContent( "Decal Offset" ), ProfileDecals[i].decals[d].offset );
                                ProfileDecals[i].decals[d].tiling = Vector2Field( new GUIContent( "Decal Tiling" ), ProfileDecals[i].decals[d].tiling );
                                GUILayout.Space( 16 );

                                if ( CenteredButton("Remove Decal", 128 ) ) {
                                    ProfileDecals[i].decals.RemoveAt( d );
                                    EndCenteredGroup();
                                    break;
                                }
                            }
                            EndCenteredGroup();
                            GUILayout.Space( 4 );
                        }

                        GUILayout.Space( 16 );

                        if ( ProfileDecals[i].decals.Count < 4 ) {
                            if ( CenteredButton( "Add new Decal", 200 ) ) {
                                ProfileDecals[i].decals.Add( new DecalDefinition() );
                            }
                        }
                        GUILayout.Space( 16 );
                    }
                    EndCenteredGroup();
                    GUILayout.Space( 16 );                   
                }
            }

            GUILayout.Space( 16 );


        }

#endif


        public override void Unload() {   
            UnloadResources();
        }


        public override void UnloadResources() {
             
            for (int i = 0; i < ProfileDecals.Count; i++ ) 
               RenderTexture.ReleaseTemporary( ProfileDecals[i].finalOutput );                
            }

        }


    }