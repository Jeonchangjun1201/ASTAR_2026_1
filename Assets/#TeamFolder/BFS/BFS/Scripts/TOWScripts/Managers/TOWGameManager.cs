using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BFS
{
    public class TOWGameManager : MonoBehaviour                                                                                         // Tug Of War manager script // 줄다리기 매니저 
    {
        [SerializeField] private TOWKeyQTEManager qteManager;
        [SerializeField] private float gameTime;
        private AbstractTeamTOW[] playerList;                                                                                           // Array that contains players(absctract class) // 플레이어 관리하는 배열
        private RopeTOW _rope;
        private TOWScoreManager _scoreManager;
        private TOWTimeManager _timeManager;

        private void Awake()
        {
            playerList = GetComponentsInChildren<AbstractTeamTOW>();                                                                    // Collect players attached to game manager // 게임 매니저에서 플레이어들을 모으고
            int cnt = 0;
            foreach (RopePull rp in playerList)                                                                                         // Initialize each player; apply team, and player script attached to them // 모든 플레이어들을 이니셜라이즈하고, 팀을 부여함
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, rp.GetComponentInParent<PlayerTOW>());
            }
            _scoreManager = new TOWScoreManager(playerList);                                                                            // Constructor; sends playerList to ScoreManager then instantiates // 생성자로 스코어 매니저를 만들고 매개변수로 플레이어 리스트 보냄
            _rope = GetComponentInChildren<RopeTOW>();
            qteManager.Initialize(_rope, playerList, _scoreManager);                                                                    // Initialize Key minigame manager // 미니게임 매니저
            _timeManager = new TOWTimeManager();
            _timeManager.OnTimerEnd += EndGame;
        }
        private void Update()                                                                                                           // TEMPORARY; FOR DEBUGGING // 임시
        {
            if(Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log($"{_scoreManager.CheckTeamScore(1)} - TeamOne");
            }
            if(Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log($"{_scoreManager.CheckTeamScore(2)} - TeamTwo");
            }
            if(Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartGame();
            }
            _timeManager.UpdateTimer();
        }
        private void OnDestroy()
        {
            _scoreManager.OnDestroyThen();                                                                                              // On destroy then calls it so score manager can unsub // 구독 해제
            _timeManager.OnTimerEnd -= EndGame;
        }

        public void StartGame()
        {
            Debug.Log("START!");
            qteManager.StartMinigame();
            _timeManager.StartTimer(gameTime);
        }

        public void EndGame()
        {
            qteManager.EndMinigame();
            Debug.Log("FINISH!");                                                                                                       // TEMPORARY; for debugging // 임시
            Debug.Log(_scoreManager.scoreBoard[(PlayerTeamTOW)1].CompareTo(_scoreManager.scoreBoard[(PlayerTeamTOW)2]) == 1 
                ? "TEAM ONE IS NUMBER ONE!" : "TEAM TWO TAKES THE FIRST PLACE!");
        }
    }

}

