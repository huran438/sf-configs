using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        #region hashSetNotValidDomain
        private static readonly HashSet<string> internalAssemblyNames = new HashSet<string>()
{
    "mscorlib",
    "System",
    "System.Core",
    "System.Security.Cryptography.Algorithms",
    "System.Net.Http",
    "System.Data",
    "System.Runtime.Serialization",
    "System.Xml.Linq",
    "System.Numerics",
    "System.Xml",
    "System.Configuration",
    "ExCSS.Unity",
    "Unity.Cecil",
    "Unity.CompilationPipeline.Common",
    "Unity.SerializationLogic",
    "Unity.TestTools.CodeCoverage.Editor",
    "Unity.ScriptableBuildPipeline.Editor",
    "Unity.Addressables.Editor",
    "Unity.ScriptableBuildPipeline",
    "Unity.CollabProxy.Editor",
    "Unity.Timeline.Editor",
    "Unity.PerformanceTesting.Tests.Runtime",
    "Unity.Settings.Editor",
    "Unity.PerformanceTesting",
    "Unity.PerformanceTesting.Editor",
    "Unity.Rider.Editor",
    "Unity.ResourceManager",
    "Unity.TestTools.CodeCoverage.Editor.OpenCover.Mono.Reflection",
    "Unity.PerformanceTesting.Tests.Editor",
    "Unity.TextMeshPro",
    "Unity.Timeline",
    "Unity.Addressables",
    "Unity.TestTools.CodeCoverage.Editor.OpenCover.Model",
    "Unity.VisualStudio.Editor",
    "Unity.TextMeshPro.Editor",
    "Unity.VSCode.Editor", 
    "UnityEditor",
    "UnityEditor.UI",
    "UnityEditor.TestRunner",
    "UnityEditor.CacheServer",
    "UnityEditor.WindowsStandalone.Extensions",
    "UnityEditor.Graphs",
    "UnityEditor.UnityConnectModule",
    "UnityEditor.UIServiceModule",
    "UnityEditor.UIElementsSamplesModule",
    "UnityEditor.UIElementsModule",
    "UnityEditor.SceneTemplateModule",
    "UnityEditor.PackageManagerUIModule",
    "UnityEditor.GraphViewModule",
    "UnityEditor.CoreModule",
    "UnityEngine",
    "UnityEngine.UI",
    "UnityEngine.XRModule",
    "UnityEngine.WindModule",
    "UnityEngine.VirtualTexturingModule",
    "UnityEngine.TestRunner",
    "UnityEngine.VideoModule",
    "UnityEngine.VehiclesModule",
    "UnityEngine.VRModule",
    "UnityEngine.VFXModule",
    "UnityEngine.UnityWebRequestWWWModule",
    "UnityEngine.UnityWebRequestTextureModule",
    "UnityEngine.UnityWebRequestAudioModule",
    "UnityEngine.UnityWebRequestAssetBundleModule",
    "UnityEngine.UnityWebRequestModule",
    "UnityEngine.UnityTestProtocolModule",
    "UnityEngine.UnityCurlModule",
    "UnityEngine.UnityConnectModule",
    "UnityEngine.UnityAnalyticsModule",
    "UnityEngine.UmbraModule",
    "UnityEngine.UNETModule",
    "UnityEngine.UIElementsNativeModule",
    "UnityEngine.UIElementsModule",
    "UnityEngine.UIModule",
    "UnityEngine.TilemapModule",
    "UnityEngine.TextRenderingModule",
    "UnityEngine.TextCoreModule",
    "UnityEngine.TerrainPhysicsModule",
    "UnityEngine.TerrainModule",
    "UnityEngine.TLSModule",
    "UnityEngine.SubsystemsModule",
    "UnityEngine.SubstanceModule",
    "UnityEngine.StreamingModule",
    "UnityEngine.SpriteShapeModule",
    "UnityEngine.SpriteMaskModule",
    "UnityEngine.SharedInternalsModule",
    "UnityEngine.ScreenCaptureModule",
    "UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule",
    "UnityEngine.ProfilerModule",
    "UnityEngine.Physics2DModule",
    "UnityEngine.PhysicsModule",
    "UnityEngine.PerformanceReportingModule",
    "UnityEngine.ParticleSystemModule",
    "UnityEngine.LocalizationModule",
    "UnityEngine.JSONSerializeModule",
    "UnityEngine.InputLegacyModule",
    "UnityEngine.InputModule",
    "UnityEngine.ImageConversionModule",
    "UnityEngine.IMGUIModule",
    "UnityEngine.HotReloadModule",
    "UnityEngine.GridModule",
    "UnityEngine.GameCenterModule",
    "UnityEngine.GIModule",
    "UnityEngine.DirectorModule",
    "UnityEngine.DSPGraphModule",
    "UnityEngine.CrashReportingModule",
    "UnityEngine.CoreModule",
    "UnityEngine.ClusterRendererModule",
    "UnityEngine.ClusterInputModule",
    "UnityEngine.ClothModule",
    "UnityEngine.AudioModule",
    "UnityEngine.AssetBundleModule",
    "UnityEngine.AnimationModule",
    "UnityEngine.AndroidJNIModule",
    "UnityEngine.AccessibilityModule",
    "UnityEngine.ARModule",
    "UnityEngine.AIModule",
    "SyntaxTree.VisualStudio.Unity.Bridge",
    "nunit.framework",
    "Newtonsoft.Json",
    "ReportGeneratorMerged",
    "Unrelated",
    "netstandard",
    "SyntaxTree.VisualStudio.Unity.Messaging"
};
        #endregion

        [MenuItem("Tools/SFramework/Refresh Configs")]
        public static void RefreshConfigs()
        {
            
            EditorUtility.DisplayProgressBar("SFramework Configs", "Refreshing configuration data. Please wait...", 0f);

            _configInstancesByType.Clear();
            _nodeIdLookupByType.Clear();

            // Cache config types for performance
            Dictionary<string, Type> configTypes;
            {
                var configType = typeof(ISFConfig);

                bool WhereTypeIsValid(Type type)
                {
                    return
                        type.IsClass &&
                        !type.IsAbstract &&
                        configType.IsAssignableFrom(type);
                }

                configTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !internalAssemblyNames.Contains(a.FullName))
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(WhereTypeIsValid)
                    .ToDictionary(type => type.Name, type => type);
            }

            FindConfigsInternal(configTypes);

            void FindNodes(KeyValuePair<Type, Dictionary<ISFConfig, string>> keyValuePair)
            {
                var allNodeIds = new List<string>();
                foreach (var (configInstance, _) in keyValuePair.Value)
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

                var splits = SplitStringsIntoDictionary(allNodeIds.ToArray());
                lock (_nodeIdLookupByType)
                {
                    _nodeIdLookupByType.TryAdd(keyValuePair.Key.Name, splits);
                }
            }

            List<Task> tasks = new List<Task>();
            foreach (var keyValue in _configInstancesByType)
            {
                tasks.Add(Task.Run(() => FindNodes(keyValue)));
            }
            Task.WaitAll(tasks.ToArray());

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
                    return result;
                }
            }

            return null;
        }

        public static void ReformatConfigs(bool jsonIndented)
        {
            foreach (var pathToConfigsFolder in GetAbsPathToConfigsFolders())
            {
                foreach (var pathToConfigFile in GetAbsPathToAllJsonSubFiles(pathToConfigsFolder))
                {
                    var text = File.ReadAllText(pathToConfigFile);
                    var repository = JObject.Parse(text);
                    repository["Version"] = ToUnixTime(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture);
                    File.WriteAllText(pathToConfigFile, 
                        repository.ToString(jsonIndented ? Formatting.Indented : Formatting.None));

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

        private static void FindConfigsInternal(Dictionary<string, Type> types)
        {
            if (!SFConfigsSettings.TryGetInstance(out var settings)) return;
            if (settings.ConfigsPaths == null) return;
            
            var regex = new Regex("(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", RegexOptions.Compiled);

            foreach (var pathToConfigsFolder in GetAbsPathToConfigsFolders())
            {
                List<Task> tasks = new List<Task>();
                foreach (var pathToConfig in GetAbsPathToAllJsonSubFiles(pathToConfigsFolder))
                {
                    tasks.Add(Task.Run(() => GetConfigFile(pathToConfig)));
                }
                Task.WaitAll(tasks.ToArray());

                void GetConfigFile(string pathToConfigFile)
                {
                    var text = File.ReadAllText(pathToConfigFile);

                    var s = "\"Type\":";
                    var first = text.IndexOf(s, 0, Mathf.Min(text.Length - s.Length, 32), StringComparison.Ordinal);
                    if (first == -1) return;
                    first = text.IndexOf("\"", first + s.Length, Mathf.Min(text.Length - s.Length, 32), StringComparison.Ordinal);
                    var end = text.IndexOf("\",", first , Mathf.Min(text.Length - first, 256), StringComparison.Ordinal);
                    if (end == -1) return;
                    var typeName = text.Substring(first+1, end - first-1);
                    
                    if (!types.TryGetValue(typeName, out var type)) return;
                    
                    text = regex.Replace(text, "$1");
                    
                    {
                        var repository = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                        if (repository == null) return;
                        
                        Dictionary<ISFConfig, string> configDictionary;
                        lock (_configInstancesByType)
                        {
                            if (!_configInstancesByType.TryGetValue(type, out configDictionary))
                            {
                                configDictionary = new();
                                _configInstancesByType.TryAdd(type, configDictionary);
                            }

                            configDictionary.TryAdd(repository, pathToConfigFile);
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetAbsPathToAllJsonSubFiles(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        private static IEnumerable<string> GetAbsPathToConfigsFolders()
        {
            if (!SFConfigsSettings.TryGetInstance(out var settings)) throw new KeyNotFoundException();
            if (settings.ConfigsPaths == null) throw new DirectoryNotFoundException();
            
            foreach (var configsPath in settings.ConfigsPaths)
            {
                if (string.IsNullOrEmpty(configsPath))
                {
                    throw new DirectoryNotFoundException("SFConfigs Path is empty. Check SFramework/Resources folder and adjust settings.");
                }
                
                yield return Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath(Application.dataPath, configsPath)));
            }
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