using System;
using System.Reflection;

namespace KSY.Networks
{
    public abstract class NetworkObjectBuilder<TNetworkObject> : INetworkObjectBuilder where TNetworkObject : NetworkObject
    {
        public NetworkObjectBuilder() => diContainer = new DIContainer();

        protected readonly DIContainer diContainer;

        DIContainer INetworkObjectBuilder.GetDIContainer() => diContainer;

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

        protected abstract TNetworkObject OnBuild();

        public TNetworkObject Build(params Assembly[] assemblies)
        {
            diContainer.AddInstance(PacketHandlerFactory.Builder.Build(assemblies, diContainer));
            diContainer.AddInstance(PacketSerializer.Builder.Build(assemblies));
            TNetworkObject val = OnBuild();
            diContainer.AddInstance(val);
            return val;
        }
    }
}
