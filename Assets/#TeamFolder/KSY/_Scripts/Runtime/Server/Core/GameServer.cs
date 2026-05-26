using KSY.Networks;
using KSY.Shared;
using KSY.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace KSY.Servers
{
    public class GameServer : ISessionFactory
    {
        private Dictionary<Session, string> playerIDMap = null;
        private Server server = null;

        public void Initialize(GameManager gameManager, DataTableManager dataTableManager)
        {
            GameManager.Instance.EventChannel?.AddListener<GameQuitEvent>((evt)=> Close());

            GameInstance.PlayMode = EPlayMode.Server;
            GameInstance.DataTableManager = dataTableManager;
            ServerInstance.GameServer = this;

            playerIDMap = new Dictionary<Session, string>();

            UnityPacketDispatcher unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
            
            //Builder 내부에 있는 DIContainer에 Singleton 인스턴스를 생성해서 추가하고 빌드한다.
            server = new ServerBuilder(this, unityPacketDispatcher)
                .AddSingleton<GameServer>(this)
                .AddSingleton<GameManager>(gameManager)
                .AddSingleton<DataTableManager>(dataTableManager)
                .Build(typeof(GameServer).Assembly, typeof(GameManager).Assembly);

            unityPacketDispatcher.Initialize(server);
        }

        #region  ISessionFactory
        Session ISessionFactory.Create(NetworkObject networkObject, Socket connectedSocket) => new Session();
        #endregion

        public void Close()
        {
            if (server != null && server.IsOpened)
            {
                CustomLog.Log("Unity 종료로 인해 서버 소켓이 안전하게 닫혔습니다.");
                server.Close();
            }
        }

        public void Listen(string ipAddress, int port)
        {
            server.Listen(ipAddress, port);
        }
        public void Listen(string ipAddress, int port, Action onAccepted)
        {
            server.Listen(ipAddress, port, onAccepted);
        }

        public void Send(IPacket packet, Func<string, Session, bool> filter = null)
        {
            server.Rooms.Room(ServerDefine.ROOM_ID).Send(packet, filter);
        }
        
        public void AddPlayer(string playerId, Session session)
        {
            server.Rooms.Room(ServerDefine.ROOM_ID).Add(playerId, session);
            playerIDMap[session] = playerId;
        }

        public string GetPlayerID(Session session)
        {
            playerIDMap.TryGetValue(session, out string playerID);
            return playerID;
        }
    }
}
