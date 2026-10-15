using UnityEngine;

namespace ArtProject
{
    public class ArtProjectRemoveInstancing : MonoBehaviour
    {
        private void Awake()
        {
            // disable all MeshInstancing components
            var meshInstancingComponents = FindObjectsOfType<ThirdParty.AnimationInstancingPlugIn.MeshInstancing>();
            foreach (var meshInstancingComponent in meshInstancingComponents)
            {
                meshInstancingComponent.enabled = false;
            }

            // enable all MeshRenderer components
            var meshRendererComponents = FindObjectsOfType<MeshRenderer>();
            foreach (var meshRendererComponent in meshRendererComponents)
            {
                meshRendererComponent.enabled = true;
            }
        }
    }
}