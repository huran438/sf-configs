using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFGlobalConfig : ISFGlobalConfig
    {
        public string Type { get; set; }
        public int Version { get; set; }
    }
}
