namespace KSY.Shared.Packets
{
    public enum EPacketType
    {
        C2S_EnterRoomRequestPacket,
        S2C_EnterRoomAnswerPacket,
        S2C_GameStartBroadCastPacket,
        S2C_EnterGameResponsePacket,
        S2C_EnterRoomBroadcastPacket,

        C2S_MoveInputPacket,
        S2C_MoveInputBroadcastPacket,
    }
}
