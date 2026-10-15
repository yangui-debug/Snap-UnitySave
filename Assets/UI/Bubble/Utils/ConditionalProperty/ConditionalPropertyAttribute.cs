using System;
using UnityEngine;

namespace UI.Bubble.Utils.ConditionalProperty
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                    AttributeTargets.Class | AttributeTargets.Struct)]
    public class ConditionalPropertyAttribute : PropertyAttribute
    {
        public readonly string conditionField;
        public readonly bool disable;
        public readonly bool expectedValue;

        public ConditionalPropertyAttribute(string conditionField, bool expectedValue = true, bool disable = true)
        {
            this.conditionField = conditionField;
            this.expectedValue = expectedValue;
            this.disable = disable;
        }
    }
}