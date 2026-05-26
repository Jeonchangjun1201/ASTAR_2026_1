using KSY.Utility;
using System;
using System.Net;
using System.Net.Sockets;

namespace KSY.Networks
{
    public class Client : NetworkObject
    {
        public Action OnConnected;

        private readonly Session session;
        private readonly PacketSerializer packetSerializer;
        private readonly IPacketDispatcher packetDispatcher;

        internal Client(INetworkObjectBuilder builder)
            : base(builder)
        {
            session = GetInstance<Session>();
            packetSerializer = GetInstance<PacketSerializer>();
            packetDispatcher = GetInstance<IPacketDispatcher>();
        }

        public void Connect(string host, int port, Action onConnected)
        {
            this.OnConnected = onConnected;
            var addressFamily = AddressFamily.InterNetworkV6;
            var socketType = SocketType.Stream;
            var protocolType = ProtocolType.Tcp;
            Socket obj = new Socket(addressFamily, socketType, protocolType)
            {
                //IPv4, IPv6 둘 다 처리 가능한지에 대한 여부
                DualMode = true
            };

            IPAddress address;
            bool isDnsAddress = !IPAddress.TryParse(host, out address);
            EndPoint remoteEndPoint = isDnsAddress ? ((EndPoint)new DnsEndPoint(host, port)) : ((EndPoint)new IPEndPoint(address, port));
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
            socketAsyncEventArgs.Completed += HandleConnected;

            bool isPending = obj.ConnectAsync(socketAsyncEventArgs);
            if (!isPending)
                HandleConnected(null, socketAsyncEventArgs);
        }

        public void Disconnect() => session.Close();

        private void HandleConnected(object sender, SocketAsyncEventArgs connectArgs)
        {
            if (connectArgs.SocketError == SocketError.Success)
            {
                OnConnected?.Invoke();
                session.Open(connectArgs.ConnectSocket, packetSerializer, packetDispatcher);
            }
            else
                CustomLog.Log($"Failed : Socket Connect\n{connectArgs.SocketError}", UnityEngine.Color.orange);
        }
    }
}