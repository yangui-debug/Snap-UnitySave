using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PrefabSpawnerEditor : EditorWindow
    {
        private GameObject prefabToSpawn;

        [MenuItem("Tool/选中对象操作工具")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PrefabSpawnerEditor), false, "子节点生成与清空");
        }

        private void OnGUI()
        {
            GUILayout.Label("子节点预制体生成", EditorStyles.boldLabel);

            GUILayout.Space(10);
            
            GUILayout.Label("拖动预制体至这里", EditorStyles.label);
            prefabToSpawn = (GameObject)EditorGUILayout.ObjectField(prefabToSpawn, typeof(GameObject), false);

            GUILayout.Space(20);

            if (GUILayout.Button("为选择的对象生成预制体子节点"))
            {
                SpawnPrefabs();
            }

            if (GUILayout.Button("清空所选择对象的子节点"))
            {
                ClearChildNode();
            }
        }
        
        private void OnDragUpdated()
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }
        
        private void OnDragPerform()
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject)
            {
                prefabToSpawn = (GameObject)DragAndDrop.objectReferences[0];
                DragAndDrop.AcceptDrag();
                Repaint();
            }
        }

        private void SpawnPrefabs()
        {
            if (prefabToSpawn == null)
            {
                Debug.LogError("没有选择复制的预制体");
                return;
            }

            var selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.LogError("没有选择复制预制体到的对象");
                return;
            }

            foreach (var selectedObject in selectedObjects)
            {
                var spawnedPrefab = PrefabUtility.InstantiatePrefab(prefabToSpawn) as GameObject;
                if (spawnedPrefab != null)
                {
                    spawnedPrefab.transform.SetParent(selectedObject.transform);
                    spawnedPrefab.transform.localPosition = Vector3.zero;
                    spawnedPrefab.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void ClearChildNode()
        {
            var selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.LogError("没有选择需要清空子节点的对象");
                return;
            }
            
            foreach (var selectedObject in selectedObjects)
            {
                for (int i = selectedObject.transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(selectedObject.transform.GetChild(i).gameObject);
                }
            }
        }
    }
}