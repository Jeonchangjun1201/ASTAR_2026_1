using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using System.Collections;
using UnityEngine;
namespace BFS
{
    public class TOWGameManager : MonoBehaviour                                                                                         // Tug Of War manager script // 줄다리기 매니저 
    {
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private TOWKeyQTEManager qteManager;
        [SerializeField] private float gameTime;
        [SerializeField] private TOWUIManager uiManager;
        [SerializeField] private DigitalClockTimerUi gameTimerUi;
        private AbstractTeamTOW[] playerList;                                                                                           // Array that contains players(absctract class) // 플레이어 관리하는 배열
        private RopeTOW _rope;
        private TOWScoreManager _scoreManager;
        private TOWGameOverManager _gameOverManager;

        private int _idleHash;
        private int _pullHash;
        private void Awake()
        {
            _idleHash = Animator.StringToHash("IDLE");
            _pullHash = Animator.StringToHash("PULL");
            playerList = GetComponentsInChildren<AbstractTeamTOW>();                                                                   // Collect players attached to game manager // 게임 매니저에서 플레이어들을 모으고
            int cnt = 0;
            foreach (RopePull rp in playerList)                                                                                        // Initialize each player; apply team, and player script attached to them // 모든 플레이어들을 이니셜라이즈하고, 팀을 부여함
            {
                rp.Initialize(cnt++ % 2 == 0 ? PlayerTeamTOW.TEAMONE : PlayerTeamTOW.TEAMTWO, rp.GetComponentInParent<PlayerTOW>());
                rp.TOWAnimator.Play(_idleHash);
            }
            _scoreManager = new TOWScoreManager(playerList, uiManager);                                                                // Constructor; sends playerList to ScoreManager then instantiates // 생성자로 스코어 매니저를 만들고 매개변수로 플레이어 리스트 보냄
            _gameOverManager = new TOWGameOverManager(qteManager, _scoreManager, uiManager);                                           // Constructpr // 생성자
            _rope = GetComponentInChildren<RopeTOW>();
            qteManager.Initialize(_rope, playerList, _scoreManager, uiManager, soundManager);                                          // Initialize Key minigame manager // 미니게임 매니저
            gameTimerUi.OnTimeEndedEvent += EndGame;
        }

        private void Start()
        {
            DigitalClockUiTimeSetEvent ev = new DigitalClockUiTimeSetEvent(114);
            AStarEventBus.Publish(ev);
            StartCoroutine(StartGameCountdownCoroutine());
        }
        private void Update()                                                                                                         
        {
            if (qteManager.IsInGame)
            {
                if (_gameOverManager.CheckForceEnd())
                    EndGame();
            }
        }
        private void OnDestroy()
        {
            _scoreManager.OnDestroyThen();                                                                                              // On destroy then calls it so score manager can unsub // 구독 해제
            if (gameTimerUi != null)
                gameTimerUi.OnTimeEndedEvent -= EndGame;
        }
        private IEnumerator StartGameCountdownCoroutine()
        {
            for(int i = 3; i >= 0; i--)
            {
                uiManager.ChangeText(uiManager.GameOverText, i.ToString(), 1);
                yield return new WaitForSeconds(1);
            }
            StartGame();
        }
        public void StartGame()                                                                                                         // Starting Game // 게임 시작
        {
            foreach(RopePull rp in playerList)
            {
                rp.TOWAnimator.Play(_pullHash);                                                                                         // Play pulling anim for all players // 모든 플레이어의 줄 당기기 애니메이션 재생
            }
            uiManager.ChangeText(uiManager.GameOverText, "START!", 3);
            soundManager.PlaySound("GameStartSFX");
            soundManager.PlaySound("TugOfWar-BGM");
            qteManager.StartMinigame();
            DigitalClockUiStartEvent startEv = new DigitalClockUiStartEvent();                                                          // Timer event start // 타이버 이벤트 시작함
            AStarEventBus.Publish(startEv);
        }
        public void EndGame()                                                                                                           // End game // 게임 종료
        {
            foreach (RopePull rp in playerList)
            {
                rp.TOWAnimator.Play(_idleHash);                                                                                         // 모든 플레이어의 IDLE 애니메이션 재생
            }                                                                                                   
            soundManager.PlaySound("GameEndSFX");
            _gameOverManager.EndGame();
        }
    }

}

