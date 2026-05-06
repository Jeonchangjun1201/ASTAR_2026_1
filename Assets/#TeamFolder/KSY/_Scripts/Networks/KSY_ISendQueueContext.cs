using System;

namespace KSY.Networks
{
    public interface KSY_ISendQueueContext : IDisposable
    {
        ArraySegment<byte> GetData();
    }
}