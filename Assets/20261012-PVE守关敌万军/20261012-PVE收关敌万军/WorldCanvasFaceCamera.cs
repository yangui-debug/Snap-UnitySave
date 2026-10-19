// ====================================================================================
//  WorldCanvasFaceCamera.cs
//  （世界空间）Canvas 广告牌脚本
//
//  功能概述：
//   1. 让世界空间 Canvas 每一帧都正对相机（永远面对相机）；
//   2. 可调节“相对原坐标”的位移 offset —— Canvas 最终位置 = 原坐标 + offset；
//   3. 可调节沿“朝向相机方向”的推离距离 forwardPush —— Canvas 旋转后自动把自身
//      从墙体 / 地形 / 柱子等场景物体里“顶”出来，防止被遮挡；
//   4. 可选：跟随锚点物体移动、全向面对 / 仅绕 Y 轴转向、轴锁定、平滑旋转。
//
//  为什么会“旋转后被场景物体遮挡”？
//     Canvas 初始摆放时正面朝向 Z 方向，不会穿墙；一旦旋转去面对相机，
//     画布平面可能转进旁边的墙体 / 地形 / 雕像里，导致文字或图片被裁掉、被挡住。
//     解决办法就是本脚本的两个位置旋钮：
//       - offset       ：在世界坐标上整体挪开（例如向上抬高、向侧面平移）；
//       - forwardPush  ：沿“Canvas → 相机”的方向推离，把画布从它身后的遮挡物里顶出来。
//
//  【使用步骤】
//    1. 场景中创建 Canvas，Inspector 把 Render Mode 改为 World Space（世界空间）；
//    2. 按需设置 Canvas 的 RectTransform 宽高和整体缩放；
//    3. 把本脚本挂到 Canvas 根物体（带 RectTransform 的那个 GameObject）上；
//    4. 可选：把 anchorTransform 拖成一个单位 / 锚点物体，画布会跟随它移动；
//    5. 调节 offset / forwardPush 让画布在任何相机角度下都不被遮挡；
//    6. 运行时 Canvas 会自动每帧转向相机，无需任何手动操作。
//
//  提示：本脚本默认只在运行时生效（Update/LateUpdate）。如需在编辑器里实时预览效果，
//        可在类名上方加 [ExecuteAlways]，此时注意 Awake 会重新记录原坐标。
// ====================================================================================

using UnityEngine;

/// <summary>
/// 让世界空间 Canvas 始终面向相机（广告牌效果），并支持：
///  - 相对“初始原坐标”（或指定锚点物体）的可调位移 offset；
///  - 沿朝向相机方向的可调推离距离 forwardPush，旋转后自动把 Canvas 从场景遮挡物中顶出来；
///  - 全方向面对（FullBillboard）/ 仅绕 Y 轴转向（YawOnly，保持文字竖直）两种朝向模式；
///  - 可选轴锁定、平滑旋转、跟随锚点物体。
/// </summary>
public class WorldCanvasFaceCamera : MonoBehaviour
{
    /// <summary>
    /// 朝向模式枚举。
    /// </summary>
    public enum FacingMode
    {
        /// <summary>完全面向相机：Canvas 的 +Z 轴始终指向相机，相机看到的是画布正面。</summary>
        FullBillboard = 0,

        /// <summary>仅绕世界 Y 轴转向相机，画布保持竖直，适合头顶血条 / 名字等不希望倾斜的 UI。</summary>
        YawOnly = 1,
    }

    [Header("相机")]
    [Tooltip("要朝向的相机；留空则自动获取 Camera.main（主相机）")]
    public Camera targetCamera;

    [Header("朝向模式")]
    [Tooltip("朝向方式：FullBillboard = 完全面向相机（画布平面正对镜头）；YawOnly = 只绕 Y 轴转向，画布始终保持竖直、文字不随相机俯仰而倾斜")]
    public FacingMode facingMode = FacingMode.FullBillboard;

    [Header("位置控制（防遮挡）")]
    [Tooltip("是否由本脚本控制 Canvas 的世界位置；关闭后只旋转朝向，位置交给其他逻辑或父物体管理")]
    public bool controlPosition = true;

    [Tooltip("锚点物体：Canvas 的基准位置取该物体每帧的世界坐标（画布会跟随它移动）；留空则以 Awake 时自身所在位置作为“原坐标”")]
    public Transform anchorTransform;

    [Tooltip("相对原坐标（锚点）的世界位移。最终位置 = 原坐标 + offset + 朝向方向 × forwardPush。用于把 Canvas 从遮挡物中整体挪开，例如向上抬高绕过矮墙、向侧面平移避开柱子")]
    public Vector3 offset = Vector3.zero;

    [Tooltip("沿“朝向相机方向”额外推离的距离（米）。Canvas 转向相机后按该距离朝相机方向平移，把画布从墙体 / 地形等身后的遮挡物里顶出来，防止旋转后被遮挡。仅在 controlPosition 开启时生效")]
    [Range(0f, 10f)]
    public float forwardPush = 0f;

    [Header("旋转微调")]
    [Tooltip("锁定 X 轴旋转：保持当前 X 欧拉角不变（常用于固定画布俯仰角度）")]
    public bool lockX = false;

    [Tooltip("锁定 Y 轴旋转：保持当前 Y 欧拉角不变")]
    public bool lockY = false;

    [Tooltip("锁定 Z 轴旋转：保持当前 Z 欧拉角不变（常用于防止画布左右滚转）")]
    public bool lockZ = false;

    [Header("平滑与初始化")]
    [Tooltip("旋转速度（度/秒）。> 0 时平滑转向相机，数值越大转向越快；0 表示每帧直接贴到目标朝向")]
    public float smoothSpeed = 0f;

    /// <summary>
    /// 运行时记录的“原坐标”：anchorTransform 为空时的位置基准点。
    /// </summary>
    private Vector3 _originPosition;

    /// <summary>
    /// 编辑器里刚把脚本挂到物体上时自动填好主相机，方便直接在场景里看到效果。
    /// </summary>
    private void Reset()
    {
        targetCamera = Camera.main;
    }

    /// <summary>
    /// 初始化：记录一次“原坐标”，并补全相机引用。
    /// </summary>
    private void Awake()
    {
        // 记录一次“原坐标”：之后每帧位置 = 原坐标 + offset（+ 朝向推离）。
        // 注意：若物体在运行中被其他逻辑移动，仍会以 Awake 时记录的这个点为基准；
        // 想重新对齐请调用 ReAnchor()，或在 Inspector 右键菜单选择“重新记录原坐标”。
        _originPosition = transform.position;

        // 未手动指定相机时自动取主相机
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    /// <summary>
    /// 用 LateUpdate 而不是 Update：确保在动画 / Timeline / 其他脚本修改旋转之后，
    /// 本脚本再覆盖一次朝向，保证 Canvas 永远正对相机，不会被别的逻辑“抢”走角度。
    /// </summary>
    private void LateUpdate()
    {
        // 相机可能中途切换（如切主相机），每次顺手补查一次
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return; // 场景里没有可用相机时直接跳过本帧

        // 防呆：锚点不能是自己（否则“基准位置 = 自己当前位置”，每帧都会原地累加位移导致漂移）
        Transform anchor = (anchorTransform != null && anchorTransform != transform)
            ? anchorTransform
            : null;

        // ------------------------------------------------------------
        // 第一步：计算最终位置（可关掉，只留旋转）
        // ------------------------------------------------------------
        // 基准点：有锚点物体就跟随锚点，否则使用 Awake 记录的原坐标
        Vector3 basePos = (anchor != null) ? anchor.position : _originPosition;

        // 先加上相对原坐标的世界位移 offset
        Vector3 finalPos = basePos + offset;

        if (controlPosition)
        {
            // 再沿“目标点 → 相机”的方向推离，把画布从身后的遮挡物里顶出来
            if (forwardPush > 0f)
            {
                Vector3 toCamera = targetCamera.transform.position - finalPos;
                if (toCamera.sqrMagnitude > 1e-8f) // 防止相机与目标点重合时除以 0 产生 NaN
                {
                    finalPos += toCamera.normalized * forwardPush;
                }
            }

            // 写入世界位置
            transform.position = finalPos;
        }
        else
        {
            // 不控制位置时，旋转计算仍基于当前实际位置
            finalPos = transform.position;
        }

        // ------------------------------------------------------------
        // 第二步：计算期望朝向（让 Canvas 正面 +Z 指向相机）
        // ------------------------------------------------------------
        Quaternion targetRot = ComputeFacingRotation(finalPos);

        // ------------------------------------------------------------
        // 第三步：轴锁定（保持指定轴的当前欧拉角不参与转向）
        // ------------------------------------------------------------
        if (lockX || lockY || lockZ)
        {
            Vector3 targetEuler = targetRot.eulerAngles;
            Vector3 currentEuler = transform.rotation.eulerAngles;
            if (lockX) targetEuler.x = currentEuler.x;
            if (lockY) targetEuler.y = currentEuler.y;
            if (lockZ) targetEuler.z = currentEuler.z;
            targetRot = Quaternion.Euler(targetEuler);
        }

        // ------------------------------------------------------------
        // 第四步：应用旋转（可选平滑）
        // ------------------------------------------------------------
        if (smoothSpeed > 0f)
        {
            // 以“度/秒”为速度向目标朝向匀速转动，避免瞬移带来的突兀感
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                smoothSpeed * Time.deltaTime);
        }
        else
        {
            // 默认：每帧直接贴到目标朝向，保证“永远面对相机”
            transform.rotation = targetRot;
        }
    }

    /// <summary>
    /// 根据 Canvas 所在位置与相机，计算让 Canvas 正面（+Z）指向相机的旋转。
    /// </summary>
    /// <param name="fromPos">Canvas 的最终世界位置（从该点看向相机）。</param>
    /// <returns>目标旋转四元数。</returns>
    private Quaternion ComputeFacingRotation(Vector3 fromPos)
    {
        // 从 Canvas 指向相机的方向
        Vector3 dir = targetCamera.transform.position - fromPos;

        // 相机与 Canvas 几乎重合的极端情况：退回使用相机自身的前方
        if (dir.sqrMagnitude < 1e-8f)
            dir = targetCamera.transform.forward;
        dir.Normalize();

        // 模式一：仅绕世界 Y 轴转向，画布保持竖直（头顶血条 / 名字推荐）
        if (facingMode == FacingMode.YawOnly)
        {
            // 把“指向相机”的方向投影到水平面（XZ 平面）
            Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (flatDir.sqrMagnitude < 1e-8f)
                flatDir = Vector3.ProjectOnPlane(targetCamera.transform.forward, Vector3.up);
            if (flatDir.sqrMagnitude < 1e-8f)
                flatDir = Vector3.forward; // 相机正上 / 正下等极端角度兜底

            return Quaternion.LookRotation(flatDir, Vector3.up);
        }

        // 模式二（默认）：完全面向相机，用相机自身的 up 作为画布的上方向
        Vector3 up = targetCamera.transform.up;
        // 当 dir 与 up 几乎平行时（例如相机几乎垂直俯拍、画布正下方仰拍），
        // Quaternion.LookRotation 会失效（无法确定滚转），做两级兜底：
        if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.999f)
        {
            up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.999f)
                up = targetCamera.transform.forward; // 极少出现的最后兜底
        }

        return Quaternion.LookRotation(dir, up);
    }

    /// <summary>
    /// 重新记录“原坐标”为物体当前世界位置。
    /// 适用场景：在场景里手动挪动 Canvas 后希望以新位置为基准；
    /// 或锚点从空改为物体 / 从物体改为空时，需要重新对齐基准。
    /// 调用方式：Inspector 右键组件 → “重新记录原坐标”，或代码里调用本方法。
    /// </summary>
    [ContextMenu("重新记录原坐标")]
    public void ReAnchor()
    {
        _originPosition = transform.position;
    }
}
