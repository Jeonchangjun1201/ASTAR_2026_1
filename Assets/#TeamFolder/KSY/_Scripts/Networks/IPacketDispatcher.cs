using System.Threading.Tasks;

namespace KSY.Networks
{
    public interface IPacketDispatcher
    {
        ValueTask Dispatch(Session session, IPacket packet);
    }
}

