using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
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

        public static HashSet<T> FindConfigs<T>(Type type, [CanBeNull] JsonSerializerSettings jsonSerializerSettings = null) where T : ISFConfig
        {
            var configs = new HashSet<T>();

            if (!SFConfigsSettings.Instance(out var settings)) return configs;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
            {
                settings.ConfigsPath
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
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = (T)JsonConvert.DeserializeObject(text, type, jsonSerializerSettings);
                    if (repository == null) continue;
                    configs.Add(repository);
                }
            }

            return configs;
        }

        public static Dictionary<ISFConfig, string> FindConfigsWithPaths(Type type, [CanBeNull] JsonSerializerSettings jsonSerializerSettings = null)
        {
            var configs = new Dictionary<ISFConfig, string>();

            if (!SFConfigsSettings.Instance(out var settings)) return configs;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[]
            {
                settings.ConfigsPath
            });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                SFDebug.Log(LogType.Warning, "Missing Repository: {0}", type.Name);
                return configs;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = JsonConvert.DeserializeObject(text, type, jsonSerializerSettings) as ISFConfig;
                    if (repository == null) continue;
                    configs[repository] = path;
                }
            }

            return configs;
        }


        public static void FindAllPaths(this ISFConfigNode[] nodes, out List<string> paths, int targetLayer = -1)
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

                if (targetLayer > -1)
                {
                    if (childLevel < targetLayer) continue;
                    childLevel = Mathf.Clamp(childLevel, 0, targetLayer);
                }

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
