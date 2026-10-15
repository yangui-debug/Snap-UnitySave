namespace XFurStudio2 {
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEngine;
    using System.IO;
#if UNITY_EDITOR
    using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

    public class XFurStudio2_Designer : MonoBehaviour {
        public class XFurDesignerInstance {

            public bool muteInstance;



            [System.NonSerialized] public MeshCollider colliderObject;
            [System.NonSerialized] public XFurStudioInstance targetXFur;
            [System.NonSerialized] public Mesh targetMesh;
            [System.NonSerialized] public Matrix4x4 targetMatrix;

            public List<DesignerDataContainer> FurData = new List<DesignerDataContainer>();


            public XFurDesignerInstance( XFurStudioInstance target ) {
                targetXFur = target;

                for ( int i = 0; i < target.FurDataProfiles.Length; i++ ) {
                    FurData.Add( new DesignerDataContainer( target.CurrentFurRenderer.isFurMaterial[i], this, i ) );
                }

                if ( targetXFur.CurrentFurRenderer.renderer.GetComponent<SkinnedMeshRenderer>() ) {
                    targetMesh = new Mesh();
                    targetXFur.CurrentFurRenderer.renderer.GetComponent<SkinnedMeshRenderer>().BakeMesh( targetMesh );
                    targetMesh.bounds = targetXFur.CurrentFurRenderer.renderer.GetComponent<SkinnedMeshRenderer>().bounds;
                }
                else {
                    targetMesh = targetXFur.CurrentMesh;
                }

                targetMatrix = targetXFur.CurrentFurRenderer.renderer.transform.localToWorldMatrix;


            }

        }




        public class DesignerDataContainer {

            [System.NonSerialized] public XFurDesignerInstance xfurInstance;
            public int index;
            public bool isFur;


            public List<RenderTexture> FurData0UndoSteps = new List<RenderTexture>();
            public List<RenderTexture> FurData1UndoSteps = new List<RenderTexture>();
            public List<RenderTexture> FurColorMapUndoSteps = new List<RenderTexture>();
            public List<RenderTexture> FurColorVariationUndoSteps = new List<RenderTexture>();
            public List<RenderTexture> FurProfileMixUndoSteps = new List<RenderTexture>();

            public int data0Undo = -1;
            public int data1Undo = -1;
            public int colorMapUndo = -1;
            public int colorMixUndo = -1;
            public int dataProfileMixUndo = -1;

            public RenderTexture FurData0;
            public RenderTexture FurData1;
            public RenderTexture FurColorMap;
            public RenderTexture FurColorVariation;
            public RenderTexture FurBlendSplatmap;

            public RenderTexture originalAlbedo;
            public RenderTexture brushPointer;

            public DesignerDataContainer( bool isFurProfile, XFurDesignerInstance owner, int matIndex ) {
                isFur = isFurProfile;
                xfurInstance = owner;
                index = matIndex;

                if ( isFur ) {
                    var rt = RenderTexture.active;
                    var rd = new RenderTextureDescriptor( 1024, 1024, RenderTextureFormat.ARGB32 );
                    rd.sRGB = false;
                    rd.autoGenerateMips = false;

                    

                    if ( owner.targetXFur.FurDataProfiles[index].FurColorMap ) {
                        FurColorMap = RenderTexture.GetTemporary( owner.targetXFur.FurDataProfiles[index].FurColorMap.width, owner.targetXFur.FurDataProfiles[index].FurColorMap.height );
                        Graphics.Blit( owner.targetXFur.FurDataProfiles[index].FurColorMap, FurColorMap );
                    }
                    else {
                        FurColorMap = RenderTexture.GetTemporary( rd );
                        FurColorMap.Create();
                        RenderTexture.active = FurColorMap;
                        GL.Clear( true, true, Color.white );
                    }

                    RenderTexture.active = rt;
                    originalAlbedo = RenderTexture.GetTemporary( FurColorMap.width, FurColorMap.height );
                    Graphics.Blit( FurColorMap, originalAlbedo );
                    brushPointer = RenderTexture.GetTemporary( rd );
                }

            }


        }



        #region INTERNAL DATA

        public CustomRenderTexture colorPicker;

        public int MaxUndoSteps = 8;

        private int ActiveTarget = 0;

        private List<XFurDesignerInstance> TargetInstances = new List<XFurDesignerInstance>();

        private Vector3 lastPos;

        private int buffPos;

        private Mesh targetMesh;

        private Bounds meshBounds;

        private Transform pointer;

        private Vector3 pivotOffset;

        private float orbitX, orbitY, distance;

        #endregion

        #region DESIGNER_DATA

        [HideInInspector] public GUISkin pidiSkin2;

        private bool invertBrush = false, symmetryMode = false, showColorPickerL = false, showColorPickerR = false;

        private float brushSize = 0.1f;
        private float brushOpacity = 1f;
        private float brushHardness = 0.85f;

        private int furMaterial = 0;
        private bool furMaterialDrop;

        private int resolution = 1;
        private bool resolutionDrop;
        private bool exportFurProfiles = false;

        private int furProperty = 0;
        private bool furPropertyDrop;

        private int FurColorVariationChannel = 0;
        private bool FurColorVariationDrop;

        //private int furProfilesChannel = 0;
        private bool furProfilesDrop;

        private int designerMode = 0;
        private bool designerModeDrop;

        private Color brushColor = Color.white;
        private Color sourceColor = Color.white;

        private List<string> furMaterialNames = new List<string>();

        private Texture2D brushTexture;

        private Texture2D brushPreview;

        private float scale = Screen.height / 1080.0f;

        private float scrollView;

        private int editColorProperty;

        private Texture2D brushColorPick, furTint1Pick, furTint2Pick, furTint3Pick, furTint4Pick, furOcclusionPick, furMainTintPick;

        #endregion



        public void OnDrawGizmos() {
            Gizmos.color = Color.blue;

            if ( pointer )
                Gizmos.DrawSphere( pointer.position, brushSize );
        }


        public void Start() {


            brushColorPick = new Texture2D( 1, 1 );
            brushColorPick.SetPixel( 0, 0, Color.white );
            brushColorPick.Apply();

            furOcclusionPick = new Texture2D( 1, 1 );
            furOcclusionPick.SetPixel( 0, 0, Color.white );
            furOcclusionPick.Apply();

            furTint1Pick = new Texture2D( 1, 1 );
            furTint1Pick.SetPixel( 0, 0, Color.white );
            furTint1Pick.Apply();

            furTint2Pick = new Texture2D( 1, 1 );
            furTint2Pick.SetPixel( 0, 0, Color.white );
            furTint2Pick.Apply();

            furTint3Pick = new Texture2D( 1, 1 );
            furTint3Pick.SetPixel( 0, 0, Color.white );
            furTint3Pick.Apply();

            furTint4Pick = new Texture2D( 1, 1 );
            furTint4Pick.SetPixel( 0, 0, Color.white );
            furTint4Pick.Apply();

            furMainTintPick = new Texture2D( 1, 1 );
            furMainTintPick.SetPixel( 0, 0, Color.white );
            furMainTintPick.Apply();

            brushTexture = Texture2D.whiteTexture;

            brushPreview = new Texture2D( 128, 128, TextureFormat.ARGB32, false );
#if UNITY_EDITOR
            GenerateBrushPreview( brushPreview );
#endif

            XFurStudioAPI.LoadPaintResources();

            pointer = new GameObject().GetComponent<Transform>();

            var targets = GameObject.FindObjectsOfType<XFurStudioInstance>();

            for ( int i = 0; i < targets.Length; i++ ) {
                TargetInstances.Add( new XFurDesignerInstance( targets[i] ) );
            }




            if ( TargetInstances.Count > 0 ) {

                furOcclusionPick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurShadowsTint );
                furOcclusionPick.Apply();

                furTint1Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorA );
                furTint1Pick.Apply();

                furTint2Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorB );
                furTint2Pick.Apply();

                furTint3Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorC );
                furTint3Pick.Apply();

                furTint4Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorD );
                furTint4Pick.Apply();

                furMainTintPick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurMainTint );
                furMainTintPick.Apply();

                furOcclusionPick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurShadowsTint );
                furOcclusionPick.Apply();

                distance = TargetInstances[ActiveTarget].targetMesh.bounds.size.magnitude;

            }


        }






        public void UpdateMaterialValues() {

            furOcclusionPick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurShadowsTint );
            furOcclusionPick.Apply();

            furTint1Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorA );
            furTint1Pick.Apply();

            furTint2Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorB );
            furTint2Pick.Apply();

            furTint3Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorC );
            furTint3Pick.Apply();

            furTint4Pick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorD );
            furTint4Pick.Apply();

            furMainTintPick.SetPixel( 0, 0, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurMainTint );
            furMainTintPick.Apply();

        }



#if UNITY_EDITOR



        public void GenerateBrushPreview( Texture2D shapeTex ) {

            for ( int y = 0; y < shapeTex.width; ++y ) {
                for ( int x = 0; x < shapeTex.width; ++x ) {
                    var dist = Vector2.Distance( new Vector2( (int)( shapeTex.width * 0.5f ), (int)( shapeTex.width * 0.5f ) ), new Vector2( x, y ) );
                    var gradPoint = Mathf.Clamp01( ( 0.8f - ( dist / ( shapeTex.width * 0.5f ) ) ) / ( 1 - Mathf.Max( Mathf.Clamp01( brushHardness * 2 ), 0.5f ) ) ) * brushOpacity;
                    shapeTex.SetPixel( x, y, new Color( gradPoint, gradPoint, gradPoint, 1 ) );
                }
            }


            shapeTex.Apply();

        }

        private void OnGUI() {

            scale = Screen.height / 1080.0f;


            if ( !pidiSkin2 ) {
                pidiSkin2 = (GUISkin)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets( "PIDI_EditorSkin_XFurStudio2" )[0] ), typeof( GUISkin ) );
                pidiSkin2 = Instantiate( pidiSkin2 );
                pidiSkin2.name = "COPYSKIN";
            }

            pidiSkin2.label.normal.textColor = Color.white;

            pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 14 * scale ), 12, 28 );
            pidiSkin2.button.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
            pidiSkin2.customStyles[0].fontSize = Mathf.Clamp( Mathf.RoundToInt( 14 * scale ), 12, 28 );
            pidiSkin2.customStyles[0].padding = new RectOffset( 0, 0, 0, 0 );
            pidiSkin2.label.padding = new RectOffset( 0, 0, 0, 0 );
            pidiSkin2.label.margin = new RectOffset( 0, 0, 0, 0 );
            pidiSkin2.label.alignment = TextAnchor.UpperLeft;


            if ( TargetInstances.Count < 1 || TargetInstances[ActiveTarget].targetXFur == null ) {

                GUILayout.BeginArea( new Rect( 0, 0, Screen.width, 40 * scale ) );
                GUI.color = Color.white;
                GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
                CenteredLabel( "There are no models with a properly configured XFur Studio Instance component. To use XFur Studio Designer there must be a model with a XFur Studio Instance component assigned in the scene." );
                GUILayout.FlexibleSpace(); GUILayout.EndVertical();
                GUILayout.EndArea();

                return;
            }

            

            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( 0, 0, Screen.width, 40 * scale ), pidiSkin2.customStyles[0] );
            GUI.color = Color.white;
            GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
            CenteredLabel( "XFur Studio™ 2 - Designer" );
            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUILayout.EndArea();

            //Brush display and active tool area

            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( 0, 40 * scale, 256 * scale, 530 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;
                GUILayout.Box( "Brush Settings", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUI.DrawTexture( new Rect( 58 * scale, 70 * scale, 140 * scale, 140 * scale ), brushPreview );

                GUILayout.Space( 180 * scale );

                CenteredLabel( "Active Tool" );
                GUILayout.Space( 10 * scale );

                var designerDropRect = new Rect( 38 * scale, 290 * scale, 160 * scale, 150 * scale );
                var furPropertyDropRect = new Rect( 38 * scale, 360 * scale, 160 * scale, 120 * scale );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                if ( CenteredButton( new string[] { "Fur Coverage", "Fur Properties", "Fur Grooming", "Color Mixing", "Color Painting" }[designerMode], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) ) ) {
                    designerModeDrop = true;
                }



                switch ( designerMode ) {

                    case 0:
                        GUILayout.Space( 40 * scale );
                        pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                        if ( designerModeDrop ) {
                            Toggle( "Remove Fur", invertBrush );
                        }
                        else {
                            invertBrush = Toggle( "Remove Fur", invertBrush );
                        }

                        break;

                    case 1:
                        GUILayout.Space( 10 * scale );
                        CenteredLabel( "Fur Property" );
                        GUILayout.Space( 10 * scale );

                        pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );

                        if ( CenteredButton( new string[] { "Fur Length", "Fur Thickness", "Fur Occlusion" }[furProperty], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) ) ) {
                            if ( !designerModeDrop ) {
                                furPropertyDrop = true;
                            }
                            else {
                                Event.current.type = EventType.MouseDown;
                            }
                        }

                        GUILayout.Space( 20 * scale );

                        if ( furPropertyDrop || designerModeDrop ) {
                            Toggle( "Invert Property", invertBrush );
                        }
                        else {
                            invertBrush = Toggle( "Invert Property", invertBrush );
                        }

                        break;

                    case 2:
                        GUILayout.Space( 40 * scale );
                        pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );

                        if ( designerModeDrop ) {
                            Toggle( "Remove Groom Data", invertBrush );
                        }
                        else {
                            invertBrush = Toggle( "Remove Groom Data", invertBrush );
                        }
                        break;

                    case 3:
                        GUILayout.Space( 10 * scale );
                        CenteredLabel( "Color Mix Mask" );
                        GUILayout.Space( 10 * scale );

                        pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                        if ( !designerModeDrop ) {
                            if ( CenteredButton( new string[] { "Fur Tint #1", "Fur Tint #2", "Fur Tint #3", "Fur Tint #4" }[FurColorVariationChannel], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) ) ) {
                                FurColorVariationDrop = true;
                            }
                        }
                        else {
                            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                            GUILayout.Box( new string[] { "Fur Tint #1", "Fur Tint #2", "Fur Tint #3", "Fur Tint #4" }[FurColorVariationChannel], pidiSkin2.customStyles[0], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) );
                            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                        }

                        GUILayout.Space( 20 * scale );

                        if ( FurColorVariationDrop || designerModeDrop ) {
                            Toggle( "Invert Color Mask", invertBrush );
                        }
                        else {
                            invertBrush = Toggle( "Invert Color Mask", invertBrush );
                        }

                        break;
                    /*
               case 4:

                   GUILayout.Space( 10 * scale );
                   CenteredLabel( "Profiles Mix Mask" );
                   GUILayout.Space( 10 * scale );

                   pidiSkin2.label.fontSize = Mathf.RoundToInt( 12 * scale );
                   if ( !designerModeDrop ) {
                       if ( CenteredButton( new string[] { "Fur Profile #1", "Fur Profile #2", "Fur Profile #3", "Fur Profile #4" }[furProfilesChannel], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) ) ) {
                           furProfilesDrop = true;
                       }
                   }
                   else {
                       GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                       GUILayout.Box( new string[] { "Fur Profile #1", "Fur Profile #2", "Fur Profile #3", "Fur Profile #4" }[furProfilesChannel], pidiSkin2.customStyles[0], GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) );
                       GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                   }

                   GUILayout.Space( 20 * scale );

                   if ( furProfilesDrop || designerModeDrop ) {
                       Toggle( "Invert Profiles Mask", invertBrush );
                   }
                   else {
                       invertBrush = Toggle( "Invert Profiles Mask", invertBrush );
                   }

                   break;
                   */

                    case 4:

                        GUILayout.Space( 40 * scale );
                        pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                        GUILayout.BeginHorizontal();
                        pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                        GUILayout.Label( "Brush Color", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                        pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                        GUILayout.FlexibleSpace();
                        if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                            showColorPickerL = true;
                        }
                        var colorRect = GUILayoutUtility.GetLastRect();
                        GUI.DrawTexture( colorRect, brushColorPick );
                        GUILayout.EndHorizontal();

                        break;

                }

                GUILayout.FlexibleSpace();

                var buffSize = brushSize;
                var buffHardness = brushHardness;
                var buffOpacity = brushOpacity;

                if ( designerModeDrop || furPropertyDrop ) {
                    SliderField( "Size", brushSize, 0, 2, showNumber: true, extendedNumber:true );
                    SliderField( "Hardness", brushHardness, 0, 1, showNumber: true, extendedNumber: true );
                    if ( designerMode > 0 ) {
                        SliderField( "Opacity", brushOpacity, 0, 1, showNumber: true, extendedNumber: true );
                    }
                }
                else {
                    brushSize = SliderField( "Size", brushSize, 0, 2, showNumber:true, extendedNumber: true );
                    brushHardness = SliderField( "Hardness", brushHardness, 0, 1, showNumber: true, extendedNumber: true );
                    if ( designerMode > 0 ) {
                        brushOpacity = SliderField( "Opacity", brushOpacity, 0, 1, showNumber: true, extendedNumber: true );
                    }
                }

                if ( brushSize != buffSize || brushOpacity != buffOpacity || brushHardness != buffHardness ) {
                    GenerateBrushPreview( brushPreview );
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();

                if ( designerModeDrop ) {

                    pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );

                    GUILayout.BeginArea( designerDropRect, pidiSkin2.box );

                    if ( GUILayout.Button( "Fur Coverage Mask", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        designerMode = 0;
                        designerModeDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Properties", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        designerMode = 1;
                        designerModeDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Grooming", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        designerMode = 2;
                        designerModeDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Color Mixing", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        designerMode = 3;
                        designerModeDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Color Painting", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        designerMode = 4;
                        designerModeDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    GUILayout.EndArea();

                    if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                        designerModeDrop = false;
                    }

                }
                else if ( furPropertyDrop ) {

                    pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                    GUILayout.BeginArea( furPropertyDropRect, pidiSkin2.box );
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( "Fur Length", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProperty = 0;
                        furPropertyDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Thickness", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProperty = 1;
                        furPropertyDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Occlusion", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProperty = 2;
                        furPropertyDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndArea();


                    if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                        furPropertyDrop = false;
                    }


                }
                else if ( FurColorVariationDrop ) {

                    pidiSkin2.label.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                    GUILayout.BeginArea( furPropertyDropRect, pidiSkin2.box );

                    if ( GUILayout.Button( "Fur Tint #1", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        FurColorVariationChannel = 0;
                        FurColorVariationDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Tint #2", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        FurColorVariationChannel = 1;
                        FurColorVariationDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Tint #3", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        FurColorVariationChannel = 2;
                        FurColorVariationDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Tint #4", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        FurColorVariationChannel = 3;
                        FurColorVariationDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    GUILayout.EndArea();

                    if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                        FurColorVariationDrop = false;
                    }

                }/*
                else if ( furProfilesDrop ) {

                    pidiSkin2.label.fontSize = Mathf.RoundToInt( 12 * scale );
                    GUILayout.BeginArea( furPropertyDropRect, pidiSkin2.box );

                    if ( GUILayout.Button( "Fur Profile #1", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProfilesChannel = 0;
                        furProfilesDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Profile #2", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProfilesChannel = 1;
                        furProfilesDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Profile #3", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProfilesChannel = 2;
                        furProfilesDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    if ( GUILayout.Button( "Fur Profile #4", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furProfilesChannel = 3;
                        furProfilesDrop = false;
                        Event.current.type = EventType.Used;
                    }
                    GUILayout.EndArea();

                    if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                        furProfilesDrop = false;
                    }

                }
                */
                GUILayout.EndArea();

            }


            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( 0, 570 * scale, 256 * scale, 300 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;
                GUILayout.Box( "Painting Settings", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                GUILayout.Space( 30 * scale );

                CenteredLabel( "Active Material" );

                GUILayout.Space( 10 * scale );


                pidiSkin2.customStyles[0].fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
                if ( GUILayout.Button( TargetInstances[ActiveTarget].targetXFur.MainRenderer.materials[furMaterial].name, pidiSkin2.customStyles[0], GUILayout.Height( 20 * scale ), GUILayout.Width( 120 * scale ) ) ) {
                    furMaterialDrop = true;
                }
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                pidiSkin2.customStyles[0].fontSize = Mathf.Clamp( Mathf.RoundToInt( 14 * scale ), 14, 284 );


                GUILayout.Space( 20 * scale );

                symmetryMode = Toggle( "Symmetry Mode", symmetryMode );

                GUILayout.Space( 10 * scale );

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();

            }
            GUILayout.EndArea();


            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( 0, 800 * scale, 256 * scale, Screen.height - 800 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;
                GUILayout.Box( "Export Settings", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                GUILayout.Space( 30 * scale );

                GUILayout.BeginHorizontal();
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( "Final Resolution", pidiSkin2.label, GUILayout.Width( 100 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.Space( 20 * scale );
                if ( GUILayout.Button( new string[] { "512", "1024", "2048", "4096" }[resolution], pidiSkin2.customStyles[0], GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                    resolutionDrop = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space( 30 * scale );

                exportFurProfiles = Toggle( "Generate Fur Profiles", exportFurProfiles );

                GUILayout.Space( 30 * scale );

                if ( CenteredButton( "Export Maps & Data", GUILayout.Width( 160 * scale ), GUILayout.Height( 30 * scale ) ) ) {
                    ExportTextures();
                }

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();




            }
            GUILayout.EndArea();


            if ( furMaterialDrop ) {

                GUILayout.BeginArea( new Rect( 50 * scale, 695 * scale, 120 * scale, Mathf.Min( TargetInstances[ActiveTarget].FurData.Count * 30, 120 ) * scale ), pidiSkin2.box );

                scrollView = GUILayout.BeginScrollView( new Vector2( 0, scrollView ), false, false, GUILayout.MaxHeight( 120 * scale ) ).y;

                pidiSkin2.label.alignment = TextAnchor.MiddleCenter;

                for ( int i = 0; i < TargetInstances[ActiveTarget].FurData.Count; i++ ) {
                    if ( GUILayout.Button( TargetInstances[ActiveTarget].targetXFur.MainRenderer.materials[i].name, pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                        furMaterial = i;
                        furMaterialDrop = false;
                    }
                }

                pidiSkin2.label.alignment = TextAnchor.UpperLeft;

                GUILayout.EndScrollView();

                GUILayout.EndArea();

                if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                    furMaterialDrop = false;
                }


            }

            if ( resolutionDrop ) {

                GUILayout.BeginArea( new Rect( 130 * scale, 900 * scale, 80 * scale, 130 * scale ), pidiSkin2.box );

                pidiSkin2.label.alignment = TextAnchor.MiddleCenter;

                if ( GUILayout.Button( "512", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                    resolution = 0;
                    resolutionDrop = false;
                }

                if ( GUILayout.Button( "1024", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                    resolution = 1;
                    resolutionDrop = false;
                }

                if ( GUILayout.Button( "2048", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                    resolution = 2;
                    resolutionDrop = false;
                }

                if ( GUILayout.Button( "4096", pidiSkin2.label, GUILayout.Height( 30 * scale ) ) ) {
                    resolution = 3;
                    resolutionDrop = false;
                }

                pidiSkin2.label.alignment = TextAnchor.UpperLeft;

                GUILayout.EndArea();

                if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
                    resolutionDrop = false;
                }

            }


            if ( showColorPickerL ) {
                GUI.color = new Color( 0.75f, 0.75f, 0.75f );
                GUILayout.BeginArea( new Rect( 256 * scale, 200 * scale, 200 * scale, 320 * scale ), pidiSkin2.customStyles[0] );
                GUI.color = Color.white;
                GUI.DrawTexture( new Rect( 30 * scale, 30 * scale, 140 * scale, 140 * scale ), colorPicker );

                GUILayout.Space( 200 * scale );

                Color.RGBToHSV( brushColor, out float hue, out float sat, out float val );

                GUILayout.BeginHorizontal();
                GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                hue = SliderField( "H", hue, 0, 1, 110 );
                sat = SliderField( "S", sat, 0, 1, 110 );
                val = SliderField( "V", val, 0, 1, 110 );

                colorPicker.material.SetFloat( "_Hue", hue );
                colorPicker.material.SetFloat( "_Sat", sat );
                colorPicker.material.SetFloat( "_Val", val );

                var targetColor = Color.HSVToRGB( hue, sat, val );

                var colorRect = new Rect( 288 * scale, 232 * scale, 138 * scale, 138 * scale );
#if ENABLE_INPUT_SYSTEM
            if ( Mouse.current.leftButton.isPressed || targetColor != brushColor ) {
                if ( colorRect.Contains( new Vector2( Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y ) ) ) {
                    sat = ( Mouse.current.position.ReadValue().x - colorRect.x ) / ( 138 * scale );
                    val = 1 - ( ( ( Screen.height - Mouse.current.position.ReadValue().y ) - colorRect.y ) / ( 138 * scale ) );
                }
#else
            if ( Input.GetMouseButton( 0 ) || targetColor != brushColor ) {
                if ( colorRect.Contains( new Vector2( Input.mousePosition.x, Screen.height - Input.mousePosition.y ) ) ) {
                        sat = ( Input.mousePosition.x - colorRect.x ) / ( 138 * scale );
                        val = 1 - ( ( ( Screen.height - Input.mousePosition.y ) - colorRect.y ) / ( 138 * scale ) );
                }
#endif


                    brushColor = Color.HSVToRGB( hue, sat, val );
                    brushColorPick.SetPixel( 0, 0, brushColor );
                    brushColorPick.Apply();
                }


                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();

                GUILayout.EndArea();


                if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
#if ENABLE_INPUT_SYSTEM
                    if ( !new Rect( 256 * scale, 200 * scale, 200 * scale, 320 * scale ).Contains( new Vector2( Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y ) ) )
#else
                    if ( !new Rect( 256 * scale, 200 * scale, 200 * scale, 320 * scale ).Contains( new Vector2( Input.mousePosition.x, Screen.height - Input.mousePosition.y ) ) )
#endif
                        showColorPickerL = false;
                }

            }


            //RIGHT SIDE PANEL

            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( Screen.width - 300 * scale, 40 * scale, 300 * scale, 500 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;

                GUILayout.Box( "Fur Material Properties", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                GUILayout.Space( 40 * scale );

                if ( TargetInstances[ActiveTarget].targetXFur.MainRenderer.isFurMaterial[furMaterial] ) {

                    GUILayout.BeginHorizontal();
                    pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label( "Fur Main Tint", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                    pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                        showColorPickerR = true;
                        editColorProperty = 0;
                    }
                    var colorRect = GUILayoutUtility.GetLastRect();
                    GUI.DrawTexture( colorRect, furMainTintPick );
                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();

                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurLength = SliderField( "Length", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurLength, width:70, showNumber:true );
                    GUILayout.FlexibleSpace();
                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurThickness = SliderField( "Thickness", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurThickness, width: 70, showNumber: true );
                    GUILayout.FlexibleSpace();
                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurThicknessCurve = SliderField( "Thick. Curve", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurThicknessCurve, width: 70, showNumber: true );
                    GUILayout.FlexibleSpace();
                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurOcclusion = SliderField( "Occlusion", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurOcclusion, width: 70, showNumber: true );
                    GUILayout.FlexibleSpace();
                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurOcclusionCurve = SliderField( "Occl. Curve", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurOcclusionCurve, width: 70, showNumber: true );
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal();
                    pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label( "Fur Occlusion Tint", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                    pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                        showColorPickerR = true;
                        editColorProperty = 1;
                    }
                    colorRect = GUILayoutUtility.GetLastRect();
                    GUI.DrawTexture( colorRect, furOcclusionPick );
                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();

                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurUVTiling = FloatField( "Fur Strands Tiling", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurUVTiling );

                    GUILayout.Space( 40 * scale );

                }
                else {

                    HelpBox( "A non-XFur enabled material was selected. This material is non-editable", MessageType.Warning );

                }

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();

            }
            GUILayout.EndArea();


            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( Screen.width - 300 * scale, 540 * scale, 300 * scale, 360 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;

                GUILayout.Box( "Color Mixing", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( "Fur Tint #1", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                    showColorPickerR = true;
                    editColorProperty = 2;
                }
                var colorRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture( colorRect, furTint1Pick );
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( "Fur Tint #2", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                    showColorPickerR = true;
                    editColorProperty = 3;
                }
                colorRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture( colorRect, furTint2Pick );
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( "Fur Tint #3", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                    showColorPickerR = true;
                    editColorProperty = 4;
                }
                colorRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture( colorRect, furTint3Pick );
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( "Fur Tint #4", pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "", GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ) ) {
                    showColorPickerR = true;
                    editColorProperty = 5;
                }
                colorRect = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture( colorRect, furTint4Pick );
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();


            }
            GUILayout.EndArea();


            GUI.color = new Color( 0.75f, 0.75f, 0.75f );
            GUILayout.BeginArea( new Rect( Screen.width - 300 * scale, 900 * scale, 300 * scale, Screen.height - 900 * scale ), pidiSkin2.customStyles[0] );
            {
                GUI.color = Color.white;

                GUILayout.Box( "Wind Simulation", pidiSkin2.customStyles[0], GUILayout.Height( 50 * scale ) );

                GUILayout.BeginHorizontal(); GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                GUILayout.FlexibleSpace();

                TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].SelfWindStrength = SliderField( "Wind Strength", TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].SelfWindStrength );

                if ( XFurStudioInstance.WindZone ) {
                    XFurStudioInstance.WindZone.WindFrequency = SliderField( "Wind Frequency", XFurStudioInstance.WindZone.WindFrequency, 0, 32 );
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();


            }
            GUILayout.EndArea();

            if ( showColorPickerR ) {
                GUI.color = new Color( 0.75f, 0.75f, 0.75f );
                GUILayout.BeginArea( new Rect( Screen.width - 500 * scale, 200 * scale, 200 * scale, 320 * scale ), pidiSkin2.customStyles[0] );
                GUI.color = Color.white;
                GUI.DrawTexture( new Rect( 30 * scale, 30 * scale, 140 * scale, 140 * scale ), colorPicker );

                GUILayout.Space( 200 * scale );

                switch ( editColorProperty ) {

                    case 0:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurMainTint;
                        break;

                    case 1:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurShadowsTint;
                        break;

                    case 2:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorA;
                        break;

                    case 3:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorB;
                        break;

                    case 4:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorC;
                        break;

                    case 5:
                        sourceColor = TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorD;
                        break;

                }

                Color.RGBToHSV( sourceColor, out float hue, out float sat, out float val );

                GUILayout.BeginHorizontal();
                GUILayout.Space( 12 * scale );
                GUILayout.BeginVertical();

                hue = SliderField( "H", hue, 0, 1, 110 );
                sat = SliderField( "S", sat, 0, 1, 110 );
                val = SliderField( "V", val, 0, 1, 110 );

                colorPicker.material.SetFloat( "_Hue", hue );
                colorPicker.material.SetFloat( "_Sat", sat );
                colorPicker.material.SetFloat( "_Val", val );

                var targetColor = Color.HSVToRGB( hue, sat, val );

                var colorRect = new Rect( Screen.width - 468 * scale, 232 * scale, 138 * scale, 138 * scale );
#if ENABLE_INPUT_SYSTEM
                if ( Mouse.current.leftButton.isPressed || targetColor != sourceColor ) {
                    if ( colorRect.Contains( new Vector2( Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y ) ) ) {
                        sat = ( Mouse.current.position.ReadValue().x - colorRect.x ) / ( 138 * scale );
                        val = 1 - ( ( ( Screen.height - Mouse.current.position.ReadValue().y ) - colorRect.y ) / ( 138 * scale ) );
                    }
#else
                if ( Input.GetMouseButton( 0 ) || targetColor != sourceColor ) {
                    if ( colorRect.Contains( new Vector2( Input.mousePosition.x, Screen.height - Input.mousePosition.y ) ) ) {
                        sat = ( Input.mousePosition.x - colorRect.x ) / ( 138 * scale );
                        val = 1 - ( ( ( Screen.height - Input.mousePosition.y ) - colorRect.y ) / ( 138 * scale ) );
                    }
#endif


                    sourceColor = Color.HSVToRGB( hue, sat, val );

                    switch ( editColorProperty ) {

                        case 0:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurMainTint = sourceColor;
                            furMainTintPick.SetPixel( 0, 0, sourceColor );
                            furMainTintPick.Apply();
                            break;

                        case 1:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurShadowsTint = sourceColor;
                            furOcclusionPick.SetPixel( 0, 0, sourceColor );
                            furOcclusionPick.Apply();
                            break;

                        case 2:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorA = sourceColor;
                            furTint1Pick.SetPixel( 0, 0, sourceColor );
                            furTint1Pick.Apply();
                            break;

                        case 3:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorB = sourceColor;
                            furTint2Pick.SetPixel( 0, 0, sourceColor );
                            furTint2Pick.Apply();
                            break;

                        case 4:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorC = sourceColor;
                            furTint3Pick.SetPixel( 0, 0, sourceColor );
                            furTint3Pick.Apply();
                            break;

                        case 5:
                            TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorD = sourceColor;
                            furTint4Pick.SetPixel( 0, 0, sourceColor );
                            furTint4Pick.Apply();
                            break;

                    }
                }


                GUILayout.EndVertical();
                GUILayout.Space( 12 * scale ); GUILayout.EndHorizontal();

                GUILayout.EndArea();


                if ( Event.current != null && Event.current.type == EventType.MouseDown ) {
#if ENABLE_INPUT_SYSTEM
                    if ( !new Rect( Screen.width - 430 * scale, 200 * scale, 200 * scale, 320 * scale ).Contains( new Vector2( Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y ) ) )
#else
                    if ( !new Rect( Screen.width - 430 * scale, 200 * scale, 200 * scale, 320 * scale ).Contains( new Vector2( Input.mousePosition.x, Screen.height - Input.mousePosition.y ) ) )
#endif
                        showColorPickerR = false;
                }

            }




        }


#endif


                public void PaintEvent() {

            if ( !TargetInstances[ActiveTarget].targetXFur.MainRenderer.isFurMaterial[furMaterial] ) {
                return;
            }

            switch ( designerMode ) {
                case 0:

                    if ( symmetryMode ) {
                        XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurMask, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                    }

                    XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurMask, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                    break;

                case 1:

                    switch ( furProperty ) {
                        case 0:

                            if ( symmetryMode ) {
                                XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurLength, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                            }

                            XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurLength, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );

                            break;

                        case 1:
                            if ( symmetryMode ) {
                                XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurThickness, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                            }

                            XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurThickness, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                            break;

                        case 2:
                            if ( symmetryMode ) {
                                XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurOcclusion, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                            }

                            XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurOcclusion, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, invertBrush ? Color.black : Color.white, brushTexture );
                            break;
                    }

                    break;


                case 2:

                    //XFurStudioAPI.TestGroom( TargetInstances[ActiveTarget].targetXFur, (RenderTexture)TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1, furMaterial );

                    if ( symmetryMode ) {
                        XFurStudioAPI.Groom( TargetInstances[ActiveTarget].targetXFur, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, Vector3.Reflect( ( pointer.position - lastPos ).normalized, TargetInstances[ActiveTarget].targetXFur.transform.right ), invertBrush );
                    }

                    XFurStudioAPI.Groom( TargetInstances[ActiveTarget].targetXFur, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, ( pointer.position - lastPos ).normalized, invertBrush );

                    break;

                case 3:


                    var finalColor = new Color( 0.5f, 0.5f, 0.5f, 0.5f );

                    switch ( FurColorVariationChannel ) {
                        case 0:
                            finalColor.r = invertBrush ? 0 : 1;
                            break;
                        case 1:
                            finalColor.g = invertBrush ? 0 : 1;
                            break;
                        case 2:
                            finalColor.b = invertBrush ? 0 : 1;
                            break;
                        case 3:
                            finalColor.a = invertBrush ? 0 : 1;
                            break;
                    }

                    if ( symmetryMode ) {
                        XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurColorBlend, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, finalColor, brushTexture );
                    }

                    XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurColorBlend, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, finalColor, brushTexture );


                    break;

                case 4:

                    TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap;

                    if ( symmetryMode ) {
                        XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurColor, furMaterial, Vector3.Reflect( pointer.position, TargetInstances[ActiveTarget].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[ActiveTarget].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, brushColor, brushTexture );
                    }

                    XFurStudioAPI.Paint( TargetInstances[ActiveTarget].targetXFur, XFurStudioAPI.PaintDataMode.FurColor, furMaterial, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, brushColor, brushTexture );
                    break;

            }
        }




        public void PushChanges() {

            switch ( designerMode ) {

                case 0:
                case 1:

                    if ( !TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0 ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData0 = RenderTexture.GetTemporary( 1024, 1024, 24, RenderTextureFormat.ARGB32 );
                        XFurStudioAPI.FillTexture( TargetInstances[ActiveTarget].targetXFur, Color.white, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0, Color.clear, furMaterial );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0 = TargetInstances[ActiveTarget].FurData[furMaterial].FurData0;
                    }
                    else if ( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0 is Texture2D ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData0 = RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0.height, 24, RenderTextureFormat.ARGB32 );
                        Graphics.Blit( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0 );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData0 = TargetInstances[ActiveTarget].FurData[furMaterial].FurData0;
                    }

#if ENABLE_INPUT_SYSTEM

                    if ( Mouse.current.leftButton.wasPressedThisFrame && TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count > 0 ) {
#else

                    if ( Input.GetMouseButtonDown( 0 ) && TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count > 0 ) {
#endif
                        return;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo > -1 ) {
                        for ( int i = 0; i < Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo, 0, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count - 1 ); i++ ) {
                            RenderTexture.ReleaseTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[i] );
                        }

                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.RemoveRange( 0, TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo );
                        TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo = -1;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count < MaxUndoSteps ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0.width, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0.height ) );
                    }

                    for ( int z = TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count - 1; z > 0; z-- ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[z - 1], TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[z] );
                    }

                    Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[0] );

                    break;

                case 2:

                    if ( !TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1 ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 = RenderTexture.GetTemporary( 1024, 1024, 24, RenderTextureFormat.ARGB32 );
                        XFurStudioAPI.FillTexture( TargetInstances[ActiveTarget].targetXFur, new Color( 0.5f, 0.5f, 0.5f, 0.5f ), TargetInstances[ActiveTarget].FurData[furMaterial].FurData1, new Color( 0.5f, 0.5f, 0.5f, 0.5f ), furMaterial );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1 = TargetInstances[ActiveTarget].FurData[furMaterial].FurData1;
                    }
                    else if ( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1 is Texture2D ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 = RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1.height, 24, RenderTextureFormat.ARGB32 );
                        Graphics.Blit( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1 = TargetInstances[ActiveTarget].FurData[furMaterial].FurData1;
                    }

#if ENABLE_INPUT_SYSTEM

                    if ( Mouse.current.leftButton.wasPressedThisFrame && TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count > 0 ) {
#else

                    if ( Input.GetMouseButtonDown( 0 ) && TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count > 0 ) {
#endif
                        return;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo > -1 ) {
                        for ( int i = 0; i < Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo, 0, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count - 1 ); i++ ) {
                            RenderTexture.ReleaseTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[i] );
                        }

                        TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.RemoveRange( 0, TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo );
                        TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo = -1;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count < MaxUndoSteps ) {
                        if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 )
                            TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1.width, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1.height ) );
                        else
                            TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurData1.height ) );
                    }

                    for ( int z = TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count - 1; z > 0; z-- ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[z - 1], TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[z] );
                    }

                    Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[0] );



                    break;

                case 3:

                    if ( !TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation = RenderTexture.GetTemporary( 1024, 1024, 24, RenderTextureFormat.ARGB32 );
                        XFurStudioAPI.FillTexture( TargetInstances[ActiveTarget].targetXFur, Color.clear, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation, Color.clear, furMaterial );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation;
                    }
                    else if ( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation is Texture2D ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation = RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation.height, 24, RenderTextureFormat.ARGB32 );
                        Graphics.Blit( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorVariation = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation;
                    }

#if ENABLE_INPUT_SYSTEM

                    if ( Mouse.current.leftButton.wasPressedThisFrame && TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count > 0 ) {

#else

                    if ( Input.GetMouseButtonDown( 0 ) && TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count > 0 ) {

#endif
                        return;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo > -1 ) {
                        for ( int i = 0; i < Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo, 0, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count - 1 ); i++ ) {
                            RenderTexture.ReleaseTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[i] );
                        }

                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.RemoveRange( 0, TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo );
                        TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo = -1;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count < MaxUndoSteps ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation.width, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation.height ) );
                    }

                    for ( int z = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count - 1; z > 0; z-- ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[z - 1], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[z] );
                    }

                    Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[0] );

                    break;

                    /*
                case 4:

                    if ( !TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap = RenderTexture.GetTemporary( 1024, 1024, 24, RenderTextureFormat.ARGB32 );
                        XFurStudioAPI.FillTexture( TargetInstances[ActiveTarget].targetXFur, Color.clear, TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap, Color.clear, furMaterial );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap = TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap;
                    }
                    else if ( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap is Texture2D ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap = RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap.height, 24, RenderTextureFormat.ARGB32 );
                        Graphics.Blit( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap, TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurBlendSplatmap = TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap;
                    }


                    if ( Input.GetMouseButtonDown( 0 ) && TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count > 0 ) {
                        return;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo > -1 ) {
                        for ( int i = 0; i < Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo, 0, TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count - 1 ); i++ ) {
                            RenderTexture.ReleaseTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[i] );
                        }

                        TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.RemoveRange( 0, TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo );
                        TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo = -1;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count < MaxUndoSteps ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap.width, TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap.height ) );
                    }

                    for ( int z = TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count - 1; z > 0; z-- ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[z - 1], TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[z] );
                    }

                    Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap, TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[0] );

                    break;

                    */
                     
                case 4:

                    if ( !TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap = RenderTexture.GetTemporary( 1024, 1024, 24, RenderTextureFormat.ARGB32 );
                        XFurStudioAPI.FillTexture( TargetInstances[ActiveTarget].targetXFur, Color.white, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap, Color.clear, furMaterial );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap;
                    }
                    else if ( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap is Texture2D ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap = RenderTexture.GetTemporary( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap.width, TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap.height, 24, RenderTextureFormat.ARGB32 );
                        Graphics.Blit( TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap );
                        TargetInstances[ActiveTarget].targetXFur.FurDataProfiles[furMaterial].FurColorMap = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap;
                    }

#if ENABLE_INPUT_SYSTEM
                    if ( Mouse.current.leftButton.wasPressedThisFrame && TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count > 0 ) {

#else
                    if ( Input.GetMouseButtonDown( 0 ) && TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count > 0 ) {

#endif
                        return;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo > -1 ) {
                        for ( int i = 0; i < Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo, 0, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count - 1 ); i++ ) {
                            RenderTexture.ReleaseTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[i] );
                        }

                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.RemoveRange( 0, TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo );
                        TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo = -1;
                    }

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count < MaxUndoSteps ) {
                        TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Add( RenderTexture.GetTemporary( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap.width, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap.height ) );
                    }

                    for ( int z = TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count - 1; z > 0; z-- ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[z - 1], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[z] );
                    }

                    Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[0] );

                    break;


            }


        }


        public void UndoChanges() {

            switch ( designerMode ) {
                case 0:
                case 1:

                    TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo++;

                    TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo], TargetInstances[ActiveTarget].FurData[furMaterial].FurData0 );
                    }

                    break;

                case 2:

                    TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo++;

                    TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo], TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 );
                    }

                    break;


                case 3:
                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo++;

                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation );
                    }
                    break;
                    /*
                case 4:
                    TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo++;

                    TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap );
                    }
                    break;
                    */
                case 4:
                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo++;

                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap );
                    }
                    break;

            }
        }


        public void RedoChanges() {

            switch ( designerMode ) {
                case 0:
                case 1:
                    TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo--;

                    TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData0UndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].data0Undo], TargetInstances[ActiveTarget].FurData[furMaterial].FurData0 );
                    }

                    break;

                case 2:
                    TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo--;

                    TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurData1UndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].data1Undo], TargetInstances[ActiveTarget].FurData[furMaterial].FurData1 );
                    }

                    break;

                case 3:
                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo--;

                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariationUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].colorMixUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorVariation );
                    }
                    break;
                    /*
                case 4:
                    TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo--;

                    TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurProfileMixUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].dataProfileMixUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurBlendSplatmap );
                    }
                    break;
                    */
                case 4:
                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo--;

                    TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo = Mathf.Clamp( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo, -1, TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps.Count - 1 );

                    if ( TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo > -1 ) {
                        Graphics.Blit( TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMapUndoSteps[TargetInstances[ActiveTarget].FurData[furMaterial].colorMapUndo], TargetInstances[ActiveTarget].FurData[furMaterial].FurColorMap );
                    }
                    break;

            }

        }



        public void Update() {

            Camera cam = GetComponent<Camera>();
            bool canPaint = true;


            if ( TargetInstances.Count < 1 || TargetInstances[ActiveTarget].targetXFur == null ) {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if ( !Keyboard.current.shiftKey.isPressed && !Keyboard.current.ctrlKey.isPressed ) {
                if ( Physics.Raycast( cam.ScreenPointToRay( Mouse.current.position.ReadValue() ), out RaycastHit hit, meshBounds.size.magnitude * 10 ) ) {
                    canPaint = Mouse.current.position.ReadValue().x > 256 * scale && Mouse.current.position.ReadValue().x < Screen.width - 256 * scale && !showColorPickerL && !showColorPickerR;

#else
            if ( !Input.GetKey( KeyCode.LeftShift ) && !Input.GetKey( KeyCode.LeftControl ) ) {
                if ( Physics.Raycast( cam.ScreenPointToRay( Input.mousePosition ), out RaycastHit hit, meshBounds.size.magnitude * 10 ) ) {
                    canPaint = Input.mousePosition.x > 256 * scale && Input.mousePosition.x < Screen.width - 256 * scale && !showColorPickerL && !showColorPickerR;


#endif
                    pointer.position = hit.point;
                    pointer.forward = hit.normal;

                    
                    if ( buffPos == 0 ) {
                        buffPos = 1;
                    }
                    else {
                        buffPos = 0;
                        lastPos = hit.point;
                    }

#if ENABLE_INPUT_SYSTEM
                    if ( Mouse.current.rightButton.wasPressedThisFrame ) {
#else
                    if ( Input.GetMouseButtonDown( 1 ) ) {
#endif
                        var xfur = hit.transform.root.GetComponentInChildren<XFurStudioInstance>();
                        if ( xfur ) {
                            for ( int i = 0; i < TargetInstances.Count; i++ ) {
                                if ( TargetInstances[i].targetXFur == xfur ) {
                                    if ( ActiveTarget != i ) {
                                        ActiveTarget = i;
                                        furMaterialNames.Clear();
                                        for ( int m = 0; m < xfur.FurDataProfiles.Length; m++ ) {
                                            furMaterialNames.Add( xfur.MainRenderer.materials[m].name );
                                        }
                                        furMaterial = 0;
                                        UpdateMaterialValues();
                                        distance = TargetInstances[ActiveTarget].targetMesh.bounds.size.magnitude;
                                    }
                                    break;
                                }
                            }
                        }
                    }

                }
                else {
                    canPaint = false;
                }
            }

#if ENABLE_INPUT_SYSTEM
            if ( Keyboard.current.xKey.wasPressedThisFrame ) {
                invertBrush = !invertBrush;
            }

            if ( Keyboard.current.sKey.wasPressedThisFrame ) {
                symmetryMode = !symmetryMode;
            }

            if ( Keyboard.current.shiftKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame ) {
                UndoChanges();
            }


            if ( Keyboard.current.shiftKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame ) {
                RedoChanges();
            }
#else
            if ( Input.GetKeyDown( KeyCode.X ) ) {
                invertBrush = !invertBrush;
            }

            if ( Input.GetKeyDown( KeyCode.S ) ) {
                symmetryMode = !symmetryMode;
            }

            if ( Input.GetKey( KeyCode.LeftShift ) && Input.GetKeyDown( KeyCode.Z ) ) {
                UndoChanges();
            }


            if ( Input.GetKey( KeyCode.LeftShift ) && Input.GetKeyDown( KeyCode.Y ) ) {
                RedoChanges();
            }

#endif

            meshBounds = TargetInstances[ActiveTarget].targetMesh.bounds;


#if ENABLE_INPUT_SYSTEM
            if ( !Keyboard.current.shiftKey.isPressed && !Keyboard.current.ctrlKey.isPressed ) {
                distance = Mathf.Clamp( distance - Mouse.current.scroll.ReadValue().y * Time.deltaTime * 0.05f, meshBounds.extents.magnitude * 0.25f, meshBounds.extents.magnitude * 3f );
            }
#else
            if ( !Input.GetKey( KeyCode.LeftShift ) && !Input.GetKey( KeyCode.LeftControl ) ) {
                distance -= Input.GetAxis( "Mouse ScrollWheel" ) * 2.5f * meshBounds.extents.magnitude;
                if ( Input.GetKey( KeyCode.KeypadPlus ) ) {
                    distance -= 3.5f * Time.deltaTime *  meshBounds.extents.magnitude; 
                }
                if ( Input.GetKey( KeyCode.KeypadMinus ) ) {
                    distance += 3.5f * Time.deltaTime * meshBounds.extents.magnitude;
                }

                distance = Mathf.Clamp( distance, meshBounds.extents.magnitude * 0.25f, meshBounds.extents.magnitude * 3f );
            }
#endif

            var buffSize = brushSize;
            var buffHardness = brushHardness;
            var buffOpacity = brushOpacity;

#if ENABLE_INPUT_SYSTEM
            if ( Keyboard.current.shiftKey.isPressed ) {

                if ( Mouse.current.leftButton.isPressed ) {
                    if ( Mathf.Abs( Mouse.current.delta.ReadValue().x * Time.deltaTime ) > Mathf.Abs( Mouse.current.delta.ReadValue().y * Time.deltaTime ) ) {
                        brushSize = Mathf.Clamp( brushSize + Mouse.current.delta.ReadValue().x * Time.deltaTime * 0.05f, 0.001f, 2f );
                    }
                    else {
                        brushHardness = Mathf.Clamp( brushHardness + Mouse.current.delta.ReadValue().y * Time.deltaTime * 0.05f, 0f, 1f );
                    }
                }

                if ( Mouse.current.rightButton.isPressed ) {
                    brushOpacity = Mathf.Clamp( brushOpacity + Mouse.current.delta.ReadValue().x * Time.deltaTime * 0.05f, 0, 1f );
                }

            }
#else
            if ( Input.GetKey( KeyCode.LeftShift ) ) {

                if ( Input.GetMouseButton( 0 ) ) {
                    if ( Mathf.Abs( Input.GetAxis( "Mouse X" ) ) > Mathf.Abs( Input.GetAxis( "Mouse Y" ) ) ) {
                        brushSize = Mathf.Clamp( brushSize + Input.GetAxis( "Mouse X" ) * 0.05f, 0.001f, 2f );
                    }
                    else {
                        brushHardness = Mathf.Clamp( brushHardness + Input.GetAxis( "Mouse Y" ) * 0.05f, 0f, 1f );
                    }
                }

                if ( Input.GetMouseButton( 1 ) ) {
                    brushOpacity = Mathf.Clamp( brushOpacity + Input.GetAxis( "Mouse X" ) * 0.05f, 0, 1f );
                }

            }
#endif
            if ( brushSize != buffSize || brushOpacity != buffOpacity || brushHardness != buffHardness ) {
                GenerateBrushPreview( brushPreview );
            }

            var rotation = Quaternion.Euler( orbitY, orbitX, 0 );

            var position = rotation * new Vector3( 0.0f, 0.0f, -distance ) + TargetInstances[ActiveTarget].targetXFur.transform.TransformPoint( meshBounds.center ) + pivotOffset;

            transform.rotation = rotation;
            transform.position = position;

#if ENABLE_INPUT_SYSTEM
            if ( !Keyboard.current.shiftKey.isPressed && Mouse.current.middleButton.isPressed ) {
                orbitX += Mouse.current.delta.ReadValue().x * Time.deltaTime * 20f;
                orbitY -= Mouse.current.delta.ReadValue().y * Time.deltaTime * 25;

                orbitY = ClampAngle( orbitY, -80, 80 );
            }
            else if ( Mouse.current.middleButton.isPressed ) {
                pivotOffset -= cam.transform.rotation * new Vector3( Mouse.current.delta.ReadValue().x * Time.deltaTime * 0.15f * distance, Mouse.current.delta.ReadValue().y * Time.deltaTime * 0.15f * distance, 0 );
            }



            if ( ( Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.leftButton.wasPressedThisFrame ) && canPaint && !Keyboard.current.shiftKey.isPressed && !Keyboard.current.ctrlKey.isPressed ) {
                PushChanges();
                return;
            }
#else

            if ( !Input.GetKey( KeyCode.LeftShift ) && Input.GetMouseButton( 2 ) ) {
                orbitX += Input.GetAxis( "Mouse X" ) * 350f * Time.deltaTime;
                orbitY -= Input.GetAxis( "Mouse Y" ) * 300 * Time.deltaTime;

                orbitY = ClampAngle( orbitY, -80, 80 );
            }
            else if ( Input.GetMouseButton( 2 ) ) {
                pivotOffset -= cam.transform.rotation * new Vector3( Input.GetAxis( "Mouse X" ) * 0.15f * distance, Input.GetAxis( "Mouse Y" ) * 0.15f * distance, 0 );
            }



            if ( ( Input.GetMouseButtonUp( 0 ) || Input.GetMouseButtonDown( 0 ) ) && canPaint && !Input.GetKey( KeyCode.LeftShift ) && !Input.GetKey( KeyCode.LeftControl ) ) {
                PushChanges();
                return;
            }

#endif


            for ( int i = 0; i < TargetInstances.Count; i++ ) {

                if ( TargetInstances[i].targetXFur.CurrentFurRenderer.renderer.GetComponent<SkinnedMeshRenderer>() ) {
                    if ( TargetInstances[i].targetXFur.CurrentMesh ) {
                        targetMesh = TargetInstances[i].targetXFur.CurrentMesh;
                    }
                    else {
                        targetMesh = new Mesh();
                        TargetInstances[i].targetXFur.CurrentFurRenderer.renderer.GetComponent<SkinnedMeshRenderer>().BakeMesh( targetMesh );
                    }
                }
                else {
                    targetMesh = TargetInstances[i].targetXFur.CurrentMesh;
                }


                if ( !TargetInstances[i].colliderObject ) {
                    TargetInstances[i].colliderObject = new GameObject(TargetInstances[i].targetXFur.name+"_COLLIDER", typeof(MeshCollider)).GetComponent<MeshCollider>();
                }

                TargetInstances[i].colliderObject.transform.position = TargetInstances[i].targetXFur.CurrentFurRenderer.renderer.transform.position;
                TargetInstances[i].colliderObject.transform.rotation = TargetInstances[i].targetXFur.CurrentFurRenderer.renderer.transform.rotation;

                TargetInstances[i].colliderObject.sharedMesh = targetMesh;


                for ( int f = 0; f < TargetInstances[i].FurData.Count; f++ ) {

                    if ( TargetInstances[i].targetXFur.MainRenderer.isFurMaterial[f] ) {

                        if ( canPaint ) {

                            XFurStudioAPI.DisplayBrush( TargetInstances[i].targetXFur, f, pointer.position, pointer.forward, brushSize, brushOpacity, brushHardness, brushTexture, TargetInstances[i].FurData[f].FurColorMap, TargetInstances[i].FurData[f].brushPointer );

                            if ( symmetryMode ) {
                                XFurStudioAPI.DisplayBrush( TargetInstances[i].targetXFur, f, Vector3.Reflect( pointer.position, TargetInstances[i].targetXFur.transform.right ), Vector3.Reflect( pointer.forward, TargetInstances[i].targetXFur.transform.right ), brushSize, brushOpacity, brushHardness, brushTexture, TargetInstances[i].FurData[f].brushPointer, TargetInstances[i].FurData[f].brushPointer );
                            }

                            TargetInstances[i].targetXFur.FurDataProfiles[f].FurColorMap = TargetInstances[i].FurData[f].brushPointer;
                        }
                        else {
                            TargetInstances[i].targetXFur.FurDataProfiles[f].FurColorMap = TargetInstances[i].FurData[f].FurColorMap;
                        }

#if ENABLE_INPUT_SYSTEM
                        if ( Mouse.current.leftButton.isPressed && canPaint && !Keyboard.current.shiftKey.isPressed ) {
#else
                        if ( Input.GetMouseButton( 0 ) && canPaint && !Input.GetKey( KeyCode.LeftShift ) ) {
#endif
                            PaintEvent();

                        }
                    }
                }
            }

        }

        public float ClampAngle( float angle, float min, float max ) {
            if ( angle < -360F )
                angle += 360F;
            if ( angle > 360F )
                angle -= 360F;
            return Mathf.Clamp( angle, min, max );
        }




#if UNITY_EDITOR
        public void ExportTextures() {


            var path = EditorUtility.SaveFolderPanel( "Export Textures", "Assets/", "XFur Data Maps" );
            var temporaryOutput = RenderTexture.GetTemporary( 512 * Mathf.RoundToInt ( Mathf.Pow( 2, resolution ) ), 512 * Mathf.RoundToInt( Mathf.Pow( 2, resolution ) ), 24, RenderTextureFormat.ARGB32 );
            var active = RenderTexture.active;
            RenderTexture.active = temporaryOutput;
            var outputTex = new Texture2D( RenderTexture.active.width, RenderTexture.active.height, TextureFormat.ARGB32, true );


            foreach ( XFurDesignerInstance instance in TargetInstances ) {
                for ( int i = 0; i < instance.targetXFur.FurDataProfiles.Length; i++ ) {

                    if ( instance.FurData[i].isFur ) {

                        instance.targetXFur.GetFurData( i, out XFurTemplate tempProfile );

                        if ( !Directory.Exists( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) ) ) {
                            Directory.CreateDirectory( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) );
                        }


                        var relativePath = path.Replace( Application.dataPath, "Assets/" );

                        if (relativePath != "Assets/" ) {
                            relativePath += "/";
                        }

                        relativePath += instance.targetXFur.name + "/";

                        if ( instance.targetXFur.FurDataProfiles[i].FurData0 && instance.targetXFur.FurDataProfiles[i].FurData0 is RenderTexture ) {
                            Graphics.Blit( instance.targetXFur.FurDataProfiles[i].FurData0, temporaryOutput );
                            outputTex.ReadPixels( new Rect( 0, 0, outputTex.width, outputTex.height ), 0, 0 );
                            outputTex.Apply();

                            var pngData = outputTex.EncodeToPNG();

                            if ( pngData != null ) {
                                File.WriteAllBytes( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurData0.png", pngData );
                                AssetDatabase.Refresh();
                                tempProfile.FurData0 = AssetDatabase.LoadAssetAtPath<Texture2D>( relativePath + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurData0.png" );
                            }
                        }

                        if ( instance.targetXFur.FurDataProfiles[i].FurData1 && instance.targetXFur.FurDataProfiles[i].FurData1 is RenderTexture ) {
                            Graphics.Blit( instance.targetXFur.FurDataProfiles[i].FurData1, temporaryOutput );
                            outputTex.ReadPixels( new Rect( 0, 0, outputTex.width, outputTex.height ), 0, 0 );
                            outputTex.Apply();

                            var pngData = outputTex.EncodeToPNG();

                            if ( pngData != null ) {
                                File.WriteAllBytes( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurData1.png", pngData );
                                AssetDatabase.Refresh();
                                tempProfile.FurData1 = AssetDatabase.LoadAssetAtPath<Texture2D>( relativePath + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurData1.png" );
                            }
                        }

                        if ( instance.targetXFur.FurDataProfiles[i].FurColorMap && instance.targetXFur.FurDataProfiles[i].FurColorMap is RenderTexture ) {
                            Graphics.Blit( instance.targetXFur.FurDataProfiles[i].FurColorMap, temporaryOutput );
                            outputTex.ReadPixels( new Rect( 0, 0, outputTex.width, outputTex.height ), 0, 0 );
                            outputTex.Apply();

                            var pngData = outputTex.EncodeToPNG();

                            if ( pngData != null ) {
                                File.WriteAllBytes( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurColorMap.png", pngData );
                                AssetDatabase.Refresh();
                                tempProfile.FurColorMap = AssetDatabase.LoadAssetAtPath<Texture2D>( relativePath + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurColorMap.png" );
                            }
                        }

                        if ( instance.targetXFur.FurDataProfiles[i].FurColorVariation && instance.targetXFur.FurDataProfiles[i].FurColorVariation is RenderTexture ) {
                            Graphics.Blit( instance.targetXFur.FurDataProfiles[i].FurColorVariation, temporaryOutput );
                            outputTex.ReadPixels( new Rect( 0, 0, outputTex.width, outputTex.height ), 0, 0 );
                            outputTex.Apply();

                            var pngData = outputTex.EncodeToPNG();

                            if ( pngData != null ) {
                                File.WriteAllBytes( path + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "/" + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurColorVariationMap.png", pngData );
                                AssetDatabase.Refresh();
                                tempProfile.FurColorVariation = AssetDatabase.LoadAssetAtPath<Texture2D>( relativePath + instance.targetXFur.name.Replace( " ", "_" ) + "_" + instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_FurColorVariationMap.png" );
                            }
                        }

                        if ( exportFurProfiles ) {
                            var asset = ScriptableObject.CreateInstance<XFurStudioFurProfile>();
                            asset.FurTemplate = tempProfile;
                            AssetDatabase.CreateAsset( asset, relativePath + instance.targetXFur.name.Replace( " ", "_" )+"_"+instance.targetXFur.MainRenderer.materials[i].name.Replace( " ", "_" ) + "_Profile.asset" );
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            RenderTexture.active = active;

        }
#endif



        private void OnDisable() {

            if ( pidiSkin2 ) {
                if ( pidiSkin2.name == "COPYSKIN" ) {
                    DestroyImmediate( pidiSkin2 );
                }
            }

            if ( brushPreview ) {
                DestroyImmediate( brushPreview );
            }


            if ( brushColorPick ) {
                DestroyImmediate( brushColorPick );
            }

            if ( furTint1Pick ) {
                DestroyImmediate( furTint1Pick );
            }

            if ( furTint2Pick ) {
                DestroyImmediate( furTint2Pick );
            }

            if ( furTint3Pick ) {
                DestroyImmediate( furTint3Pick );
            }

            if ( furTint4Pick ) {
                DestroyImmediate( furTint4Pick );
            }

            if ( furOcclusionPick ) {
                DestroyImmediate( furOcclusionPick );
            }

            if ( furMainTintPick ) {
                DestroyImmediate( furMainTintPick );
            }

            for ( int i = 0; i < TargetInstances.Count; i++ ) {
                for ( int x = 0; x < TargetInstances[i].FurData.Count; x++ ) {

                    if ( TargetInstances[i].FurData[x].originalAlbedo ) {
                        RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].originalAlbedo );
                    }

                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].brushPointer );
                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].FurColorMap );
                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].FurColorVariation );
                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].FurData0 );
                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].FurData1 );
                    RenderTexture.ReleaseTemporary( TargetInstances[i].FurData[x].FurBlendSplatmap );

                    foreach ( RenderTexture t in TargetInstances[i].FurData[x].FurColorMapUndoSteps ) {
                        RenderTexture.ReleaseTemporary( t );
                    }

                    foreach ( RenderTexture t in TargetInstances[i].FurData[x].FurColorVariationUndoSteps ) {
                        RenderTexture.ReleaseTemporary( t );
                    }

                    foreach ( RenderTexture t in TargetInstances[i].FurData[x].FurData0UndoSteps ) {
                        RenderTexture.ReleaseTemporary( t );
                    }

                    foreach ( RenderTexture t in TargetInstances[i].FurData[x].FurData1UndoSteps ) {
                        RenderTexture.ReleaseTemporary( t );
                    }

                    foreach ( RenderTexture t in TargetInstances[i].FurData[x].FurProfileMixUndoSteps ) {
                        RenderTexture.ReleaseTemporary( t );
                    }

                }
            }

        }


#if UNITY_EDITOR

#region PIDI 2020 EDITOR


        public void HelpBox( string message, MessageType messageType ) {
            GUILayout.Space( 8 * scale );
            GUILayout.BeginHorizontal(); GUILayout.Space( 8 * scale );
            GUILayout.BeginVertical( pidiSkin2.customStyles[5] );

            GUILayout.Space( 4 * scale );
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();

            var mType = "INFO";

            switch ( messageType ) {
                case MessageType.Error:
                    mType = "ERROR";
                    break;

                case MessageType.Warning:
                    mType = "WARNING";
                    break;
            }

            var tStyle = new GUIStyle();
            tStyle.fontSize = Mathf.RoundToInt( 16 * scale );
            tStyle.fontStyle = FontStyle.Bold;
            tStyle.normal.textColor = Color.black;

            GUILayout.Label( mType, tStyle );

            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space( 4 * scale );

            GUILayout.BeginHorizontal(); GUILayout.Space( 8 * scale ); GUILayout.BeginVertical();
            tStyle.fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
            tStyle.fontStyle = FontStyle.Normal;
            tStyle.wordWrap = true;
            GUILayout.TextArea( message, tStyle );

            GUILayout.Space( 8 * scale );
            GUILayout.EndVertical(); GUILayout.Space( 8 * scale ); GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space( 8 * scale ); GUILayout.EndHorizontal();
            GUILayout.Space( 8 * scale );
        }


        /// <summary>
        /// Draws a standard object field in the PIDI 2020 style
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="label"></param>
        /// <param name="inputObject"></param>
        /// <param name="allowSceneObjects"></param>
        /// <returns></returns>
        public T ObjectField<T>( GUIContent label, T inputObject, bool allowSceneObjects = true ) where T : UnityEngine.Object {

            GUILayout.Space( 4 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label );
            GUI.color = Color.gray;
            inputObject = (T)EditorGUILayout.ObjectField( inputObject, typeof( T ), allowSceneObjects );
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space( 4 );
            return inputObject;
        }


        /// <summary>
        /// Draws a centered button in the standard PIDI 2020 editor style
        /// </summary>
        /// <param name="label"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public bool CenteredButton( string label, params GUILayoutOption[] options ) {
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            var tempBool = GUILayout.Button( label, pidiSkin2.button, options );
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            return tempBool;
        }


        /// <summary>
        /// Draws a label centered in the Editor window
        /// </summary>
        /// <param name="label"></param>
        public void CenteredLabel( string label ) {

            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            GUILayout.Label( label, pidiSkin2.label );
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();

        }


        /// <summary>
        /// Custom integer field following the PIDI 2020 editor skin
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        public int IntField( string label, int currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Height( 20 * scale ) );
            pidiSkin2.label.alignment = TextAnchor.UpperLeft;
            var tempSize = pidiSkin2.customStyles[4].fontSize;
            pidiSkin2.customStyles[4].fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
            int.TryParse( GUILayout.TextField( "" + currentValue, pidiSkin2.customStyles[4], GUILayout.Height( 20 * scale ), GUILayout.Width( 80 * scale ) ), out currentValue );
            pidiSkin2.customStyles[4].fontSize = tempSize;
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;
        }

        /// <summary>
        /// Custom float field following the PIDI 2020 editor skin
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        public float FloatField( string label, float currentValue ) {

            pidiSkin2.customStyles[4].margin = new RectOffset( 0, 0, 0, 0 );
            pidiSkin2.customStyles[4].padding = new RectOffset( 0, 0, 0, 0 );

            GUILayout.BeginHorizontal();
            pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Height( 30 * scale ), GUILayout.MaxWidth( 120 * scale ) );
            pidiSkin2.label.alignment = TextAnchor.UpperLeft;
            GUILayout.FlexibleSpace();
            var tempSize = pidiSkin2.customStyles[4].fontSize;
            pidiSkin2.customStyles[4].fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
            float.TryParse( GUILayout.TextField( currentValue.ToString( "0.00" ), pidiSkin2.customStyles[4], GUILayout.Height( 20 * scale ), GUILayout.MaxWidth( 60 * scale ) ), out currentValue );
            pidiSkin2.customStyles[4].fontSize = tempSize;
            GUILayout.EndHorizontal();

            return currentValue;
        }


        /// <summary>
        /// Custom slider using the PIDI 2020 editor skin and adding a custom suffix to the float display
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <param name="minSlider"></param>
        /// <param name="maxSlider"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public float SliderField( string label, float currentValue, float minSlider = 0.0f, float maxSlider = 1.0f, float width = 80, bool showNumber = false, bool extendedNumber = false ) {

            GUILayout.BeginHorizontal();
            pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Height( 30 * scale ) );
            pidiSkin2.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.gray;
            currentValue = GUILayout.HorizontalSlider( currentValue, minSlider, maxSlider, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.Height( 20 * scale ), GUILayout.Width( width * scale ) ); ;
            GUI.color = Color.white;
            if ( showNumber ) {
                GUILayout.Space( 8 );
                var tempSize = pidiSkin2.customStyles[4].fontSize;
                pidiSkin2.customStyles[4].fontSize = Mathf.Clamp( Mathf.RoundToInt( 12 * scale ), 12, 24 );
                if ( extendedNumber ) {
                    pidiSkin2.customStyles[4].fontSize = Mathf.Clamp( Mathf.RoundToInt( 11 * scale ), 11, 20 );
                    float.TryParse( GUILayout.TextField( currentValue.ToString( "0.000" ), pidiSkin2.customStyles[4], GUILayout.Height( 20 * scale ), GUILayout.MaxWidth( 60 * scale ) ), out currentValue );
                }
                else {
                    float.TryParse( GUILayout.TextField( currentValue.ToString( "0.00" ), pidiSkin2.customStyles[4], GUILayout.Height( 20 * scale ), GUILayout.MaxWidth( 50 * scale ) ), out currentValue );
                }
                pidiSkin2.customStyles[4].fontSize = tempSize;
            }
            GUILayout.EndHorizontal();

            return currentValue;
        }


        /// <summary>
        /// Custom slider using the PIDI 2020 editor skin and adding a custom suffix to the float display
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <param name="minSlider"></param>
        /// <param name="maxSlider"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public int IntSliderField( GUIContent label, int currentValue, int minSlider = 0, int maxSlider = 1 ) {

            GUILayout.Space( 4 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label );
            GUI.color = Color.gray;
            currentValue = (int)GUILayout.HorizontalSlider( currentValue, minSlider, maxSlider, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb );
            GUI.color = Color.white;
            GUILayout.Space( 12 );
            currentValue = Mathf.Clamp( EditorGUILayout.IntField( int.Parse( currentValue.ToString( "n0" ) ), pidiSkin2.customStyles[4], GUILayout.MaxWidth( 40 * scale ) ), minSlider, maxSlider );
            GUILayout.EndHorizontal();
            GUILayout.Space( 4 );

            return currentValue;
        }



        /// <summary>
        /// Draw a custom toggle that instead of using a check box uses an Enable/Disable drop down menu
        /// </summary>
        /// <param name="label"></param>
        /// <param name="toggleValue"></param>
        /// <returns></returns>
        public bool Toggle( string label, bool toggleValue, bool trueFalseToggle = false, params GUILayoutOption[] options ) {

            GUILayout.BeginHorizontal();
            if ( label != null ) {
                pidiSkin2.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label( label, pidiSkin2.label, GUILayout.Height( 30 * scale ) );
                pidiSkin2.label.alignment = TextAnchor.UpperLeft;
                GUILayout.FlexibleSpace();
                toggleValue = GUILayout.Toggle( toggleValue, "", GUILayout.Height( 30 * scale ) );
            }
            else {
                toggleValue = GUILayout.Toggle( toggleValue, "", GUILayout.Height( 30 * scale ) );
            }
            GUILayout.EndHorizontal();
            return toggleValue;
        }


#endregion




#endif




    }


#endif



}
