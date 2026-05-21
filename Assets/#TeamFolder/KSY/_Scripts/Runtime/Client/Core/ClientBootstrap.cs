using KSY.Shared;
using KSY.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Clients
{
    public class ClientBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private DataTableManager dataTableManager;

        public void StartClient()
        {
            CustomLog.Log("StartClient", Color.blue);
            InputManager.Initialize();
            gameManager.Initialize();

            GameClient gameClient = new GameClient();
            gameClient.Initialize(gameManager, dataTableManager);
            gameClient.Connect(ConnectInfo.IPAddress, ConnectInfo.Port);
        }
    }
}

