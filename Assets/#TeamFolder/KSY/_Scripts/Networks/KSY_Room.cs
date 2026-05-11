using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_Room : MonoBehaviour
    {

        //인터페이스에서는 내부에서 암묵적으로 public 접근 제한자를 띄고 있다.
        public interface ICallback
        {
            void OnAdded(KSY_Room room, KSY_Session session);
            void OnRemoved(KSY_Room room, KSY_Session session);
        }

        //여러 스레드가 동시에 접근하더라도 데이터의 무결성을 보장하는 스레드 세이프한 딕셔너리.
        //Concurrent : 동시 발생의, 공동으로 작용하는
        private readonly ConcurrentDictionary<string, KSY_Session> sessions;
        private readonly ConcurrentDictionary<KSY_Session, Action<KSY_Session>> sessionClosedHandlers;
        private readonly int roomIDHash;
        private readonly KSY_PacketSerializer packetSerializer;
        private readonly ICallback callback;

        public int RoomIDHash => roomIDHash;

        public KSY_Room(string roomID, KSY_PacketSerializer packetSerializer, ICallback callback)
        {
            roomIDHash = roomID.GetHashCode();
            this.packetSerializer = packetSerializer;
            this.callback = callback;
            sessions = new ConcurrentDictionary<string, KSY_Session>();
            sessionClosedHandlers = new ConcurrentDictionary<KSY_Session, Action<KSY_Session>>();
        }

        public void Add(string sessionID, KSY_Session session)
        {
            if(!string.IsNullOrEmpty(sessionID) && session != null && sessions.TryAdd(sessionID, session))
            {
                callback.OnAdded(this, session);
                sessionClosedHandlers[session] = HandleSessionClosed;
            }

            //함수 내에서 매개변수를 사용하지 않음을 명시적으로 드러냄 '_'
            void HandleSessionClosed(KSY_Session _)
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

        public void Send(KSY_IPacket packet, Func<string, KSY_Session, bool> filter = null)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            RoomPacketSendQueueContext 
        }
    }
}
