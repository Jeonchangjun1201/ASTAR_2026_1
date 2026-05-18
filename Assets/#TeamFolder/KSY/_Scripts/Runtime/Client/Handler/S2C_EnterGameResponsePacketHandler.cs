using Cysharp.Threading.Tasks;
using KSY.Clients;
using KSY.Networks;
using KSY.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Servers
{
    [PacketHandler(typeof(S2C_EnterGameResponsePacket))]
    public class S2C_EnterGameResponsePacketHandler : IPacketHandler<S2C_EnterGameResponsePacket>
    {
        private readonly GameManager gameManager = null;
        private readonly DataTableManager dataTableManager = null;

        public S2C_EnterGameResponsePacketHandler(GameManager gameManager, DataTableManager dataTableManager)
        {
            this.gameManager = gameManager;
            this.dataTableManager = dataTableManager;
        }

        async ValueTask IPacketHandler<S2C_EnterGameResponsePacket>.HandlePacket(Session session, S2C_EnterGameResponsePacket packet)
        {
            ClientInstance.MyPlayerID = packet.PlayerID;

            await SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);

            foreach (KeyValuePair<string, UnitDataDTO> element in packet.Players)
            {
                string playerID = element.Key;
                UnitDataDTO unitData = element.Value;

                Unit unitPrefab = dataTableManager.gameConfigTable.GetUnitPrefab();
                Unit unit = Object.Instantiate(unitPrefab, unitData.Position, Quaternion.identity);
                unit.Initialize(playerID, unitData.Height, unitData.CurrentHP, unitData.WeaponID);
                gameManager.AddPlayer(playerID, unit);
            }

            foreach (KeyValuePair<string, ItemDataDTO> element in packet.Items)
            {
                string itemUUID = element.Key;
                ItemDataDTO itemData = element.Value;

                ItemTableRow itemTableRow = dataTableManager.itemTable.GetRow(itemData.ItemID);
                if (itemTableRow == null)
                    continue;

                ItemBase item = Object.Instantiate(itemTableRow.itemPrefab, itemData.Position, Quaternion.identity);
                item.Initialize(itemData.ItemID, itemUUID, itemData.Height);
                gameManager.AddItem(itemUUID, item);
            }

            InputManager.EnableInput<PlayerInputReader>();

            Unit myPlayer = gameManager.GetPlayer(packet.PlayerID);
            myPlayer.gameObject.AddComponent<UnitInputComponent>();
        }
    }
}