using DG.Tweening;
using UnityEngine;

public class TempBuildingBubble : MonoBehaviour
{
    [SerializeField] private float shrinkCoef = 1.2f;
    private Vector3 originScale;

    public void Init(Quaternion lookAtRot)
    {
        originScale = transform.localScale;

        // 看向屏幕
        transform.LookAt(transform.position + lookAtRot * Vector3.forward, lookAtRot * Vector3.up);
    }

    public void ShowBubble()
    {
        transform.localScale = originScale * 0.8f;
        transform.DOScale(originScale * shrinkCoef, 0.15f).OnComplete(() => { transform.DOScale(originScale, 0.15f); });
    }

    public void CloseBubble()
    {
        transform.DOScale(originScale * 0.8f, 0.15f).OnComplete(() => { Destroy(gameObject); });
    }
}