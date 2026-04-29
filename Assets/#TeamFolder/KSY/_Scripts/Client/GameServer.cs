using System;
using System.Net;

namespace KSY.Client
{
    public class GameServer : Session
    {
        public override void OnConnect(EndPoint endPoint)
        {

        }

        public override void OnDisconnected(EndPoint endPoint)
        {

        }

        public override int OnReceive(ArraySegment<byte> buffer)
        {
            return 0;
        }

        public override void OnSend(int numOfBytes)
        {

        }
    }
}
