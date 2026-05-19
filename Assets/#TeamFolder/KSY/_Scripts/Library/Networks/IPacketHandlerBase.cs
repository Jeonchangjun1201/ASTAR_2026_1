using System.Threading.Tasks;

namespace KSY.Networks
{
    public interface IPacketHandlerBase
    {
        ValueTask HandlePacket(Session session, IPacket packet);
    }
}

