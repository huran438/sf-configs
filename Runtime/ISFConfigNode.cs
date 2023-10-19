using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfigNode
    {
        [JsonProperty(Order = -30)]
        public string Name { get; set; }
        
        [JsonIgnore]
        public ISFConfigNode[] Nodes { get; }
    }
}