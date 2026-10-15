using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFurStudio2 {

    [ExecuteAlways]
    public class XFurStudio2_WindZone : MonoBehaviour {

        public string Version { get { return "v2.2.0"; } }

        [Range( 0, 32 )] public float WindFrequency = 8.0f;
        [Range( 0, 2 )] public float WindStrength = 0.3f;

        [Range( 0, 1 )] public float SnowIntensity = 0;
        [Range( 0, 1 )] public float RainIntensity = 0;

        [Range( 0, 1 )] public float SnowWindInfluence = 0;
        [Range( 0, 1 )] public float RainWindInfluence = 0;

        public Vector3 SnowDirection = Vector3.down;
        public Vector3 RainDirection = Vector3.down;
        
        public Vector3 SnowAbsDirection = Vector3.down;
        public Vector3 RainAbsDirection = Vector3.down;


#if UNITY_EDITOR
        public bool snowGizmos, rainGizmos;
#endif

#if UNITY_EDITOR
        private Mesh arrowMesh;
#endif

        public void Start() {

        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            
            if ( !arrowMesh ) {
                arrowMesh = (Mesh)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets( "XFurStudio2_Arrow" )[0] ), typeof( Mesh ) );
            }
            else {

                Gizmos.color = Color.green;
                Gizmos.DrawWireMesh( arrowMesh, transform.position, transform.rotation, Vector3.one * 0.2f * WindStrength );

                if ( rainGizmos ) {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireMesh( arrowMesh, transform.position, Quaternion.LookRotation( ( RainAbsDirection + ( transform.forward * WindStrength * RainWindInfluence ) ).normalized, Vector3.up ), Vector3.one * 0.1f );
                }

                if ( snowGizmos ) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireMesh( arrowMesh, transform.position, Quaternion.LookRotation( ( SnowAbsDirection + ( transform.forward * WindStrength * SnowWindInfluence ) ).normalized, Vector3.up ), Vector3.one * 0.1f );
                }
            }

        }
#endif

        public void Update() {

            Shader.SetGlobalVector( "_XFurWindDirectionFreq", new Vector4(transform.forward.x,transform.forward.y,transform.forward.z,WindFrequency) );
            Shader.SetGlobalFloat( "_XFurWindStrength", WindStrength );


            SnowDirection = ( SnowAbsDirection + ( transform.forward * WindStrength * SnowWindInfluence ) ).normalized;
            RainDirection = ( RainAbsDirection + ( transform.forward * WindStrength * RainWindInfluence ) ).normalized;

        }

    }


}