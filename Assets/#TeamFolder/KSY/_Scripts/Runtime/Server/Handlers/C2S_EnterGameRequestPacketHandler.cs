using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Object = UnityEngine.Object;

namespace KSY.Servers.Handlers
{
    [PacketHandler(typeof(C2S_EnterGameRequestPacket))]
    public class C2S_EnterGameRequestPacketHandler : IPacketHandler<C2S_EnterGameRequestPacket>
    {
        private readonly GameManager gameManager = null;
        private readonly GameServer gameServer = null;
        private readonly DataTableManager dataTableManager = null;

        public C2S_EnterGameRequestPacketHandler(GameManager gameManager, GameServer gameServer, DataTableManager dataTableManager)
        {
            this.gameManager = gameManager;
            this.gameServer = gameServer;
            this.dataTableManager = dataTableManager;
        }

        ValueTask IPacketHandler<C2S_EnterGameRequestPacket>.HandlePacket(Session session, C2S_EnterGameRequestPacket packet)
        {
            string playerName = packet.PlayerName;
            gameServer.AddPlayer(playerName, session);
            int playerCount = gameServer.GetPlayerCount();

            if(playerCount == 3)
                CustomLog.Log($"플레이어가 모두 접속했습니다. 게임을 시작하겠습니다.", Color.green);
            else
            {
                CustomLog.Log($"플레이어 한 명이 접속했습니다. 현재 인원수 : {playerCount}", Color.green);
                return default;
            }


            //Player unitPrefab = dataTableManager.gameConfigTable.GetPlayerPrefab();
            //Player unit = Object.Instantiate(unitPrefab, Vector3.zero, Quaternion.identity);
            //unit.Initialize(playerName);
            //gameManager.AddPlayer(playerName, unit);

            Dictionary<string, UnitDataDTO> players = new Dictionary<string, UnitDataDTO>();
            gameManager.ForEachPlayer((otherPlayerID, otherPlayer) => {
                players[otherPlayerID] = new CreatePlayerData(otherPlayer).unitData;
            });

            S2C_EnterGameResponsePacket responsePacket = new S2C_EnterGameResponsePacket()
            {
                PlayerID = playerName,
                Players = players,
            };
            session.SendAsync(responsePacket);

            S2C_EnterMainGameBroadcastPacket broadcastPacket = new S2C_EnterMainGameBroadcastPacket()
            {
                PlayerID = playerName,
                UnitData = new CreatePlayerData(unit).unitData
            };
            gameServer.Send(broadcastPacket, (sessionID, session) => sessionID != playerName);
            CustomLog.Log("Send : S2C_EnterMainGameBroadcastPacket", UnityEngine.Color.red);

            return new ValueTask();
        }
    }
}