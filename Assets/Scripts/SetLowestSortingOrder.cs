using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SetLowestSortingOrder : MonoBehaviour
{
    private void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            // 确保Renderer有Sorting Layer属性（比如ParticleRenderer、SpriteRenderer、MeshRenderer带Material的）
            if (renderer != null)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_SortingOrder"))
                    {
                        material.SetInt("_SortingOrder", -32768);
                    }
                }

                // 同时设置Renderer本身的SortingOrder（一般用于粒子特效、SpriteRenderer）
                renderer.sortingOrder = -32768;
            }
        }
    }
}
