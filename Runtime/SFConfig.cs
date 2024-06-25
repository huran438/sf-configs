using System;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    [Serializable]
    public abstract class SFConfig : ISFConfig
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
        public string Type { get; set; }
        public int Version { get; set; }
        public string Id { get; set; }
        public string FullId { get; set; }
        public ISFConfigNode Parent { get; set; }
        public abstract ISFConfigNode[] Children { get; }
    }
}
