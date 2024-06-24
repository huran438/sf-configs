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

            if (Parent == null)
            {
                FullId = Id;
            }
            foreach (var child in Children)
            {
                var fullId = Parent == null ? $"{Id}/{child.Id}" : $"{Parent.Id}/{Id}/{child.Id}";
                child.FullId = fullId;
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