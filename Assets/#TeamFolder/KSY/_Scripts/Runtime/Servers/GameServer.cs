using KSY.Networks;
using KSY.Shared;
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
            GameInstance.PlayMode = EPlayMode.Server;
            GameInstance.DataTableManager = dataTableManager;
            ServerInstance.GameServer = this;

            playerIDMap = new Dictionary<Session, string>();

            UnityPacketDispatcher unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
            server = new ServerBuilder(this, unityPacketDispatcher)
                .AddSingleton<GameServer>(this)
                .AddSingleton<GameManager>(gameManager)
                .AddSingleton<DataTableManager>(dataTableManager)
                .Build(typeof(GameServer).Assembly, typeof(GameManager).Assembly);

            unityPacketDispatcher.Initialize(server);
        }

        public void Listen(int port)
        {
            server.Listen(port);
        }

        public void AddPlayer(IPacket packet, Func<string, Session, bool> filter = null)
        {
            server.Rooms.Room(ServerDefine.ROOM_ID).Send(packet, filter);
        }
        public Session Create(NetworkObject networkObject, Socket connectedSocket)
        {
            throw new System.NotImplementedException();
        }
    }
}
