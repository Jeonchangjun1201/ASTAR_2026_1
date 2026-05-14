using KSY.Networks;
using KSY.Shared;
using KSY.Utility;
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
        private ListeningDataSO listeningData;
        
        public async void StartServer()
        {
            CustomLog.Assert(listeningData != null, $"ServerBootStrap :{listeningData.name}가 null입니다!! ");
            if(listeningData == null) return;
            int port = listeningData.Port;

            gameManager.Initialize();

            GameServer gameServer = new GameServer();
            gameServer.Initialize(gameManager, dataTableManager);
            gameServer.Listen(port);

            await SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
        }
    }
}


