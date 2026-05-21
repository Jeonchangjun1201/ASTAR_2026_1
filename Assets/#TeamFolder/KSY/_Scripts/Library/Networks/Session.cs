using KSY.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class Session
    {
        private readonly object sendLocker;
        private SendQueue sendQueue;
        private SocketAsyncEventArgs sendArgs;

        private ReceiveBuffer receiveBuffer;
        private SocketAsyncEventArgs receiveArgs;

        private Socket connectedSocket;
        private PacketSerializer packetSerializer;
        private IPacketDispatcher packetDispatcher;

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

        public event Action<Session> OnOpenedEvent;
        public event Action<Session> OnClosedEvent;
        public event Action<Session, Exception> OnErrorEvent;

        public Session()
        {
            sendLocker = new object();
        }

        public void Open(Socket connectedSocket, PacketSerializer packetSerializer, IPacketDispatcher packetDispatcher)
        {
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
            sendQueue = new SendQueue();
            sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += HandleSend;
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

        public void SendAsync(IPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            SendAsync(new PacketSendQueueContext(packetSerializer, packet));
        }

        internal void SendAsync(ISendQueueContext sendQueueContext)
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

            List<ArraySegment<byte>> dataList = null;
            lock (sendLocker)
            {
                sendQueue.Enqueue(sendQueueContext);
                if (!sendQueue.TryFlush(out dataList))
                    return;
            }

            sendArgs.BufferList = dataList;
            bool pending = connectedSocket.SendAsync(sendArgs);
            if (!pending)
                HandleSend(null, sendArgs);
        }

        private void HandleSend(object sender, SocketAsyncEventArgs sendArgs)
        {
            if (!IsOpened)
            {
                CustomLog.Log("Failed : The session has not been opened.");
                Close();
                return;
            }

            if (sendArgs.SocketError != SocketError.Success ||
                sendArgs.BytesTransferred <= 0)
            {
                CustomLog.Log("Failed : Send Packet.");
                Close();
                return;
            }

            List<ArraySegment<byte>> bufferList = null;
            lock (sendLocker)
            {
                sendQueue.Clear();
                bool hasPacketData = sendQueue.TryFlush(out bufferList);
                if (!hasPacketData)
                    return;
            }

            sendArgs.BufferList = bufferList;
            bool pending = connectedSocket.SendAsync(sendArgs);
            if (!pending)
                HandleSend(null, sendArgs);      
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
            if (!IsOpened)
            {
                CustomLog.Log("Failed : The session has not been opened.");
                Close();
                return;
            }

            if (receiveArgs.SocketError != SocketError.Success || 
                receiveArgs.BytesTransferred <= 0)
            {
                CustomLog.Log("Failed : Receive Packet.");
                Close();
                return;
            }

            receiveBuffer.MoveWriteIndex(receiveArgs.BytesTransferred);
            while (true)
            {
                int bytesProcessed = await HandlePacket(receiveBuffer.UsedBuffer);
                if (bytesProcessed <= 0)
                    break;

                receiveBuffer.MoveReadIndex(bytesProcessed);
            }
        }

        private async ValueTask<int> HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 2)
                return 0;

            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if (packetSize > ushort.MaxValue || packetSize > buffer.Count)
                return 0;

            ArraySegment<byte> packetData = new ArraySegment<byte>(buffer.Array, buffer.Offset + 2, buffer.Count - 2);
            try
            {
                IPacket packet = packetSerializer.Deserialize(packetData);
                if (packet != null)
                    await packetDispatcher.Dispatch(this, packet);
            }
            catch (Exception ex)
            {
                this.OnErrorEvent?.Invoke(this, ex);
            }

            return packetSize;
        }
    }
}

