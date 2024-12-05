using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SFramework.Configs.Runtime
{
    public class SFConfigsService : ISFConfigsService
    {
        private readonly Dictionary<Type, HashSet<object>> _repositoriesByType = new();
        private readonly HashSet<ISFConfig> _configs = new();

        public UniTask Init(CancellationToken cancellationToken)
        {
            foreach (var type in GetInheritedClasses())
            {
                var textAssets = Resources.LoadAll<TextAsset>(string.Empty);

                foreach (var textAsset in textAssets)
                {
                    var text = Regex.Replace(textAsset.text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                    if (!text.StartsWith($"{{\"Type\":\"{type.Name}\"") && !text.EndsWith($"\"Type\":\"{type.Name}\"}}")) continue;
                    var config = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                    if (config == null) continue;

                    if (config is ISFNodesConfig nodesConfig)
                    {
                        nodesConfig.BuildTree();
                    }

                    if (!_repositoriesByType.ContainsKey(type))
                        _repositoriesByType[type] = new HashSet<object>();
                    _repositoriesByType[type].Add(config);
                    _configs.Add(config);
                }
            }

            return UniTask.CompletedTask;
        }

        private Type[] GetInheritedClasses()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && typeof(ISFConfig).IsAssignableFrom(t))
                .ToArray();
        }

        public void Dispose()
        {
            _repositoriesByType.Clear();
        }
        
        public IEnumerable<ISFConfig> Configs => _configs;
        
        
        public IEnumerable<T> GetConfigs<T>() where T : ISFConfig
        {
            return Configs.Cast<T>();
        }
        
        public bool TryGetConfigs<T>(out T[] configs) where T : ISFConfig
        {
            if (_repositoriesByType.TryGetValue(typeof(T), out var repo))
            {
                configs = repo.Cast<T>().ToArray();
                return true;
            }

            configs = Array.Empty<T>();
            return false;
        }
        public bool TryGetNodesConfigs<T>(out T[] configs) where T : ISFNodesConfig
        {
            if (_repositoriesByType.TryGetValue(typeof(T), out var repo))
            {
                configs = repo.Cast<T>().ToArray();
                return true;
            }

            configs = Array.Empty<T>();
            return false;
        }
        public bool TryGetGlobalConfig<T>(out T config) where T : ISFGlobalConfig
        {
            if (_repositoriesByType.TryGetValue(typeof(T), out var repo))
            {
                if (repo.Count > 0)
                {
                    config = (T) repo.FirstOrDefault();
                    return true;
                }

                config = Activator.CreateInstance<T>();
                return false;
            }

            config = Activator.CreateInstance<T>();
            return false;
        }
    }
}
