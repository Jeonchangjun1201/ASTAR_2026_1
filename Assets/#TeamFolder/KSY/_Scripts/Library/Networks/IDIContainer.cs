using System;

namespace KSY.Networks
{
    public interface IDIContainer : IAsyncDisposable
    {
        TInstance GetInstance<TInstance>() where TInstance : class;
        object GetInstance(Type type);
    }
}

