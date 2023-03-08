using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace SFramework.Repositories.Runtime
{
    public class SFRepositoryProvider : ISFRepositoryProvider
    {
        private Dictionary<Type, HashSet<object>> repositoriesByType = new();

        public SFRepositoryProvider()
        {
            foreach (var type in GetInheritedClasses())
            {
                var textAssets = Resources.LoadAll<TextAsset>(string.Empty);

                foreach (var textAsset in textAssets)
                {
                    var text = Regex.Replace(textAsset.text, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                    if (!text.StartsWith($"{{\"Type\":\"{type.Name}\"") && !text.EndsWith($"\"Type\":\"{type.Name}\"}}")) continue;
                    var repository = JsonConvert.DeserializeObject(text, type) as ISFRepository;
                    if (repository == null) continue;
                    if (!repositoriesByType.ContainsKey(type))
                        repositoriesByType[type] = new HashSet<object>();
                    repositoriesByType[type].Add(repository);
                }
            }
        }

        public IEnumerable<T> GetRepositories<T>() where T : ISFRepository
        {
            return repositoriesByType.TryGetValue(typeof(T), out var repo) ? repo.Cast<T>().ToList() : new List<T>();
        }

        private static Type[] GetInheritedClasses()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && typeof(ISFRepository).IsAssignableFrom(t))
                .ToArray();
        }
    }
}