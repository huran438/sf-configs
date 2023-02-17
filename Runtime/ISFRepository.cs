using Newtonsoft.Json;

namespace SFramework.Repositories.Runtime
{
    public interface ISFRepository
    {
        [JsonIgnore]
        public string _Name { get; }
        
        public string Type { get; }
        
        [JsonIgnore]
        public ISFNode[] Nodes { get; }
    }
}