using System.Net.Sockets;

namespace KSY.Client
{
    public abstract class Session
    {
        private Socket _socket;
        private int _disconnected = 0;
        ReceiveBuffer _receiveBuffer;

    }
}
