using SFramework.Core.Runtime;

namespace SFramework.Configs.Runtime
{
    public abstract class SFNodeModel<T> : ISFNodeModel<T> where T : SFConfigNode
    {
        protected SFNodeModel(T node)
        {
            this.Inject();
            Node = node;
        }
        
        public T Node { get; }
        public abstract void Dispose();
    }
}
