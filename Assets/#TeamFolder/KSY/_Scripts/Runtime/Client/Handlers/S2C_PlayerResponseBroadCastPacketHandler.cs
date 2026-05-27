using KSY.Networks;
using KSY.Shared;
using KSY.Utility;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Clients
{
    [PacketHandler(typeof(S2C_PlayerResponseBroadCastPacket))]
    public class S2C_PlayerResponseBroadCastPacketHandler : IPacketHandler<S2C_PlayerResponseBroadCastPacket>
    {
        private readonly GameManager gameManager = null;
        private readonly DataTableManager dataTableManager = null;

        public S2C_PlayerResponseBroadCastPacketHandler(GameManager gameManager, GameClient gameClient, DataTableManager dataTableManager)
        {
            this.gameManager = gameManager;
            this.dataTableManager = dataTableManager;
        }

        ValueTask IPacketHandler<S2C_PlayerResponseBroadCastPacket>.HandlePacket(Session session, S2C_PlayerResponseBroadCastPacket packet)
        {
            Player playerPrefab = dataTableManager.gameConfigTable.GetPlayerPrefab();
            string playerName = packet.PlayerName;
            Vector3 position = packet.Position;
            Player player = Object.Instantiate(playerPrefab, position, Quaternion.identity);
            gameManager.AddPlayer(playerName, player);

            if(playerName == gameManager.MyPlayerName)
            {
                InputManager.EnableInput<PlayerInputReader>();
                Player myPlayer = gameManager.GetPlayer(playerName);
                myPlayer.gameObject.AddComponent<PlayerInputComponent>();
            }

            CustomLog.Log("클라이언트에서 플레이어를 생성합니다.");

            InputManager.CanInput = true;
            return new ValueTask();
        }
    }
}