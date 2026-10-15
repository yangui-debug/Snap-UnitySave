using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArtOfWar_II_Art.Integration.Editor.Scripts
{
    public class SpecificShaderVariantAdder : EditorWindow
    {
        private ShaderVariantCollection _collection;
        private Shader _shader;
        private PassType _passType;
        private string _keywords;

        // 添加编辑器窗口
        [MenuItem("Tools/Specific Shader Variant Adder")]
        public static void ShowWindow()
        {
            GetWindow<SpecificShaderVariantAdder>("Specific Shader Variant Adder");
        }

        void OnGUI()
        {
            _collection = (ShaderVariantCollection) EditorGUILayout.ObjectField("Collection", _collection, typeof(ShaderVariantCollection), false);
            _shader = (Shader) EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
            _passType = (PassType) EditorGUILayout.EnumPopup("Pass Type", _passType);
            _keywords = EditorGUILayout.TextField("Keywords", _keywords);

            if (GUILayout.Button("Add Specific Variant") && _collection && _shader)
            {
                AddSpecificVariantToCollection(_collection, _shader, _passType, _keywords.Split(' '));
            }
        }

        private void AddSpecificVariantToCollection(ShaderVariantCollection collection, Shader shader, PassType passType, string[] keywords)
        {
            // 创建着色器变体
            ShaderVariantCollection.ShaderVariant variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
            collection.Add(variant);

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log("Specific variant added to Shader Variant Collection");
        }
    }
}