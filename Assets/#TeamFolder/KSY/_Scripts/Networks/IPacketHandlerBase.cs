using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Networks
{
    public interface IPacketHandlerBase
    {
        ValueTask HandlePacket(Session session, IPacket packet);
    }
}

