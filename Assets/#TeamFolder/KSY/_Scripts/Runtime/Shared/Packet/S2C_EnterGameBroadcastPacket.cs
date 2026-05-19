using MemoryPack;
using KSY.Networks;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.S2C_EnterGameBroadcastPacket)]
    [MemoryPackable]
    public class S2C_EnterGameBroadcastPacket : IPacket
    {
        public string PlayerID { get; set; }
        public UnitDataDTO UnitData { get; set; }
    }
}
