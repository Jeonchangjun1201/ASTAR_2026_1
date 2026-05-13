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
        private readonly Lazy<RoomWorker>[] dedicatedWorker;
        private readonly Lazy<PacketSerializer> packetSerializer;
        private readonly Lazy<PacketHandlerFactory> packetHandlerFactory;

        public RoomManager(IPacketDispatcher roomPacketDispatcher, DIContainer diContainer, int workerCount, int capacityPerWorker)
        {
            this.roomPacketDispatcher = roomPacketDispatcher;
            rooms = new ConcurrentDictionary<string, Lazy<Room>>();
            sessionRoomMap = new ConcurrentDictionary<Session, Room>();
            packetSerializer = new Lazy<PacketSerializer>(()=>>diContainer.GetInstance<PacketSerializer>());
            packetHandlerFactory = new Lazy<PacketHandlerFactory>(()=>diContainer.GetInstance<PacketHandlerFactory>());
            workers = new Lazy<RoomWorker>[workerCount];
            for (int num = 0; num < workerCount; num++)
            {
                workers[num] = WorkerFactory(capacityPerWorker);
            }

            dedicatedWorker = WorkerFactory(capacityPerWorker)
        }

        #region IRoomManager
        Room IRoomManager.Room(string roomID)
        {
            if (string.IsNullOrEmpty(roomID)) return null;
            return rooms.GetOrAdd(roomID, RoomFactory).Value;
        }
        #endregion
       
        #region IPacketDispatcher
        public ValueTask Dispatch(Session session, IPacket packet)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            if (packet == null)
                throw new ArgumentNullException("packet");

            try
            {
                if (sessionRoomMap.TryGetValue(session, out var value))
                {
                    int num = (value.RoomIDHash & 0x7FFFFFFF) % workers.Length;
                    return workers[num].Value.EnqueueAsync(session, packet);
                }
            }
        }

        #endregion

        #region IAsyncDisposable
        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Room.ICallback
        public void OnAdded(Room room, Session session)
        {
            throw new NotImplementedException();
        }

        public void OnRemoved(Room room, Session session)
        {
            throw new NotImplementedException();
        }
        #endregion



    }
}
