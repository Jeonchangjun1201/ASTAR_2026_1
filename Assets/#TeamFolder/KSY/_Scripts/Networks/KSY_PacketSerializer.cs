using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                Type[] array = (from t in assemblies.SelectMany((Assembly a) => a.GetType())
                              where typeof(KSY_IPacket).IsAssignableFrom(t)
                              where t.IsDefined(typeof(PacketAttribute)), inherit: false)
                              where !t.IsAbstract && !t.IsInterface
                              select t).ToArray();
            }
        }

        private Dictionary<Type, ushort> packetIDMap;
        private Dictionary<ushort, Func<ArraySegment<byte>, KSY_IPacket>> factories;
        private KSY_PacketSerializer()
        {

        }

        public ArrayPoolBufferWriter Seralize(KSY_IPacket packet)
        {
            if(packet == null)
            {
                throw new ArgumentNullException("packet");
            }
        }
    }
}


