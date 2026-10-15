

namespace XFurStudio2 {

#if UNITY_EDITOR
    using UnityEditor;
#endif

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    [System.Serializable]
    public class XFurStudioModule {

#if UNITY_EDITOR
        public GUISkin pidiSkin2;
#endif
        public bool enabled { get { return isEnabled; } }

        [SerializeField] protected bool isEnabled;

        public enum ModuleQuality:int { VeryLow, Low, Normal, High }

        public string moduleName;

        /// <summary> The development status of this module : 0 = EXPERIMENTAL, 1 = PREVIEW, 2 = BETA, 3 = STABLE </summary>
        public int moduleStatus;

        public bool experimentalFeatures;

        public string version = "2.0";

        public bool hasMobileMode;

        public bool hasSRPMode;

        public bool criticalError;

        public string name = "XFur Module";

        [SerializeField] protected XFurStudioInstance xfurInstance;

        public XFurStudioInstance Owner { get { return xfurInstance; } }

        public string ModuleStatus { get { if ( criticalError ) return "Critical Error"; else switch ( moduleStatus-(experimentalFeatures?1:0) ) { default:return "Experimental"; case 0:return "Experimental"; case 1:return "Preview"; case 2:return "Beta"; case 3:return "Stable"; } } }


#if UNITY_EDITOR
        public bool executeInEditor;

        public virtual void UpdateModule() {

        }

        protected bool[] modFolds = new bool[32];

        public virtual void ModuleUI() {
            if ( !pidiSkin2 ) {
                if ( AssetDatabase.FindAssets( "l: XFurStudio2UI" ).Length > 0 ) {
                    pidiSkin2 = (GUISkin)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets( "l: XFurStudio2UI" )[0] ), typeof( GUISkin ) );
                }
            }
        }


#endif


        public static implicit operator bool( XFurStudioModule other ) {
            return other != null;
        }

        public virtual void Setup(XFurStudioInstance xfurOwner, bool update = false ) {

            xfurInstance = xfurOwner;

        }


        public virtual void Load() {

        }

        public virtual void MainLoop() {

        }


        public virtual void MainRenderLoop(  MaterialPropertyBlock block, int furProfileIndex ) {

        }

        public virtual void Unload() {

        }

        
        public virtual void UnloadResources() {

        }


        /// <summary>
        /// Enables this module and loads its resources.
        /// </summary>
        public virtual void Enable() {
            if ( !isEnabled ) {
                isEnabled = true;
                Load();
            }
        }

        /// <summary>
        /// Disables this module and unloads its resources
        /// </summary>
        public virtual void Disable() {
            if ( isEnabled ) {
                isEnabled = false;
                Unload();
                UnloadResources();
            }
        }


#if UNITY_EDITOR


#region PIDI 2020 EDITOR


        private void LoadUIResources() {
            if ( !pidiSkin2 ) {
                if ( AssetDatabase.FindAssets( "l: XFurStudio2UI" ).Length > 0 ) {
                    pidiSkin2 = (GUISkin)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets( "l: XFurStudio2UI" )[0] ), typeof( GUISkin ) );
                }
            }
        }


        public void XFurModuleStatus() {
            LoadUIResources();
            GUILayout.BeginHorizontal();
            GUILayout.Label( moduleName + ", v" + version, pidiSkin2.label, GUILayout.Width( 140 ) );
            GUILayout.Space( 64 );
            GUILayout.Label( " Status : " + ModuleStatus, pidiSkin2.label );
            GUILayout.FlexibleSpace();
            isEnabled = EnableDisableToggle( null, isEnabled, false, GUILayout.MaxWidth( EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 200 ) ) && !criticalError;
            GUILayout.EndHorizontal();
        }


        public void HelpBox( string message, MessageType messageType ) {
            GUILayout.Space( 8 );
            GUILayout.BeginHorizontal(); GUILayout.Space( 8 );
            GUILayout.BeginVertical( pidiSkin2.customStyles[5] );

            GUILayout.Space( 4 );
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
            tStyle.fontSize = 11;
            tStyle.fontStyle = FontStyle.Bold;
            tStyle.normal.textColor = Color.black;

            GUILayout.Label( mType, tStyle );

            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space( 4 );

            GUILayout.BeginHorizontal(); GUILayout.Space( 8 ); GUILayout.BeginVertical();
            tStyle.fontSize = 9;
            tStyle.fontStyle = FontStyle.Normal;
            tStyle.wordWrap = true;
            GUILayout.TextArea( message, tStyle );

            GUILayout.Space( 8 );
            GUILayout.EndVertical(); GUILayout.Space( 8 ); GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space( 8 ); GUILayout.EndHorizontal();
            GUILayout.Space( 8 );
        }


        public Color ColorField( GUIContent label, Color currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue = EditorGUILayout.ColorField( currentValue );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;

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
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            inputObject = (T)EditorGUILayout.ObjectField( inputObject, typeof( T ), allowSceneObjects );
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
        public bool CenteredButton( string label, float width ) {
            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            var tempBool = GUILayout.Button( label, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button, GUILayout.MaxWidth( width ) );
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space( 2 );
            return tempBool;
        }

        /// <summary>
        /// Draws a button in the standard PIDI 2020 editor style
        /// </summary>
        /// <param name="label"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public bool StandardButton( string label, float width ) {
            var tempBool = GUILayout.Button( label, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button, GUILayout.MaxWidth( width ) );
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
        /// Begins a custom centered group similar to a foldout that can be expanded with a button
        /// </summary>
        /// <param name="label"></param>
        /// <param name="groupFoldState"></param>
        /// <returns></returns>
        public bool BeginCenteredGroup( string label, ref bool groupFoldState ) {

            if ( GUILayout.Button( label, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button ) ) {
                groupFoldState = !groupFoldState;
            }
            GUILayout.BeginHorizontal(); GUILayout.Space( 12 );
            GUILayout.BeginVertical();
            return groupFoldState;
        }


        /// <summary>
        /// Finishes a centered group
        /// </summary>
        public void EndCenteredGroup() {
            GUILayout.EndVertical();
            GUILayout.Space( 12 );
            GUILayout.EndHorizontal();
        }



        /// <summary>
        /// Custom integer field following the PIDI 2020 editor skin
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        public int IntField( GUIContent label, int currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue = EditorGUILayout.IntField( currentValue, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
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
        public float FloatField( GUIContent label, float currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue = EditorGUILayout.FloatField( currentValue, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;
        }


        /// <summary>
        /// Custom text field following the PIDI 2020 editor skin
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        public string TextField( GUIContent label, string currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue = EditorGUILayout.TextField( currentValue, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;
        }


        public Vector2 Vector2Field( GUIContent label, Vector2 currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue.x = EditorGUILayout.FloatField( currentValue.x, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.y = EditorGUILayout.FloatField( currentValue.y, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;

        }

        public Vector3 Vector3Field( GUIContent label, Vector3 currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue.x = EditorGUILayout.FloatField( currentValue.x, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.y = EditorGUILayout.FloatField( currentValue.y, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.z = EditorGUILayout.FloatField( currentValue.z, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

            return currentValue;

        }


        public Vector4 Vector4Field( GUIContent label, Vector4 currentValue ) {

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            currentValue.x = EditorGUILayout.FloatField( currentValue.x, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.y = EditorGUILayout.FloatField( currentValue.y, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.z = EditorGUILayout.FloatField( currentValue.z, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.Space( 8 );
            currentValue.w = EditorGUILayout.FloatField( currentValue.w, EditorGUIUtility.isProSkin?pidiSkin2.customStyles[4]:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textField );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );

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
        public float SliderField( GUIContent label, float currentValue, float minSlider = 0.0f, float maxSlider = 1.0f, string suffix = "" ) {

            GUILayout.Space( 4 );
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel( label, pidiSkin2.label, pidiSkin2.label );
            currentValue = EditorGUILayout.Slider( currentValue, minSlider, maxSlider );
            GUILayout.EndHorizontal();
            GUILayout.Space( 4 );

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
            EditorGUILayout.PrefixLabel( label, pidiSkin2.label, pidiSkin2.label );
            currentValue = EditorGUILayout.IntSlider( currentValue, minSlider, maxSlider );
            GUILayout.EndHorizontal();
            GUILayout.Space( 4 );

            return currentValue;
        }


        /// <summary>
        /// Draw a custom popup field in the PIDI 2020 style
        /// </summary>
        /// <param name="label"></param>
        /// <param name="toggleValue"></param>
        /// <returns></returns>
        public int PopupField( GUIContent label, int selected, string[] options ) {


            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );
            selected = EditorGUILayout.Popup( selected, options, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button );
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );
            return selected;
        }



        /// <summary>
        /// Draw a custom toggle that instead of using a check box uses an Enable/Disable drop down menu
        /// </summary>
        /// <param name="label"></param>
        /// <param name="toggleValue"></param>
        /// <returns></returns>
        public bool EnableDisableToggle( GUIContent label, bool toggleValue, bool trueFalseToggle = false, params GUILayoutOption[] options ) {

            int option = toggleValue ? 1 : 0;

            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            if ( label != null ) {
                GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );

                if ( trueFalseToggle ) {
                    option = EditorGUILayout.Popup( option, new string[] { "FALSE", "TRUE" }, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button );
                }
                else {
                    option = EditorGUILayout.Popup( option, new string[] { "DISABLED", "ENABLED" }, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button );
                }
            }
            else {
                if ( trueFalseToggle ) {
                    option = EditorGUILayout.Popup( option, new string[] { "FALSE", "TRUE" }, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button, options );
                }
                else {
                    option = EditorGUILayout.Popup( option, new string[] { "DISABLED", "ENABLED" }, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button, options );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );
            return option == 1;
        }




        /// <summary>
        /// Draw an enum field but changing the labels and names of the enum to Upper Case fields
        /// </summary>
        /// <param name="label"></param>
        /// <param name="userEnum"></param>
        /// <returns></returns>
        public System.Enum StandardEnumField( GUIContent label, System.Enum userEnum ) {

            return EditorGUILayout.EnumPopup( label, userEnum, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button );

        }


        /// <summary>
        /// Draw a layer mask field in the PIDI 2020 style
        /// </summary>
        /// <param name="label"></param>
        /// <param name="selected"></param>
        public LayerMask LayerMaskField( GUIContent label, LayerMask selected ) {

            List<string> layers = null;
            string[] layerNames = null;

            if ( layers == null ) {
                layers = new List<string>();
                layerNames = new string[4];
            }
            else {
                layers.Clear();
            }

            int emptyLayers = 0;
            for ( int i = 0; i < 32; i++ ) {
                string layerName = LayerMask.LayerToName( i );

                if ( layerName != "" ) {

                    for ( ; emptyLayers > 0; emptyLayers-- ) layers.Add( "Layer " + (i - emptyLayers) );
                    layers.Add( layerName );
                }
                else {
                    emptyLayers++;
                }
            }

            if ( layerNames.Length != layers.Count ) {
                layerNames = new string[layers.Count];
            }
            for ( int i = 0; i < layerNames.Length; i++ ) layerNames[i] = layers[i];


            GUILayout.Space( 2 );
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, pidiSkin2.label, GUILayout.Width( EditorGUIUtility.labelWidth ) );

            selected.value = EditorGUILayout.MaskField( selected.value, layerNames, EditorGUIUtility.isProSkin?pidiSkin2.button:EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button );

            GUILayout.EndHorizontal();
            GUILayout.Space( 2 );
            return selected;
        }



#endregion


#endif


    }
}