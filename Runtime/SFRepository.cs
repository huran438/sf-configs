using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Repositories.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFRepository : ISFRepository
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public abstract ISFNode[] Nodes { get; }
    }
}