// 仅供编辑状态的 Game 视图预览。和另两个脚本一起放入项目，勿放入 Editor 文件夹。
// 发布构建中此类完全排除；无需手动挂载到物体。
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[AddComponentMenu("")]
public class UICoinRangePreview : Graphic
{
    [System.NonSerialized] public UICoinSpawner owner;
    [System.NonSerialized] public bool maskFill;
    private Matrix4x4 area;

    public override Texture mainTexture
    {
        get { return maskFill && owner != null && owner.范围蒙版图片 != null ? owner.范围蒙版图片 : Texture2D.whiteTexture; }
    }

    public void Refresh() { SetVerticesDirty(); SetMaterialDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (owner == null || Application.isPlaying || !owner.编辑时Game显示范围) return;
        area = owner.EditorPreviewAreaMatrix();
        Vector2 size = owner.EditorPreviewSize();
        float radius = Mathf.Max(0f, owner.散落范围半径);
        if (maskFill)
        {
            if (owner.散落形状 == UICoinSpawner.ScatterShape.图片蒙版 && owner.范围蒙版图片 != null)
                AddMaskQuad(mesh, size);
            return;
        }
        switch (owner.散落形状)
        {
            case UICoinSpawner.ScatterShape.方形或矩形:
            case UICoinSpawner.ScatterShape.图片蒙版:
                Vector2 half = size * 0.5f;
                Line(mesh, new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y));
                Line(mesh, new Vector2(half.x, -half.y), new Vector2(half.x, half.y));
                Line(mesh, new Vector2(half.x, half.y), new Vector2(-half.x, half.y));
                Line(mesh, new Vector2(-half.x, half.y), new Vector2(-half.x, -half.y));
                break;
            case UICoinSpawner.ScatterShape.椭圆:
                Arc(mesh, size * 0.5f, 0f, 360f);
                break;
            case UICoinSpawner.ScatterShape.圆环:
                Arc(mesh, Vector2.one * radius, 0f, 360f);
                Arc(mesh, Vector2.one * Mathf.Clamp(owner.圆环内半径, 0f, radius), 0f, 360f);
                break;
            case UICoinSpawner.ScatterShape.扇形:
                float angle = Mathf.Clamp(owner.扇形张角, 0f, 360f);
                float start = owner.扇形朝向角 - angle * 0.5f;
                Arc(mesh, Vector2.one * radius, start, angle);
                if (angle < 360f)
                {
                    Line(mesh, Vector2.zero, Direction(start) * radius);
                    Line(mesh, Vector2.zero, Direction(start + angle) * radius);
                }
                break;
            default:
                Arc(mesh, Vector2.one * radius, 0f, 360f);
                break;
        }
        // 十字显示真正爆出点；范围偏移或参考 UI 改动后，也能看到二者的距离。
        Vector3 center = rectTransform.InverseTransformPoint(owner.transform.position);
        AddLine(mesh, (Vector2)center + Vector2.left * 7f, (Vector2)center + Vector2.right * 7f);
        AddLine(mesh, (Vector2)center + Vector2.down * 7f, (Vector2)center + Vector2.up * 7f);
    }

    private Vector2 Point(Vector2 point)
    {
        return (Vector2)rectTransform.InverseTransformPoint(area.MultiplyPoint3x4((Vector3)point));
    }

    private void Line(VertexHelper mesh, Vector2 from, Vector2 to) { AddLine(mesh, Point(from), Point(to)); }

    private void AddLine(VertexHelper mesh, Vector2 from, Vector2 to)
    {
        Vector2 offset = new Vector2(-(to - from).y, (to - from).x).normalized * Mathf.Max(0.5f, owner.Game预览线宽) * 0.5f;
        AddQuad(mesh, from - offset, from + offset, to + offset, to - offset, owner.Game预览颜色, false);
    }

    private static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void Arc(VertexHelper mesh, Vector2 radii, float start, float sweep)
    {
        Vector2 previous = Vector2.Scale(Direction(start), radii);
        for (int i = 1; i <= 96; i++)
        {
            Vector2 next = Vector2.Scale(Direction(start + sweep * i / 96f), radii);
            Line(mesh, previous, next);
            previous = next;
        }
    }

    private void AddMaskQuad(VertexHelper mesh, Vector2 size)
    {
        Vector2 half = size * 0.5f;
        Color tint = owner.Game预览颜色;
        tint.a *= 0.25f;
        AddQuad(mesh, Point(new Vector2(-half.x, -half.y)), Point(new Vector2(-half.x, half.y)),
            Point(new Vector2(half.x, half.y)), Point(new Vector2(half.x, -half.y)), tint, true);
    }

    private static void AddQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint, bool uv)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert((Vector3)a, tint, Vector2.zero);
        mesh.AddVert((Vector3)b, tint, uv ? Vector2.up : Vector2.zero);
        mesh.AddVert((Vector3)c, tint, uv ? Vector2.one : Vector2.zero);
        mesh.AddVert((Vector3)d, tint, uv ? Vector2.right : Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
#endif
