using System;
using System.Collections.Generic;
using System.Reflection;

namespace KSY.Networks
{
    public static class Builder
    {
        public static KSY_PacketSerializer Build(Assembly[] assemblies)
        {
            KSY_PacketSerializer packetSerializer = new KSY_PacketSerializer
            {
                packetIDMap = new Dictionary<Type, ushort>(),
                factories = new Dictionary<ushort, Func<ArraySegment<byte>, KSY_IPacketDispatcher>>
            }
        }
    }
    public class KSY_PacketSerializer 
    {

    }
}


