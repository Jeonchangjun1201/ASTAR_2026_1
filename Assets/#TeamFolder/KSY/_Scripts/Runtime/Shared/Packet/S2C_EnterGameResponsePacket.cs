using KSY.Networks;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.S2C_EnterGameResponsePacket)]
    public class S2C_EnterGameResponsePacket : IPacket
    {
        public string PlayerID { get; set; }
        public Dictionary<string, UnitDataDTO> Players { get; set; }
    }
}
