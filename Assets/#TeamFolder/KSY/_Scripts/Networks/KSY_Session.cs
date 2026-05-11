using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

        private KSY_ReceiveBuffer receiveBuffer;
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
            //Argument, ���ڸ� ���Ѵ�. ���ڴ� Parameter�ʹ� �ٸ� �������� �Ű������� �޼��忡 ���޵Ǵ� �� ���� ��ü�� ���ϰ�
            //Argument�� ������ ���޵Ǵ� ���� ���Ѵ�. ������ ���ͷ��� ���� ����.
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
            receiveBuffer = new KSY_ReceiveBuffer(65535);
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
                throw new ArgumentNullException("packet");
            }

            SendAsync(new KSY_PacketSendQueueContext(packetSerializer, packet));
        }

        internal void SendAsync(KSY_ISendQueueContext sendQueueContext)
        {
            if (sendQueueContext == null)
            {
                throw new ArgumentNullException("sendQueueContext");
            }

            if (!IsOpened)
            {
                sendQueueContext.Dispose();
                Close();
                throw new InvalidOperationException("Session is not opened");
            }

            List<ArraySegment<byte>> bufferList = null;
            lock (sendLocker)
            {
                sendQueue.Enqueue(sendQueueContext);
                if (!sendQueue.TryFlush(out bufferList))
                {
                    return;
                }
            }

            sendArgs.BufferList = bufferList;
            if (!connectedSocket.SendAsync(sendArgs))
            {
                HandleSent(null, sendArgs);
            }
        }

        private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
        {
            if (!IsOpened)
            {
                Close();
                return;
            }

            if (sendArgs.SocketError != 0 || sendArgs.BytesTransferred <= 0)
            {
                Close();
                return;
            }

            List<ArraySegment<byte>> bufferList = null;
            lock (sendLocker)
            {
                sendQueue.Clear();
                if (!sendQueue.TryFlush(out bufferList))
                {
                    return;
                }
            }

            sendArgs.BufferList = bufferList;
            if (!connectedSocket.SendAsync(sendArgs))
            {
                HandleSent(null, sendArgs);
            }
        }

        private void ReceiveAsync()
        {
            if (!IsOpened)
            {
                Close();
                return;
            }

            receiveBuffer.CleanUp();
            receiveArgs.SetBuffer(receiveBuffer.FreeBuffer);
        }

        private async void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
        {

        }

        private async ValueTask<int> HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 2)
            {
                return 0;
            }

            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if (packetSize > ushort.MaxValue || packetSize > buffer.Count)
            {
                return 0;
            }

            ArraySegment<byte> packetData = new ArraySegment<byte>(buffer.Array, buffer.Offset + 2, packetSize - 2);
            try
            {
                KSY_IPacket packet = packetSerializer.Deserialize();
            }
        }
    }
}

