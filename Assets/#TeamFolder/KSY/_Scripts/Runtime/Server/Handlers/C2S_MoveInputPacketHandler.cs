using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Servers.Handlers
{
    [PacketHandler(typeof(C2S_MoveInputPacket))]
    public class C2S_MoveInputPacketHandler : IPacketHandler<C2S_MoveInputPacket>
    {
        private readonly GameManager gameManager = null;
        private readonly GameServer gameServer = null;

        public C2S_MoveInputPacketHandler(GameManager gameManager, GameServer gameServer)
        {
            this.gameManager = gameManager;
            this.gameServer = gameServer;
        }

        ValueTask IPacketHandler<C2S_MoveInputPacket>.HandlePacket(Session session, C2S_MoveInputPacket packet)
        {
            string playerID = gameServer.GetPlayerName(session);
            if (string.IsNullOrEmpty(playerID) == true)
                return new ValueTask();

            PlayerDataDTO playerData = gameManager.GetPlayerData(playerID);
            Player player = null;

            if (player == null)
                return new ValueTask();

            player.MovementComponent.SetMoveDirection(packet.MoveInput);

            S2C_MoveInputBroadcastPacket broadcastPacket = new S2C_MoveInputBroadcastPacket()
            {
                PlayerId = playerID,
                Position = player.MovementComponent.MyTransform.position,
                MoveInput = packet.MoveInput
            };
            gameServer.Send(broadcastPacket);

            return new ValueTask();
        }
    }
}