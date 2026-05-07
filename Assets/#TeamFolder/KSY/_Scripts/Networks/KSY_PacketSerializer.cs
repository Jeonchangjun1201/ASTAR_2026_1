using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace KSY.Networks
{
    public class KSY_PacketSerializer
    {
        public static class Builder
        {
            public static KSY_PacketSerializer Build(Assembly[] assemblies)
            {
                KSY_PacketSerializer packetSerializer = new KSY_PacketSerializer
                {
                    packetIDMap = new Dictionary<Type, ushort>(),
                    factories = new Dictionary<ushort, Func<ArraySegment<byte>, KSY_IPacket>>()
                };
                Type[] array = (from t in assemblies.SelectMany((Assembly a) => a.GetTypes())
                                where typeof(KSY_IPacket).IsAssignableFrom(t)
                                where t.IsDefined(typeof(KSY_PacketAttribute), inherit: false)
                                where t.IsDefined(typeof(MemoryPackableAttribute), inherit: false)
                                where !t.IsAbstract && !t.IsInterface
                                select t).ToArray();
                foreach (Type packetType in array)
                {
                    KSY_PacketAttribute customAttribute = packetType.GetCustomAttribute<KSY_PacketAttribute>(inherit: false);
                    if(customAttribute != null)
                    {
                        packetSerializer.packetIDMap[packetType] = customAttribute.PacketID;
                        packetSerializer.factories[customAttribute.PacketID] = (ArraySegment<byte> packetData) => CreatePacket(packetType, packetData);
                    }
                }

                return packetSerializer;
            }

            private static KSY_IPacket CreatePacket(Type packetType, ArraySegment<byte> packetData)
            {
                return MemoryPackSerializer.Deserialize(packetType, packetData) as KSY_IPacket;
            }
        }

        private Dictionary<Type, ushort> packetIDMap;
        private Dictionary<ushort, Func<ArraySegment<byte>, KSY_IPacket>> factories;
        private KSY_PacketSerializer()
        {

        }

        //public ArrayPoolBufferWriter Seralize(KSY_IPacket packet)
        //{
        //    if (packet == null)
        //    {
        //        throw new ArgumentNullException("packet");
        //    }
        //}
    }
}


