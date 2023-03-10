using System.Collections.Generic;
using UnityEngine.Scripting;

namespace SFramework.Repositories.Runtime
{
    [Preserve]
    public interface ISFRepositoryProvider
    {
        public IEnumerable<T> GetRepositories<T>() where T : ISFRepository;
    }
}