using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Networks
{
    public interface KSY_IPacketHandlerBase
    {
        ValueTask HandlePacket(KSY_Session session, KSY_IPacket packet);
    }
}

