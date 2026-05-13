using System.Net.Sockets;
using System.Threading;
using KSY.Servers;

namespace KSY.Networks
{
    public class Server : NetworkObject
    {
        private readonly ISessionFactory sessionFactory;
        private readonly PacketSerializer packetSerializer;
        private readonly IPacketDispatcher packetDispatcher;
        private readonly IRoomManager roomManager;
        private Socket listenSocket;
        private SocketAsyncEventArgs acceptArgs;
        private int isClosed;

        public IRoomManager Rooms => roomManager;
        public bool IsOpened => Volatile.Read(ref isClosed) == 0;

        internal Server(INetworkObjectBuilder builder) : base(builder)
        {
            
        }
    }
}

