using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSY.Utility;
using MemoryPack;

namespace KSY.Networks
{
    public class PacketSerializer
    {
        public static class Builder
        {
            public static PacketSerializer Build(Assembly[] assemblies)
            {
                PacketSerializer packetSerializer = new PacketSerializer
                {
                    packetIDMap = new Dictionary<Type, ushort>(),
                    factories = new Dictionary<ushort, Func<ArraySegment<byte>, IPacket>>()
                };
                Type[] array = (from t in assemblies.SelectMany((Assembly a) => a.GetTypes())
                                where typeof(IPacket).IsAssignableFrom(t)
                                where t.IsDefined(typeof(PacketAttribute), inherit: false)
                                where t.IsDefined(typeof(MemoryPackableAttribute), inherit: false)
                                where !t.IsAbstract && !t.IsInterface
                                select t).ToArray();
                foreach (Type packetType in array)
                {
                    PacketAttribute customAttribute = packetType.GetCustomAttribute<PacketAttribute>(inherit: false);
                    if(customAttribute != null)
                    {
                        packetSerializer.packetIDMap[packetType] = customAttribute.PacketID;
                        packetSerializer.factories[customAttribute.PacketID] = (ArraySegment<byte> packetData) => CreatePacket(packetType, packetData);
                    }
                }

                return packetSerializer;
            }

            private static IPacket CreatePacket(Type packetType, ArraySegment<byte> packetData)
            {
                return MemoryPackSerializer.Deserialize(packetType, packetData) as IPacket;
            }
        }

        private Dictionary<Type, ushort> packetIDMap;
        private Dictionary<ushort, Func<ArraySegment<byte>, IPacket>> factories;

        private PacketSerializer()
        {
        }

        public ArrayPoolBufferWriter Serialize(IPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");
            
            Type type = packet.GetType();

            if (!packetIDMap.TryGetValue(type, out ushort id))
                throw new InvalidOperationException(type.FullName + " PacketID not found");
            
            ArrayPoolBufferWriter bufferWriter = new ArrayPoolBufferWriter();
            try
            {
                sbyte ushortBit = 2;
                //BinaryPrimitives : 'Primitives'는 '원시의','초기의'라는 의미로 특정 엔디안 방식으로 데이터를 읽고 쓸 때 도움을 준다.
                BinaryPrimitives.WriteUInt16LittleEndian(bufferWriter.GetSpan(ushortBit), 0);
                bufferWriter.Advance(ushortBit);
                BinaryPrimitives.WriteUInt16LittleEndian(bufferWriter.GetSpan(ushortBit), id);
                bufferWriter.Advance(ushortBit);
                MemoryPackSerializer.Serialize(type, in bufferWriter, packet);

                int writtenCount = bufferWriter.WrittenCount;
                if (writtenCount > ushort.MaxValue)
                {
                    CustomLog.LogError("Packet Serialization");
                    throw new InvalidProgramException($"Packet is too large. Size: {writtenCount}, Max: {ushort.MaxValue}");
                }
                BinaryPrimitives.WriteUInt16LittleEndian(bufferWriter.WrittenSegment.AsSpan(0, 2), (ushort)writtenCount);

                return bufferWriter;
            }
            catch
            {
                CustomLog.LogError("Packet Serialization");
                bufferWriter.Dispose();
                throw;
            }
        }

        public IPacket Deserialize(ArraySegment<byte> packetData)
        {
            if (packetData.Count < 2)
            {
                return null;
            }

            ushort key = BitConverter.ToUInt16(packetData.Array, packetData.Offset);
            if (!factories.TryGetValue(key, out var value))
            {
                return null;
            }

            return value(new ArraySegment<byte>(packetData.Array, packetData.Offset + 2, packetData.Count - 2));
        }
    }
}


