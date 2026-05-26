using KSY.Networks;
using MemoryPack;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.C2S_EnterRoomRequestPacket)]
    [MemoryPackable]
    public partial class C2S_EnterRoomRequestPacket : IPacket
    {
        public string PlayerName { get; set; }
    }
}
