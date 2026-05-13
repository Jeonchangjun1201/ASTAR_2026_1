using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class RoomWorker : IAsyncDisposable
    {
        private readonly Channel<(Session, IPacket)> channel;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Task loopTask;
        private readonly IPacketDispatcher packetDispatcher;

        public RoomWorker(IPacketDispatcher packetDispatcher, int capacity)
        {
            this.packetDispatcher = packetDispatcher;
            cancellationTokenSource = new CancellationTokenSource();
        }
        public ValueTask DisposeAsync()
        {
        }
    }
}