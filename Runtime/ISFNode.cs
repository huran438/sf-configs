using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Repositories.Runtime
{
    [Preserve]
    public interface ISFNode
    {
        [JsonProperty(Order = -30)]
        public string Name { get; set; }
        
        [JsonIgnore]
        public ISFNode[] Nodes { get; }
    }
}