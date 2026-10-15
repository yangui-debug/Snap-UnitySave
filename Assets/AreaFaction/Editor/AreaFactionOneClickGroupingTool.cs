using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Com.i89.AreaFaction.Editor
{
    public class AreaFactionOneClickGroupingTool : EditorWindow
    {
        private GameObject sourceRootObject;
        private string[] factionNames = new string[] { "Magma_Land_01", "Green_Land_03", "Ice_Land_01", "Magma_Land_03", "Green_Land_02", "Land_Blue" };
        private float neighborThreshold = 6f;

        [MenuItem("Tools/AreaFaction/一键分组工具")]
        public static void ShowWindow()
        {
            AreaFactionOneClickGroupingTool window = GetWindow<AreaFactionOneClickGroupingTool>("一键分组工具");
            window.minSize = new Vector2(350, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("一键分组工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            sourceRootObject = (GameObject)EditorGUILayout.ObjectField(
                "源根物体",
                sourceRootObject,
                typeof(GameObject),
                true
            );

            GUILayout.Space(10);
            GUILayout.Label("队伍配置", EditorStyles.boldLabel);

            for (int i = 0; i < factionNames.Length; i++)
            {
                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = i < 26 ? ((char)('A' + i)).ToString() : $"队伍{i + 1}";
                    factionNames[i] = EditorGUILayout.TextField($"队伍 {label}", factionNames[i]);
                }
            }

            GUILayout.Space(10);
            neighborThreshold = EditorGUILayout.FloatField("相邻阈值", neighborThreshold);
            EditorGUILayout.HelpBox("相邻阈值：判断两个物体是否相邻的距离标准", MessageType.Info);

            GUILayout.Space(10);

            if (sourceRootObject != null)
            {
                int childCount = CountAllChildren(sourceRootObject);
                EditorGUILayout.LabelField($"当前子物体数量: {childCount}");
            }

            GUILayout.Space(20);

            if (GUILayout.Button("执行一键分组", GUILayout.Height(40)))
            {
                ExecuteOneClickGrouping();
            }
        }

        private int CountAllChildren(GameObject parent)
        {
            int count = 0;
            foreach (Transform child in parent.transform)
            {
                count++;
                count += CountAllChildren(child.gameObject);
            }
            return count;
        }

        private void ExecuteOneClickGrouping()
        {
            if (sourceRootObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择源根物体", "确定");
                return;
            }

            // 第一步：按队伍分组到A、B、C、D、E
            Dictionary<string, List<GameObject>> objectsByFaction = CategorizeChildren(sourceRootObject);
            List<GameObject> teamParents = new List<GameObject>();

            Transform sourceTransform = sourceRootObject.transform;

            // 创建队伍父物体并移动对应物体
            for (int i = 0; i < factionNames.Length; i++)
            {
                string factionName = factionNames[i];
                string parentName = i < 26 ? ((char)('A' + i)).ToString() : $"队伍{i + 1}";

                if (!objectsByFaction.ContainsKey(factionName) || objectsByFaction[factionName].Count == 0)
                {
                    continue;
                }

                GameObject parentObject = new GameObject(parentName);
                parentObject.transform.SetParent(sourceTransform);
                parentObject.transform.localPosition = Vector3.zero;
                parentObject.transform.localRotation = Quaternion.identity;
                parentObject.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(parentObject, "创建队伍父物体");

                teamParents.Add(parentObject);

                foreach (GameObject child in objectsByFaction[factionName])
                {
                    Undo.SetTransformParent(child.transform, parentObject.transform, "移动到队伍");
                    child.transform.localPosition = child.transform.localPosition;
                    child.transform.localRotation = child.transform.localRotation;
                    child.transform.localScale = child.transform.localScale;
                }
            }

            // 第二步：为每个队伍创建分队
            foreach (GameObject teamParent in teamParents)
            {
                DivideIntoSquads(teamParent, neighborThreshold);
            }

            EditorUtility.DisplayDialog("成功", "一键分组完成！", "确定");
        }

        private Dictionary<string, List<GameObject>> CategorizeChildren(GameObject root)
        {
            Dictionary<string, List<GameObject>> objectsByFaction = new Dictionary<string, List<GameObject>>();

            foreach (string fn in factionNames)
            {
                objectsByFaction[fn] = new List<GameObject>();
            }

            List<GameObject> allChildren = new List<GameObject>();
            CollectAllChildren(root, allChildren);

            foreach (GameObject child in allChildren)
            {
                string baseName = ExtractBaseName(child.name);

                if (objectsByFaction.ContainsKey(baseName))
                {
                    objectsByFaction[baseName].Add(child);
                }
            }

            return objectsByFaction;
        }

        private void DivideIntoSquads(GameObject teamParent, float threshold)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in teamParent.transform)
            {
                children.Add(child.gameObject);
            }

            if (children.Count == 0)
            {
                return;
            }

            bool[] visited = new bool[children.Count];
            List<List<GameObject>> squads = new List<List<GameObject>>();

            for (int i = 0; i < children.Count; i++)
            {
                if (!visited[i])
                {
                    List<GameObject> squad = new List<GameObject>();
                    FloodFillCollect(children, visited, i, threshold, squad);
                    squads.Add(squad);
                }
            }

            Transform teamTransform = teamParent.transform;

            for (int i = 0; i < squads.Count; i++)
            {
                string squadName = $"分队{i + 1}";
                GameObject squadParent = new GameObject(squadName);
                squadParent.transform.SetParent(teamTransform);
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
        }

        private void CollectAllChildren(GameObject parent, List<GameObject> children)
        {
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
                CollectAllChildren(child.gameObject, children);
            }
        }

        private string ExtractBaseName(string rawName)
        {
            int bracketIndex = rawName.IndexOf('(');
            if (bracketIndex > 0)
            {
                return rawName.Substring(0, bracketIndex).Trim();
            }
            return rawName;
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
    }
}