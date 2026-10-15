using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Com.i89.AreaFaction.Editor
{
    public class AreaFactionGroupingTool : EditorWindow
    {
        private GameObject sourceRootObject;
        private string[] factionNames = new string[] { "Magma_Land_01", "Green_Land_03", "Ice_Land_01", "Magma_Land_03", "Green_Land_02", "Land_Blue" };
        private GameObject[] parentObjects = new GameObject[6];
        private bool[] foldouts = new bool[6] { true, true, true, true, true, true };

        [MenuItem("Tools/AreaFaction/队伍分组工具")]
        public static void ShowWindow()
        {
            AreaFactionGroupingTool window = GetWindow<AreaFactionGroupingTool>("队伍分组工具");
            window.minSize = new Vector2(400, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("队伍分组工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            sourceRootObject = (GameObject)EditorGUILayout.ObjectField(
                "源根物体",
                sourceRootObject,
                typeof(GameObject),
                true
            );

            GUILayout.Space(10);
            GUILayout.Label("队伍名称配置", EditorStyles.boldLabel);

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
            GUILayout.Label("分组预览", EditorStyles.boldLabel);

            if (sourceRootObject != null)
            {
                Dictionary<string, List<GameObject>> objectsByFaction = CategorizeChildren(sourceRootObject);

                for (int i = 0; i < factionNames.Length; i++)
                {
                    string factionName = factionNames[i];
                    int count = objectsByFaction.ContainsKey(factionName) ? objectsByFaction[factionName].Count : 0;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string label = i < 26 ? ((char)('A' + i)).ToString() : $"队伍{i + 1}";
                        EditorGUILayout.LabelField($"  {label}: {factionName}", $"{count} 个物体");
                    }
                }

                int uncategorized = 0;
                foreach (var kvp in objectsByFaction)
                {
                    bool isDefined = false;
                    foreach (string fn in factionNames)
                    {
                        if (fn == kvp.Key)
                        {
                            isDefined = true;
                            break;
                        }
                    }
                    if (!isDefined)
                    {
                        uncategorized += kvp.Value.Count;
                    }
                }
                if (uncategorized > 0)
                {
                    EditorGUILayout.LabelField($"  未分类: {uncategorized} 个物体");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请选择源根物体以查看预览", MessageType.Info);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("执行分组", GUILayout.Height(35)))
            {
                ExecuteGrouping();
            }
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

        private void ExecuteGrouping()
        {
            if (sourceRootObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择源根物体", "确定");
                return;
            }

            Dictionary<string, List<GameObject>> objectsByFaction = CategorizeChildren(sourceRootObject);

            Transform sourceTransform = sourceRootObject.transform;

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
                Undo.RegisterCreatedObjectUndo(parentObject, "创建分组父物体");

                parentObjects[i] = parentObject;

                foreach (GameObject child in objectsByFaction[factionName])
                {
                    Undo.SetTransformParent(child.transform, parentObject.transform, "移动到分组");
                    child.transform.localPosition = child.transform.localPosition;
                    child.transform.localRotation = child.transform.localRotation;
                    child.transform.localScale = child.transform.localScale;
                }
            }

            EditorUtility.DisplayDialog("成功", "分组完成！", "确定");
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
    }
}