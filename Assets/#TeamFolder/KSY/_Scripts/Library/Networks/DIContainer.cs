using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class DIContainer : IDIContainer, IAsyncDisposable
    {
        private readonly Dictionary<Type, object> instances;

        public DIContainer()
        {
            instances = new Dictionary<Type, object>();
        }

        #region IDIContainer
        public TInstance GetInstance<TInstance>() where TInstance : class
        {
            return GetInstance(typeof(TInstance)) as TInstance;
        }
        public object GetInstance(Type type)
        {
            if (instances.TryGetValue(type, out object value))
                return value;
            
            object[] array = (from x in instances 
                              where type.IsAssignableFrom(x.Key)
                              select x.Value).ToArray();

            if (array.Length == 0)
                return null;

            return array[0];
        }
        #endregion

        public void AddInstance<TInstance>(TInstance instance) where TInstance : class
        {
            AddInstance(typeof(TInstance), instance);
        }
        public void AddInstance(Type type, object instance)
        {
            if(type.IsValueType)
            {
                throw new InvalidOperationException("Cannot register an instance of " + type.FullName + " in the DIContainer because it is not a reference event. Only reference-event instance can be registered.");
            }

            instances[type] = instance;
        }
        public async ValueTask DisposeAsync()
        {
            HashSet<object> disposed = new HashSet<object>();
            foreach (object value in instances.Values)
            {
                if(disposed.Add(value))
                {
                    if(value is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
