using SFramework.Core.Runtime;

namespace SFramework.Configs.Runtime
{
    public interface ISFConfigsGenerator
    {
        void GetGenerationData(out SFGenerationData[] generationData);
    }
}