using KSY.Shared;
using KSY.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Clients
{
    public class MiniGameManager : MonoBehaviour
    {
        [SerializeField] private List<MiniGameDataSO> miniGamesToApply = new List<MiniGameDataSO>();

        public static MiniGameManager Instance { get; private set; }

        public MiniGame CurrentMiniGame { get; private set; }


        private Dictionary<string, MiniGame> miniGames = null;
        private Dictionary<string, MiniGamePlayer> players = null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); 
                Initialize(); 
            }
            else
                Destroy(gameObject);
        }

        private void Initialize()
        {
            miniGames = miniGamesToApply.ToDictionary(
                x => x.SceneName,
                x => new MiniGame(x)
            );
        }

        public void OnEnterRoom(string playerName)
        {
            if(!players.TryGetValue(playerName))
        }

        public void StartGame(string sceneName)
        {
            if (miniGames != null && miniGames.TryGetValue(sceneName, out MiniGame targetGame))
            {
                CurrentMiniGame = targetGame;
                SceneManager.LoadScene(CurrentMiniGame.Data.SceneName);
            }
            else
            {
                CustomLog.LogError($"{sceneName}과 같은 이름의 씬이 없습니다");
            }
        }
    }
}