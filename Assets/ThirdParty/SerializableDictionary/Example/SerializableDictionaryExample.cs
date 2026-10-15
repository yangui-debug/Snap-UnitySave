using System.Collections.Generic;
using UnityEngine;

public class SerializableDictionaryExample : MonoBehaviour
{
    // The dictionaries can be accessed through a property
    [SerializeField] StringStringDictionary m_stringStringDictionary = default;

    public IDictionary<string, string> StringStringDictionary
    {
        get => m_stringStringDictionary;
        set => m_stringStringDictionary.CopyFrom(value);
    }

    [SerializeField] private ObjectColorDictionary m_objectColorDictionary = default;
    [SerializeField] private StringColorArrayDictionary m_objectColorArrayDictionary = default;

    void Reset()
    {
        // access by property
        StringStringDictionary = new Dictionary<string, string>() {{"first key", "value A"}, {"second key", "value B"}, {"third key", "value C"}};
        m_objectColorDictionary = new ObjectColorDictionary() {{gameObject, Color.blue}, {this, Color.red}};
    }
}