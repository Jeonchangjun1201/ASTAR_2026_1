using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Networks
{
    public interface IPacketDispatcher
    {
        ValueTask Dispatch(Session session, IPacket packet);
    }
}

