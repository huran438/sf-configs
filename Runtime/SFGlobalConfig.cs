using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFGlobalConfig : ISFConfig
    {
        [SerializeField, JsonIgnore]
        private string _id;

        public string Type { get; set; }
        public int Version { get; set; }

        public string Id
        {
            get => _id;
            set => _id = value;
        }
    }
}
