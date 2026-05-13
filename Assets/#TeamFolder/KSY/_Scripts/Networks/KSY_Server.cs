using System.Net.Sockets;
using System.Threading;
using KSY.Servers;

namespace KSY.Networks
{
    public class KSY_Server : KSY_NetworkObject
    {
        private readonly KSY_ISessionFactory sessionFactory;
        private readonly KSY_PacketSerializer packetSerializer;
        private readonly IPacketDispatcher packetDispatcher;
        private readonly KSY_IRoomManager roomManager;
        private Socket listenSocket;
        private SocketAsyncEventArgs acceptArgs;
        private int isClosed;

        public KSY_IRoomManager Rooms => roomManager;
        public bool IsOpened => Volatile.Read(ref isClosed) == 0;

        internal KSY_Server(KSY_INetworkObjectBuilder builder) : base(builder)
        {
            
        }
    }
}

