using System;
using System.Net;
using System.Net.Sockets;

namespace KSY.Client 
{
    public class Connector 
    {
        private Func<Session> _sessionGenerator;

        public void Connect(IPEndPoint endPoint, Func<Session>howMakeSession) 
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.UserToken = socket;
            args.RemoteEndPoint = endPoint;
            args.Completed += _onConnectedComplete;
            _sessionGenerator = howMakeSession;


            _connect(args);
        }
        private void _connect(SocketAsyncEventArgs args) 
        {
            Socket socket = args.UserToken as Socket;
            bool pending = socket.ConnectAsync(args);


            if(!pending) 
            {
                _onConnectedComplete(null, args);
            }
        }
        private void _onConnectedComplete(object sender, SocketAsyncEventArgs args) 
        {
            bool isSuccess = args.SocketError == SocketError.Success;
            if(isSuccess)
            {
                Socket acceptSocket = args.ConnectSocket;
                EndPoint remoteEndPoint = args.RemoteEndPoint;
                Session acceptSession = _sessionGenerator.Invoke();


                acceptSession.Start(acceptSocket);
                acceptSession.OnConnect(remoteEndPoint);
            }
            else
            {
                // Exception Process
                Socket socket = (Socket)args.UserToken;
                socket.Close();
            }
            args.Dispose();
        }
    }
}

