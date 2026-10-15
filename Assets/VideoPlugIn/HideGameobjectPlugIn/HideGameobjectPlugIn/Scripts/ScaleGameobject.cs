using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ScaleGameobject : MonoBehaviour
{
    [Space, Header("放缩参数:")][Header("放缩动画数据挂载")][SerializeField] private List<ScaleAnimatorData> scaleDatas;
    [Header("特效高度偏移量")][SerializeField] private float effectHeightOffset = 0f;
    private Vector3 scaleValue;
    private bool isTrigger;
    private string uiLayerName = "UI";
    
    private void Start()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Default"); // 确保渲染 Default 层
        }
        gameObject.layer = LayerMask.NameToLayer("Default");
        scaleValue = transform.localScale;
        isTrigger = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTrigger && other.CompareTag("TriggerGameobject"))
        {
            isTrigger = true;
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("UI"));
            PlayerScaleAnimator();
        }
    }
    
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void PlayerScaleAnimator()
    {
        Sequence sequence = DOTween.Sequence();
        Collider collider = GetComponent<Collider>();
        Vector3 effectPosition = collider != null ? collider.bounds.center + Vector3.up * effectHeightOffset : transform.position;
        
        foreach (var scaleData in scaleDatas)
        {
            if (scaleData.effectPrefab != null)
            {
                GameObject effect = Instantiate(scaleData.effectPrefab, effectPosition, Quaternion.identity);
                Destroy(effect, scaleData.duration); // 自动销毁特效
            }
            
            sequence.Append(transform.DOScale(scaleValue * scaleData.scaleValue, scaleData.duration)
                .SetEase(Ease.Linear));
        }
        sequence.Play();
    }
}

[Serializable]
public class ScaleAnimatorData
{
    [Header("放缩比例")][SerializeField] public float scaleValue;
    [Header("放缩动画时间")][SerializeField] public float duration;
    [Header("触发特效")][SerializeField] public GameObject effectPrefab;
}