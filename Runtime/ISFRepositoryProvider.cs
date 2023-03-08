using System;
using System.Collections.Generic;

namespace SFramework.Repositories.Runtime
{
    public interface ISFRepositoryProvider
    {
        public IEnumerable<T> GetRepositories<T>() where T : ISFRepository;
    }
}