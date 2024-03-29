using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SFramework.Configs.Runtime;
using UnityEditor;
using UnityEngine;

namespace SFramework.Configs.Editor
{
    public static class SFConfigsEditorExtensions
    {
        public static HashSet<ISFConfig> FindRepositories(Type type)
        {
            var _repositories = new HashSet<ISFConfig>();

            if (!SFConfigsSettings.Instance(out var settings)) return _repositories;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { settings.ConfigsPath });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Repository: {type.Name}");
                return _repositories;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                    if (repository == null) continue;
                    _repositories.Add(repository);
                }
            }

            return _repositories;
        }

        public static Dictionary<ISFConfig, string> FindRepositoriesWithPaths(Type type)
        {
            var _repositories = new Dictionary<ISFConfig, string>();

            if (!SFConfigsSettings.Instance(out var settings)) return _repositories;

            var assetsGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { settings.ConfigsPath });

            if (assetsGuids == null || assetsGuids.Length == 0)
            {
                Debug.LogWarning($"Missing Repository: {type.Name}");
                return _repositories;
            }

            foreach (var assetsGuid in assetsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetsGuid);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                text = Regex.Replace(text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                if (text.StartsWith($"{{\"Type\":\"{type.Name}\"") || text.EndsWith($"\"Type\":\"{type.Name}\"}}"))
                {
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                    if (repository == null) continue;
                    _repositories[repository] = path;
                }
            }

            return _repositories;
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

            path += node.Name;

            if (node.Nodes == null)
            {
                paths.Add(path);
                return paths;
            }

            if (node.Nodes.Length == 0)
            {
                paths.Add(path);
                return paths;
            }

            foreach (var child in node.Nodes)
            {
                var childPaths = GetChildPaths(child, path + "/");
                if (childPaths == null) continue;
                paths.AddRange(childPaths);
            }

            return paths;
        }
    }
}