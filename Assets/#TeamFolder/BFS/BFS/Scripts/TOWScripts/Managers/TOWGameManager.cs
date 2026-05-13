using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{
    public class TOWGameManager : MonoBehaviour                                                                                         // Tug Of War manager script // 줄다리기 매니저 
    {
        [SerializeField] private TOWKeyQTEManager qteManager;
        [SerializeField] private float gameTime;
        [SerializeField] private TOWUIManager uiManager;
        private AbstractTeamTOW[] playerList;                                                                                           // Array that contains players(absctract class) // 플레이어 관리하는 배열
        private RopeTOW _rope;
        private TOWScoreManager _scoreManager;
        private TOWTimeManager _timeManager;
        private TOWGameOverManager _gameOverManager;
        private void Awake()
        {
            playerList = GetComponentsInChildren<AbstractTeamTOW>();                                                                    // Collect players attached to game manager // 게임 매니저에서 플레이어들을 모으고
            int cnt = 0;
            foreach (RopePull rp in playerList)                                                                                         // Initialize each player; apply team, and player script attached to them // 모든 플레이어들을 이니셜라이즈하고, 팀을 부여함
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, rp.GetComponentInParent<PlayerTOW>());
            }
            _scoreManager = new TOWScoreManager(playerList, uiManager);                                                                            // Constructor; sends playerList to ScoreManager then instantiates // 생성자로 스코어 매니저를 만들고 매개변수로 플레이어 리스트 보냄
            _gameOverManager = new TOWGameOverManager(qteManager, _scoreManager, uiManager);                                            // Constructpr // 생성자
            _rope = GetComponentInChildren<RopeTOW>();
            qteManager.Initialize(_rope, playerList, _scoreManager, uiManager);                                                         // Initialize Key minigame manager // 미니게임 매니저
            _timeManager = new TOWTimeManager(uiManager);
            _timeManager.OnTimerEnd += EndGame;
        }
        private void Update()                                                                                                           // TEMPORARY; FOR DEBUGGING // 임시
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartGame();
            }
            if (qteManager.IsInGame)
            {
                _timeManager.UpdateTimer();
                if (_gameOverManager.CheckForceEnd())
                    EndGame();
            }
        }
        private void OnDestroy()
        {
            _scoreManager.OnDestroyThen();                                                                                              // On destroy then calls it so score manager can unsub // 구독 해제
            _timeManager.OnTimerEnd -= EndGame;
        }
        public void StartGame()
        {
            uiManager.ChangeText(uiManager.GameOverText, "START!");
            qteManager.StartMinigame();
            _timeManager.StartTimer(gameTime);
        }
        public void EndGame()
        {
            _gameOverManager.EndGame();
        }
    }

}

