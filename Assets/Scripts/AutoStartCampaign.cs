using UnityEngine;

namespace Com.i89.AreaFaction
{
    public class AutoStartCampaign : MonoBehaviour
    {
        [Header("多阵营网格脚本引用")]
        [Tooltip("将挂载了 AreaFactionMultiCampaignGrid 的物体拖到这里。如果不拖，代码会自动在当前物体上寻找。")]
        public AreaFactionMultiCampaignGrid gridScript;

        [Header("延迟启动配置")]
        [Tooltip("游戏运行后延迟多少秒开始推演？(建议留0.5~1秒缓冲，确保海量格子和材质构建完毕)")]
        public float startDelay = 0.5f;

        private void Start()
        {
            // 1. 如果你忘记手动拖拽赋值，代码会尝试在当前物体上自动寻找
            if (gridScript == null)
            {
                gridScript = GetComponent<AreaFactionMultiCampaignGrid>();
            }

            // 2. 如果彻底找不到，在控制台报错提醒你
            if (gridScript == null)
            {
                Debug.LogError("【自动触发脚本】未找到 AreaFactionMultiCampaignGrid 脚本！请确保把它拖入变量槽，或者挂载在同一个物体上。");
                return;
            }

            // 3. 根据设定的延迟时间，自动开启宏观剧本推演
            if (startDelay > 0f)
            {
                Invoke(nameof(TriggerCampaign), startDelay);
            }
            else
            {
                TriggerCampaign();
            }
        }

        // 实际触发宏观剧本的方法
        private void TriggerCampaign()
        {
            Debug.Log("【自动触发脚本】已接管控制权，正在自动开启多阵营剧本推演！");

            // 对应原脚本中 [ContextMenu("2. 开启剧本推演 (Start Campaign)")] 下的方法
            gridScript.StartCampaign();
        }
    }
}