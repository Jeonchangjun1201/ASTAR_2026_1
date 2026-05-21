using KSY.Networks;
using MemoryPack;
using System.Collections.Generic;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.S2C_EnterGameResponsePacket)]
    [MemoryPackable]
    public partial class S2C_EnterGameResponsePacket : IPacket
    {
        public string PlayerID { get; set; }
        public Dictionary<string, UnitDataDTO> Players { get; set; }
    }
}