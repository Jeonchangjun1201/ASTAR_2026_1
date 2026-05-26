using KSY.Utility;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class RoomManager : IRoomManager, IPacketDispatcher, IAsyncDisposable, Room.ICallback
    {
        private readonly IPacketDispatcher roomPacketDispatcher;
        private readonly ConcurrentDictionary<string, Lazy<Room>> rooms;
        private readonly ConcurrentDictionary<Session, Room> sessionRoomMap;
        private readonly Lazy<RoomWorker>[] workers;
        private readonly Lazy<RoomWorker> dedicatedWorker;
        private readonly Lazy<PacketSerializer> packetSerializer;
        private readonly Lazy<PacketHandlerFactory> packetHandlerFactory;

        public RoomManager(IPacketDispatcher roomPacketDispatcher, DIContainer diContainer, int workerCount, int capacityPerWorker)
        {
            this.roomPacketDispatcher = roomPacketDispatcher;
            rooms = new ConcurrentDictionary<string, Lazy<Room>>();
            sessionRoomMap = new ConcurrentDictionary<Session, Room>();
            packetSerializer = new Lazy<PacketSerializer>(()=>diContainer.GetInstance<PacketSerializer>());
            packetHandlerFactory = new Lazy<PacketHandlerFactory>(()=>diContainer.GetInstance<PacketHandlerFactory>());
            workers = new Lazy<RoomWorker>[workerCount];
            for (int index = 0; index < workerCount; index++)
                this.workers[index] = WorkerFactory(capacityPerWorker);
            this.dedicatedWorker = this.WorkerFactory(capacityPerWorker);
        }

        #region IRoomManager
        Room IRoomManager.Room(string roomID)
        {
            if (string.IsNullOrEmpty(roomID)) return null;
            return rooms.GetOrAdd(roomID, RoomFactory).Value;
        }
        #endregion
       
        #region IPacketDispatcher
        ValueTask IPacketDispatcher.Dispatch(Session session, IPacket packet)
        {
            if (session == null)
                CustomLog.LogError("session is nul");
            if (packet == null)
                CustomLog.LogError("packet is null");
                
            try
            {
                if (sessionRoomMap.TryGetValue(session, out var value))
                {
                    int num = (value.RoomIDHash & 0x7FFFFFFF) % workers.Length;
                    return workers[num].Value.EnqueueAsync(session, packet);
                }

                return dedicatedWorker.Value.EnqueueAsync(session, packet);
            }
            catch
            {
                CustomLog.LogError("Error : RoomManager.Dispatch");
                session.Close();
            }

            return default(ValueTask);
        }

        #endregion

        #region IAsyncDisposable
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            Lazy<RoomWorker>[] array = workers;
            foreach (Lazy<RoomWorker> worker in workers)
            {
                if (worker.IsValueCreated)
                {
                    await ((IAsyncDisposable)worker.Value).DisposeAsync();
                }
            }

            if (dedicatedWorker.IsValueCreated)
                await((IAsyncDisposable)dedicatedWorker.Value).DisposeAsync();
        }
        #endregion

        #region Room.ICallback
        void Room.ICallback.OnAdded(Room room, Session session)
        {
            if (room != null && session != null)
                sessionRoomMap[session] = room;
        }

        void Room.ICallback.OnRemoved(Room room, Session session)
        {
            if (session != null)
                sessionRoomMap.TryRemove(session, out var _);
        }
        #endregion

        private Lazy<Room> RoomFactory(string roomID)
        {
            return new Lazy<Room>(() => new Room(roomID, packetSerializer.Value, this));
        }

        private Lazy<RoomWorker> WorkerFactory(int capacityPerWorker)
        {
            Func<RoomWorker> workerFactory = () =>
            {
                IPacketDispatcher activeDispatcher = roomPacketDispatcher is RoomPacketDispatcher
                    ? roomPacketDispatcher
                    : new RoomPacketDispatcher(packetHandlerFactory.Value);

                return new RoomWorker(activeDispatcher, capacityPerWorker);
            };

            return new Lazy<RoomWorker>(workerFactory);
        }

        //private Lazy<RoomWorker> WorkerFactory(int capacityPerWorker)
        //{
        //    Func<RoomWorker> workerFactory = () => new RoomWorker(roomPacketDispatcher ?? new RoomPacketDispatcher(packetHandlerFactory.Value), capacityPerWorker);
        //    return new Lazy<RoomWorker>(workerFactory);
        //}
    }
}
