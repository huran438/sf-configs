using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
            
            var configTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (!t.IsAbstract && t.IsClass && typeof(ISFConfig).IsAssignableFrom(t))
                        configTypes.Add(t);
                }
            }
            int total = configTypes.Count;
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
            var resultSets = new Dictionary<int, HashSet<string>>();
            resultSets[0] = new HashSet<string> { "-" };
            var sb = new StringBuilder(64);

            foreach (var path in paths)
            {
                var parts = path.Split('/');
                sb.Clear();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append('/');
                    sb.Append(parts[i]);
                    var partialPath = sb.ToString();

                    if (!resultSets.TryGetValue(i, out var set))
                    {
                        set = new HashSet<string> { "-" };
                        resultSets[i] = set;
                    }
                    set.Add(partialPath);
                }
            }

            var result = new Dictionary<int, string[]>(resultSets.Count);
            foreach (var kvp in resultSets)
            {
                var arr = new string[kvp.Value.Count];
                kvp.Value.CopyTo(arr);
                result[kvp.Key] = arr;
            }
            return result;
        }

        public static string[] GetNodePaths(string typeName, int indentLevel)
        {
            if (!_nodePathsByType.TryGetValue(typeName, out var d))
                return null;

            if (indentLevel == -1)
            {
                int maxLevel = -1;
                foreach (var key in d.Keys)
                    if (key > maxLevel) maxLevel = key;
                indentLevel = maxLevel;
            }

            return d.TryGetValue(indentLevel, out var result) ? result : null;
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

                foreach (var assetsGuid in assetsGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                    var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                    text = _jsonWhitespaceRegex.Replace(text, "$1");
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

        private static ICollection<ISFConfig> FindConfigs(Type type)
        {
            return _configsByType.TryGetValue(type, out var configs) ? configs.Keys : Array.Empty<ISFConfig>();
        }

        public static IReadOnlyDictionary<ISFConfig, string> FindConfigsWithPaths(Type type)
        {
            return _configsByType.TryGetValue(type, out var configs) ? configs : EmptyConfigDict;
        }

        private static readonly Dictionary<ISFConfig, string> EmptyConfigDict = new();

        private static void FindAllPaths(this ISFConfigNode[] nodes, out List<string> paths)
        {
            var ids = new HashSet<string>();
            var sb = new StringBuilder(64);

            foreach (var root in nodes)
            {
                GetChildPathsOptimized(root, sb, ids);
            }

            paths = new List<string>(ids.Count);
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                    paths.Add(id);
            }
        }

        private static void GetChildPathsOptimized(ISFConfigNode node, StringBuilder sb, HashSet<string> results)
        {
            int startLen = sb.Length;
            sb.Append(node.Id);

            if (node.Children == null || node.Children.Length == 0)
            {
                results.Add(sb.ToString());
            }
            else
            {
                foreach (var child in node.Children)
                {
                    sb.Append('/');
                    GetChildPathsOptimized(child, sb, results);
                    sb.Length = startLen + node.Id.Length;
                }
            }

            sb.Length = startLen;
        }
    }
}