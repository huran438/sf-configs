using System;
using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    [Serializable]
    public abstract class SFRepository : ISFRepository
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public abstract ISFNode[] Nodes { get; }
    }
}