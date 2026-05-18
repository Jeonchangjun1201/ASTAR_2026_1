using System.Threading.Tasks;

namespace KSY.Networks
{
    public interface IPacketHandler<TPacket> : IPacketHandlerBase where TPacket : class, IPacket
    {
        protected ValueTask HandlePacket(Session session, TPacket packet);

        ValueTask IPacketHandlerBase.HandlePacket(Session session, IPacket packet)
        {
            return HandlePacket(session, packet as TPacket);
        }
    }
}

