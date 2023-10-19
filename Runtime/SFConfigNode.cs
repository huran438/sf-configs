using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFConfigNode : ISFConfigNode
    {
        [SerializeField, JsonIgnore]
        private string _name;
        public string Name
        {
            get => _name;
            set => _name = value;
        }
        public abstract ISFConfigNode[] Nodes { get; }
    }
}