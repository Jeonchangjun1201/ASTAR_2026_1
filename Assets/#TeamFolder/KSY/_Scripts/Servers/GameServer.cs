using KSY.Networks;
using KSY.Shared;
using System.Collections.Generic;
using System.Net.Sockets;

namespace KSY.Servers
{
    public class GameServer : ISessionFactory
    {
        private Dictionary<Session, string> playerIDMap = null;
        private Server server = null;

        public void Initialize(KSY_GameManager gameManager, KSY_DataTableManager dataTableManager)
        {
            KSY_GameInstance.PlayMode = KSY_EPlayMode.Server;
            KSY_GameInstance.DataTableManager = dataTableManager;
            ServerInstance.GameServer = this;

            playerIDMap = new Dictionary<Session, string>();

            UnityPacketDispatcher unityPacketDispatcher = gameManager.gameObject.AddComponent<UnityPacketDispatcher>();
            server = new ServerBuilder(this, unityPacketDispatcher).Add
        }
        public Session Create(NetworkObject networkObject, Socket connectedSocket)
        {
            throw new System.NotImplementedException();
        }
    }
}
