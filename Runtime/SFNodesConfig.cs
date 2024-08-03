using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFNodesConfig : ISFNodesConfig
    {
        public void BuildTree()
        {
            FullId = Id;
            if (Children == null) return;
            foreach (var child in Children)
            {
                child.FullId = $"{Id}/{child.Id}";
                child.Parent = this;
                child.BuildTree();
            }
        }
        
        [SerializeField, JsonIgnore]
        private string _id;

        public string Type { get; set; }
        public int Version { get; set; }

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public string FullId { get; set; }
        public ISFConfigNode Parent { get; set; }
        public abstract ISFConfigNode[] Children { get; }
    }
}
