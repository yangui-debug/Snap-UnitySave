using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using EPOOutline;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Outlinable))]
[ExecuteAlways]
public class UIOutlinableBridge : MonoBehaviour
{
    private Image m_Image;
    private Outlinable m_Outlinable;

    private GameObject m_ProxyGo;
    private SpriteRenderer m_ProxyRenderer;

    [Header("代理物体 Z 轴偏移量")]
    [Tooltip("控制描边相比于 UI 往相机方向挪动多少。如果被其他 3D 物体遮挡，可以调大这个值")]
    public float zOffset = -0.1f;

    private void OnEnable()
    {
        m_Image = GetComponent<Image>();
        m_Outlinable = GetComponent<Outlinable>();
        CreateProxy();
    }

    private void OnDisable()
    {
        DestroyProxy();
    }

    private void Update()
    {
        if (m_Image == null || m_Outlinable == null) return;

        if (m_ProxyGo == null || m_ProxyRenderer == null)
        {
            CreateProxy();
        }

        SyncProxy();
    }

    private void CreateProxy()
    {
        DestroyProxy();

        m_ProxyGo = new GameObject("[UI_Outline_Proxy]");

        // 【修改】不再作为子物体挂载，直接放到根目录，彻底脱离 Canvas 空间的坐标干扰
        m_ProxyGo.transform.SetParent(null);
        m_ProxyGo.hideFlags = HideFlags.DontSave;

        m_ProxyRenderer = m_ProxyGo.AddComponent<SpriteRenderer>();
        m_ProxyGo.layer = LayerMask.NameToLayer("Default");

        if (m_Outlinable != null)
        {
            while (m_Outlinable.OutlineTargets.Count > 0)
            {
                m_Outlinable.RemoveTarget(m_Outlinable.OutlineTargets[0]);
            }

            var target = new OutlineTarget(m_ProxyRenderer);
            m_Outlinable.AddTarget(target);
        }
    }

    private void SyncProxy()
    {
        if (m_Image.sprite == null)
        {
            m_ProxyRenderer.enabled = false;
            return;
        }

        m_ProxyRenderer.enabled = m_Image.enabled && m_Image.gameObject.activeInHierarchy;
        m_ProxyRenderer.sprite = m_Image.sprite;
        m_ProxyRenderer.color = new Color(1, 1, 1, m_Image.color.a);

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && m_Image.sprite != null)
        {
            // 1. 强行同步世界位置与旋转
            m_ProxyGo.transform.rotation = rectTransform.rotation;

            // 2. 根据 UI 朝向计算其向前的偏移向量，确保正好贴在 UI 前面一点点
            Vector3 forwardOffset = rectTransform.forward * zOffset;
            m_ProxyGo.transform.position = rectTransform.position + forwardOffset;

            // 3. 计算缩放（考虑 Canvas 自身的缩放）
            float ppu = m_Image.sprite.pixelsPerUnit;
            if (ppu <= 0f) ppu = 100f;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            float spriteWidth = m_Image.sprite.rect.width / ppu;
            float spriteHeight = m_Image.sprite.rect.height / ppu;

            if (spriteWidth > 0 && spriteHeight > 0)
            {
                // 将 RectTransform 尺寸与物体的 LossyScale（全局缩放）结合
                float scaleX = (width / spriteWidth) * rectTransform.lossyScale.x;
                float scaleY = (height / spriteHeight) * rectTransform.lossyScale.y;
                m_ProxyGo.transform.localScale = new Vector3(scaleX, scaleY, rectTransform.lossyScale.z);
            }
        }
    }

    private void DestroyProxy()
    {
        if (m_ProxyGo != null)
        {
            if (Application.isPlaying)
                Destroy(m_ProxyGo);
            else
                DestroyImmediate(m_ProxyGo);
        }
    }
}