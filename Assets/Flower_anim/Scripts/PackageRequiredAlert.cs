using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR

[ExecuteInEditMode]
public class PackageRequiredAlert : MonoBehaviour
{
    public static bool IsPackageInstalled(string packageId)
    {
        if (!File.Exists("Packages/manifest.json"))
            return false;

        string jsonText = File.ReadAllText("Packages/manifest.json");
        return jsonText.Contains(packageId);
    }

    [ContextMenu("1")]
    private void Awake()
    {
        if (!IsPackageInstalled("com.unity.formats.alembic"))
        {
            EditorUtility.DisplayDialog("Alembic not INSTALLED " , "Install Alembic Importer in Package Manager to work proprely!!!", "Ok, Commander!!!");
        }
    }
}
#endif
