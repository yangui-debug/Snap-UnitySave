using System.Collections.Generic;
using UnityEngine;

namespace Playables.Core
{
    /// <summary>
    /// 仅服务于timeline的简易对象池
    /// 1. 根据明确的prefab创建
    /// 2. 维护池子中内容和按单位记录释放的内容
    /// </summary>
    public static class PlayablePool
    {
        private static Dictionary<string, Queue<GameObject>> cache;
        private static Dictionary<int, Dictionary<int, List<GameObject>>> agentEffects;
        private static HashSet<int> recycled = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnInitialize()
        {
            cache = new Dictionary<string, Queue<GameObject>>();
            agentEffects = new Dictionary<int, Dictionary<int, List<GameObject>>>();
            recycled = new HashSet<int>();
        }

        public static GameObject Get(this PlayableAgent agent, GameObject prefab, GameObject receiverAgent)
        {
            GameObject result = default;
            if (cache.TryGetValue(prefab.name, out Queue<GameObject> queue))
            {
                while (!result && queue.Count > 0)
                {
                    result = queue.Dequeue();
                }
            }

            if (!result)
            {
                result = Object.Instantiate(prefab);
                result.name = prefab.name;
            }
            else
            {
                result.SetActive(true);
                recycled.Remove(result.GetInstanceID());
            }

            int key = agent.GetInstanceID();
            int receiverId = receiverAgent.GetInstanceID();
            if (!agentEffects.TryGetValue(key, out Dictionary<int, List<GameObject>> dic))
            {
                dic = agentEffects[key] = new Dictionary<int, List<GameObject>>();
            }

            if (!dic.TryGetValue(receiverId, out List<GameObject> list))
            {
                list = dic[receiverId] = new List<GameObject>();
            }

            list.Add(result);

            return result;
        }

        public static void Put(this PlayableAgent agent, GameObject obj, GameObject receiverAgent)
        {
            InternalPut(obj);

            if (agentEffects.TryGetValue(agent.GetInstanceID(), out Dictionary<int, List<GameObject>> dic))
            {
                if (dic.TryGetValue(receiverAgent.GetInstanceID(), out List<GameObject> list))
                {
                    list.Remove(obj);
                }
            }
        }

        public static void RecycleEffects(this PlayableAgent agent, GameObject receiverAgent)
        {
            if (!agent || agentEffects == null || !receiverAgent)
            {
                return;
            }

            int key = agent.GetInstanceID();
            int receiverId = receiverAgent.GetInstanceID();
            if (agentEffects.TryGetValue(key, out Dictionary<int, List<GameObject>> dic) && dic != null)
            {
                if (dic.TryGetValue(receiverId, out List<GameObject> list) && list != null)
                {
                    foreach (GameObject obj in list)
                    {
                        InternalPut(obj);
                    }
                }
            }
        }

        public static void RecycleAll()
        {
            foreach (Dictionary<int, List<GameObject>> objectDic in agentEffects.Values)
            {
                foreach (List<GameObject> objects in objectDic.Values)
                {
                    foreach (GameObject obj in objects)
                    {
                        InternalPut(obj);
                    }
                }
            }

            agentEffects.Clear();
        }

        public static void InternalPut(GameObject obj)
        {
            int instanceId = obj.GetInstanceID();
            if (obj && !recycled.Contains(instanceId))
            {
                string key = obj.name;
                if (!cache.TryGetValue(key, out Queue<GameObject> queue))
                {
                    queue = cache[key] = new Queue<GameObject>();
                }

                queue.Enqueue(obj);
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(null);

                recycled.Add(instanceId);
            }
        }
    }
}