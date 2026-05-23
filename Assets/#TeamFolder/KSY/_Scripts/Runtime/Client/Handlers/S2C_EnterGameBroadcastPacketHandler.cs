using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Clients.Handlers
{
    [PacketHandler(typeof(S2C_EnterGameBroadcastPacket))]
    public class S2C_EnterGameBroadcastPacketHandler : IPacketHandler<S2C_EnterGameBroadcastPacket>
    {
        private readonly GameManager gameManager = null;
        private readonly DataTableManager dataTableManager = null;

        public S2C_EnterGameBroadcastPacketHandler(GameManager gameManager, DataTableManager dataTableManager)
        {
            CustomLog.Log("Create : S2C_EnterGameBroadcastPacketHandler", Color.orange);
            this.gameManager = gameManager;
            this.dataTableManager = dataTableManager;
        }

        ValueTask IPacketHandler<S2C_EnterGameBroadcastPacket>.HandlePacket(Session session, S2C_EnterGameBroadcastPacket packet)
        {
            CustomLog.Log("S2C_EnterGameBroadcastPacketHandler : HandlePacket", Color.orange);
            Player unitPrefab = dataTableManager.gameConfigTable.GetPlayerPrefab();
            Player unit = Object.Instantiate(unitPrefab, packet.UnitData.Position, Quaternion.identity);
            unit.Initialize(packet.PlayerID);
            gameManager.AddPlayer(packet.PlayerID, unit);
            return new ValueTask();
        }
    }
}