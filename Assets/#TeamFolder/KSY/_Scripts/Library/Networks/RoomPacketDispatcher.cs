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
            return pktHandler?.HandlePacket(session, packet) ?? default(ValueTask);
        }
    }
}
