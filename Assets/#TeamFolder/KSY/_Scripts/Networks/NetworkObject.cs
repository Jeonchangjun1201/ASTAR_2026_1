using System;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class NetworkObject : IDIContainer, IAsyncDisposable
    {
        private readonly DIContainer diContainer;

        public NetworkObject(INetworkObjectBuilder builder)
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
