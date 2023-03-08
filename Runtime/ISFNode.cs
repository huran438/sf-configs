using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    public interface ISFNode
    {
        [JsonProperty(Order = -30)]
        public string Name { get; set; }
        
        [JsonIgnore]
        public ISFNode[] Nodes { get; }
    }
}