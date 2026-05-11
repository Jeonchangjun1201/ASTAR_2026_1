using KSY.Networks;
using KSY.Servers;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_Server : KSY_NetworkObject
    {
        private readonly KSY_ISessionFactory sessionFactory;
        private readonly KSY_PacketSerializer packetSerializer;
        private readonly KSY_IPacketDispatcher packetDispatcher;
        private readonly IRoomManager
    }
}

