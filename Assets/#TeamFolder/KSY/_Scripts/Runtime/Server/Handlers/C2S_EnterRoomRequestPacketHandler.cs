using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Servers.Handlers
{
    [PacketHandler(typeof(C2S_EnterRoomRequestPacket))]
    public class C2S_EnterRoomRequestPacketHandler : IPacketHandler<C2S_EnterRoomRequestPacket>
    {
        private static int teamCount = 0;
        private Color[] teamColors = { Color.red, Color.yellow, Color.green, Color.blue };

        private readonly GameManager _gameManager = null;
        private readonly GameServer gameServer = null;
        private readonly DataTableManager dataTableManager = null;

        public C2S_EnterRoomRequestPacketHandler(GameManager gameManager, GameServer gameServer, DataTableManager dataTableManager)
        {
            teamCount = 0;
            this._gameManager = gameManager;
            this.gameServer = gameServer;
            this.dataTableManager = dataTableManager;
        }

        ValueTask IPacketHandler<C2S_EnterRoomRequestPacket>.HandlePacket(Session session, C2S_EnterRoomRequestPacket packet)
        {
            CustomLog.Log("C2S_EnterRoomRequestPacketHandler : HamdlePacket", Color.orange);
            string playerName = packet.PlayerName;
            Color teamColor = teamColors[teamCount];
            PlayerDataDTO playerData = new PlayerDataDTO(playerName, teamCount, 0, teamColor);
            gameServer.AddPlayer(playerName, session, playerData);
            int playerCount = gameServer.GetPlayerCount();
            teamCount++;

            if (playerCount != 4)
            {
                CustomLog.Log($"플레이어 한 명이 접속했습니다. 접속한 플레이어 : {playerName}, 현재 인원수 : {playerCount}", Color.green);
            }
            else
            {
                CustomLog.Log($"플레이어 한 명이 접속했습니다. 접속한 플레이어 : {playerName}, 현재 인원수 : {playerCount}", Color.green);
                CustomLog.Log($"플레이어가 모두 접속했습니다. 게임을 시작하겠습니다.", Color.green);

                var miniGameData = _gameManager.SelectRandomMiniGame();

                CustomLog.Log($"랜덤한 미니게임이 선택되었습니다. S2C_GameStartBroadCastPacket 패킷 전송을 시작하겠습니다.", Color.green);

                S2C_GameStartBroadCastPacket startPacket = new S2C_GameStartBroadCastPacket()
                {
                    PlayerList = gameServer.GetPlayers(),
                    StartMiniGame = miniGameData.SceneName
                };

                CustomLog.Log($"패킷 생성에 성공했습니다.", Color.green);

                gameServer.Send(startPacket);

                CustomLog.Log("Send : S2C_GameStartBroadCastPacket", Color.green);
            }

            return new ValueTask();
        }
    }
}