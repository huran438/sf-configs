using System;
using System.Collections.Generic;
using SFramework.Core.Runtime;
using UnityEngine.Scripting;



namespace SFramework.Configs.Runtime
{
    [Preserve]
    public interface ISFConfigsService : ISFService
    {
        public IEnumerable<ISFConfig> Configs { get; }
        public bool TryGetConfigs<T>(out T[] configs) where T : ISFConfig;
        public bool TryGetGlobalConfig<T>(out T config) where T : ISFGlobalConfig;
        
        [Obsolete]
        public IEnumerable<T> GetConfigs<T>() where T : ISFConfig;
    }
}