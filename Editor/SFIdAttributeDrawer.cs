using System;
using System.Collections.Generic;
using System.Linq;
using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Configs.Editor
{
    [CustomPropertyDrawer(typeof(SFIdAttribute), true)]
    public class SFIdAttributeDrawer : PropertyDrawer
    {
        private readonly HashSet<ISFNodesConfig> _configs = new HashSet<ISFNodesConfig>();
        private readonly List<string> _paths = new List<string>();
        private bool _canDraw;

        private bool CheckAndLoadDatabase(Type type)
        {
            if (_configs.Count != 0) return true;

            foreach (var config in SFConfigsEditorExtensions.FindConfigs<ISFNodesConfig>(type))
            {
                _configs.Add(config);
            }

            return _configs.Count != 0;
        }

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

            if (!CheckAndLoadDatabase(sfTypeAttribute.Type)) return;

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                property.stringValue = string.Empty;
            }
            
            _paths.Clear();
            _paths.Add("-");

            foreach (var config in _configs)
            {
                config.Children.FindAllPaths(out var ids, sfTypeAttribute.Indent);

                if (sfTypeAttribute.Indent == 0)
                {
                    _paths.Add(config.Id);
                }
                else
                {
                    foreach (var id in ids)
                    {
                        _paths.Add(string.Join("/", config.Id, id));
                    }
                }
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

                var index = _paths.IndexOf(name);

                EditorGUI.BeginChangeCheck();

                if (index == 0)
                {
                    GUI.backgroundColor = Color.red;
                }

                index = EditorGUI.Popup(position, index, _paths.ToArray());

                GUI.backgroundColor = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = index == 0 ? string.Empty : _paths.ElementAt(index);
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
