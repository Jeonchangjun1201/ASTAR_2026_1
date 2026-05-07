using System;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_PacketSendQueueContext : KSY_ISendQueueContext, IDisposable
    {
        private readonly ArrayPoolBufferWriter bufferWriter;
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ArraySegment<byte> GetData()
        {
            throw new NotImplementedException();
        }
    }
}

