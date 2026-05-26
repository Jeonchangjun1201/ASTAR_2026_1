using KSY.Networks;
using KSY.Shared.Packets;
using MemoryPack;
using System.Collections.Generic;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.S2C_GameStartBroadCastPacket)]
    [MemoryPackable]
    public partial class S2C_GameStartBroadCastPacket : IPacket
    {
        public List<PlayerDataDTO> PlayerList { get; set; }
        public string StartMiniGame { get; set; }
    }
}
