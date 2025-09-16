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
    [InitializeOnLoad]
    public static class SFConfigsEditorUtility
    {
        private static readonly Dictionary<Type, Dictionary<ISFConfig, string>> _configsByType = new();
        private static readonly Dictionary<string, Dictionary<int, string[]>> _nodePathsByType = new();
        private static bool _isInitialized;
        private static readonly Regex _jsonWhitespaceRegex = new("(\"(?:[^\\\"\\\\]|\\\\.)*\")|\\s+", RegexOptions.Compiled);

        static SFConfigsEditorUtility()
        {
            EditorApplication.update += EnsureInitialized;
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            RefreshConfigs();
            Debug.Log("SFConfigs Editor Initialized");
            EditorApplication.update -= EnsureInitialized;
        }


        [MenuItem("Tools/SFramework/Refresh Configs")]
        public static void RefreshConfigs()
        {
            EditorUtility.DisplayProgressBar("SFramework Configs", "Refreshing configuration data. Please wait...", 0f);
            _configsByType.Clear();
            _nodePathsByType.Clear();

            if (!SFConfigsSettings.TryGetInstance(out var settings) || settings.ConfigsPaths == null || settings.ConfigsPaths.Length == 0)
            {
                SFDebug.Log(LogType.Error, "SFConfigs Paths not set or empty.");
                EditorUtility.ClearProgressBar();
                return;
            }
            
            var allAssets = new List<(string Path, string Text)>();
            foreach (var configsPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(configsPath)) continue;
                var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { configsPath });
                if (guids == null) continue;
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var raw = AssetDatabase.LoadAssetAtPath<TextAsset>(path)?.text;
                    if (string.IsNullOrEmpty(raw)) continue;
                    allAssets.Add((path, _jsonWhitespaceRegex.Replace(raw, "$1")));
                }
            }
            
            var assetsByType = new Dictionary<string, List<(string Path, string Text)>>();
            const string typeKey = "\"Type\":\"";
            foreach (var (path, text) in allAssets)
            {
                var idx = text.IndexOf(typeKey, StringComparison.Ordinal);
                if (idx < 0) continue;
                var start = idx + typeKey.Length;
                var end = text.IndexOf('"', start);
                if (end < 0) continue;
                var typeName = text.Substring(start, end - start);
                if (!assetsByType.TryGetValue(typeName, out var list))
                    assetsByType[typeName] = list = new List<(string, string)>();
                list.Add((path, text));
            }
            
            var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && t.IsClass && typeof(ISFConfig).IsAssignableFrom(t));
            int total = configTypes.Count();
            int index = 0;
            bool createdAny = false;
            foreach (var type in configTypes)
            {
                index++;
                EditorUtility.DisplayProgressBar("SFramework Configs", $"Processing {type.Name} ({index}/{total})...", (float)index / total);

                if (assetsByType.TryGetValue(type.Name, out var entries))
                {
                    var dict = new Dictionary<ISFConfig, string>();
                    foreach (var (path, txt) in entries)
                        if (JsonConvert.DeserializeObject(txt, type) is ISFConfig inst)
                            dict[inst] = path;
                    _configsByType[type] = dict;
                }
                else
                {
                    // No configs found for this type. Create a default one and save it.
                    if (TryCreateDefaultConfig(type, settings, out var createdConfig, out var createdPath))
                    {
                        var dict = new Dictionary<ISFConfig, string>
                        {
                            [createdConfig] = createdPath
                        };
                        _configsByType[type] = dict;
                        createdAny = true;
                        SFDebug.Log(LogType.Log, $"Created default config for type {type.Name} at {createdPath}");
                    }
                    else
                    {
                        _configsByType[type] = new Dictionary<ISFConfig, string>();
                        SFDebug.Log(LogType.Warning, $"No configs found for type {type.Name} and failed to create a default config.");
                    }
                }
            }
            
            foreach (var kv in _configsByType)
            {
                var allIds = new List<string>();
                foreach (var cfg in kv.Value.Keys)
                    if (cfg is ISFNodesConfig nodes && nodes.Children != null)
                    {
                        nodes.Children.FindAllPaths(out var ids);
                        foreach (var id in ids)
                            allIds.Add($"{nodes.Id}/{id}");
                    }
                    else if (cfg is ISFNodesConfig simple && simple.Children == null)
                        allIds.Add(simple.Id);
                _nodePathsByType[kv.Key.Name] = BuildPathsByIndentLevel(allIds.ToArray());
            }

            EditorUtility.ClearProgressBar();
            if (createdAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static Dictionary<int, string[]> BuildPathsByIndentLevel(string[] paths)
        {
            var result = new Dictionary<int, List<string>>();
            
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
            
            return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        public static string[] GetNodePaths(string typeName, int indentLevel)
        {
            if (_nodePathsByType.TryGetValue(typeName, out var d))
            {
                if (indentLevel == -1)
                {
                    indentLevel = d.Keys.Last();
                }

                if (d.TryGetValue(indentLevel, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        private static bool TryCreateDefaultConfig(Type type, SFConfigsSettings settings, out ISFConfig createdConfig, out string createdPath)
        {
            createdConfig = null;
            createdPath = null;
            if (settings == null || settings.ConfigsPaths == null || settings.ConfigsPaths.Length == 0)
                return false;

            foreach (var rawPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(rawPath)) continue;
                var basePath = rawPath.Replace('\\', '/').Trim('/');
                if (!EnsureFolderExists(basePath)) continue;

                var assetPath = $"{basePath}/{type.Name}.json";
                try
                {
                    if (!(Activator.CreateInstance(type) is ISFConfig instance))
                        continue;

                    instance.Type = type.Name;
                    instance.Version = ToUnixTime(DateTime.UtcNow);

                    if (instance is ISFNodesConfig nodes && string.IsNullOrEmpty(nodes.Id))
                    {
                        nodes.Id = type.Name;
                    }

                    var json = JsonConvert.SerializeObject(instance, Formatting.Indented);
                    var relativeInsideAssets = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
                    var absolutePath = System.IO.Path.Combine(Application.dataPath, relativeInsideAssets);
                    System.IO.File.WriteAllText(absolutePath, json);
                    AssetDatabase.ImportAsset(assetPath);

                    createdConfig = instance;
                    createdPath = assetPath;
                    return true;
                }
                catch (Exception ex)
                {
                    SFDebug.Log(LogType.Error, $"Failed to create default config for {type.Name} at {basePath}: {ex}");
                }
            }

            return false;
        }

        private static bool EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return false;
            folderPath = folderPath.Replace('\\', '/').Trim('/');

            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            var parts = folderPath.Split('/');
            if (parts.Length == 0)
                return false;

            if (!string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                SFDebug.Log(LogType.Error, $"Configs path must be under 'Assets': {folderPath}");
                return false;
            }

            var current = parts[0]; // Assets
            for (int i = 1; i < parts.Length; i++)
            {
                var next = parts[i];
                var combined = $"{current}/{next}";
                if (!AssetDatabase.IsValidFolder(combined))
                {
                    AssetDatabase.CreateFolder(current, next);
                }
                current = combined;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        public static void FormatConfigs(bool indentJson)
        {
            if (!SFConfigsSettings.TryGetInstance(out var settings)) return;
            if (settings.ConfigsPaths == null) return;

            foreach (var configsPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(configsPath))
                {
                    SFDebug.Log(LogType.Error,
                        "SFConfigs Path is empty. Check SFramework/Resources folder and adjust settings.");
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
                    System.IO.File.WriteAllText(path,
                        repository.ToString(indentJson ? Formatting.Indented : Formatting.None));
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
                    SFDebug.Log(LogType.Error,
                        "SFConfigs Path is empty. Check SFramework/Resources folder and adjust settings.");
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

            if (!_configsByType.TryGetValue(type, out var _configs)) return configs;
            foreach (var (config, _) in _configs)
            {
                configs.Add(config);
            }

            return configs;
        }

        public static Dictionary<ISFConfig, string> FindConfigsWithPaths(Type type)
        {
            var configs = new Dictionary<ISFConfig, string>();

            if (!_configsByType.TryGetValue(type, out var _configs)) return configs;

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