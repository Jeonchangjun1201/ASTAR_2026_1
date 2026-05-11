using System.Net.Sockets;
using KSY.Servers;

namespace KSY.Networks
{
    public class KSY_Server : KSY_NetworkObject
    {
        private readonly KSY_ISessionFactory sessionFactory;
        private readonly KSY_PacketSerializer packetSerializer;
        private readonly KSY_IPacketDispatcher packetDispatcher;
        private readonly KSY_IRoomManager roomManager;
        private Socket listenSocket;
    }
}

