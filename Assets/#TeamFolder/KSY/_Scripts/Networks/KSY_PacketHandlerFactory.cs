using System;
using System.Collections.Generic;
using System.Linq;
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
                    factories = new Dictionary<Type, Func<KSY_DIContainer, KSY_IPacketHandlerBase>>(),
                    diContainer = dIContainer;
                };
                //IsAssignableFrom : 형변환이 가능한지 확인하는 메서드 (상속 관계, 인터페이스 구현 여부 등 확인)
                Type[] array = (from t in assemblies.SelectMany((Assembly a)=>a.GetTypes())
                                where typeof(KSY_IPacketHandlerBase).IsAssignableFrom(t)
            }
        }

        private KSY_DIContainer diContainer;
        private Dictionary<Type, Func<KSY_DIContainer, KSY_IPacketHandlerBase>> factories;

        private KSY_PacketHandlerFactory()
        {
        }

        public KSY_IPacketHandlerBase Create(Type packetType)
        {
            if (!factories.TryGetValue(packetType, out var value))
            {
                return null;
            }

            return value(diContainer);
        }
    } 
}
