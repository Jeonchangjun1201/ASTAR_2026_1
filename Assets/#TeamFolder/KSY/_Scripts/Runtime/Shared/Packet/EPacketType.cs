namespace KSY.Shared.Packets
{
    public enum EPacketType
    {
        C2S_EnterRoomRequestPacket,
        S2C_GameStartBroadCastPacket,

        C2S_PlayerResponsePacket,
        S2C_PlayerResponseBroadcastPacket,

        C2S_MoveInputPacket,
        S2C_MoveInputBroadcastPacket,
    }
}
