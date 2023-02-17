using SFramework.Core.Runtime;

namespace SFramework.Repositories.Runtime
{
    public interface ISFRepositoryGenerator
    {
        void GetGenerationData(out SFGenerationData[] generationData);
    }
}