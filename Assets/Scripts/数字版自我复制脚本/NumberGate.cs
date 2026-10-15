using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数字道具板：进入触发后，让进入者复制 N 个自己
/// 把本脚本挂在有 Collider（isTrigger=true）的“数字板”物体上
/// </summary>
[DisallowMultipleComponent]
public class NumberGate : MonoBehaviour
{
    [Header("要生成的额外副本数量")]
    [Min(0)] public int copies = 3;               // 生成“额外”数量；例如3=再生成3个

    [Header("触发控制")]
    public bool oneShot = true;                   // 板子是否一次性
    public float perActorCooldown = 0.5f;         // 同一个进入者的冷却，避免抖动多次触发
    private readonly Dictionary<GameObject, float> _lastEnterTime = new();

    private bool _consumed;

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || (_consumed && oneShot)) return;

        // 查找进入者身上的 AutoCloner
        var cloner = other.GetComponentInParent<AutoCloner>();
        if (cloner == null) return;

        // 冷却防抖
        if (_lastEnterTime.TryGetValue(other.gameObject, out var t) &&
            Time.time - t < perActorCooldown) return;

        _lastEnterTime[other.gameObject] = Time.time;

        cloner.CloneSelf(copies);

        if (oneShot) _consumed = true;
    }
}
