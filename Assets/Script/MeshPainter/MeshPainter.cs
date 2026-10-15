using UnityEngine;

[ExecuteAlways]
public class MeshPainter : MonoBehaviour
{
    [Header("范围与位置设置")]
    public Vector3 centerOffset = Vector3.zero;
    [Range(0.1f, 50f)]
    public float brushRadius = 5f;

    [Header("目标设置")]
    public int targetSetIndex = 1; 
    public LayerMask decoLayer;

    [Header("可视化设置")]
    public bool showRangeAlways = true; 
    public Color rangeColor = Color.green;

    void Update()
    {
        if (!Application.isPlaying && !showRangeAlways) return;
        Vector3 finalCenter = transform.TransformPoint(centerOffset);
        Collider[] hits = Physics.OverlapSphere(finalCenter, brushRadius, decoLayer);
        foreach (var hit in hits)
        {
            MeshSetHandler handler = hit.GetComponent<MeshSetHandler>();
            if (handler != null) handler.SwitchToSet(targetSetIndex);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showRangeAlways) return;
        DrawBrushGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (showRangeAlways) return;
        DrawBrushGizmo();
    }

    private void DrawBrushGizmo()
    {
        Gizmos.color = rangeColor;
        Vector3 finalCenter = transform.TransformPoint(centerOffset);
        Gizmos.DrawWireSphere(finalCenter, brushRadius);
        Gizmos.color = new Color(rangeColor.r, rangeColor.g, rangeColor.b, 0.1f);
        Gizmos.DrawSphere(finalCenter, brushRadius);
    }
}