using KSY.Client;
using KSY.Shared;
using UnityEngine;

namespace KSY.Server
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
        }
    }
}


