using KSY.Client;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_Session : MonoBehaviour
    {
        private readonly object sendLocker;
        private KSY_SendQueue sendQueue;
        private SocketAsyncEventArgs sendArgs;

        private ReceiveBuffer receiveBuffer;
        private SocketAsyncEventArgs receiveArgs;

        private Socket connectedSocket;
        private KSY_PacketSerializer packetSerializer;
        private KSY_IPacketDispatcher packetDispatcher;

        private int isClosed = 1;
        public bool IsOpened
        {
            get
            {
                if (Volatile.Read(ref isClosed) == 0 && connectedSocket != null)
                {
                    return connectedSocket.Connected;
                }

                return false;
            }
        }

        public event Action<KSY_Session> OnOpenedEvent;
        public event Action<KSY_Session> OnClosedEvent;
        public event Action<KSY_Session, Exception> OnErrorEvent;

        public KSY_Session()
        {
            sendLocker = new object();
        }

        public void Open(Socket connectedSocket, KSY_PacketSerializer packetSerializer, KSY_IPacketDispatcher packetDispatcher)
        {
            //Argument, 인자를 말한다. 인자는 Parameter와는 다른 개념으로 매개변수는 메서드에 전달되는 값 형식 자체를 말하고
            //Argument는 실제로 전달되는 값을 말한다. 변수와 리터럴과 같은 관계.
            if (connectedSocket == null)
                throw new ArgumentNullException("connectedSocket");

            if (packetSerializer == null)
                throw new ArgumentNullException("packetSerializer");

            if (packetDispatcher == null)
                throw new ArgumentNullException("packetDispatcher");

            this.connectedSocket = connectedSocket;
            this.packetSerializer = packetSerializer;
            this.packetDispatcher = packetDispatcher;
            Volatile.Write(ref isClosed, 0);
            sendQueue = new KSY_SendQueue();
            sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += HandleSent;
            receiveBuffer = new ReceiveBuffer(65535);
            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += HandleReceived;
            ReceiveAsync();
            this.OnOpenedEvent?.Invoke(this);
        }

        public void Close()
        {
            try
            {
                if (Volatile.Read(ref isClosed) != 1)
                {
                    Volatile.Write(ref isClosed, 1);
                    connectedSocket?.Close();
                }
            }
            catch
            {
            }
            finally
            {
                receiveArgs?.Dispose();
                sendArgs?.Dispose();
                receiveArgs = null;
                sendArgs = null;
                connectedSocket = null;
                lock (sendLocker)
                {
                    sendQueue?.Dispose();
                    sendQueue = null;
                }

                this.OnClosedEvent?.Invoke(this);
            }
        }

        public void SendAsync(KSY_IPacket packet)
        {
            if (packet == null)
            {
                SendAsync(new KSY_PacketSendQueueContext());
            }
        }

        internal void SendAsync(KSY_ISendQueueContext sendQueueContext)
        {

        }

        private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
        {

        }

        private void ReceiveAsync()
        {

        }

        private async void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
        {

        }

        private async ValueTask<int> HandlePacket(ArraySegment<byte> buffer)
        {

        }
    }
}

