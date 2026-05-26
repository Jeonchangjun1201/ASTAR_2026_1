using KSY.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace KSY.Networks
{
    public class Room
    {
        public interface ICallback
        {
            void OnAdded(Room room, Session session);
            void OnRemoved(Room room, Session session);
        }

        private readonly ConcurrentDictionary<string, Session> sessions;
        private readonly ConcurrentDictionary<Session, Action<Session>> sessionClosedHandlers;
        private readonly int roomIDHash;
        private readonly PacketSerializer packetSerializer;
        private readonly ICallback callback;

        public int RoomIDHash => roomIDHash;

        public Room(string roomID, PacketSerializer packetSerializer, ICallback callback)
        {
            roomIDHash = roomID.GetHashCode();
            this.packetSerializer = packetSerializer;
            this.callback = callback;
            sessions = new ConcurrentDictionary<string, Session>();
            sessionClosedHandlers = new ConcurrentDictionary<Session, Action<Session>>();
        }

        public void Add(string sessionID, Session session)
        {
            if(!string.IsNullOrEmpty(sessionID) && session != null && sessions.TryAdd(sessionID, session))
            {
                callback.OnAdded(this, session);
                sessionClosedHandlers[session] = HandleSessionClosed;
                session.OnClosedEvent += HandleSessionClosed;
            }

            void HandleSessionClosed(Session _)
            {
                Remove(sessionID);
            }
        }

        public void Remove(string sessionID)
        {
            if (!string.IsNullOrEmpty(sessionID) && sessions.TryRemove(sessionID, out var value) && value != null)
            {
                if (sessionClosedHandlers.TryRemove(value, out var value2))
                {
                    value.OnClosedEvent -= value2;
                }

                callback.OnRemoved(this, value);
            }
        }

        public void Send(IPacket packet, Func<string, Session, bool> filter = null)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            RoomPacketSendQueueContext roomPacketSendQueueContext = null;
            bool flag = false;
            foreach(KeyValuePair<string, Session> session in sessions)
            {
                string key = session.Key;
                Session value = session.Value;
                if (value != null && value.IsOpened && (filter == null || filter(key, value)))
                {
                    if(roomPacketSendQueueContext == null)
                        roomPacketSendQueueContext = new RoomPacketSendQueueContext(packetSerializer, packet, 1);
                    else
                        roomPacketSendQueueContext.AddReference();

                    try
                    {
                        value.SendAsync(roomPacketSendQueueContext);
                        CustomLog.Log($"Send Packet Server to Client : {key}", UnityEngine.Color.green);
                        flag = true;
                    }
                    catch
                    {
                        roomPacketSendQueueContext?.Dispose();
                        throw;
                    }
                }
            }

            if(!flag)
                roomPacketSendQueueContext?.Dispose();
        }

        public Session Session(string sessionID)
        {
            if(string.IsNullOrEmpty(sessionID))
            {
                return null;
            }

            sessions.TryGetValue(sessionID, out var value);
            return value;
        }
    }
}
