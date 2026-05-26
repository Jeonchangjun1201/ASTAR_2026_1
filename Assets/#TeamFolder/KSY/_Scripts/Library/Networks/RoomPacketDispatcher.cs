using KSY.Utility;
using System;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class RoomPacketDispatcher : IPacketDispatcher
    {
        private PacketHandlerFactory packetHandlerFactory;

        public RoomPacketDispatcher(PacketHandlerFactory packetHandlerFactory) => this.packetHandlerFactory = packetHandlerFactory;
        
        public ValueTask Dispatch(Session session, IPacket packet)
        {
            Type pktType = packet.GetType();
            IPacketHandlerBase pktHandler = packetHandlerFactory.Create(pktType);
            if (pktHandler == null)
                CustomLog.LogError("pkt handler is null");
            else
                CustomLog.Log("pkt handler is not null", UnityEngine.Color.green);
            return pktHandler?.HandlePacket(session, packet) ?? default(ValueTask);
        }
    }
}
