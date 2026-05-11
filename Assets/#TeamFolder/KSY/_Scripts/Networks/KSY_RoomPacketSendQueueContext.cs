using System;
using System.Threading;

namespace KSY.Networks
{
    public class KSY_RoomPacketSendQueueContext : KSY_ISendQueueContext, IDisposable
    {
        private readonly KSY_ArrayPoolBufferWriter bufferWriter;
        private readonly ArraySegment<byte> data;
        private int remainingReferenceCount;
        //true : 1, false : 0
        private int isDisposed;

        public KSY_RoomPacketSendQueueContext(KSY_PacketSerializer packetSerializer, KSY_IPacket packet, int referenceCount)
        {
            if (referenceCount <= 0)
            {
                //Arguemnt : 인수
                throw new ArgumentOutOfRangeException("referenceCount");
            }

            bufferWriter = packetSerializer.Serialize(packet);
            data = bufferWriter.WrittenSegment;
            remainingReferenceCount = referenceCount;
        }
        public ArraySegment<byte> GetData()
        {
            //Volatile.Read() : CPU에 있는 최신 값을 바로 가져와서 반환함.
            if (Volatile.Read(ref isDisposed) != 0)
            {
                throw new ObjectDisposedException("RoomPacketSendQueueContext");
            }
            
            return data;
        }
        public void AddReference()
        {
            if (Volatile.Read(ref isDisposed) == 0)
            {
                Interlocked.Increment(ref remainingReferenceCount);
            }
        }
        public void Dispose()
        {
            //Interlocked.Decrement() : -1 감소 시킨 후의 값을 반환함.
            //Interlocked.Exchange() : 변경 이전의 값을 반환함.
            if(Interlocked.Decrement(ref remainingReferenceCount) <= 0 && Interlocked.Exchange(ref isDisposed, 1) == 0)
            {
                bufferWriter.Dispose();
            }
        }
    }
}

