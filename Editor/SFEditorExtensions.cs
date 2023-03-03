using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SFramework.Repositories.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Repositories.Editor
{
    public static partial class SFEditorExtensions
    {
        public static HashSet<ISFRepository> FindRepositories(Type type)
        {
            var _repositories = new HashSet<ISFRepository>();

            if (!SFRepositorySettings.Instance(out var settings)) return _repositories;
            
            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new []{settings.RepositoriesPath});

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Repository: {type.Name}");
                return _repositories;
            }

            foreach (var assetsGuid  in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFRepository;
                    if (repository == null) continue;
                    _repositories.Add(repository);
                }
            }

            return _repositories;
        }
        
        public static Dictionary<ISFRepository, string> FindRepositoriesWithPaths(Type type)
        {
            var _repositories = new Dictionary<ISFRepository, string>();

            if (!SFRepositorySettings.Instance(out var settings)) return _repositories;
            
            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new []{settings.RepositoriesPath});

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Repository: {type.Name}");
                return _repositories;
            }

            foreach (var assetsGuid  in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFRepository;
                    if (repository == null) continue;
                    _repositories[repository] = path;
                }
            }

            return _repositories;
        }
    }
}
