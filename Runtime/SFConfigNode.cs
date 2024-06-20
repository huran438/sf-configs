using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFConfigNode : ISFConfigNode
    {
        public void BuildTree()
        {
            if (Children == null) return;
            foreach (var child in Children)
            {
                child.FullId = $"{Parent.Id}/{Id}/{child.Id}";
                child.Parent = this;
                child.BuildTree();
            }
        }
        
        [FormerlySerializedAs("_name")]
        [SerializeField, JsonIgnore]
        private string _id;
        
        public string Id
        {
            get => _id;
            set => _id = value;
        }
        public string FullId { get; set; }
        public ISFConfigNode Parent { get; set; }
        public abstract ISFConfigNode[] Children { get; }
    }
}