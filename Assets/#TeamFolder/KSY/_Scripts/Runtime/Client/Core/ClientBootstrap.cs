using KSY.Shared;
using UnityEngine;

namespace KSY.Clients
{
    public class ClientBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private DataTableManager dataTableManager;

        public void StartClient(string host, int port)
        {
            InputManager.Initialize();
            gameManager.Initialize();

            GameClient gameClient = new GameClient();
            gameClient.Initialize(gameManager, dataTableManager);
            gameClient.Connect(host, port);
        }
    }
}

