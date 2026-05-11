using System;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class KSY_NetworkObject : KSY_IDIContainer, IAsyncDisposable
    {
        private readonly KSY_DIContainer diContainer;

        public KSY_NetworkObject(KSY_INetworkObjectBuilder builder)
        {
            diContainer = builder.GetDIContainer();
        }

        public TInstance GetInstance<TInstance>() where TInstance : class
        {
            return diContainer.GetInstance<TInstance>();
        }

        public object GetInstance(Type type)
        {
            return diContainer.GetInstance(type);
        }
        
        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return diContainer.DisposeAsync(); 
        }
    }
}
