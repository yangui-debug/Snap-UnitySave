using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Com.i89.AreaFaction.Editor
{
    public class AreaFactionDirectSquadAdder : EditorWindow
    {
        private List<GameObject> selectedSquads = new List<GameObject>();
        private AreaFactionMultiCampaignGrid targetGrid;
        private Color[] teamColors = new Color[]
        {
            new Color(0.85f, 0.25f, 0.2f),   // 红 (A)
            new Color(0.25f, 0.45f, 0.85f),   // 蓝 (B)
            new Color(0.3f, 0.7f, 0.3f),      // 绿 (C)
            new Color(0.8f, 0.7f, 0.2f),      // 黄 (D)
            new Color(0.7f, 0.3f, 0.7f),      // 紫 (E)
            new Color(0.2f, 0.8f, 0.8f)       // 青 (F)
        };

        [MenuItem("Tools/AreaFaction/直接选择分队添加工具")]
        public static void ShowWindow()
        {
            AreaFactionDirectSquadAdder window = GetWindow<AreaFactionDirectSquadAdder>("直接选择分队添加工具");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("直接选择分队添加工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 选择目标网格组件
            targetGrid = (AreaFactionMultiCampaignGrid)EditorGUILayout.ObjectField(
                "目标网格组件",
                targetGrid,
                typeof(AreaFactionMultiCampaignGrid),
                true
            );

            // 选择分队物体
            GUILayout.Space(10);
            GUILayout.Label("选择分队物体", EditorStyles.boldLabel);
            
            // 显示当前选择的分队
            if (selectedSquads.Count > 0)
            {
                EditorGUILayout.HelpBox($"已选择 {selectedSquads.Count} 个分队", MessageType.Info);
                for (int i = 0; i < selectedSquads.Count; i++)
                {
                    if (selectedSquads[i] != null)
                    {
                        EditorGUILayout.LabelField($"{i + 1}. {selectedSquads[i].name}");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请选择分队物体", MessageType.Info);
            }

            // 选择按钮
            if (GUILayout.Button("选择分队物体"))
            {
                // 打开对象选择窗口
                Object[] objects = Selection.objects;
                selectedSquads.Clear();
                foreach (Object obj in objects)
                {
                    if (obj is GameObject)
                    {
                        selectedSquads.Add((GameObject)obj);
                    }
                }
            }

            // 队伍颜色配置
            GUILayout.Space(10);
            GUILayout.Label("队伍颜色配置", EditorStyles.boldLabel);
            for (int i = 0; i < teamColors.Length; i++)
            {
                string teamName = ((char)('A' + i)).ToString();
                teamColors[i] = EditorGUILayout.ColorField($"队伍 {teamName}", teamColors[i]);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("添加选中的分队", GUILayout.Height(40)))
            {
                AddSelectedSquads();
            }

            if (GUILayout.Button("清除所有队伍", GUILayout.Height(30)))
            {
                ClearFactions();
            }
        }

        private void AddSelectedSquads()
        {
            if (targetGrid == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择目标网格组件", "确定");
                return;
            }

            if (selectedSquads.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请选择至少一个分队物体", "确定");
                return;
            }

            // 应用到组件
            Undo.RecordObject(targetGrid, "添加选中的分队");
            SerializedObject serializedObject = new SerializedObject(targetGrid);
            SerializedProperty factionsProp = serializedObject.FindProperty("factions");

            int addedCount = 0;

            foreach (GameObject squadObject in selectedSquads)
            {
                if (squadObject == null)
                    continue;

                // 从物体名称中提取队伍信息
                string squadName = squadObject.name;
                string factionName = squadName;
                Color factionColor = Color.white;

                // 尝试从名称中提取队伍标识（如 D_分队1 中的 D）
                if (squadName.Length > 0)
                {
                    char firstChar = squadName[0];
                    if (firstChar >= 'A' && firstChar <= 'F')
                    {
                        int teamIndex = firstChar - 'A';
                        if (teamIndex < teamColors.Length)
                        {
                            factionColor = teamColors[teamIndex];
                        }
                    }
                }

                // 创建新的阵营配置
                List<GameObject> initialCells = new List<GameObject>();
                initialCells.Add(squadObject);

                // 添加新的阵营配置
                int newIndex = factionsProp.arraySize;
                factionsProp.InsertArrayElementAtIndex(newIndex);
                SerializedProperty configProp = factionsProp.GetArrayElementAtIndex(newIndex);
                configProp.FindPropertyRelative("factionName").stringValue = factionName;
                configProp.FindPropertyRelative("baseColor").colorValue = factionColor;

                // 填充格子列表
                SerializedProperty cellsProp = configProp.FindPropertyRelative("initialCells");
                cellsProp.ClearArray();
                for (int i = 0; i < initialCells.Count; i++)
                {
                    cellsProp.InsertArrayElementAtIndex(i);
                    cellsProp.GetArrayElementAtIndex(i).objectReferenceValue = initialCells[i];
                }

                addedCount++;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetGrid);

            EditorUtility.DisplayDialog("成功", $"已添加 {addedCount} 个分队", "确定");
        }

        private void ClearFactions()
        {
            if (targetGrid == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择目标网格组件", "确定");
                return;
            }

            Undo.RecordObject(targetGrid, "清除所有队伍");
            SerializedObject serializedObject = new SerializedObject(targetGrid);
            SerializedProperty factionsProp = serializedObject.FindProperty("factions");
            factionsProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetGrid);

            EditorUtility.DisplayDialog("成功", "已清除所有队伍", "确定");
        }
    }
}