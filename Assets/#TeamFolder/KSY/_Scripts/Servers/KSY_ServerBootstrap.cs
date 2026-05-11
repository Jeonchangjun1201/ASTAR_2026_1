using KSY.Networks;
using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public class KSY_ServerBootstrap : KSY_NetworkObject
    {
        [SerializeField]
        private KSY_GameManager gameManager = null;
        [SerializeField]
        private KSY_DataTableManager dataTableManager = null;
        
        public async void StartServer()
        {
            gameManager.Initialize();

            KSY_GameServer gameServer = new KSY_GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            gameServer.Listen();
            //gameServer.Initalize(gameManager, dataTableManager);
        }
    }
}


