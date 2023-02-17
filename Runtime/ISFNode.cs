using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    public interface ISFNode
    {
        [JsonIgnore]
        public string _Name { get; }
        
        [JsonIgnore]
        public ISFNode[] Nodes { get; }
    }
}