using System;

namespace KSY.Networks
{
    public interface KSY_IDIContainer : IAsyncDisposable
    {
        TInstance GetInstance<TInstance>() where TInstance : class;
        object GetInstance(Type type);
    }
}

