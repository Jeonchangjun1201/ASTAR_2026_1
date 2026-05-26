using KSY.Shared;
using KSY.Utility;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Servers
{
    public class ServerBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameManager gameManager = null;
        [SerializeField]
        private DataTableManager dataTableManager = null;
        [SerializeField]
        private string inGameSceneName;
        
        public async void StartServer(string ipAddress, int port, Action onAccepted)
        {
            gameManager.Initialize();

            GameServer gameServer = new GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            gameServer.Listen(ipAddress, port, onAccepted);

            //await SceneManager.LoadSceneAsync(TestDefine.TEST_LOAD_SCENE_NAME, LoadSceneMode.Single);
        }
    }
}


