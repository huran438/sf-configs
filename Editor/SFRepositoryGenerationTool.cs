using System;
using System.Linq;
using SFramework.Core.Editor;
using SFramework.Repositories.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Repositories.Editor
{
    [Serializable]
    public sealed class SFRepositoryGenerationTool : ISFEditorTool
    {
        [MenuItem("Edit/SFramework/Generate Repositories Scripts")]
        private static void GenerateScripts()
        {
            EditorUtility.DisplayProgressBar("Scripts Generation", "Wait...", 0);
            
            var databaseCodeGenerator = new SFDatabaseCodeGenerator();

            foreach (var type in GetInheritedClasses())
            {
                var repositories = SFEditorExtensions.FindRepositories(type);
                foreach (ISFRepositoryGenerator repository in repositories)
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
                .Where(t => t.IsClass && typeof(ISFRepositoryGenerator).IsAssignableFrom(t))
                .ToArray();
        }

        public string Title => "Core";
    }
    
    
}