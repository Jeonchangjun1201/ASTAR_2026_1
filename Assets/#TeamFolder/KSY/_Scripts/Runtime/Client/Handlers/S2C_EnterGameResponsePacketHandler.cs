using Cysharp.Threading.Tasks;
using KSY.Clients;
using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Clients.Handlers
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

            await SceneManager.LoadSceneAsync(TestDefine.TEST_LOAD_SCENE_NAME, LoadSceneMode.Single);

            foreach (KeyValuePair<string, UnitDataDTO> element in packet.Players)
            {
                string playerID = element.Key;
                UnitDataDTO unitData = element.Value;

                Player unitPrefab = dataTableManager.gameConfigTable.GetPlayerPrefab();
                Player unit = Object.Instantiate(unitPrefab, unitData.Position, Quaternion.identity);
                unit.Initialize(playerID);
                gameManager.AddPlayer(playerID, unit);
            }

            InputManager.EnableInput<PlayerInputReader>();

            Player myPlayer = gameManager.GetPlayer(packet.PlayerID);
            myPlayer.gameObject.AddComponent<UnitInputComponent>();
        }
    }
}