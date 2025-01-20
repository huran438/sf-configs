using System;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFGlobalConfig : ISFGlobalConfig
    {
        public string Type { get; set; }
        public long Version { get; set; }
    }
}
