using System;

namespace KSY.Networks
{
    public interface ISendQueueContext : IDisposable
    {
        ArraySegment<byte> GetData();
    }
}