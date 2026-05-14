using KSY.Networks;
using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public class ServerBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager = null;
        [SerializeField]
        private DataTableManager dataTableManager = null;
        
        public async void StartServer()
        {
            gameManager.Initialize();

            GameServer gameServer = new GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            gameServer.Listen();
            //gameServer.Initalize(gameManager, dataTableManager);
        }
    }
}


