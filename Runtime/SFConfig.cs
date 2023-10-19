using System;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFConfig : ISFConfig
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public abstract ISFConfigNode[] Nodes { get; }
    }
}