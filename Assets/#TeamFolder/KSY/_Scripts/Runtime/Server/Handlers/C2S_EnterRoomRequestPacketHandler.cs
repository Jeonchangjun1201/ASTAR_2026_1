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

        private readonly GameServer gameServer = null;

        public C2S_EnterRoomRequestPacketHandler(GameManager gameManager, GameServer gameServer, DataTableManager dataTableManager)
        {
            teamCount = 0;
            this.gameServer = gameServer;
        }

        ValueTask IPacketHandler<C2S_EnterRoomRequestPacket>.HandlePacket(Session session, C2S_EnterRoomRequestPacket packet)
        {
            CustomLog.Log("C2S_EnterRoomRequestPacketHandler : HamdlePacket", Color.orange);
            string playerName = packet.PlayerName;
            Color teamColor = teamColors[teamCount];
            PlayerDataDTO playerData = new PlayerDataDTO(teamCount, 0, teamColor);
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
                S2C_GameStartBroadCastPacket startPacket = new S2C_GameStartBroadCastPacket()
                {
                    Players = gameServer.GetPlayers(),
                    StartMiniGame = "여기다가 시작 미니게임 이름 넣기"
                };
                gameServer.Send(startPacket);
                CustomLog.Log("Send : S2C_GameStartBroadCastPacket", Color.green);
            }

            return new ValueTask();
        }
    }
}