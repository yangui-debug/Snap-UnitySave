#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArtOfWar_II_Art.Integration.Scripts.Builder;
using ArtOfWar_II_Art.Integration.Scripts.Builder.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public class SceneGridData
    {
        public Vector3Int source;
    }

    [ExecuteInEditMode]
    public class SceneBuilder : MonoBehaviour
    {
        [SerializeField] private int fontSize = 10;

        private MGEditorSetting setting;

        private Dictionary<Vector3Int, ObjectPlacement> objects;
        private Dictionary<Vector3Int, SceneGridData> grids;
        private Transform container;

        private const string prefabPath = "Assets/ArtOfWar-II_Art/Scene/NewWorld/Prefabs";

        private void Awake()
        {
            EditorSceneManager.OpenScene("Assets/ArtOfWar-II_Art/Scene/NewWorld/NewWorld.unity", OpenSceneMode.Additive);
        }

        public void OnDrawGizmos()
        {
            DrawGrids();
        }

        private void DrawGrids()
        {
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.fontSize = fontSize;

            foreach (KeyValuePair<Vector3Int, SceneGridData> pair in grids)
            {
                if (pair.Value.source == pair.Key)
                {
                    style.normal.textColor = Color.red;
                    Vector3 labelPos = MGEditorCalc.CalcWorldPosition(pair.Key, setting);
                    Handles.Label(labelPos, $"{pair.Key}", style);
                }
            }
        }

        private void OnEnable()
        {
            setting = MGEditorSetting.BuildIn();
            grids = new Dictionary<Vector3Int, SceneGridData>();
            objects = new Dictionary<Vector3Int, ObjectPlacement>();

            container = GameObject.Find("BuilderContainer").transform;
            Refresh();

            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            Refresh();
        }

        private void Refresh()
        {
            objects.Clear();
            grids.Clear();
            foreach (ObjectPlacement placement in container.GetComponentsInChildren<ObjectPlacement>())
            {
                Vector3Int p = MGEditorCalc.PlaceToIntPos(placement.transform.position, placement.size, setting);
                placement.position = p;
                objects[p] = placement;

                Foreach3(p, placement.size, pv => { grids[pv] = new SceneGridData {source = p}; });

                placement.transform.position = MGEditorCalc.CalcPlacePosition(p, placement.size, setting);
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            SceneView.RepaintAll();

            HandleBuildOp();

            DrawGUI();
        }

        private void HandleBuildOp()
        {
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 point = hit.point;
                point.y = 0;

                Vector3Int p = MGEditorCalc.CalcHitPosition(point, setting);
                Vector3 wp = MGEditorCalc.CalcPlacePosition(p, Vector3Int.one, setting);

                Handles.color = Color.green;
                Handles.DrawWireCube(wp, Vector3.one);

                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.LeftControl)
                    {
                        // add
                        TryAdd(p);
                    }
                    else if (e.keyCode == KeyCode.Z)
                    {
                        // remove
                        TryRemove(p);
                    }
                }
            }
        }

        private void TryAdd(Vector3Int p)
        {
            GameObject prefab = GetSelectPrefab();
            if (prefab && prefab.TryGetComponent(out ObjectPlacement placement))
            {
                if (All(p, placement.size, pv => !grids.ContainsKey(pv)) ||
                    grids.TryGetValue(p, out SceneGridData data) && !objects.ContainsKey(data.source))
                {
                    Object newObject = PrefabUtility.InstantiatePrefab(prefab);
                    Undo.RegisterCreatedObjectUndo(newObject, newObject.name);

                    Foreach3(p, placement.size, pv => { grids[pv] = new SceneGridData {source = p}; });

                    if (newObject is GameObject newObj && newObj.TryGetComponent(out ObjectPlacement instancing))
                    {
                        instancing.transform.position = MGEditorCalc.CalcPlacePosition(p, placement.size, setting);
                        instancing.transform.SetParent(container);
                        instancing.position = p;
                        objects[p] = instancing;
                    }
                }
            }
        }

        private void TryRemove(Vector3Int p)
        {
            if (grids.TryGetValue(p, out SceneGridData data) && objects.TryGetValue(data.source, out ObjectPlacement placement))
            {
                Foreach3(data.source, placement.size, pv => { grids.Remove(pv); });
                Undo.DestroyObjectImmediate(placement.gameObject);
            }
        }

        private void DrawGUI()
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1.0f, 1.0f, 1.0f, 1f);

            Handles.BeginGUI();

            GUILayout.BeginVertical("Box", GUILayout.Width(300), GUILayout.Height(260));

            // 在面板内添加文案

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;

            GUILayout.Label("基本操作：\n" +
                            "# 添加\n" +
                            "1. 选中想要创建的Prefab资源(Project视图中)\n" +
                            "2. 使鼠标焦点回到Scene视图(鼠标右键点击Scene视图即可)\n" +
                            "3. 长按 LeftControl 键, 鼠标在格子内滑动即可自动创建\n\n" +
                            "# 删除\n" +
                            "1. 长按 LeftAlt 键, 鼠标在格子内滑动即可自动删除\n\n" +
                            "# 其他\n" +
                            "1. 如果出现异常可以尝试点击Refresh进行刷新后再尝试\n", labelStyle);

            labelStyle.fontStyle = FontStyle.Normal;
            labelStyle.normal.textColor = Color.red;
            GUILayout.Label("# 警告\n" +
                            "1. 地图上物体以格子为单位进行放置, 不支持二分之一格子单位的坐标\n" +
                            "2. 绝对禁止工具生成单位后再手动拖拽\n" +
                            "2. 请绝对不要手动向场景中添加任何游戏物体", labelStyle);

            GUILayout.Space(20);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", GUILayout.Width(150)))
            {
                SaveConfig();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(150)))
            {
                Refresh();
            }

            if (GUILayout.Button("Fix Prefab Refer", GUILayout.Width(150)))
            {
                FixPrefabRefer();
            }

            if (GUILayout.Button("Fix Config", GUILayout.Width(150)))
            {
                FixObjectPlacementConfig();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(20);

            // 结束面板绘制
            GUILayout.EndVertical();

            GUI.backgroundColor = originalColor;
            Handles.EndGUI();
        }

        private GameObject GetSelectPrefab()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject)
            {
                PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(selectedObject);
                if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                {
                    return selectedObject;
                }
            }

            return null;
        }

        private void SaveConfig()
        {
            Refresh();

            BuiltConfig cfg = new BuiltConfig();
            cfg.mapSize = new MapSize {pos = new V3d {x = -20, y = 0, z = 20}, width = 40, height = 40};

            Dictionary<Vector3Int, int> map = MapConfigHandler.LoadConfig();
            Dictionary<int, Area> areas = new Dictionary<int, Area>();
            Dictionary<int, ItemSize> itemSizes = new Dictionary<int, ItemSize>();

            Dictionary<int, ObjectPlacement> placements = new Dictionary<int, ObjectPlacement>();

            foreach (KeyValuePair<Vector3Int, ObjectPlacement> pair in objects)
            {
                Vector3Int p = pair.Key;
                p.y = 0; // TODO
                if (!map.TryGetValue(p, out int id))
                {
                    continue;
                }

                int areaId = id / 100;
                if (!areas.TryGetValue(areaId, out Area area))
                {
                    areas[areaId] = area = new Area {areaId = areaId};
                    area.decorates = new List<BuiltItem>();
                }

                int type = pair.Value.type + 10000;
                Vector3 r = pair.Value.transform.rotation.eulerAngles;
                BuiltItem item = new BuiltItem
                {
                    type = type, level = 1,
                    pos = new V3d {x = p.x, y = p.y, z = p.z},
                    rot = new V3d {x = (int) r.x, y = (int) r.y, z = (int) r.z}
                };
                area.decorates.Add(item);

                if (!itemSizes.ContainsKey(type))
                {
                    ItemSize itemSize = new ItemSize {type = type, pos = new[] {pair.Value.size.x, pair.Value.size.z}};
                    itemSizes[type] = itemSize;
                }

                placements[type] = pair.Value;
            }

            cfg.areas = areas.Values.ToList();

            string folder = $"{Application.dataPath}/SceneFiles";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText($"{folder}/map.json", JsonUtility.ToJson(cfg));

            ItemSizeConfig sizeConfig = new ItemSizeConfig {sizes = itemSizes.Values.ToList()};
            File.WriteAllText($"{folder}/size.json", JsonUtility.ToJson(sizeConfig));

            using (StreamWriter writer = new StreamWriter(new FileStream($"{folder}/建筑物基础数值.csv", FileMode.Create, FileAccess.Write)))
            {
                foreach (ObjectPlacement value in placements.Values)
                {
                    writer.WriteLine($"-,{value.type + 10000},-,{value.size.x},{value.size.z},{value.size.y},5,0,0,0,\"6,7\"");
                }
            }

            using (StreamWriter writer = new StreamWriter(new FileStream($"{folder}/装饰物基础数值.csv", FileMode.Create, FileAccess.Write)))
            {
                foreach (ObjectPlacement value in placements.Values)
                {
                    writer.WriteLine($"-,{value.type + 10000},100,10");
                }
            }

            using (StreamWriter writer = new StreamWriter(new FileStream($"{folder}/装饰物等级数值.csv", FileMode.Create, FileAccess.Write)))
            {
                foreach (ObjectPlacement value in placements.Values)
                {
                    string costItems = "\"[{\"itemId\":121,\"itemCnt\":10}]\"";
                    string rewards = "\"[{\"itemId\":103,\"itemCnt\":100}]\"";
                    writer.WriteLine($"{value.type + 10000},1,12,{costItems},{rewards}");
                }
            }

            // boss id: 2513 2905 3007 2406 2304 2806 2708

            Debug.Log("Save Done");
        }

        private void FixObjectPlacementConfig()
        {
            foreach (ObjectPlacement placement in container.GetComponentsInChildren<ObjectPlacement>())
            {
                string fbxPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(placement.gameObject);
                ObjectPlacement prefab = AssetDatabase.LoadAssetAtPath<ObjectPlacement>(fbxPath);

                if (prefab && int.TryParse(prefab.name.Replace("D_", string.Empty), out int id))
                {
                    if (prefab.type < 10000)
                    {
                        Debug.Log($"fix {prefab.name} type: {placement.type} to {id}");
                        placement.type = id;

                        ObjectPlacement tempInstance = (ObjectPlacement) PrefabUtility.InstantiatePrefab(prefab);
                        tempInstance.type = id + 10000;

                        PrefabUtility.ApplyPrefabInstance(tempInstance.gameObject, InteractionMode.UserAction);
                        DestroyImmediate(tempInstance);
                    }
                }
                else
                {
                    Debug.LogError("error: " + placement.name);
                }
            }
        }

        private void FixPrefabRefer()
        {
            foreach (ObjectPlacement placement in container.GetComponentsInChildren<ObjectPlacement>())
            {
                string fbxPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(placement.gameObject);
                if (string.IsNullOrEmpty(fbxPath))
                {
                    fbxPath = $"{prefabPath}/{placement.name}.prefab";
                    ObjectPlacement prefab = AssetDatabase.LoadAssetAtPath<ObjectPlacement>(fbxPath);

                    Transform newInstance = ((ObjectPlacement) PrefabUtility.InstantiatePrefab(prefab)).transform;
                    Transform oldTrans = placement.transform;
                    newInstance.SetPositionAndRotation(oldTrans.position, oldTrans.rotation);
                    newInstance.SetParent(oldTrans.parent);

                    DestroyImmediate(placement.gameObject);

                    Debug.Log("fix: " + fbxPath);
                }
            }
        }

        private static bool All(Vector3Int p, Vector3Int size, Func<Vector3Int, bool> handler)
        {
            bool r = true;
            ForeachCond(p, size, pv =>
            {
                if (!handler.Invoke(pv))
                {
                    r = false;
                }

                return !r;
            });

            return r;
        }

        private static void ForeachCond(Vector3Int p, Vector3Int size, Func<Vector3Int, bool> breakCond)
        {
            if (breakCond == null)
            {
                return;
            }

            for (int x = p.x + size.x - 1; x >= p.x; x--)
            {
                for (int z = p.z - size.z + 1; z <= p.z; z++)
                {
                    bool result = breakCond.Invoke(new Vector3Int(x, p.y, z));
                    if (result)
                    {
                        return;
                    }
                }
            }
        }

        private static void Foreach3(Vector3Int p, Vector3Int size, Action<Vector3Int> handler)
        {
            if (handler == null)
            {
                return;
            }

            for (int x = p.x + size.x - 1; x >= p.x; x--)
            {
                for (int z = p.z - size.z + 1; z <= p.z; z++)
                {
                    handler.Invoke(new Vector3Int(x, p.y, z));
                }
            }
        }
    }
}
#endif