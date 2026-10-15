using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Com.i89.AreaFaction.Editor
{
    public class AreaFactionSquadDividingTool : EditorWindow
    {
        private GameObject targetParentObject;
        private float neighborThreshold = 6f;
        private string squadName = "Magma_Land_01";

        [MenuItem("Tools/AreaFaction/分队划分工具")]
        public static void ShowWindow()
        {
            AreaFactionSquadDividingTool window = GetWindow<AreaFactionSquadDividingTool>("分队划分工具");
            window.minSize = new Vector2(350, 200);
        }

        private void OnGUI()
        {
            GUILayout.Label("分队划分工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            targetParentObject = (GameObject)EditorGUILayout.ObjectField(
                "队伍父物体", 
                targetParentObject, 
                typeof(GameObject), 
                true
            );

            squadName = EditorGUILayout.TextField("队伍名称", squadName);

            neighborThreshold = EditorGUILayout.FloatField("相邻阈值", neighborThreshold);
            EditorGUILayout.HelpBox("相邻阈值：判断两个物体是否相邻的距离标准", MessageType.Info);

            GUILayout.Space(10);

            if (targetParentObject != null)
            {
                int childCount = targetParentObject.transform.childCount;
                EditorGUILayout.LabelField($"当前子物体数量: {childCount}");
                
                int estimatedSquadCount = EstimateSquadCount(targetParentObject);
                EditorGUILayout.LabelField($"预估分队数量: {estimatedSquadCount}");
            }

            GUILayout.Space(20);

            if (GUILayout.Button("执行分队划分", GUILayout.Height(35)))
            {
                ExecuteDividing();
            }
        }

        private int EstimateSquadCount(GameObject parent)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
            }

            if (children.Count == 0) return 0;

            bool[] visited = new bool[children.Count];
            int squadCount = 0;

            for (int i = 0; i < children.Count; i++)
            {
                if (!visited[i])
                {
                    squadCount++;
                    FloodFillVisit(children, visited, i, neighborThreshold);
                }
            }

            return squadCount;
        }

        private void ExecuteDividing()
        {
            if (targetParentObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择队伍父物体", "确定");
                return;
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in targetParentObject.transform)
            {
                children.Add(child.gameObject);
            }

            if (children.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "选择的物体没有子物体", "确定");
                return;
            }

            bool[] visited = new bool[children.Count];
            List<List<GameObject>> squads = new List<List<GameObject>>();

            for (int i = 0; i < children.Count; i++)
            {
                if (!visited[i])
                {
                    List<GameObject> squad = new List<GameObject>();
                    FloodFillCollect(children, visited, i, neighborThreshold, squad);
                    squads.Add(squad);
                }
            }

            Transform sourceTransform = targetParentObject.transform;

            for (int i = 0; i < squads.Count; i++)
            {
                string squadName = $"分队{i + 1}";
                GameObject squadParent = new GameObject(squadName);
                squadParent.transform.SetParent(sourceTransform);
                squadParent.transform.localPosition = Vector3.zero;
                squadParent.transform.localRotation = Quaternion.identity;
                squadParent.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(squadParent, "创建分队父物体");

                foreach (GameObject member in squads[i])
                {
                    Undo.SetTransformParent(member.transform, squadParent.transform, "移动到分队");
                    member.transform.localPosition = member.transform.localPosition;
                    member.transform.localRotation = member.transform.localRotation;
                    member.transform.localScale = member.transform.localScale;
                }
            }

            EditorUtility.DisplayDialog("成功", $"已将 {squads.Count} 个分队划分完成！", "确定");
        }

        private void FloodFillCollect(List<GameObject> allObjects, bool[] visited, int startIndex, float threshold, List<GameObject> result)
        {
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIndex);
            visited[startIndex] = true;

            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();
                result.Add(allObjects[currentIndex]);

                for (int i = 0; i < allObjects.Count; i++)
                {
                    if (!visited[i])
                    {
                        float distance = Vector3.Distance(
                            allObjects[currentIndex].transform.position,
                            allObjects[i].transform.position
                        );

                        if (distance <= threshold)
                        {
                            visited[i] = true;
                            queue.Enqueue(i);
                        }
                    }
                }
            }
        }

        private void FloodFillVisit(List<GameObject> allObjects, bool[] visited, int startIndex, float threshold)
        {
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIndex);
            visited[startIndex] = true;

            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();

                for (int i = 0; i < allObjects.Count; i++)
                {
                    if (!visited[i])
                    {
                        float distance = Vector3.Distance(
                            allObjects[currentIndex].transform.position,
                            allObjects[i].transform.position
                        );

                        if (distance <= threshold)
                        {
                            visited[i] = true;
                            queue.Enqueue(i);
                        }
                    }
                }
            }
        }
    }
}