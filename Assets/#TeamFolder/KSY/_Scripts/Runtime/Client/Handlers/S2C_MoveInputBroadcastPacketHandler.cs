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
            CustomLog.Log("S2C_MoveInputBroadcastPacketHandler : HandlePacket", UnityEngine.Color.orange);
            Unit unit = gameManager.GetPlayer(packet.PlayerId);
            if (unit == null)
            {
                CustomLog.Log("S2C_MoveInputBroadcastPacketHandler : Not Found Player", UnityEngine.Color.orange);
                return new ValueTask();
            }

            unit.transform.position = packet.Position;
            unit.UnitMovementComponent.SetMovementInput(packet.MoveInput);
            return new ValueTask();
        }
    }
}