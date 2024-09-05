using System.Collections.Generic;
using SFramework.Core.Runtime;
using UnityEditor;

namespace SFramework.Configs.Editor
{
    public static class SFConfigsSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            var provider = new SettingsProvider("Project/SFramework/Configs", SettingsScope.Project)
            {
                guiHandler = _ =>
                {
                    if (!SFCoreSettings.Instance(out var settings)) return;
                    var settingsSO = new SerializedObject(settings);
                    EditorGUILayout.PropertyField(settingsSO.FindProperty("ConfigsPath"));
                    settingsSO.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssetIfDirty(settingsSO.targetObject);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "SF", "Configs" })
            };

            return provider;
        }
    }
}