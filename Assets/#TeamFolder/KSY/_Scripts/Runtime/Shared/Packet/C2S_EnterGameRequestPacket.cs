using KSY.Networks;
using MemoryPack;
using System.Numerics;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.C2S_EnterGameRequestPacket)]
    [MemoryPackable]
    public partial class C2S_EnterGameRequestPacket : IPacket
    {

    }
}
