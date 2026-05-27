using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using System.Threading.Tasks;

namespace KSY.Clients.Handlers
{
    [PacketHandler(typeof(S2C_MoveInputBroadcastPacket))]
    public class S2C_MoveInputBroadcastPacketHandler : IPacketHandler<S2C_MoveInputBroadcastPacket>
    {
        private readonly GameManager gameManager = null;

        public S2C_MoveInputBroadcastPacketHandler(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        ValueTask IPacketHandler<S2C_MoveInputBroadcastPacket>.HandlePacket(Session session, S2C_MoveInputBroadcastPacket packet)
        {
            PlayerDataDTO playerData = gameManager.GetPlayerData(packet.PlayerId);
            Player player = null;
            if (player == null)
            {
                CustomLog.Log("S2C_MoveInputBroadcastPacketHandler : Not Found Player", UnityEngine.Color.orange);
                return new ValueTask();
            }

            CustomLog.Log($"before position : {player.transform.position}, after position : {packet.Position}");
            player.MovementComponent.MyTransform.position = packet.Position;
            player.MovementComponent.SetMoveDirection(packet.MoveInput);
            return new ValueTask();
        }
    }
}