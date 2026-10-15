using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFurStudio2 {

    [CreateAssetMenu(fileName = "XFur Studio 2 Database", menuName = "XFur Studio 2/New Database Asset")]
    public class XFurStudioDatabase : ScriptableObject {

        private string databaseVersion = "v2.3.0";

        public string Version { get { return databaseVersion; } }
                
        public enum XFurRenderingMode { Standard, Universal, HighDefinition }

        public bool receiveURPShadows = true;

        public bool IsCreated { get; protected set; }

        [SerializeField] protected List<Texture2D> xfurStrandMaps = new List<Texture2D>();

        public XFurRenderingMode RenderingMode = XFurRenderingMode.Standard;

        public bool MobileMode;

        public bool MultiProfileBlending = true;
        
        public bool BakeFurData;
        
        public bool Virtual3DStrands = true;

        public bool UseNormalmaps;


        [SerializeField] protected List<XFurStudioFurProfile> referenceFurProfiles = new List<XFurStudioFurProfile>();

        [SerializeField] protected Texture2DArray referencedFurStrands;

        [SerializeField] protected Shader XFShells, Basic8, Basic4, XFURP, XFHDRP, XFHDRPEmissive;

        [SerializeField] protected Material XFShellsMaterial, XFShellsMaterialDouble;
        
        public Material XFShellI { get { 
                switch ( RenderingMode ) {
                    default : 
                        return XFShellsMaterial;

                    case XFurRenderingMode.Universal:
                        return URPShells;

                    case XFurRenderingMode.HighDefinition:
                        return HDRPShells;
                }
            }
        }


        public bool HasDoubleSide { 
            get { 
                if (RenderingMode == XFurRenderingMode.Universal ) {
                    return URPShellsDouble;
                }
                else if (RenderingMode == XFurRenderingMode.HighDefinition ) {
                    return HDRPShellsDouble;
                }
                else {
                    return XFShellDoubleI && XFShellDoubleF; 
                }
            }
        }



        public Material XFShellDoubleI {
            get {
                switch ( RenderingMode ) {
                    default:
                        return XFShellsMaterialDouble;

                    case XFurRenderingMode.Universal:
                        return URPShellsDouble;

                    case XFurRenderingMode.HighDefinition:
                        return HDRPShellsDouble;
                }
            }
        }


        public Material XFShellF { get {
                switch ( RenderingMode ) {
                    default:
                        return XFShellsMaterialFWD;

                    case XFurRenderingMode.Universal:
                        return URPShells;

                    case XFurRenderingMode.HighDefinition:
                        return HDRPShells;
                }
            } 
        }


        public Material XFShellDoubleF { get {
                switch ( RenderingMode ) {
                    default:
                        return XFShellsMaterialFWDDouble;

                    case XFurRenderingMode.Universal:
                        return URPShellsDouble;

                    case XFurRenderingMode.HighDefinition:
                        return HDRPShellsDouble;
                }
            } 
        }


        public Material HDRPEmit { get { return HDRPEmissive; } }

        public Material HDRPEmitDouble { get { return HDRPEmissiveDouble; } }


        [SerializeField] protected Material XFShellsMaterialFWD, XFShellsMaterialFWDDouble;

        [SerializeField] protected Material Basic4Mat, Basic8Mat;

        public Material BasicShellsEight { get { return Basic8Mat; } }
        public Material BasicShellsFour { get { return Basic4Mat; } }

        [SerializeField] protected Material URPShells, URPShellsDouble;

        [SerializeField] protected Material HDRPShells, HDRPShellsDouble, HDRPEmissive, HDRPEmissiveDouble;

        [SerializeField] protected Shader VFXProgressive, GPUPhysics, PainterFill, PainterUnwrap, PainterBase;



        public void UpdateShadows() {

            if ( !receiveURPShadows ) {
                URPShells.shaderKeywords = new string[] { "_RECEIVE_SHADOWS_OFF" };
                URPShellsDouble.shaderKeywords = URPShells.shaderKeywords;
            }
            else {
                URPShells.shaderKeywords = new string[0];
                URPShellsDouble.shaderKeywords = URPShells.shaderKeywords;
            }

        }



#if UNITY_EDITOR
        public string[] FurStrandMapNames { get {
                var tNames = new List<string>();
                tNames.Add( "Override" );

                foreach(Texture2D tex in xfurStrandMaps ) {
                    tNames.Add( tex.name );
                }

                return tNames.ToArray();
            } }
#endif

        public bool XFShellsReady { get { return XFShellsMaterial && XFShells; } }
        public bool BasicReady { get { return BasicShellsFour && BasicShellsEight && Basic8 && Basic4; } }
        public bool URPReady { get { return XFURP && URPShells; } }
        public bool HDRPReady { get { return XFHDRP && HDRPShells; } }



        public List<XFurStudioFurProfile> Profiles {
            get { return referenceFurProfiles; }
        }


        public Texture2DArray ProfileStrands {
            get {
                return referencedFurStrands;
            }
#if UNITY_EDITOR
            set {
                referencedFurStrands = value;
            }
#endif
        }


        public string[] ProfileNames { get {
                if (referenceFurProfiles.Count < 1 ) {
                    return new string[0];
                }
                else {
                    var names = new List<string>();
                    for( int i = 0; i < referenceFurProfiles.Count; i++ ) {
                        if ( referenceFurProfiles[i] ) {
                            names.Add( referenceFurProfiles[i].name );
                        }
                        else {
                            names.Add( "UNASSIGNED PROFILE" );
                        }
                    }

                    return names.ToArray();
                }
            } 
        }

#if UNITY_EDITOR
        public void LoadResources() {

            IsCreated = true;

            if ( !VFXProgressive ) {
                VFXProgressive = Shader.Find( "Hidden/XFur Studio 2/VFX/VFXProgressive" );
            }

            if ( !GPUPhysics ) {
                GPUPhysics = Shader.Find( "Hidden/XFur Studio 2/Physics/GPU Physics" );
            }            

            if ( !PainterBase ) {
                PainterBase = Shader.Find( "Hidden/XFur Studio 2/Designer/Painter" );
            }            

            if ( !PainterFill ) {
                PainterFill = Shader.Find( "Hidden/XFur Studio 2/Designer/Filler" );
            }

            if ( !PainterUnwrap ) {
                PainterUnwrap = Shader.Find( "Hidden/XFur Studio 2/Designer/Auto Unwrap" );
            }

            if ( !XFShells ) {
                XFShells = Shader.Find( "Hidden/XFur Studio 2/XF Shells" );
            }


            if ( !Basic4 ) {
                Basic4 = Shader.Find( "Hidden/XFur Studio 2/Basic Shells 4" );
            }
            
            if ( !Basic8 ) {
                Basic8 = Shader.Find( "Hidden/XFur Studio 2/Basic Shells 8" );
            }


            if ( !XFHDRP ) {
                XFHDRP = Shader.Find( "Hidden/XFur Studio 2/HDRP Shells" );
            }

            if ( !XFHDRPEmissive ) {
                XFHDRPEmissive = Shader.Find( "Hidden/XFur Studio 2/HDRP Shells Emissive" );
            }

            if ( !HDRPShells ) {
                if ( XFHDRP ) {
                    HDRPShells = new Material( XFHDRP );
                    HDRPShells.name = "Internal HDRPShells";
                    AssetDatabase.AddObjectToAsset( HDRPShells, this );
                    HDRPShells.enableInstancing = true;
                }
            }
            else {
                HDRPShells.enableInstancing = true;
            }

            if( !HDRPShellsDouble ) {
                if ( XFHDRP ) {
                    HDRPShellsDouble = new Material( XFHDRP );
                    HDRPShellsDouble.name = "Internal HDRPShellsD";
                    AssetDatabase.AddObjectToAsset( HDRPShellsDouble, this );
                    HDRPShellsDouble.enableInstancing = true;
                    HDRPShellsDouble.SetFloat( "_DoubleSidedEnable", 1 );
                    HDRPShellsDouble.SetFloat( "_DoubleSidedNormalMode", 1 );
                }
            }
            else {
                HDRPShellsDouble.enableInstancing = true;
                HDRPShellsDouble.SetFloat( "_DoubleSidedEnable", 1 );
                HDRPShellsDouble.SetFloat( "_DoubleSidedNormalMode", 1 );
            }

            if ( !HDRPEmissive ) {
                if ( XFHDRP ) {
                    HDRPEmissive = new Material( XFHDRPEmissive );
                    HDRPEmissive.name = "Internal HDRPEmissive";
                    AssetDatabase.AddObjectToAsset( HDRPEmissive, this );
                    HDRPEmissive.enableInstancing = true;
                }
            }
            else {
                HDRPEmissive.enableInstancing = true;
            }

            if( !HDRPEmissiveDouble ) {
                if ( XFHDRP ) {
                    HDRPEmissiveDouble = new Material( Shader.Find( "Hidden/XFur Studio 2/HDRP Shells EmissiveD" ) );
                    HDRPEmissiveDouble.name = "Internal HDRPEmissiveD";
                    AssetDatabase.AddObjectToAsset( HDRPEmissiveDouble, this );
                    HDRPEmissiveDouble.enableInstancing = true;
                    HDRPEmissiveDouble.SetFloat( "_DoubleSidedEnable", 1 );
                    HDRPEmissiveDouble.SetFloat( "_DoubleSidedNormalMode", 1 );
                }
            }
            else {
                HDRPEmissiveDouble.enableInstancing = true;
                HDRPEmissiveDouble.shader = Shader.Find( "Hidden/XFur Studio 2/HDRP Shells EmissiveD" );
                HDRPEmissiveDouble.SetFloat( "_DoubleSidedEnable", 1 );
                HDRPEmissiveDouble.SetFloat( "_DoubleSidedNormalMode", 1 );
            }




            if ( !XFURP ) {
                XFURP = Shader.Find( "Hidden/XFur Studio 2/URP Shells" );
            }

            if ( !URPShells ) {
                if ( XFURP ) {
                    URPShells = new Material( XFURP );
                    URPShells.name = "Internal URPShells";
                    AssetDatabase.AddObjectToAsset( URPShells, this );
                    URPShells.enableInstancing = true;
                }
            }
            else {
                URPShells.enableInstancing = true;
            }

            if ( !URPShellsDouble ) {
                if ( XFURP ) {
                    URPShellsDouble = new Material( XFURP );
                    URPShellsDouble.shader = Shader.Find( "Hidden/XFur Studio 2/URP ShellsD" );
                    URPShellsDouble.name = "Internal URPShellsD";
                    AssetDatabase.AddObjectToAsset( URPShellsDouble, this );
                    URPShellsDouble.enableInstancing = true;
                }
            }
            else {
                URPShellsDouble.shader = Shader.Find( "Hidden/XFur Studio 2/URP ShellsD" );
                URPShellsDouble.enableInstancing = true;
            }



            if ( !XFShellsMaterial ) {
                if ( XFShells ) {
                    XFShellsMaterial = new Material( XFShells );
                    XFShellsMaterial.name = "Internal XFShells";
                    XFShellsMaterial.enableInstancing = true;
                    AssetDatabase.AddObjectToAsset( XFShellsMaterial, this );
                }
            }
            else {
                XFShellsMaterial.enableInstancing = true;

                if ( MultiProfileBlending ) {
                    XFShellsMaterial.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    XFShellsMaterial.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }


            if ( !XFShellsMaterialDouble ) {
                if ( XFShells ) {
                    XFShellsMaterialDouble = new Material( XFShells );
                    XFShellsMaterialDouble.name = "Internal XFShellsD";
                    XFShellsMaterialDouble.enableInstancing = true;
                    XFShellsMaterialDouble.SetFloat( "_XFurCullFur", 0 );
                    AssetDatabase.AddObjectToAsset( XFShellsMaterialDouble, this );
                }
            }
            else {
                XFShellsMaterialDouble.enableInstancing = true;
                XFShellsMaterialDouble.SetFloat( "_XFurCullFur", 0 );

                if ( MultiProfileBlending ) {
                    XFShellsMaterialDouble.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    XFShellsMaterialDouble.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }


            if ( !BasicShellsEight  ) {
                if ( Basic8 ) {
                    Basic8Mat = new Material( Basic8 );
                    Basic8Mat.name = "Internal Basic Eight";
                    AssetDatabase.AddObjectToAsset( Basic8Mat, this );
                }
            }
            else {
                if ( MultiProfileBlending ) {
                    Basic8Mat.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    Basic8Mat.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }
            
            if ( !Basic4Mat ) {
                if ( Basic4 ) {
                    Basic4Mat = new Material( Basic4 );
                    Basic4Mat.name = "Internal Basic Four";
                    AssetDatabase.AddObjectToAsset( Basic4Mat, this );
                }
            }
            else {
                if ( MultiProfileBlending ) {
                    Basic4Mat.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    Basic4Mat.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }
            

            if ( !XFShellsMaterialFWD ) {
                if ( XFShells ) {
                    XFShellsMaterialFWD = new Material( XFShells );
                    XFShellsMaterialFWD.enableInstancing = false;
                    XFShellsMaterialFWD.name = "Internal XFShells FW";
                    AssetDatabase.AddObjectToAsset( XFShellsMaterialFWD, this );
                }
            }
            else {
                if ( MultiProfileBlending ) {
                    XFShellsMaterialFWD.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    XFShellsMaterialFWD.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }
            

            if ( !XFShellsMaterialFWDDouble ) {
                if ( XFShells ) {
                    XFShellsMaterialFWDDouble = new Material( XFShells );
                    XFShellsMaterialFWDDouble.enableInstancing = false;
                    XFShellsMaterialFWDDouble.name = "Internal XFShells DFW";
                    XFShellsMaterialFWDDouble.SetFloat( "_XFurCullFur", 0 );
                    AssetDatabase.AddObjectToAsset( XFShellsMaterialFWDDouble, this );
                }
            }
            else {
                XFShellsMaterialFWDDouble.SetFloat( "_XFurCullFur", 0 );
                if ( MultiProfileBlending ) {
                    XFShellsMaterialFWDDouble.EnableKeyword( "FURPROFILES_BLENDED" );
                }
                else {
                    XFShellsMaterialFWDDouble.DisableKeyword( "FURPROFILES_BLENDED" );
                }
            }

            if ( XFShellsMaterial && XFShellsMaterial.name != "Internal XFShells" ) {
                XFShellsMaterial.name = "Internal XFShells";
            }

        }
#endif


        public void UpdateNormalmapUse() {

            if ( XFShellF ) {
                if ( UseNormalmaps )
                    XFShellF.EnableKeyword( "USE_NORMALMAP" );
                else
                    XFShellF.DisableKeyword( "USE_NORMALMAP" );
            }
            
            if ( XFShellI ) {
                if ( UseNormalmaps )
                    XFShellI.EnableKeyword( "USE_NORMALMAP" );
                else
                    XFShellI.DisableKeyword( "USE_NORMALMAP" );
            }
            
            if ( XFShellDoubleI ) {
                if ( UseNormalmaps )
                    XFShellDoubleI.EnableKeyword( "USE_NORMALMAP" );
                else
                    XFShellDoubleI.DisableKeyword( "USE_NORMALMAP" );
            }
            
            if ( XFShellDoubleF ) {
                if ( UseNormalmaps )
                    XFShellDoubleF.EnableKeyword( "USE_NORMALMAP" );
                else
                    XFShellDoubleF.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( URPShells ) {
                if ( UseNormalmaps )
                    URPShells.EnableKeyword( "USE_NORMALMAP" );
                else
                    URPShells.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( URPShellsDouble ) {
                if ( UseNormalmaps )
                    URPShellsDouble.EnableKeyword( "USE_NORMALMAP" );
                else
                    URPShellsDouble.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( HDRPShells ) {
                if ( UseNormalmaps )
                    HDRPShells.EnableKeyword( "USE_NORMALMAP" );
                else
                    HDRPShells.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( HDRPShellsDouble ) {
                if ( UseNormalmaps )
                    HDRPShellsDouble.EnableKeyword( "USE_NORMALMAP" );
                else
                    HDRPShellsDouble.DisableKeyword( "USE_NORMALMAP" );
            }
            
            if ( HDRPEmit ) {
                if ( UseNormalmaps )
                    HDRPEmit.EnableKeyword( "USE_NORMALMAP" );
                else
                    HDRPEmit.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( HDRPEmitDouble ) {
                if ( UseNormalmaps )
                    HDRPEmitDouble.EnableKeyword( "USE_NORMALMAP" );
                else
                    HDRPEmitDouble.DisableKeyword( "USE_NORMALMAP" );
            }


            if ( Basic4Mat ) {
                if ( UseNormalmaps )
                    Basic4Mat.EnableKeyword( "USE_NORMALMAP" );
                else
                    Basic4Mat.DisableKeyword( "USE_NORMALMAP" );
            }

            if ( Basic8Mat ) {
                if ( UseNormalmaps )
                    Basic8Mat.EnableKeyword( "USE_NORMALMAP" );
                else
                    Basic8Mat.DisableKeyword( "USE_NORMALMAP" );
            }

        }


    }


}