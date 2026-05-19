using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

        internal Server(INetworkObjectBuilder builder) 
        : base(builder)
        {
            sessionFactory = GetInstance<ISessionFactory>();
            packetSerializer = GetInstance<PacketSerializer>();
            packetDispatcher = GetInstance<IPacketDispatcher>();
            roomManager = GetInstance<IRoomManager>();
        }

        public void Listen(int port, int backlog = 10)
        {
            Volatile.Write(ref isClosed, 0);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            listenSocket.Listen(backlog);
            acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += HandleAccepted;
            AcceptAsync();
        }

        public void Close()
        {
            try
            {
                if (Volatile.Read(ref isClosed) != 1)
                {
                    Volatile.Write(ref isClosed, 1);
                    listenSocket?.Close();
                }
            }
            catch
            {
            }
            finally
            {
                acceptArgs?.Dispose();
                ((IAsyncDisposable)this).DisposeAsync();
                acceptArgs = null;
                listenSocket = null;
            }
        }

        private void AcceptAsync()
        {
            acceptArgs.AcceptSocket = null;
            if (!listenSocket.AcceptAsync(acceptArgs))
                HandleAccepted(null, acceptArgs);
        }

        public void HandleAccepted(object sender, SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs.SocketError != SocketError.Success || acceptArgs.AcceptSocket == null)
            {
                AcceptAsync();
                return;
            }

            sessionFactory.Create(this, acceptArgs.AcceptSocket).Open(acceptArgs.AcceptSocket, packetSerializer, packetDispatcher);
            AcceptAsync();
        }
    }
}

