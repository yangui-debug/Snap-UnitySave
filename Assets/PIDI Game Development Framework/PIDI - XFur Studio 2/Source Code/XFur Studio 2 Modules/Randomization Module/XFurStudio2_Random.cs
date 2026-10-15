namespace XFurStudio2 {

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    [System.Serializable]
    public class XFurStudio2_Random : XFurStudioModule {


        [System.Serializable]
        public class RandomizationSettings {

            /// <summary>
            /// Enables / Disables randomization for this profile
            /// </summary>
            public bool Enabled;

            /// <summary>
            /// The randomziation mode : 0 values, 1 profiles, 2 both
            /// </summary>
            public int RandomizationMode = 1;

            /// <summary>
            /// Whether to randomize the color map
            /// </summary>
            public bool RandomizeColorMap;

            /// <summary>
            /// Whether to randomize the color variation map
            /// </summary>
            public bool RandomizeColorMix;

            /// <summary>
            /// Whether to randomize the fur data maps
            /// </summary>
            public bool RandomizeDataMaps;

            /// <summary>
            /// Whether to randomize the fur strands maps
            /// </summary>
            public bool RandomizeFurStrands;

            /// <summary>
            /// The internal Fur Template used as a target when randomizing by values.
            /// </summary>
            public XFurTemplate RandomizeTo = new XFurTemplate( true );

            /// <summary>
            /// A list of profiles to pick a random one when randomizing by profiles
            /// </summary>
            public List<XFurStudioFurProfile> RandomProfiles = new List<XFurStudioFurProfile>();
        }

        /// <summary>
        /// Whether the randomization process should happen automatically on the Load function
        /// </summary>
        public bool RandomizeOnLoad = true;

        /// <summary>
        /// The list of randomization settings for each profile.
        /// </summary>
        public List<RandomizationSettings> RandomSettings = new List<RandomizationSettings>();

        public override void Setup(XFurStudioInstance xfurOwner, bool update = false) {
            if ( !update ) {
                moduleName = "Randomization";
                moduleStatus = 3;
                isEnabled = true;
                version = "2.0";
                experimentalFeatures = false;
                hasMobileMode = true;
                hasSRPMode = true;
            }
            xfurInstance = xfurOwner;

        }

        public XFurStudio2_Random() {

        }

        public XFurStudio2_Random( XFurStudio2_Random source ) {
            RandomSettings = source.RandomSettings;
            RandomizeOnLoad = source.RandomizeOnLoad;
            moduleName = source.moduleName;
            moduleStatus = source.moduleStatus;
            isEnabled = source.isEnabled;
            version = source.version;
            experimentalFeatures = source.experimentalFeatures;
            hasMobileMode = source.hasMobileMode;
            hasSRPMode = source.hasSRPMode;
        }


        public override void Load() {
            base.Load();

            if (RandomizeOnLoad)
                RandomizeProfiles();

        }


        /// <summary>
        /// Triggers the randomization process based on the settings of each fur profile
        /// </summary>
        public void RandomizeProfiles() {

            if ( Application.isPlaying ) {
                for ( int i = 0; i < RandomSettings.Count; i++ ) {
                    switch ( RandomSettings[i].RandomizationMode ) {
                        case 0 :
                            
                            float lerpValue = Random.Range( 0.0f, 1.0f );
                            var intLerp = Random.Range( 0, 2 );

                            if ( RandomSettings[i].RandomizeColorMap ) {
                                if ( RandomSettings[i].RandomizeTo.FurColorMap ) {
                                    if (intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[i].FurColorMap = RandomSettings[i].RandomizeTo.FurColorMap;
                                    }
                                }
                            }

                            if ( RandomSettings[i].RandomizeDataMaps ) {
                                if ( RandomSettings[i].RandomizeTo.FurData0 ) {
                                    if (intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[i].FurData0 = RandomSettings[i].RandomizeTo.FurData0;
                                    }
                                }
                                
                                if ( RandomSettings[i].RandomizeTo.FurData1 ) {
                                    if (intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[i].FurData1 = RandomSettings[i].RandomizeTo.FurData1;
                                    }
                                }
                            }

                            if ( RandomSettings[i].RandomizeColorMix ) {
                                if ( RandomSettings[i].RandomizeTo.FurColorVariation ) {
                                    if (intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[i].FurColorVariation = RandomSettings[i].RandomizeTo.FurColorVariation;
                                    }
                                }

                                xfurInstance.FurDataProfiles[i].FurColorA = Color.Lerp( RandomSettings[i].RandomizeTo.FurColorA, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurColorB = Color.Lerp( RandomSettings[i].RandomizeTo.FurColorB, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurColorC = Color.Lerp( RandomSettings[i].RandomizeTo.FurColorC, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurColorD = Color.Lerp( RandomSettings[i].RandomizeTo.FurColorD, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );

                            }

                            if ( RandomSettings[i].RandomizeFurStrands ) {
                                if ( RandomSettings[i].RandomizeTo.FurStrandsAsset ) {
                                    if (intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[i].FurStrandsAsset = RandomSettings[i].RandomizeTo.FurStrandsAsset;
                                    }
                                }
                            }


                            xfurInstance.FurDataProfiles[i].FurMainTint = Color.Lerp( RandomSettings[i].RandomizeTo.FurMainTint, xfurInstance.FurDataProfiles[i].FurMainTint, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurLength = Mathf.Lerp( RandomSettings[i].RandomizeTo.FurLength, xfurInstance.FurDataProfiles[i].FurLength, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurThickness = Mathf.Lerp( RandomSettings[i].RandomizeTo.FurThickness, xfurInstance.FurDataProfiles[i].FurThickness, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurThicknessCurve = Mathf.Lerp( RandomSettings[i].RandomizeTo.FurThicknessCurve, xfurInstance.FurDataProfiles[i].FurThicknessCurve, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurOcclusion = Mathf.Lerp( RandomSettings[i].RandomizeTo.FurOcclusion, xfurInstance.FurDataProfiles[i].FurOcclusion, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurOcclusionCurve = Mathf.Lerp( RandomSettings[i].RandomizeTo.FurOcclusionCurve, xfurInstance.FurDataProfiles[i].FurOcclusionCurve, lerpValue );
                            xfurInstance.FurDataProfiles[i].FurShadowsTint = Color.Lerp( RandomSettings[i].RandomizeTo.FurShadowsTint, xfurInstance.FurDataProfiles[i].FurShadowsTint, lerpValue );
                            break;


                        case 1 :

                            if ( RandomSettings[i].RandomProfiles.Count > 0 ) {

                                int profile = Random.Range( 0, RandomSettings[i].RandomProfiles.Count );

                                if ( RandomSettings[i].RandomProfiles[profile] != null ) {
                                    xfurInstance.SetFurData( i, RandomSettings[i].RandomProfiles[profile].FurTemplate, RandomSettings[i].RandomizeColorMap, RandomSettings[i].RandomizeDataMaps, RandomSettings[i].RandomizeColorMix, RandomSettings[i].RandomizeFurStrands );
                                }

                            }

                            break;


                        case 2:

                            if ( RandomSettings[i].RandomProfiles.Count > 0 ) {

                                int profile = Random.Range( 0, RandomSettings[i].RandomProfiles.Count );

                                lerpValue = Random.Range( 0.0f, 1.0f );
                                intLerp = Random.Range( 0, 2 );

                                if ( RandomSettings[i].RandomizeColorMap ) {
                                    if ( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorMap ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[i].FurColorMap = RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorMap;
                                        }
                                    }
                                }

                                if ( RandomSettings[i].RandomizeDataMaps ) {
                                    if ( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurData0 ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[i].FurData0 = RandomSettings[i].RandomProfiles[profile].FurTemplate.FurData0;
                                        }
                                    }

                                    if ( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurData1 ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[i].FurData1 = RandomSettings[i].RandomProfiles[profile].FurTemplate.FurData1;
                                        }
                                    }
                                }

                                if ( RandomSettings[i].RandomizeColorMix ) {
                                    if ( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorVariation ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[i].FurColorVariation = RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorVariation;
                                        }
                                    }

                                    xfurInstance.FurDataProfiles[i].FurColorA = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorA, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[i].FurColorB = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorB, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[i].FurColorC = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorC, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[i].FurColorD = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurColorD, xfurInstance.FurDataProfiles[i].FurColorA, lerpValue );

                                }

                                if ( RandomSettings[i].RandomizeFurStrands ) {
                                    if ( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurStrandsAsset ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[i].FurStrandsAsset = RandomSettings[i].RandomProfiles[profile].FurTemplate.FurStrandsAsset;
                                        }
                                    }
                                }



                                xfurInstance.FurDataProfiles[i].FurMainTint = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurMainTint, xfurInstance.FurDataProfiles[i].FurMainTint, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurLength = Mathf.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurLength, xfurInstance.FurDataProfiles[i].FurLength, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurThickness = Mathf.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurThickness, xfurInstance.FurDataProfiles[i].FurThickness, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurThicknessCurve = Mathf.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurThicknessCurve, xfurInstance.FurDataProfiles[i].FurThicknessCurve, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurOcclusion = Mathf.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurOcclusion, xfurInstance.FurDataProfiles[i].FurOcclusion, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurOcclusionCurve = Mathf.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurOcclusionCurve, xfurInstance.FurDataProfiles[i].FurOcclusionCurve, lerpValue );
                                xfurInstance.FurDataProfiles[i].FurShadowsTint = Color.Lerp( RandomSettings[i].RandomProfiles[profile].FurTemplate.FurShadowsTint, xfurInstance.FurDataProfiles[i].FurShadowsTint, lerpValue );
                            
                            }
                            
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Triggers the randomization process based on the settings of each fur profile
        /// </summary>
        public void RandomizeProfiles( int index ) {

            if ( Application.isPlaying ) {
                    switch ( RandomSettings[index].RandomizationMode ) {
                        case 0:

                            float lerpValue = Random.Range( 0.0f, 1.0f );
                            var intLerp = Random.Range( 0, 2 );

                            if ( RandomSettings[index].RandomizeColorMap ) {
                                if ( RandomSettings[index].RandomizeTo.FurColorMap ) {
                                    if ( intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[index].FurColorMap = RandomSettings[index].RandomizeTo.FurColorMap;
                                    }
                                }
                            }

                            if ( RandomSettings[index].RandomizeDataMaps ) {
                                if ( RandomSettings[index].RandomizeTo.FurData0 ) {
                                    if ( intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[index].FurData0 = RandomSettings[index].RandomizeTo.FurData0;
                                    }
                                }

                                if ( RandomSettings[index].RandomizeTo.FurData1 ) {
                                    if ( intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[index].FurData1 = RandomSettings[index].RandomizeTo.FurData1;
                                    }
                                }
                            }

                            if ( RandomSettings[index].RandomizeColorMix ) {
                                if ( RandomSettings[index].RandomizeTo.FurColorVariation ) {
                                    if ( intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[index].FurColorVariation = RandomSettings[index].RandomizeTo.FurColorVariation;
                                    }
                                }

                                xfurInstance.FurDataProfiles[index].FurColorA = Color.Lerp( RandomSettings[index].RandomizeTo.FurColorA, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurColorB = Color.Lerp( RandomSettings[index].RandomizeTo.FurColorB, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurColorC = Color.Lerp( RandomSettings[index].RandomizeTo.FurColorC, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurColorD = Color.Lerp( RandomSettings[index].RandomizeTo.FurColorD, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );

                            }

                            if ( RandomSettings[index].RandomizeFurStrands ) {
                                if ( RandomSettings[index].RandomizeTo.FurStrandsAsset ) {
                                    if ( intLerp < 1 ) {
                                        xfurInstance.FurDataProfiles[index].FurStrandsAsset = RandomSettings[index].RandomizeTo.FurStrandsAsset;
                                    }
                                }
                            }


                            xfurInstance.FurDataProfiles[index].FurMainTint = Color.Lerp( RandomSettings[index].RandomizeTo.FurMainTint, xfurInstance.FurDataProfiles[index].FurMainTint, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurLength = Mathf.Lerp( RandomSettings[index].RandomizeTo.FurLength, xfurInstance.FurDataProfiles[index].FurLength, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurThickness = Mathf.Lerp( RandomSettings[index].RandomizeTo.FurThickness, xfurInstance.FurDataProfiles[index].FurThickness, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurThicknessCurve = Mathf.Lerp( RandomSettings[index].RandomizeTo.FurThicknessCurve, xfurInstance.FurDataProfiles[index].FurThicknessCurve, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurOcclusion = Mathf.Lerp( RandomSettings[index].RandomizeTo.FurOcclusion, xfurInstance.FurDataProfiles[index].FurOcclusion, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurOcclusionCurve = Mathf.Lerp( RandomSettings[index].RandomizeTo.FurOcclusionCurve, xfurInstance.FurDataProfiles[index].FurOcclusionCurve, lerpValue );
                            xfurInstance.FurDataProfiles[index].FurShadowsTint = Color.Lerp( RandomSettings[index].RandomizeTo.FurShadowsTint, xfurInstance.FurDataProfiles[index].FurShadowsTint, lerpValue );
                            break;


                        case 1:

                            if ( RandomSettings[index].RandomProfiles.Count > 0 ) {

                                int profile = Random.Range( 0, RandomSettings[index].RandomProfiles.Count );

                                if ( RandomSettings[index].RandomProfiles[profile] != null ) {
                                    xfurInstance.SetFurData( index, RandomSettings[index].RandomProfiles[profile].FurTemplate, RandomSettings[index].RandomizeColorMap, RandomSettings[index].RandomizeDataMaps, RandomSettings[index].RandomizeColorMix, RandomSettings[index].RandomizeFurStrands );
                                }

                            }

                            break;


                        case 2:

                            if ( RandomSettings[index].RandomProfiles.Count > 0 ) {

                                int profile = Random.Range( 0, RandomSettings[index].RandomProfiles.Count );

                                lerpValue = Random.Range( 0.0f, 1.0f );
                                intLerp = Random.Range( 0, 2 );

                                if ( RandomSettings[index].RandomizeColorMap ) {
                                    if ( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorMap ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[index].FurColorMap = RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorMap;
                                        }
                                    }
                                }

                                if ( RandomSettings[index].RandomizeDataMaps ) {
                                    if ( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurData0 ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[index].FurData0 = RandomSettings[index].RandomProfiles[profile].FurTemplate.FurData0;
                                        }
                                    }

                                    if ( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurData1 ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[index].FurData1 = RandomSettings[index].RandomProfiles[profile].FurTemplate.FurData1;
                                        }
                                    }
                                }

                                if ( RandomSettings[index].RandomizeColorMix ) {
                                    if ( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorVariation ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[index].FurColorVariation = RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorVariation;
                                        }
                                    }

                                    xfurInstance.FurDataProfiles[index].FurColorA = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorA, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[index].FurColorB = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorB, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[index].FurColorC = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorC, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );
                                    xfurInstance.FurDataProfiles[index].FurColorD = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurColorD, xfurInstance.FurDataProfiles[index].FurColorA, lerpValue );

                                }

                                if ( RandomSettings[index].RandomizeFurStrands ) {
                                    if ( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurStrandsAsset ) {
                                        if ( intLerp < 1 ) {
                                            xfurInstance.FurDataProfiles[index].FurStrandsAsset = RandomSettings[index].RandomProfiles[profile].FurTemplate.FurStrandsAsset;
                                        }
                                    }
                                }



                                xfurInstance.FurDataProfiles[index].FurMainTint = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurMainTint, xfurInstance.FurDataProfiles[index].FurMainTint, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurLength = Mathf.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurLength, xfurInstance.FurDataProfiles[index].FurLength, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurThickness = Mathf.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurThickness, xfurInstance.FurDataProfiles[index].FurThickness, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurThicknessCurve = Mathf.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurThicknessCurve, xfurInstance.FurDataProfiles[index].FurThicknessCurve, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurOcclusion = Mathf.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurOcclusion, xfurInstance.FurDataProfiles[index].FurOcclusion, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurOcclusionCurve = Mathf.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurOcclusionCurve, xfurInstance.FurDataProfiles[index].FurOcclusionCurve, lerpValue );
                                xfurInstance.FurDataProfiles[index].FurShadowsTint = Color.Lerp( RandomSettings[index].RandomProfiles[profile].FurTemplate.FurShadowsTint, xfurInstance.FurDataProfiles[index].FurShadowsTint, lerpValue );

                            }

                            break;
                    }
                }
            
        }

#if UNITY_EDITOR

        public bool[] randomFolds;

        public override void ModuleUI() {

            base.ModuleUI();
            GUILayout.Space( 16 );

            //UnityEditor.Undo.RecordObject( this, xfurInstance.name + xfurInstance.GetInstanceID() + this.name );

            if ( xfurInstance.MainRenderer.materials != null ) {

                if ( RandomSettings == null || RandomSettings.Count < xfurInstance.MainRenderer.materials.Length ) {
                    RandomSettings = new List<RandomizationSettings>();
                    for (int i = 0; i < xfurInstance.MainRenderer.materials.Length; i++ ) {
                        RandomSettings.Add( new RandomizationSettings() );
                    }
                }

                if (randomFolds == null || randomFolds.Length < RandomSettings.Count ) {
                    randomFolds = new bool[RandomSettings.Count];
                }


                RandomizeOnLoad = EnableDisableToggle( new GUIContent( "Randomize On Start", "Applies all random settings and randomizes the profiles of this instance upon loading it (on the OnStart function)" ), RandomizeOnLoad, true );

                GUILayout.Space( 16 );


                for ( int i = 0; i < xfurInstance.MainRenderer.materials.Length; i++ ) {
                    if (xfurInstance.MainRenderer.isFurMaterial[i])
                        RandomSettings[i].Enabled = EnableDisableToggle( new GUIContent( "Randomize "+xfurInstance.MainRenderer.materials[i].name ), RandomSettings[i].Enabled );
                }

                GUILayout.Space( 16 );

                for (int i = 0; i < RandomSettings.Count; i++ ) {
                    if (xfurInstance.MainRenderer.isFurMaterial[i] && RandomSettings[i].Enabled ) {

                        if (BeginCenteredGroup(xfurInstance.MainRenderer.materials[i].name, ref randomFolds[i] ) ) {
                            GUILayout.Space( 16 );

                            RandomSettings[i].RandomizationMode = PopupField( new GUIContent( "Randomization Mode", "Defines how the fur settings will be randomized\n\nRandomize Values : Each fur value is randomized between the original fur settings and the settings in the randomization module\n\nPick Random Profile : A random fur profile from a list is picked and assigned to the character\n\nRandomize Values & Profiles : Pick a random profile from a list, and randomize the fur settings between the original settings assigned to this character and the ones in the random profile" ), RandomSettings[i].RandomizationMode, new string[] { "Randomize Values", "Pick Random Profile", "Randomize Values and Profiles" } );

                            GUILayout.Space( 16 );

                            RandomSettings[i].RandomizeColorMap = EnableDisableToggle( new GUIContent( "Randomize Color Map" ), RandomSettings[i].RandomizeColorMap, true );
                            RandomSettings[i].RandomizeDataMaps = EnableDisableToggle( new GUIContent( "Randomize Data Maps" ), RandomSettings[i].RandomizeDataMaps, true );
                            RandomSettings[i].RandomizeColorMix = EnableDisableToggle( new GUIContent( "Randomize Color Mixing" ), RandomSettings[i].RandomizeColorMix, true );
                            RandomSettings[i].RandomizeFurStrands = EnableDisableToggle( new GUIContent( "Randomize Strands Asset" ), RandomSettings[i].RandomizeFurStrands, true );

                            GUILayout.Space( 16 );

                            switch ( RandomSettings[i].RandomizationMode ) {

                                case 0:

                                    if ( RandomSettings[i].RandomizeFurStrands ) {
                                            RandomSettings[i].RandomizeTo.FurStrandsAsset = ObjectField<XFurStudioStrandsAsset>( new GUIContent( "Fur Strands Asset", "The texture map used to generate the fur strands for this fur profile" ), RandomSettings[i].RandomizeTo.FurStrandsAsset );
                                    }
                                    
                                    if ( RandomSettings[i].RandomizeColorMap ) {
                                        RandomSettings[i].RandomizeTo.FurColorMap = ObjectField<Texture>( new GUIContent( "Fur Color Map", "The texture that controls the color / albedo applied over the whole fur surface" ), RandomSettings[i].RandomizeTo.FurColorMap );
                                    }

                                    if ( RandomSettings[i].RandomizeDataMaps ) {
                                        xfurInstance.FurDataProfiles[i].FurData0 = ObjectField<Texture>( new GUIContent( "Fur Data Map", "The texture that controls the parameters of the fur :\n\n R = fur mask\n G = length\n B = occlusion\n A = thickness" ), xfurInstance.FurDataProfiles[i].FurData0 );
                                        xfurInstance.FurDataProfiles[i].FurData1 = ObjectField<Texture>( new GUIContent( "Fur Grooming Map", "The texture that controls the direction of the fur :\n\n RGB = fur direction\n A = stiffness" ), xfurInstance.FurDataProfiles[i].FurData1 );
                                        
                                    }

                                    if ( RandomSettings[i].RandomizeColorMix ) {
                                        GUILayout.Space( 8 );
                                        if ( !xfurInstance.FurDatabase.MobileMode ) {
                                            RandomSettings[i].RandomizeTo.FurColorVariation = ObjectField<Texture>( new GUIContent( "Fur Color Variation", "The texture that controls four additional coloring variations to be applied over the fur, either all four to the whole fur or two to the undercoat and two to the overcoat by using the four color channels." ), RandomSettings[i].RandomizeTo.FurColorVariation );
                                        }

                                        if ( RandomSettings[i].RandomizeTo.FurColorVariation ) {
                                            GUILayout.Space( 8 );
                                            RandomSettings[i].RandomizeTo.FurColorA = ColorField( new GUIContent( "Fur Color A", "The fur color to be applied on the red channel of the Color Variation map" ), RandomSettings[i].RandomizeTo.FurColorA );
                                            RandomSettings[i].RandomizeTo.FurColorB = ColorField( new GUIContent( "Fur Color B", "The fur color to be applied on the green channel of the Color Variation map" ), RandomSettings[i].RandomizeTo.FurColorB );
                                            RandomSettings[i].RandomizeTo.FurColorC = ColorField( new GUIContent( "Fur Color C", "The fur color to be applied on the blue channel of the Color Variation map" ), RandomSettings[i].RandomizeTo.FurColorC );
                                            RandomSettings[i].RandomizeTo.FurColorD = ColorField( new GUIContent( "Fur Color D", "The fur color to be applied on the alpha channel of the Color Variation map" ), RandomSettings[i].RandomizeTo.FurColorD );
                                        }
                                    }

                                    GUILayout.Space( 16 );

                                    RandomSettings[i].RandomizeTo.FurLength = SliderField( new GUIContent( "Fur Length", "The maximum overall length of the fur. This will be multiplied by the actual fur profile length and the length painted in XFur Studio™ - Designer" ), RandomSettings[i].RandomizeTo.FurLength );

                                    GUILayout.Space( 8 );
                                    RandomSettings[i].RandomizeTo.FurThickness = SliderField( new GUIContent( "Fur Thickness", "The maximum overall thickness of the fur. This will be multiplied by the actual fur profile thickness and the thickness painted in XFur Studio™ - Designer" ), RandomSettings[i].RandomizeTo.FurThickness );
                                    RandomSettings[i].RandomizeTo.FurThicknessCurve = SliderField( new GUIContent( "Thickness Curve", "How the fur strands' thickness bias will change from the root to the top of each strand" ), RandomSettings[i].RandomizeTo.FurThicknessCurve );
                                    GUILayout.Space( 8 );

                                    RandomSettings[i].RandomizeTo.FurShadowsTint = ColorField( new GUIContent( "Occlusion Tint" ), RandomSettings[i].RandomizeTo.FurShadowsTint );
                                    RandomSettings[i].RandomizeTo.FurOcclusion = SliderField( new GUIContent( "Fur Occlusion / Shadowing", "The shadowing applied over the surface of the fur strands as a simple occlusion pass. Multiplied by the per-profile occlusion value and the one painted through XFur Studio™ - Designer" ), RandomSettings[i].RandomizeTo.FurOcclusion );
                                    RandomSettings[i].RandomizeTo.FurOcclusionCurve = SliderField( new GUIContent( "Fur Occlusion Curve", "How the shadowing / occlusion of the fur will go from the root to the tip of each strand" ), RandomSettings[i].RandomizeTo.FurOcclusionCurve );

                                    GUILayout.Space( 16 );

                                    if (CenteredButton( "Copy from current Settings", 256 ) ) {
                                        xfurInstance.GetFurData( i, out RandomSettings[i].RandomizeTo );
                                    }

                                    GUILayout.Space( 24 );

                                    break;

                                default :

                                    for ( int p = 0; p < RandomSettings[i].RandomProfiles.Count; p++ ) {
                                        GUILayout.BeginHorizontal();

                                        RandomSettings[i].RandomProfiles[p] = ObjectField<XFurStudioFurProfile>( new GUIContent( "Fur Profile " + p ), RandomSettings[i].RandomProfiles[p] );

                                        if ( StandardButton("X", 24 ) ) {
                                            RandomSettings[i].RandomProfiles.RemoveAt( p );
                                            GUILayout.EndHorizontal();
                                            break;
                                        }

                                        GUILayout.EndHorizontal();
                                    }

                                    GUILayout.Space( 16 );

                                    if ( CenteredButton("Add new Random Profile", 256 ) ) {
                                        RandomSettings[i].RandomProfiles.Add( null );
                                    }

                                    GUILayout.Space( 16 );

                                    break;

                            }

                        }
                        EndCenteredGroup();

                    }
                }


            
            }

            GUILayout.Space( 16 );
        }

        public override void UpdateModule() {
            moduleName = "Randomization";
            moduleStatus = 3;
            version = "2.0";
            experimentalFeatures = false;
            hasMobileMode = true;
            hasSRPMode = true;
        }

#endif

    }

}