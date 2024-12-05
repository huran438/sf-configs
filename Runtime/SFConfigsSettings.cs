using System;
using SFramework.Core.Runtime;

namespace SFramework.Configs.Runtime
{
    [Serializable]
    public class SFConfigsSettings : SFrameworkSettings<SFConfigsSettings>
    {
        public string[] ConfigsPaths;
    }
}