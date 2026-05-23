using KSY.Networks;
using MemoryPack;
using UnityEngine;

namespace KSY.Shared.Packets
{
    [Packet((ushort)EPacketType.C2S_MoveInputPacket)]
    [MemoryPackable]
    public partial class C2S_MoveInputPacket : IPacket
    {
        public Vector3 MoveInput { get; set; }
    }
}

