using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class RoomWorker : IAsyncDisposable
    {
        //Channel : Producer와 Consumer 사이의 비동기적 데이터 교환
        private readonly Channel<(Session, IPacket)> channel;
        //CancellationTokenSource : CancellationTokenSource가 주체, CancellationToken이 객체
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Task loopTask;
        private readonly IPacketDispatcher packetDispatcher;

        public RoomWorker(IPacketDispatcher packetDispatcher, int capacity)
        {
            this.packetDispatcher = packetDispatcher;
            cancellationTokenSource = new CancellationTokenSource();
            channel = Channel.CreateBounded<(Session, IPacket)>(new BoundedChannelOptions(capacity : capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                //큐에 빈 공간이 생길 때까지 WriteAsync가 완료되지 않고 비동기적으로 대기함.    
                FullMode = BoundedChannelFullMode.Wait,
                //기다리고 있던 작업(ReadAsync, WriteAsync)이 끝났을 때 후속 작업을 누가 실행하느냐.
                // (true시 작업을 하던 스레드가 후속 작업을 실행함.) 
                AllowSynchronousContinuations = false
            });
            loopTask = Task.Run(()=> ProcessLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }

        public ValueTask EnqueueAsync(Session session, IPacket packet) => channel.Writer.WriteAsync((session, packet));
        
        private async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            _= 1;
            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    (Session, IPacket) item;
                    while(channel.Reader.TryRead(out item))
                    {
                        try
                        {
                            await packetDispatcher.Dispatch(item.Item1, item.Item2);
                        }
                        catch (Exception value)
                        {
                            Console.WriteLine(value);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            channel.Writer.TryComplete();
            cancellationTokenSource.Cancel();
            try
            {
                await loopTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
    }
}