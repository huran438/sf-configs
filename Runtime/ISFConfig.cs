using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfig
    {
        [JsonProperty(Order = -438)]
        public string Type { get; set; }
        
        [JsonProperty(Order = -437)]
        public int Version { get; set; }
    }
}