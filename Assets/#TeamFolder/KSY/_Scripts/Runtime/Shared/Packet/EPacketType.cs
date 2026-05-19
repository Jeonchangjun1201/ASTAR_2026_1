namespace KSY.Shared.Packets
{
    public enum EPacketType
    {
        C2S_EnterGameRequestPacket,
        S2C_EnterGameResponsePacket,
        S2C_EnterGameBroadcastPacket,

        C2S_MoveInputPacket,
        S2C_MoveInputBroadcastPacket,
    }
}
