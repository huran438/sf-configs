using Newtonsoft.Json;
using SFramework.Core.Runtime;
using UnityEngine;

namespace SFramework.Configs.Runtime
{
    [JsonConverter(typeof(SFRestrictionConverter))]
    public abstract class SFRestriction : ISFInjectable
    {
        protected SFRestriction()
        {
            if (!Application.isPlaying) return;
            this.Inject();
        }

        public abstract bool IsPasses();
    }
}