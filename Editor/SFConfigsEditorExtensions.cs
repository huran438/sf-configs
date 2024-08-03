using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SFramework.Configs.Editor
{
    public static class SFConfigsEditorExtensions
    {
        public static void RegenerateConfigs(bool indented)
        {
            if (!SFConfigsSettings.Instance(out var settings)) return;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
            {
                settings.ConfigsPath
            });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning("No Configs Found");
                return;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                var repository = JObject.Parse(text);
                System.IO.File.WriteAllText(path, repository.ToString(indented ? Formatting.Indented : Formatting.None));
            }
        }

        private static Dictionary<Type, HashSet<ISFConfig>> _configsCache = new();

        private static List<Type> GetTypesImplementingInterface<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            var typesImplementingInterface = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
                typesImplementingInterface.AddRange(types);
            }

            return typesImplementingInterface;
        }

        [DidReloadScripts]
        [MenuItem("Tools/Reload Configs", false, -9999)]
        public static void ReloadCache()
        {
            _configsCache.Clear();
            var types = GetTypesImplementingInterface<ISFConfig>();

            foreach (var type in types)
            {
                _configsCache[type] = FindConfigs(type);
            }
        }

        public static HashSet<ISFConfig> FindConfigsCached(Type type)
        {
            return _configsCache.TryGetValue(type, out var result) ? result : new HashSet<ISFConfig>();
        }

        private static HashSet<ISFConfig> FindConfigs(Type type, [CanBeNull] JsonSerializerSettings jsonSerializerSettings = null)
        {
            var configs = new HashSet<ISFConfig>();

            if (!SFConfigsSettings.Instance(out var settings)) return configs;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
            {
                settings.ConfigsPath
            });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Config: {type.Name}");
                return configs;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (!text.Contains($"\"Type\":\"{type.Name}\"")) continue;
                if (JsonConvert.DeserializeObject(text, type, jsonSerializerSettings) is ISFConfig config)
                {
                    configs.Add(config);
                }
            }

            return configs;
        }

        public static Dictionary<ISFConfig, string> FindConfigsWithPaths(Type type, [CanBeNull] JsonSerializerSettings jsonSerializerSettings = null)
        {
            var repositories = new Dictionary<ISFConfig, string>();

            if (!SFConfigsSettings.Instance(out var settings)) return repositories;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
            {
                settings.ConfigsPath
            });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Config: {type.Name}");
                return repositories;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (!text.Contains($"\"Type\":\"{type.Name}\"")) continue;
                if (JsonConvert.DeserializeObject(text, type, jsonSerializerSettings) is ISFConfig config)
                {
                    repositories[config] = path;
                }
            }

            return repositories;
        }

        private static HashSet<string> _ids = new();

        public static void FindAllPaths(this ISFConfigNode[] nodes, out List<string> paths, int targetLayer = -1)
        {
            _ids.Clear();
            paths = new List<string>();

            foreach (var root in nodes)
            {
                var childPaths = GetChildPaths(root, "");
                if (childPaths != null)
                {
                    _ids.UnionWith(childPaths);
                }
            }

            foreach (var id in _ids)
            {
                var split = id.Split('/');
                var path = string.Empty;
                var childLevel = split.Length;

                if (targetLayer > -1 && childLevel >= targetLayer)
                {
                    childLevel = Mathf.Clamp(childLevel, 0, targetLayer);
                }

                for (var i = 0; i < childLevel; i++)
                {
                    if (i > 0) path += "/";
                    path += split[i];
                }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }
        }

        private static List<string> GetChildPaths(ISFConfigNode node, string path)
        {
            var paths = new List<string>
            {
                path + node.Id
            };

            if (node.Children == null || node.Children.Length == 0) return paths;

            foreach (var child in node.Children)
            {
                var childPaths = GetChildPaths(child, path + node.Id + "/");
                if (childPaths != null)
                {
                    paths.AddRange(childPaths);
                }
            }

            return paths;
        }
    }
}
