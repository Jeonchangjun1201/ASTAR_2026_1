using MemoryPack;
using KSY.Networks;
using UnityEngine;

namespace KSY.Shared
{
    [Packet((ushort)EPacketType.S2C_MoveInputBroadcastPacket)]
    [MemoryPackable]
    public class S2C_MoveInputBroadcastPacket : IPacket
    {
        public string PlayerId;
        public Vector2 Position;
        public Vector2 MoveInput;
    }
}
