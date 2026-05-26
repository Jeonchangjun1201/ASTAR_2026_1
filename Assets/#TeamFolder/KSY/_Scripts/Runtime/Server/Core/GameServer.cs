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
        private Dictionary<Session, string> playerNameMap = null;
        private Dictionary<string, PlayerDataDTO> playerDataMap = null;
        private Server server = null;

        public void Initialize(GameManager gameManager, DataTableManager dataTableManager)
        {
            GameManager.Instance.EventChannel?.AddListener<GameQuitEvent>((evt)=> Close());

            GameInstance.PlayMode |= EPlayMode.Server;
            GameInstance.DataTableManager = dataTableManager;
            ServerInstance.GameServer = this;

            playerNameMap = new Dictionary<Session, string>();
            playerDataMap = new Dictionary<string, PlayerDataDTO>();

            if (!gameManager.TryGetComponent(out UnityPacketDispatcher unityPacketDispatcher))
                unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
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
        
        public void AddPlayer(string playerName, Session session, PlayerDataDTO playerDataDTO)
        {
            server.Rooms.Room(ServerDefine.ROOM_ID).Add(playerName, session);
            playerNameMap[session] = playerName;
            playerDataMap[playerName] = playerDataDTO;
        }

        public string GetPlayerName(Session session)
        {
            playerNameMap.TryGetValue(session, out string playerName);
            return playerName;
        }

        public PlayerDataDTO GetPlayerData(string playerName)
        {
            playerDataMap.TryGetValue(playerName, out PlayerDataDTO data);
            return data;
        }

        public int GetPlayerCount() => playerNameMap.Count;
        public Dictionary<string, PlayerDataDTO> GetPlayers() => new Dictionary<string, PlayerDataDTO>(playerDataMap);
    }
}
