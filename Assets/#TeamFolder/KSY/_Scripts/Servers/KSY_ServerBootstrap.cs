using KSY.Client;
using KSY.Shared;
using UnityEngine;

namespace KSY.Servers
{
    public class KSY_ServerBootstrap : MonoBehaviour
    {
        [SerializeField]
        private KSY_GameManager gameManager = null;
        [SerializeField]
        private KSY_DataTableManager dataTableManager = null;
        
        public async void StartServer()
        {
            gameManager.Initialize();

            KSY_GameServer gameServer = new KSY_GameServer();
            //gameServer.Initalize(gameManager, dataTableManager);
        }
    }
}


