using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Configs.Editor
{
    public static class SFConfigsEditorExtensions
    {
        private static readonly Dictionary<Type, Dictionary<ISFConfig, string>> _configInstancesByType = new();
        private static Dictionary<string, Dictionary<int, string[]>> _nodeIdLookupByType = new();

        [MenuItem("Tools/SFramework/Refresh Configs")]
        [InitializeOnLoadMethod]
        public static void RefreshConfigs()
        {
            EditorUtility.DisplayProgressBar("SFramework Configs", "Refreshing configuration data. Please wait...", 0f);

            _configInstancesByType.Clear();
            _nodeIdLookupByType.Clear();

            // Cache config types for performance
            var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && type.IsClass && typeof(ISFConfig).IsAssignableFrom(type))
                .ToArray();

            const int progressUpdateStep = 10;
            for (var i = 0; i < configTypes.Length; i++)
            {
                var configType = configTypes[i];
                _configInstancesByType.Add(configType, FindConfigsInternal(configType));

                if (i % progressUpdateStep == 0 || i == configTypes.Length - 1)
                {
                    EditorUtility.DisplayProgressBar(
                        "SFramework Configs",
                        $"Refreshing configuration data for {configType.Name}...",
                        Mathf.InverseLerp(0, configTypes.Length, i)
                    );
                }
            }

            foreach (var (configType, configInstances) in _configInstancesByType)
            {
                var allNodeIds = new List<string>();
                foreach (var (configInstance, _) in configInstances)
                {
                    if (configInstance is not ISFNodesConfig nodesConfig)
                        continue;

                    if (nodesConfig.Children != null)
                    {
                        nodesConfig.Children.FindAllPaths(out var childNodeIds);

                        for (var j = 0; j < childNodeIds.Count; j++)
                        {
                            var childNodeId = childNodeIds[j];
                            var fullNodeId = string.Join("/", nodesConfig.Id, childNodeId);
                            allNodeIds.Add(fullNodeId);
                        }
                    }
                    else
                    {
                        allNodeIds.Add(nodesConfig.Id);
                    }
                }

                _nodeIdLookupByType.TryAdd(configType.Name, SplitStringsIntoDictionary(allNodeIds.ToArray()));
            }

            EditorUtility.ClearProgressBar();
        }

        static Dictionary<int, string[]> SplitStringsIntoDictionary(string[] paths)
        {
            var result = new Dictionary<int, List<string>>();

            // Ensure that "-" is included in the level 0
            result[0] = new List<string> { "-" };

            foreach (var path in paths)
            {
                string[] parts = path.Split('/');
                for (int i = 0; i < parts.Length; i++)
                {
                    string partialPath = string.Join("/", parts.Take(i + 1));

                    if (!result.ContainsKey(i))
                    {
                        result[i] = new List<string> { "-" };
                    }

                    if (!result[i].Contains(partialPath))
                    {
                        result[i].Add(partialPath);
                    }
                }
            }

            // Convert List<string> to string[] in the dictionary
            return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        public static string[] GetPaths(string type, int indent)
        {
            if (_nodeIdLookupByType.TryGetValue(type, out var d))
            {
                if (indent == -1)
                {
                    indent = d.Keys.Last();
                }

                if (d.TryGetValue(indent, out var result))
                {
                    return result.ToArray();
                }
            }

            return Array.Empty<string>();
        }

        public static void ReformatConfigs(bool jsonIndented)
        {
            if (!SFConfigsSettings.TryGetInstance(out var settings)) return;
            if (settings.ConfigsPaths == null) return;

            foreach (var configsPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(configsPath))
                {
                    SFDebug.Log(LogType.Error, "SFConfigs Path is empty. Check SFramework/Resources folder and adjust settings.");
                    return;
                }

                var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
                {
                    configsPath
                });

                if (assetsGuids == null || assetsGuids.Length == 0)
                {
                    SFDebug.Log(LogType.Warning, "No Configs Found");
                    return;
                }

                foreach (var assetsGuid in assetsGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                    var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                    var repository = JObject.Parse(text);
                    repository["Version"] = ToUnixTime(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture);
                    System.IO.File.WriteAllText(path, repository.ToString(jsonIndented ? Formatting.Indented : Formatting.None));
                }
            }
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        
        
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnixTime(this long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
        
        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        private static Dictionary<ISFConfig, string> FindConfigsInternal(Type type)
        {
            var configs = new Dictionary<ISFConfig, string>();
            if (!SFConfigsSettings.TryGetInstance(out var settings)) return configs;
            if (settings.ConfigsPaths == null) return configs;

            foreach (var configsPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(configsPath))
                {
                    SFDebug.Log(LogType.Error, "SFConfigs Path is empty. Check SFramework/Resources folder and adjust settings.");
                    return configs;
                }

                var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
                {
                    configsPath
                });

                if (assetsGuids == null || assetsGuids.Length == 0)
                {
                    SFDebug.Log(LogType.Warning, "Missing Config: {0}", type.Name);
                    return configs;
                }

                var regex = new Regex("(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", RegexOptions.Compiled);

                foreach (var assetsGuid in assetsGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                    var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                    text = regex.Replace(text, "$1");
                    if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                    {
                        var repository = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                        if (repository == null) continue;
                        configs.TryAdd(repository, path);
                    }
                }
            }

            return configs;
        }

        private static HashSet<ISFConfig> FindConfigs(Type type)
        {
            var configs = new HashSet<ISFConfig>();

            if (!_configInstancesByType.TryGetValue(type, out var _configs)) return configs;
            foreach (var (config, _) in _configs)
            {
                configs.Add(config);
            }

            return configs;
        }

        public static Dictionary<ISFConfig, string> FindConfigsWithPaths(Type type)
        {
            var configs = new Dictionary<ISFConfig, string>();

            if (!_configInstancesByType.TryGetValue(type, out var _configs)) return configs;

            foreach (var (config, path) in _configs)
            {
                configs.Add(config, path);
            }

            return configs;
        }

        private static void FindAllPaths(this ISFConfigNode[] nodes, out List<string> paths)
        {
            var ids = new HashSet<string>();
            paths = new List<string>();

            foreach (var root in nodes)
            {
                var childPaths = GetChildPaths(root, "");

                if (childPaths == null) continue;

                foreach (var path in childPaths)
                {
                    ids.Add(path);
                }
            }


            foreach (var id in ids)
            {
                var split = id.Split("/");
                var path = string.Empty;
                var childLevel = split.Length;

                for (var i = 0; i < childLevel; i++)
                {
                    path += split[i];
                    if (i < childLevel - 1)
                    {
                        path += "/";
                    }
                }

                if (string.IsNullOrWhiteSpace(path)) continue;

                paths.Add(path);
            }
        }

        private static List<string> GetChildPaths(ISFConfigNode node, string path)
        {
            var paths = new List<string>();

            path += node.Id;

            if (node.Children == null)
            {
                paths.Add(path);
                return paths;
            }

            if (node.Children.Length == 0)
            {
                paths.Add(path);
                return paths;
            }

            foreach (var child in node.Children)
            {
                var childPaths = GetChildPaths(child, path + "/");
                if (childPaths == null) continue;
                paths.AddRange(childPaths);
            }

            return paths;
        }
    }
}