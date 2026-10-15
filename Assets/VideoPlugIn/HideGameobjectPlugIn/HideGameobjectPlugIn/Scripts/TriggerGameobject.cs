using UnityEditor;
using UnityEngine;

public class TriggerGameobject : MonoBehaviour
{
    void Start()
    {
        AddTag("TriggerGameobject");
        gameObject.tag = "TriggerGameobject";
    }
    
    private static bool TagExists(string tag)
    {
        foreach (string existingTag in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (existingTag.Equals(tag))
            {
                return true;
            }
        }
        return false;
    }

    private static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (!TagExists(tag))
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
