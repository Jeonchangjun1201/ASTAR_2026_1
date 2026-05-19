using KSY.Networks;
using KSY.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KSY.Servers
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
            string playerID = Guid.NewGuid().ToString();
            gameServer.AddPlayer(playerID, session);

            Unit unitPrefab = dataTableManager.gameConfigTable.GetUnitPrefab();
            Unit unit = Object.Instantiate(unitPrefab, Vector3.zero, Quaternion.identity);
            unit.Initialize(playerID);
            gameManager.AddPlayer(playerID, unit);

            Dictionary<string, UnitDataDTO> players = new Dictionary<string, UnitDataDTO>();
            gameManager.ForEachPlayer((otherPlayerID, otherPlayer) => {
                players[otherPlayerID] = new CreateUnitDataDTO(otherPlayer).unitData;
            });

            S2C_EnterGameResponsePacket responsePacket = new S2C_EnterGameResponsePacket()
            {
                PlayerID = playerID,
                Players = players,
            };
            session.SendAsync(responsePacket);

            S2C_EnterGameBroadcastPacket broadcastPacket = new S2C_EnterGameBroadcastPacket()
            {
                PlayerID = playerID,
                UnitData = new CreateUnitData(unit).unitData
            };
            gameServer.Send(broadcastPacket, (sessionID, session) => sessionID != playerID);

            return new ValueTask();
        }
    }
}