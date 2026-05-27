using KSY.Networks;
using MemoryPack;
using UnityEngine;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.C2S_PlayerResponsePacket)]
    [MemoryPackable]
    public partial class C2S_PlayerResponsePacket : IPacket
    {
        public string PlayerName { get; set; }
        public Vector3 Position { get; set; }
    }
}
