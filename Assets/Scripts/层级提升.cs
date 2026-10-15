using UnityEngine;
using TMPro;

public class TMPSetSortingOrder : MonoBehaviour
{
    public int sortingOrder = 1000;
    public string sortingLayerName = "Default"; // 你想放到哪个Sorting Layer

    void Start()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }
    }
}