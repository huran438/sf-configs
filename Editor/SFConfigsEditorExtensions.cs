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
        private static readonly Dictionary<Type, Dictionary<ISFConfig, string>> _loadedConfigs = new();
        private static Dictionary<string, Dictionary<int, string[]>> _test2 = new();

        [MenuItem("Tools/SFramework/Refresh Configs")]
        [InitializeOnLoadMethod]
        public static void RefreshConfigs()
        {
            EditorUtility.DisplayProgressBar("SFramework Configs", "Refreshing Configs Data. Please wait.", 0f);

            _loadedConfigs.Clear();
            _test2.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && t.IsClass && typeof(ISFConfig).IsAssignableFrom(t))
                .ToArray();

            for (var index = 0; index < types.Length; index++)
            {
                var type = types[index];
                _loadedConfigs.Add(type, FindConfigsInternal(type));

                EditorUtility.DisplayProgressBar("SFramework Configs", "Refreshing Configs Data. Please wait.",
                    Mathf.InverseLerp(0, types.Length, index));
            }

            foreach (var (type, configs) in _loadedConfigs)
            {
                var fullIds = new List<string>();
                foreach (var (config, _) in configs)
                {
                    if (config is not ISFNodesConfig nodesConfig) continue;

                    if (nodesConfig.Children != null)
                    {
                        nodesConfig.Children.FindAllPaths(out var ids);

                        for (var i = 0; i < ids.Count; i++)
                        {
                            var id = ids[i];
                            var finalId = string.Join("/", nodesConfig.Id, id);
                            fullIds.Add(finalId);
                        }
                    }
                    else
                    {
                        fullIds.Add(nodesConfig.Id);
                    }
                }

                _test2.TryAdd(type.Name, SplitStringsIntoDictionary(fullIds.ToArray()));
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
            if (_test2.TryGetValue(type, out var d))
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

            if (!_loadedConfigs.TryGetValue(type, out var _configs)) return configs;
            foreach (var (config, _) in _configs)
            {
                configs.Add(config);
            }

            return configs;
        }

        public static Dictionary<ISFConfig, string> FindConfigsWithPaths(Type type)
        {
            var configs = new Dictionary<ISFConfig, string>();

            if (!_loadedConfigs.TryGetValue(type, out var _configs)) return configs;

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