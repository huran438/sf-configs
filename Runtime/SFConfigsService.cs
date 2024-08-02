using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SFramework.Core.Runtime;
using UnityEngine;

namespace SFramework.Configs.Runtime
{
    public class SFConfigsService : ISFConfigsService
    {
        private readonly Dictionary<Type, HashSet<object>> _repositoriesByType = new();

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
                }
            }

            return UniTask.CompletedTask;
        }

        public IEnumerable<T> GetConfigs<T>() where T : ISFConfig
        {
            return _repositoriesByType.TryGetValue(typeof(T), out var repo) ? repo.Cast<T>().ToList() : new List<T>();
        }

        private static Type[] GetInheritedClasses()
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



    }
}
