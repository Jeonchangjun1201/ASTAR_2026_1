using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Servers
{
    [PacketHandler(typeof(C2S_PlayerResponsePacket))]
    public class C2S_PlayerResponsePacketHandler : IPacketHandler<C2S_PlayerResponsePacket>
    {
        private GameServer _gameServer;
        public C2S_PlayerResponsePacketHandler(GameServer gameServer)
        {
            CustomLog.Log("Create : S2C_EnterRoomBroadcastPacketHandler", Color.orange);
            this._gameServer = gameServer;
        }
        ValueTask IPacketHandler<C2S_PlayerResponsePacket>.HandlePacket(Session session, C2S_PlayerResponsePacket packet)
        {
            CustomLog.Log("플레이어를 서버에서 스폰합니다.");

            string playerName = packet.PlayerName;
            _gameServer.Send(new S2C_PlayerResponseBroadCastPacket()
            {
                PlayerName = playerName
            });

            return default;
        }
    }
}
