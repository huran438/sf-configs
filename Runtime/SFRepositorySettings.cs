using SFramework.Core.Runtime;
using UnityEngine;

namespace SFramework.Repositories.Runtime
{
    public class SFRepositorySettings : SFProjectSettings<SFRepositorySettings>
    {
        public string RepositoriesPath => repositoriesPath;
        
        [SerializeField]
        private string repositoriesPath = "Assets";
    }
}