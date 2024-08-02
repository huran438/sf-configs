﻿using System;
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
        private HashSet<ISFNodesConfig> _configs;

        public SFIdAttributeDrawer()
        {
            _configs = new HashSet<ISFNodesConfig>();
        }

        private bool CheckAndLoadDatabase(Type type)
        {
            if (_configs.Count != 0) return true;
            _configs = SFConfigsEditorExtensions.FindConfigs<ISFNodesConfig>(type);
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

            if (!CheckAndLoadDatabase(sfTypeAttribute.Type)) return;

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;


            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                property.stringValue = string.Empty;
            }

            var paths = new List<string> { "-" };

            foreach (var repository in _configs)
            {
                repository.Children.FindAllPaths(out var ids, sfTypeAttribute.Indent);

                if (sfTypeAttribute.Indent == 0)
                {
                    paths.Add(repository.Id);
                }
                else
                {
                    foreach (var id in ids)
                    {
                        paths.Add(string.Join("/", repository.Id, id));
                    }
                }
            }
            
            if (!string.IsNullOrWhiteSpace(property.stringValue) && !paths.Contains(property.stringValue))
            {
                GUI.backgroundColor = Color.red;
                property.stringValue = EditorGUI.TextField(position, property.stringValue);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                var name = paths.Contains(property.stringValue)
                    ? property.stringValue
                    : paths[0];

                var _index = paths.IndexOf(name);

                EditorGUI.BeginChangeCheck();

                if (_index == 0)
                {
                    GUI.backgroundColor = Color.red;
                }

                _index = EditorGUI.Popup(position, _index, paths.ToArray());

                GUI.backgroundColor = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = _index == 0 ? string.Empty : paths.ElementAt(_index);
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}