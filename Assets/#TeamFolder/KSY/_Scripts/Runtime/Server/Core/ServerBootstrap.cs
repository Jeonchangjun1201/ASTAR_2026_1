using KSY.Shared;
using System;
using UnityEngine;

namespace KSY.Servers
{
    public class ServerBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager = null;
        [SerializeField]
        private DataTableManager dataTableManager = null;
        
        public async void StartServer(string ipAddress, int port, Action onAccepted)
        {
            gameManager.Initialize();

            GameServer gameServer = new GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            gameServer.Listen(ipAddress, port, onAccepted);
        }
    }
}


