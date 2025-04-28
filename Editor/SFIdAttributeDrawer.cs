using System;
using System.Linq;
using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Configs.Editor
{
    [CustomPropertyDrawer(typeof(SFIdAttribute), true)]
    public class SFIdAttributeDrawer : PropertyDrawer
    {
        private bool _canDraw;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Use string field!");
                GUI.backgroundColor = Color.white;
                return;
            }

            var sfTypeAttribute = attribute as SFIdAttribute;

            if (sfTypeAttribute == null)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Attribute is null!");
                GUI.backgroundColor = Color.white;
                return;
            }

            var _paths = SFConfigsEditorExtensions.GetPaths(sfTypeAttribute.Type.Name, sfTypeAttribute.Indent);
            if (_paths == null || _paths.Length == 0)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Paths is empty! Current value: " + property.stringValue);
                GUI.backgroundColor = Color.white;
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                property.stringValue = string.Empty;
            }
            
            if (!string.IsNullOrWhiteSpace(property.stringValue) && !_paths.Contains(property.stringValue))
            {
                GUI.backgroundColor = Color.red;
                property.stringValue = EditorGUI.TextField(position, property.stringValue);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                var name = _paths.Contains(property.stringValue)
                    ? property.stringValue
                    : _paths[0];

                var index = Array.IndexOf(_paths, name);

                EditorGUI.BeginChangeCheck();

                if (index == 0)
                {
                    GUI.backgroundColor = Color.red;
                }

                index = EditorGUI.Popup(position, index, _paths);

                GUI.backgroundColor = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = index == 0 ? string.Empty : _paths[index];
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}