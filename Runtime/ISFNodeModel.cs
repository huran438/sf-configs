namespace SFramework.Configs.Runtime
{
    public interface ISFNodeModel<out T> : ISFModel where T : SFConfigNode
    {
        T Node { get; }
    }
}
