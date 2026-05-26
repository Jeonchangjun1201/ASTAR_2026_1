using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Clients.Handlers
{
    [PacketHandler(typeof(S2C_GameStartBroadCastPacket))]
    public class S2C_GameStartBroadCastPacketHandler : IPacketHandler<S2C_GameStartBroadCastPacket>
    {
        private GameManager _gameManager;
        public S2C_GameStartBroadCastPacketHandler(GameManager gameManager)
        {
            CustomLog.Log("Create : S2C_EnterRoomBroadcastPacketHandler", Color.orange);
            this._gameManager = gameManager;
        }

        ValueTask IPacketHandler<S2C_GameStartBroadCastPacket>.HandlePacket(Session session, S2C_GameStartBroadCastPacket packet)
        {
            CustomLog.Log("S2C_EnterRoomBroadcastPacketHandler : HandlePacket", Color.orange);

            var players = packet.Players;
            foreach(var element in players)
                _gameManager.AddPlayer(element.Key, element.Value);

            SceneManager.LoadScene("KSY_MiniGameSelect");
            return new ValueTask();
        }
    }
}