using MemoryPack;
using KSY.Networks;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.S2C_S2C_EnterMainGameBroadcastPacket)]
    [MemoryPackable]
    public partial class S2C_EnterMainGameBroadcastPacket : IPacket
    {
        public string PlayerID { get; set; }
        public UnitDataDTO UnitData { get; set; }
    }
}
