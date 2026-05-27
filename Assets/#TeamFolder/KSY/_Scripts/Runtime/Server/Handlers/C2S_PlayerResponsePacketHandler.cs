using DG.Tweening.Core.Easing;
using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Servers
{
    [PacketHandler(typeof(C2S_PlayerResponsePacket))]
    public class C2S_PlayerResponsePacketHandler : IPacketHandler<C2S_PlayerResponsePacket>
    {
        private readonly GameServer _gameServer;
        private readonly GameManager _gameManager;
        private readonly DataTableManager _dataTableManager;
        public C2S_PlayerResponsePacketHandler(GameManager gameManager, GameServer gameServer, DataTableManager dataTableManager)
        {
            CustomLog.Log("Create : S2C_EnterRoomBroadcastPacketHandler", Color.orange);
            this._gameServer = gameServer;
            this._gameManager = gameManager;
            this._dataTableManager = dataTableManager;
        }
        ValueTask IPacketHandler<C2S_PlayerResponsePacket>.HandlePacket(Session session, C2S_PlayerResponsePacket packet)
        {
            CustomLog.Log("플레이어를 서버에서 스폰합니다.");

            string playerName = packet.PlayerName;
            Vector3 position = packet.Position;
            _gameServer.Send(new S2C_PlayerResponseBroadCastPacket()
            {
                PlayerName = playerName,
                Position = position
            });

            CustomLog.Log("클라이언트에서 플레이어를 생성합니다.");
            return new ValueTask();
        }
    }
}
