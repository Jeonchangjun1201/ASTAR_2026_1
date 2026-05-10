using System;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class KSY_NetworkObject : KSY_IDIContainer, IAsyncDisposable
    {
        private readonly KSY_DIContainer diContainer;
        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public TInstance GetInstance<TInstance>() where TInstance : class
        {
            throw new NotImplementedException();
        }

        public object GetInstance(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
