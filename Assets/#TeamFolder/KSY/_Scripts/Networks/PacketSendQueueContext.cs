using System;
using System.Threading;

namespace KSY.Networks
{
    public class PacketSendQueueContext : ISendQueueContext, IDisposable
    {
        private readonly ArrayPoolBufferWriter bufferWriter;
        private readonly ArraySegment<byte> data;
        private int isDisposed;

        public PacketSendQueueContext(KSY_PacketSerializer packetSerializer, IPacket packet)
        {
            bufferWriter = packetSerializer.Serialize(packet);
            data = bufferWriter.WrittenSegment;
        }

        public ArraySegment<byte> GetData()
        {
            if (Volatile.Read(ref isDisposed) != 0)
            {
                throw new ObjectDisposedException("PacketSendQueueContext");
            }

            return data;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            {
                bufferWriter.Dispose();
            }
        }
    }
}

