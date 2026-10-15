using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Com.i89.AreaFaction.Editor
{
    [CustomEditor(typeof(AreaFactionMultiCampaignGrid))]
    public class AreaFactionMultiCampaignSquadEditor : UnityEditor.Editor
    {
        // 工具窗口类
        public class AreaFactionMultiCampaignSquadToolWindow : EditorWindow
        {
            private GameObject targetSquadObject;
            private TeamColorsAsset teamColorsAsset;
            private Color squadColor = Color.white;
            private bool useCustomColor = false;

            [MenuItem("Tools/AreaFaction/分队填充工具")]
            public static void ShowWindow()
            {
                AreaFactionMultiCampaignSquadToolWindow window = GetWindow<AreaFactionMultiCampaignSquadToolWindow>("分队填充工具");
                window.minSize = new Vector2(400, 400);
                window.AutoLoadTeamColorsAsset();
            }

            private void OnGUI()
            {
                GUILayout.Label("分队填充工具", EditorStyles.boldLabel);
                GUILayout.Space(10);

                // 选择颜色资产
                teamColorsAsset = (TeamColorsAsset)EditorGUILayout.ObjectField(
                    "队伍颜色资产",
                    teamColorsAsset,
                    typeof(TeamColorsAsset),
                    false
                );

                if (teamColorsAsset == null)
                {
                    EditorGUILayout.HelpBox("请创建或选择一个 TeamColors 资产文件", MessageType.Warning);
                    if (GUILayout.Button("创建默认颜色资产"))
                    {
                        CreateDefaultTeamColorsAsset();
                    }
                    if (GUILayout.Button("自动加载颜色资产"))
                    {
                        AutoLoadTeamColorsAsset();
                    }
                }

                GUILayout.Space(10);

                // 选择分队物体
                targetSquadObject = (GameObject)EditorGUILayout.ObjectField(
                    "分队物体",
                    targetSquadObject,
                    typeof(GameObject),
                    true
                );

                GUILayout.Space(10);

                // 小队颜色配置
                useCustomColor = EditorGUILayout.Toggle("使用自定义颜色", useCustomColor);
                
                if (useCustomColor)
                {
                    // 支持HDR的颜色选择器
                    squadColor = EditorGUILayout.ColorField(
                        new GUIContent("小队颜色", "支持HDR颜色"),
                        squadColor,
                        true, // showAlpha
                        true, // showHDR
                        true  // usePureWhite
                    );
                }
                else if (teamColorsAsset != null && targetSquadObject != null && targetSquadObject.transform.parent != null)
                {
                    string teamName = targetSquadObject.transform.parent.name;
                    Color defaultColor = teamColorsAsset.GetTeamColor(teamName);
                    EditorUtility.SetDirty(targetSquadObject.transform.parent.gameObject);
                    EditorGUILayout.ColorField(
                        new GUIContent("默认队伍颜色", "基于队伍标识的默认颜色"),
                        defaultColor,
                        true, // showAlpha
                        true, // showHDR
                        true  // usePureWhite
                    );
                }

                GUILayout.Space(10);
                EditorGUILayout.HelpBox("使用说明：\n1. 选择一个分队物体（如 分队2-Green_Land_02）\n2. 工具会自动加载颜色资产\n3. 可选择使用自定义颜色或默认队伍颜色\n4. 点击添加分队按钮", MessageType.Info);

                GUILayout.Space(20);
                if (GUILayout.Button("添加分队", GUILayout.Height(40)))
                {
                    AddSingleSquad();
                }

                if (GUILayout.Button("清除所有队伍", GUILayout.Height(30)))
                {
                    ClearFactions();
                }
            }

            private void AutoLoadTeamColorsAsset()
            {
                // 尝试从Resources文件夹加载TeamColors资产
                TeamColorsAsset asset = Resources.Load<TeamColorsAsset>("TeamColors");
                if (asset != null)
                {
                    teamColorsAsset = asset;
                }
                else
                {
                    // 如果没有找到，尝试创建默认资产
                    CreateDefaultTeamColorsAsset();
                }
            }

            private void CreateDefaultTeamColorsAsset()
            {
                TeamColorsAsset asset = ScriptableObject.CreateInstance<TeamColorsAsset>();
                
                // 设置默认颜色，强度为1.5（通过RGB值乘以1.5实现）
                asset.teamAColor = new Color(1.275f, 0.375f, 0.3f, 1f);   // 红 (A) - 强度 1.5
                asset.teamBColor = new Color(0.375f, 0.675f, 1.275f, 1f);  // 蓝 (B) - 强度 1.5
                asset.teamCColor = new Color(0.45f, 1.05f, 0.45f, 1f);     // 绿 (C) - 强度 1.5
                asset.teamDColor = new Color(1.2f, 1.05f, 0.3f, 1f);      // 黄 (D) - 强度 1.5
                asset.teamEColor = new Color(1.05f, 0.45f, 1.05f, 1f);     // 紫 (E) - 强度 1.5
                asset.teamFColor = new Color(0.3f, 1.2f, 1.2f, 1f);       // 青 (F) - 强度 1.5

                // 创建资产文件
                string path = "Assets/Resources/TeamColors.asset";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.Refresh();

                // 选择新创建的资产
                teamColorsAsset = asset;
                EditorUtility.DisplayDialog("成功", "已创建默认颜色资产", "确定");
            }

            private void AddSingleSquad()
            {
                if (targetSquadObject == null)
                {
                    EditorUtility.DisplayDialog("错误", "请选择分队物体", "确定");
                    return;
                }

                if (teamColorsAsset == null)
                {
                    // 尝试自动加载
                    AutoLoadTeamColorsAsset();
                    if (teamColorsAsset == null)
                    {
                        EditorUtility.DisplayDialog("错误", "请选择队伍颜色资产", "确定");
                        return;
                    }
                }

                // 查找场景中所有的 AreaFactionMultiCampaignGrid 组件
                AreaFactionMultiCampaignGrid[] grids = FindObjectsOfType<AreaFactionMultiCampaignGrid>();
                if (grids.Length == 0)
                {
                    EditorUtility.DisplayDialog("错误", "场景中没有 AreaFactionMultiCampaignGrid 组件", "确定");
                    return;
                }

                // 选择第一个组件进行操作
                AreaFactionMultiCampaignGrid grid = grids[0];

                // 获取父物体名称作为队伍标识
                string teamName = targetSquadObject.transform.parent != null ? targetSquadObject.transform.parent.name : "未知";
                string squadName = targetSquadObject.name;
                
                // 生成 factionName: 父物体名称_分队名称
                string factionName = $"{teamName}_{squadName}";

                // 检查是否已经存在相同名称的分队
                bool isDuplicate = false;
                int duplicateIndex = -1;
                for (int i = 0; i < grid.factions.Count; i++)
                {
                    if (grid.factions[i].factionName == factionName)
                    {
                        isDuplicate = true;
                        duplicateIndex = i;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    bool shouldAdd = EditorUtility.DisplayDialog(
                        "重复添加",
                        $"已存在 [ID:{duplicateIndex}] {factionName}，是否重复添加？",
                        "是",
                        "否"
                    );
                    if (!shouldAdd)
                    {
                        return;
                    }
                }

                // 获取颜色
                Color finalColor = useCustomColor ? squadColor : teamColorsAsset.GetTeamColor(teamName);

                // 创建初始格子列表
                List<GameObject> initialCells = new List<GameObject>();
                initialCells.Add(targetSquadObject);

                // 创建阵营配置
                AreaFactionMultiCampaignGrid.FactionConfig config = new AreaFactionMultiCampaignGrid.FactionConfig
                {
                    factionName = factionName,
                    baseColor = finalColor,
                    initialCells = initialCells
                };

                // 应用到组件
                Undo.RecordObject(grid, "添加分队");
                SerializedObject serializedObject = new SerializedObject(grid);
                SerializedProperty factionsProp = serializedObject.FindProperty("factions");
                
                // 添加到现有列表
                int newIndex = factionsProp.arraySize;
                factionsProp.InsertArrayElementAtIndex(newIndex);
                SerializedProperty configProp = factionsProp.GetArrayElementAtIndex(newIndex);
                configProp.FindPropertyRelative("factionName").stringValue = config.factionName;
                configProp.FindPropertyRelative("baseColor").colorValue = config.baseColor;

                // 填充格子列表
                SerializedProperty cellsProp = configProp.FindPropertyRelative("initialCells");
                cellsProp.ClearArray();
                for (int j = 0; j < config.initialCells.Count; j++)
                {
                    cellsProp.InsertArrayElementAtIndex(j);
                    cellsProp.GetArrayElementAtIndex(j).objectReferenceValue = config.initialCells[j];
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(grid);

                EditorUtility.DisplayDialog("成功", $"已添加分队: {factionName}", "确定");
            }

            private void ClearFactions()
            {
                // 查找场景中所有的 AreaFactionMultiCampaignGrid 组件
                AreaFactionMultiCampaignGrid[] grids = FindObjectsOfType<AreaFactionMultiCampaignGrid>();
                if (grids.Length == 0)
                {
                    EditorUtility.DisplayDialog("错误", "场景中没有 AreaFactionMultiCampaignGrid 组件", "确定");
                    return;
                }

                // 选择第一个组件进行操作
                AreaFactionMultiCampaignGrid grid = grids[0];

                // 二次确认
                bool shouldClear = EditorUtility.DisplayDialog(
                    "清除所有队伍",
                    "确定要清除所有队伍配置吗？此操作不可撤销。",
                    "确定",
                    "取消"
                );
                if (!shouldClear)
                {
                    return;
                }

                Undo.RecordObject(grid, "清除所有队伍");
                SerializedObject serializedObject = new SerializedObject(grid);
                SerializedProperty factionsProp = serializedObject.FindProperty("factions");
                factionsProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(grid);

                EditorUtility.DisplayDialog("成功", "已清除所有队伍", "确定");
            }
        }

        // 配置管理窗口
        public class CampaignConfigManagerWindow : EditorWindow
        {
            private int selectedConfigIndex = -1;
            private List<CampaignConfigAsset> configs = new List<CampaignConfigAsset>();
            private string[] configNames = new string[0];
            private string newConfigName = "剧本配置1";
            private Vector2 scrollPosition;

            [MenuItem("Tools/AreaFaction/配置管理工具")]
            public static void ShowWindow()
            {
                CampaignConfigManagerWindow window = GetWindow<CampaignConfigManagerWindow>("配置管理工具");
                window.minSize = new Vector2(400, 400);
                window.RefreshConfigs();
            }

            private void OnGUI()
            {
                GUILayout.Label("配置管理工具", EditorStyles.boldLabel);
                GUILayout.Space(10);

                // 配置下拉列表
                GUILayout.Label("选择配置", EditorStyles.boldLabel);
                
                // 自动刷新配置列表
                RefreshConfigsIfNeeded();

                selectedConfigIndex = EditorGUILayout.Popup("配置列表", selectedConfigIndex, configNames);

                GUILayout.Space(20);
                GUILayout.Label("保存新配置", EditorStyles.boldLabel);

                // 配置名称输入框
                newConfigName = EditorGUILayout.TextField("配置名称", newConfigName);

                GUILayout.Space(10);

                // 保存当前配置
                if (GUILayout.Button("保存当前配置", GUILayout.Height(30)))
                {
                    SaveCurrentConfig();
                }

                // 加载配置
                if (GUILayout.Button("加载选中配置", GUILayout.Height(30)))
                {
                    LoadSelectedConfig();
                }

                // 显示配置信息
                if (selectedConfigIndex >= 0 && selectedConfigIndex < configs.Count)
                {
                    CampaignConfigAsset config = configs[selectedConfigIndex];
                    GUILayout.Space(20);
                    GUILayout.Label("配置信息", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("名称: " + config.configName);
                    EditorGUILayout.LabelField("描述: " + config.description);
                    EditorGUILayout.LabelField("创建日期: " + config.creationDate.ToString());
                    EditorGUILayout.LabelField("阵营数量: " + config.factions.Count);
                    EditorGUILayout.LabelField("剧本阶段: " + config.campaignScript.Count);
                }
            }

            private void RefreshConfigsIfNeeded()
            {
                // 获取当前Resources文件夹中的所有配置
                var currentConfigs = Resources.LoadAll<CampaignConfigAsset>("").ToList();
                
                // 检查是否与缓存的配置不同
                if (currentConfigs.Count != configs.Count)
                {
                    RefreshConfigs();
                    return;
                }
                
                // 检查名称是否一致
                bool namesMatch = true;
                for (int i = 0; i < currentConfigs.Count; i++)
                {
                    if (i >= configs.Count || currentConfigs[i].configName != configs[i].configName)
                    {
                        namesMatch = false;
                        break;
                    }
                }
                
                if (!namesMatch)
                {
                    RefreshConfigs();
                }
            }

            private void RefreshConfigs()
            {
                // 从Resources文件夹加载所有配置
                configs = Resources.LoadAll<CampaignConfigAsset>("").ToList();
                configNames = configs.Select(c => c.configName).ToArray();
                selectedConfigIndex = Mathf.Clamp(selectedConfigIndex, -1, configs.Count - 1);
            }

            private int GetNextConfigId()
            {
                // 查找最大的ID
                int maxId = 0;
                foreach (var config in configs)
                {
                    if (config.configName.StartsWith("剧本配置"))
                    {
                        string idStr = config.configName.Substring(4);
                        if (int.TryParse(idStr, out int id))
                        {
                            maxId = Mathf.Max(maxId, id);
                        }
                    }
                }
                return maxId + 1;
            }

            private void SaveCurrentConfig()
            {
                if (string.IsNullOrWhiteSpace(newConfigName))
                {
                    EditorUtility.DisplayDialog("错误", "配置名称不能为空", "确定");
                    return;
                }

                // 查找场景中所有的 AreaFactionMultiCampaignGrid 组件
                AreaFactionMultiCampaignGrid[] grids = FindObjectsOfType<AreaFactionMultiCampaignGrid>();
                if (grids.Length == 0)
                {
                    EditorUtility.DisplayDialog("错误", "场景中没有 AreaFactionMultiCampaignGrid 组件", "确定");
                    return;
                }

                AreaFactionMultiCampaignGrid grid = grids[0];

                // 使用用户输入的名称
                string configName = newConfigName;

                // 创建新的配置资产
                CampaignConfigAsset config = ScriptableObject.CreateInstance<CampaignConfigAsset>();
                config.configName = configName;
                config.description = "用户保存的配置";
                config.creator = System.Environment.UserName;
                config.creationDate = System.DateTime.Now;

                // 保存配置
                config.SaveFromGrid(grid);

                // 保存到Resources文件夹
                string path = "Assets/Resources/" + configName + ".asset";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.Refresh();

                // 自动刷新配置列表
                RefreshConfigs();

                // 生成下一个默认名称
                newConfigName = "剧本配置" + GetNextConfigId();

                EditorUtility.DisplayDialog("成功", "配置已保存为: " + configName, "确定");
            }

            private void LoadSelectedConfig()
            {
                if (selectedConfigIndex < 0 || selectedConfigIndex >= configs.Count)
                {
                    EditorUtility.DisplayDialog("错误", "请选择要加载的配置", "确定");
                    return;
                }

                CampaignConfigAsset config = configs[selectedConfigIndex];

                // 查找场景中所有的 AreaFactionMultiCampaignGrid 组件
                AreaFactionMultiCampaignGrid[] grids = FindObjectsOfType<AreaFactionMultiCampaignGrid>();
                if (grids.Length == 0)
                {
                    EditorUtility.DisplayDialog("错误", "场景中没有 AreaFactionMultiCampaignGrid 组件", "确定");
                    return;
                }

                AreaFactionMultiCampaignGrid grid = grids[0];

                // 加载配置
                config.LoadToGrid(grid);
                EditorUtility.SetDirty(grid);

                EditorUtility.DisplayDialog("成功", "已加载配置: " + config.configName, "确定");
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorUtility.SetDirty(target);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("分队管理", EditorStyles.boldLabel);

            // 快速操作按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开分队填充工具"))
                {
                    AreaFactionMultiCampaignSquadToolWindow.ShowWindow();
                }

                if (GUILayout.Button("清除所有队伍"))
                {
                    ClearFactions();
                }
            }

            // 配置管理按钮
            GUILayout.Space(10);
            EditorGUILayout.LabelField("配置管理", EditorStyles.boldLabel);
            if (GUILayout.Button("打开配置管理工具"))
            {
                CampaignConfigManagerWindow.ShowWindow();
            }
        }

        private void ClearFactions()
        {
            AreaFactionMultiCampaignGrid grid = (AreaFactionMultiCampaignGrid)target;
            if (grid == null)
            {
                return;
            }

            // 二次确认
            bool shouldClear = EditorUtility.DisplayDialog(
                "清除所有队伍",
                "确定要清除所有队伍配置吗？此操作不可撤销。",
                "确定",
                "取消"
            );
            if (!shouldClear)
            {
                return;
            }

            Undo.RecordObject(grid, "清除所有队伍");
            SerializedProperty factionsProp = serializedObject.FindProperty("factions");
            factionsProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(grid);

            EditorUtility.DisplayDialog("成功", "已清除所有队伍", "确定");
        }
    }
}