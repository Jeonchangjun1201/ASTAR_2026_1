using KSY.Networks;
using KSY.Shared;
using KSY.Utility;
using System.Threading.Tasks;

namespace KSY.Clients
{
    [PacketHandler(typeof(S2C_PlayerResponseBroadCastPacket))]
    public class S2C_PlayerResponseBroadCastPacketHandler : IPacketHandler<S2C_PlayerResponseBroadCastPacket>
    {
        private readonly GameManager gameManager = null;
        private readonly GameClient gameClient = null;

        public S2C_PlayerResponseBroadCastPacketHandler(GameManager gameManager, GameClient gameClient)
        {
            this.gameManager = gameManager;
            this.gameClient = gameClient;
        }

        ValueTask IPacketHandler<S2C_PlayerResponseBroadCastPacket>.HandlePacket(Session session, S2C_PlayerResponseBroadCastPacket packet)
        {
            CustomLog.Log("클라이언트에서 플레이어를 생성합니다.");
            return new ValueTask();
        }
    }
}