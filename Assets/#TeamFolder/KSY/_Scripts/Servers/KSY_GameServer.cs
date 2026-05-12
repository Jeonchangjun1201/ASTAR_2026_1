using KSY.Networks;
using KSY.Shared;
using System.Collections.Generic;
using System.Net.Sockets;

namespace KSY.Servers
{
    public class KSY_GameServer : KSY_ISessionFactory
    {
        private Dictionary<KSY_Session, string> playerIDMap = null;
        private KSY_Server server = null;

        public void Initialize(KSY_GameManager gameManager, KSY_DataTableManager dataTableManager)
        {
            KSY_GameInstance.PlayMode = KSY_EPlayMode.Server;
            KSY_GameInstance.DataTableManager = dataTableManager;
            KSY_ServerInstance.GameServer = this;

            playerIDMap = new Dictionary<KSY_Session, string>();

            UnityPacketDispatcher
        }
        public KSY_Session Create(KSY_NetworkObject networkObject, Socket connectedSocket)
        {
            throw new System.NotImplementedException();
        }
    }
}
