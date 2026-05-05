using System;

namespace KSY.Network
{
    public interface KSY_ISendQueueContext : IDisposable
    {
        ArraySegment<byte> GetData();
    }
}