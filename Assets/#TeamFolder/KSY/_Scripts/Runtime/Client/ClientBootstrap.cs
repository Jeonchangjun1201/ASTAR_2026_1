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

        [SerializeField]
        private string ipAddress;

        [SerializeField]
        private int port;

        public void StartClient()
        {
            InputManager.Initialize();
            gameManager.Initialize();

            GameClient gameClient = new GameClient();
            gameClient.Initialize(gameManager, dataTableManager);
            gameClient.Connect(ipAddress, port);
        }
    }
}

