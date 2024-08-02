using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfig
    {
        [JsonProperty(Order = -20)]
        public int Version { get; set; }
        
        [JsonProperty(Order = -50)]
        public string Type { get; set; }
    }
}