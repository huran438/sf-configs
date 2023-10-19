using System.Collections.Generic;
using SFramework.Core.Runtime;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfigsService : ISFService
    {
        public IEnumerable<T> GetRepositories<T>() where T : ISFConfig;
    }
}