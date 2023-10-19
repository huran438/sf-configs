using System;
using System.Linq;
using SFramework.Configs.Runtime;
using SFramework.Core.Editor;
using UnityEditor;

namespace SFramework.Configs.Editor
{
    [Serializable]
    public sealed class SFConfigsGenerationTool : ISFEditorTool
    {
        [MenuItem("Edit/SFramework/Generate Repositories Scripts")]
        private static void GenerateScripts()
        {
            EditorUtility.DisplayProgressBar("Scripts Generation", "Wait...", 0);
            
            var databaseCodeGenerator = new SFDatabaseCodeGenerator();

            foreach (var type in GetInheritedClasses())
            {
                var repositories = SFConfigsEditorExtensions.FindRepositories(type);
                foreach (ISFConfigsGenerator repository in repositories)
                {
                    if(repository == null) continue;
                    repository.GetGenerationData(out var generationData);
                    databaseCodeGenerator.Generate(generationData);
                }
            }
            
       
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.ClearProgressBar();
        }
        
        private static Type[] GetInheritedClasses()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && typeof(ISFConfigsGenerator).IsAssignableFrom(t))
                .ToArray();
        }

        public string Title => "Core";
    }
    
    
}