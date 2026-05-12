using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_PacketHandlerFactory : MonoBehaviour
    {
        public static class Builder
        {
            public static KSY_PacketHandlerFactory Build(Assembly[] assemblies, KSY_DIContainer dIContainer)
            {
                KSY_PacketHandlerFactory packetHandlerFactory = new KSY_PacketHandlerFactory
                {
                    factories 
                }
            }
        }

        private KSY_DIContainer diContainer;

        private Dictionary<Type, Func<KSY_DIContainer, IPacketHandlerBase>> factories;
    } 
}
