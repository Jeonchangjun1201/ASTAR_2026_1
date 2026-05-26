using KSY.Networks;
using KSY.Shared.Packets;
using MemoryPack;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.S2C_PlayerResponseBroadcastPacket)]
    [MemoryPackable]
    public partial class S2C_PlayerResponseBroadCastPacket : IPacket
    {
        public string PlayerName { get; set; }
    }
}
