using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace SFramework.Configs.Runtime
{
    public class SFConfigsService : ISFConfigsService
    {
        private Dictionary<Type, HashSet<object>> repositoriesByType = new();
        
        SFConfigsService()
        {
            foreach (var type in GetInheritedClasses())
            {
                var textAssets = Resources.LoadAll<TextAsset>(string.Empty);

                foreach (var textAsset in textAssets)
                {
                    var text = Regex.Replace(textAsset.text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                    if (!text.StartsWith($"{{\"Type\":\"{type.Name}\"") && !text.EndsWith($"\"Type\":\"{type.Name}\"}}")) continue;
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFConfig;
                    if (repository == null) continue;
                    if (!repositoriesByType.ContainsKey(type))
                        repositoriesByType[type] = new HashSet<object>();
                    repositoriesByType[type].Add(repository);
                }
            }
        }

        public IEnumerable<T> GetRepositories<T>() where T : ISFConfig
        {
            return repositoriesByType.TryGetValue(typeof(T), out var repo) ? repo.Cast<T>().ToList() : new List<T>();
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
            // TODO release managed resources here
        }

   
    }
}