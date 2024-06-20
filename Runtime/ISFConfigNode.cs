using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfigNode
    {
        void BuildTree();
        
        [JsonProperty(Order = -30)]
        public string Id { get; set; }
        
        [JsonIgnore]
        public string FullId { get; set; }
        
        [JsonIgnore]
        public ISFConfigNode Parent { get; set; }
        
        [JsonIgnore]
        public ISFConfigNode[] Children { get; }
    }
}