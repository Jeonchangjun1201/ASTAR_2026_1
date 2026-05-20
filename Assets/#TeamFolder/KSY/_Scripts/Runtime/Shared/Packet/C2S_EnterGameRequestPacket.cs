using KSY.Networks;
using MemoryPack;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.C2S_EnterGameRequestPacket)]
    [MemoryPackable]
    public partial class C2S_EnterGameRequestPacket : IPacket
    {

    }
}
