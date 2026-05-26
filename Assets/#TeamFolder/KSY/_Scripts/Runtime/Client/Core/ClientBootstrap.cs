using KSY.Shared;
using KSY.Utility;
using System;
using UnityEngine;

namespace KSY.Clients
{
    public class ClientBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private DataTableManager dataTableManager;

        public void StartClient(string host, int port, Action onConnected)
        {
            InputManager.Initialize();
            gameManager.Initialize();

            CustomLog.Log("StartClient", Color.yellow);
            GameClient gameClient = new GameClient();
            gameClient.Initialize(gameManager, dataTableManager);
            gameClient.Connect(host, port, onConnected);
        }
    }
}

