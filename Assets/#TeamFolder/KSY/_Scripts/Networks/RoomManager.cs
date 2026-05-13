using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace KSY.Networks
{
    public class RoomManager : IRoomManager, IPacketDispatcher, IAsyncDisposable, Room.ICallback
    {
        private readonly IPacketDispatcher roomPacketDispatcher;
        private readonly ConcurrentDictionary<string, Lazy<Room>> rooms;
        private readonly ConcurrentDictionary<Session, Room> sessionMap;
        private readonly Lazy<RoomWorker>[] sessionRoomMap;
        public ValueTask Dispatch(Session session, IPacket packet)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void OnAdded(Room room, Session session)
        {
            throw new NotImplementedException();
        }

        public void OnRemoved(Room room, Session session)
        {
            throw new NotImplementedException();
        }

        public Room Room(string roomID)
        {
            throw new NotImplementedException();
        }
    }
}
