using System;

namespace SFramework.Repositories.Runtime
{
    [Serializable]
    public abstract class SFNode : ISFNode
    {
        public string _Name => Name;
        public string Name;
        public abstract ISFNode[] Nodes { get; }
    }
}