using KSY.Networks;
using MemoryPack;
using UnityEngine;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.C2S_MoveInputPacket)]
    [MemoryPackable]
    public class C2S_MoveInputPacket : IPacket
    {
        public Vector2 MoveInput { get; set; }
    }
}

