using System;
using System.Reflection;

namespace KSY.Networks
{
    public abstract class NetworkObjectBuilder<TNetworkObject> : INetworkObjectBuilder where TNetworkObject : NetworkObject
    {
        protected readonly DIContainer diContainer;
        public NetworkObjectBuilder() => diContainer = new DIContainer();

        #region INetworkObjectBuilder
        DIContainer INetworkObjectBuilder.GetDIContainer() => diContainer;
        #endregion
        
        public NetworkObjectBuilder<TNetworkObject> AddSingleton<TInstnace>(TInstnace instance) where TInstnace : class
        {
            diContainer.AddInstance(instance);
            return this;
        }        
        public NetworkObjectBuilder<TNetworkObject> AddSingleton(Type type, object instance)
        {
            diContainer.AddInstance(type, instance);
            return this;
        }
        public TNetworkObject Build(params Assembly[] assemblies)
        {
            diContainer.AddInstance(PacketHandlerFactory.Builder.Build(assemblies, diContainer));
            diContainer.AddInstance(PacketSerializer.Builder.Build(assemblies));
            TNetworkObject val = OnBuild();
            diContainer.AddInstance(val);
            return val;
        }

        protected abstract TNetworkObject OnBuild();
    }
}
