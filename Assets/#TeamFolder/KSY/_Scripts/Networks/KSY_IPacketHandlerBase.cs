using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Networks
{
    public interface KSY_IPacketHandlerBase
    {
        ValueTask HandlePacket(Session session, IPacket packet);
    }
}

