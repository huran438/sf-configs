using System.Collections.Generic;
using SFramework.Repositories.Runtime;
using UnityEditor;

namespace SFramework.Repositories.Editor
{
    public static class SFRepositorySettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            var provider = new SettingsProvider("Project/SFramework/Repository", SettingsScope.Project)
            {
                guiHandler = (_) =>
                {
                    if (!SFRepositorySettings.Instance(out var settings)) return;
                    var settingsSO = new SerializedObject(settings);
                    EditorGUILayout.PropertyField(settingsSO.FindProperty("repositoriesPath"));
                    settingsSO.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssetIfDirty(settingsSO.targetObject);
                },

                keywords = new HashSet<string>(new[] { "SF", "Repository" })
            };

            return provider;
        }
    }
}