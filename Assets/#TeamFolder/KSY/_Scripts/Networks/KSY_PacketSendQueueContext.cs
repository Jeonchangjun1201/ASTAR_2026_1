using System;
using System.Threading;

namespace KSY.Networks
{
    public class KSY_PacketSendQueueContext : KSY_ISendQueueContext, IDisposable
    {
        private readonly KSY_ArrayPoolBufferWriter bufferWriter;
        private readonly ArraySegment<byte> data;
        private int isDisposed;

        public KSY_PacketSendQueueContext(KSY_PacketSerializer packetSerializer, KSY_IPacket packet)
        {
            bufferWriter = packetSerializer.Seralize(packet);
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

