using System;
using SFramework.Core.Runtime;

namespace SFramework.Configs.Runtime
{
    [Serializable]
    public class SFConfigsSettings : SFProjectSettings<SFConfigsSettings>
    {
        public string ConfigsPath = "Assets";
    }
}