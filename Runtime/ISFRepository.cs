using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    public interface ISFRepository : ISFNode
    {
        [JsonProperty(Order = -20)]
        public int Version { get; set; }
        
        [JsonProperty(Order = -50)]
        public string Type { get; set; }
    }
}