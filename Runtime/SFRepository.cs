using System;
using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    [Serializable]
    public abstract class SFRepository : ISFRepository
    {
        [JsonIgnore]
        public string _Name => Name;
        public string Type => GetType().Name;
        
        [JsonIgnore]
        public abstract ISFNode[] Nodes { get; }
        public string Name;
    }
}