using UnityEngine;
/// <summary>只挂在红方兵营。实例化入口调用 Install，按值复制红兵配置；不更改生成数量、频率、随机位置或桥路线。</summary>
[DisallowMultipleComponent, AddComponentMenu("战斗/红兵生成接入（新版）")]
public sealed class RedMeleeSpawnSetup : MonoBehaviour
{
    [Tooltip("复制给本兵营新生成的红方近战，每名红兵拥有独立设置与状态。关闭设置总开关可恢复旧行为。")]
    public RedMeleeSettings redSettings = new RedMeleeSettings();
    public void Install(GameObject soldier)
    {
        if (!isActiveAndEnabled || soldier == null) return;
        foreach (var attack in soldier.GetComponentsInChildren<SoldierAnimationController>(true))
        {
            if (attack.myFaction != FactionType.Red || attack.是远程单位 || attack.GetComponent<SoldierAIMovement>() == null) continue;
            var local = attack.GetComponent<RedMeleeSelf>();
            if (local == null) local = attack.gameObject.AddComponent<RedMeleeSelf>();
            local.Configure(redSettings);
        }
    }
    void OnDrawGizmosSelected()
    {
        if (!isActiveAndEnabled || !redSettings.enabled || gameObject.scene.name != redSettings.sceneName) return;
        Gizmos.color = Color.green; Gizmos.DrawWireCube(redSettings.center, redSettings.size);
    }
}
