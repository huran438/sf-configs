using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Repositories.Runtime
{
    [Preserve]
    public interface ISFRepository : ISFNode
    {
        [JsonProperty(Order = -20)]
        public int Version { get; set; }
        
        [JsonProperty(Order = -50)]
        public string Type { get; set; }
    }
}