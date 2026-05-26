using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Servers.Handlers
{
    [PacketHandler(typeof(C2S_EnterRoomRequestPacket))]
    public class C2S_EnterRoomRequestPacketHandler : IPacketHandler<C2S_EnterRoomRequestPacket>
    {
        private static int teamCount = 0;
        private Color[] teamColors = { Color.red, Color.yellow, Color.green, Color.blue };

        private readonly GameManager gameManager = null;
        private readonly GameServer gameServer = null;
        private readonly DataTableManager dataTableManager = null;

        public C2S_EnterRoomRequestPacketHandler(GameManager gameManager, GameServer gameServer, DataTableManager dataTableManager)
        {
            teamCount = 0;
            this.gameManager = gameManager;
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
                CustomLog.Log($"여기서 시작 미니게임 씬 이름 넣기.", Color.green);

                var miniGameData = gameManager.SelectRandomMiniGame();

                CustomLog.Log($"중단점 1.", Color.beige);

                S2C_GameStartBroadCastPacket startPacket = new S2C_GameStartBroadCastPacket()
                {
                    PlayerList = gameServer.GetPlayers(),
                    StartMiniGame = miniGameData.SceneName
                };

                CustomLog.Log($"중단점 2.", Color.beige)
                    ;
                gameServer.Send(startPacket);

                CustomLog.Log("Send : S2C_GameStartBroadCastPacket", Color.green);
            }

            return new ValueTask();
        }
    }
}