using System;
using SFramework.Core.Runtime;
using UnityEngine;

namespace SFramework.Repositories.Runtime
{
    [Serializable]
    public class SFRepositorySettings : SFProjectSettings<SFRepositorySettings>
    {
        public string RepositoriesPath => repositoriesPath;
        
        [SerializeField]
        private string repositoriesPath = "Assets";
    }
}