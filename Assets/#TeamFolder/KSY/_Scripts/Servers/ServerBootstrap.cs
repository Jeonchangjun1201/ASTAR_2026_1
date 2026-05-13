using KSY.Networks;
using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public class ServerBootstrap : MonoBehaviour
    {
        [SerializeField]
        private KSY_GameManager gameManager = null;
        [SerializeField]
        private KSY_DataTableManager dataTableManager = null;
        
        public async void StartServer()
        {
            gameManager.Initialize();

            GameServer gameServer = new GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            //gameServer.Listen();
            //gameServer.Initalize(gameManager, dataTableManager);
        }
    }
}


