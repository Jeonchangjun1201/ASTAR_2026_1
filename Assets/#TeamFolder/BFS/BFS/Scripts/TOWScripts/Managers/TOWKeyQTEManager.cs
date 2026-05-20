using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BFS
{
    public class TOWKeyQTEManager : MonoBehaviour                                                                // Key minigame manager script // 키 미니게임 매니저
    {
        private IRopeTOW _rope;
        private List<AbstractTeamTOW> _teamList = new List<AbstractTeamTOW>();                                   // List that contains players, used for sub/unsubing GetInput method to OnInputPressed action //구독 및 해제를 돕는 플레이어 리스트
        private Dictionary<ITeamTOW, Vector2> _goalDict = new Dictionary<ITeamTOW, Vector2>();                   // Dictionary that contains required key to press for each player(Key: ITeamTOW interface(player), Value: Vector2(Required input key to press)) //플레이어와 플레이어가 눌러야 하는 키를 담은 딕셔너리
        private TOWScoreManager _scoreManager;                                                                   // Score manager, this exists so that rope doesn't move when team's score is 0 // 스코어 매니저
        private TOWUIManager _uiManager;
        private char _inputShower;                                                                               // Char variable that shows which key to press // 무슨 키를 눌러야 하는 지 알려주는 char변수
        private bool _isPenalty = false;                                                                         // Bool variable, used to detect if player should have penalty or not // bool변수, 패널티 주는 용도
        private bool _isInGame = false;
        private float _penaltyTime = 2.5f;                                                                       // How long minigame is going to be disabled for player // 얼마나 오래동안 미니게임을 멈출 지
        private float _defaultRopePower = 0.12f;
        public bool IsInGame => _isInGame;
        public void Initialize(IRopeTOW rope, AbstractTeamTOW[] playerList, TOWScoreManager scoreManager, TOWUIManager uiManager)        // Initialize
        {
            _rope = rope;
            foreach (AbstractTeamTOW t in playerList)                                                            // Subs to each player's OnInputPressed action // 플레이어의 OnInputPressed에 구독
            {
                _teamList.Add(t);
                t.OnInputPressed += GetInput;
            }
            foreach (ITeamTOW t in playerList)                                                                   // Adds each player to dictionary, for input key management // 딕셔너리에 플레이어 추가
            {
                _goalDict.Add(t, Vector2.zero);
            }
            _scoreManager = scoreManager;
            _uiManager = uiManager;
        }
        public void StartMinigame()
        {
            _isInGame = true;
            foreach (ITeamTOW t in _teamList)
                DeclareGoal(t);                                                                                  // Initiate the input key minigame // 미니게임 시작
        }
        private void OnDestroy()
        {
            foreach (AbstractTeamTOW t in _teamList)                                                             // Unsub //구독 해제
            {
                t.OnInputPressed -= GetInput;
            }
        }
        private IEnumerator NextInputCoroutine(ITeamTOW team, bool val)                                          // Coroutine, will give penalty to a player if they messed up with the minigame // 코루틴, 패널티 주는 용도
        {
            if (val)                                                                                             // If parameter(IsCorrect) is true, then give next required input // 매개변수가 true면 다음 목표 입력 키 전달
                DeclareGoal(team);
            else                                                                                                 // Else, make playeer do nothing for penalty time, then give next required input // 아니면 플레이어가 아무것도 못하도록 만든다
            {
                _isPenalty = true;
                _uiManager.ChangeText(_uiManager.GoalText, "<color=red>Wrong Input!</color>", _penaltyTime);
                yield return new WaitForSeconds(_penaltyTime);
                _isPenalty = false;
                DeclareGoal(team);
            }
        }
        public void GiveScore(ITeamTOW team, bool IsCorrect)                                                     // Method that decides whether player pressed correct key or not // 플레이어가 맞게 키를 눌렀는지 확인하는 메서드
        {
            Vector3 moveDir;                                                                                     // Local variable, decides direction for the rope to move // 지역 변수, 줄을 어디로 움직일지 결정
            switch (team.Team)                                                                                   // The side for each team are opposite. So modify the default movement direction for rope based on the team // 팀에 따라 이동 방향을 다르게 조정
            {
                case PlayerTeamTOW.TEAMONE:
                    moveDir = Vector3.left;
                    break;
                default:
                    moveDir = Vector3.right;
                    break;
            }
            moveDir *= _defaultRopePower;                                                                        // Modifying movement distance // 움직이는 거리 조정
            StartCoroutine(NextInputCoroutine(team, IsCorrect));                                                 // Coroutine, gives player a penalty if they pressed wrong key. // 잘못된 키를 눌렀을 때 패널티를 주는 코루틴
            if (_scoreManager.scoreBoard[team.Team] <= 0 && !IsCorrect)
            {
                return;
            }
            else
            {
                _rope.MoveRope(IsCorrect ? moveDir :                                                             // If player pressed correct key, then move it to default direction // 플레이어가 맞는 키를 입력했으면, 기본 방향으로 이동
                        (moveDir * -1));                                                                         // Else, move rope to opposite direction unless corresponding team has score of 0 // 아니면, 반대 방향으로 이동. 해당하는 팀의 점수가 0이면 움직이지 않음
                _uiManager.AddValue(team, IsCorrect);
            }
            team.ReceiveScore(team, IsCorrect ? 1 : -1);                                                         // Give team a score; 1 if they pressed correct key, -1 else // 팀에 점수 부여; 알맞은 키는 1, 아니면 -1
        }
        public Vector2 DeclareGoal(ITeamTOW team)                                                                // Sets the required input key for each player // 각 플레이어에게 목표 입력 키 적용
        {
            Vector2 goal = RollInput(team);                                                                      // Declare local variable, then give it Shuffled movement input key // 지역 변수 선언
            Debug.Log($"{_inputShower} - {team.Team} | {team.ObjectName}");                                      // TEMPORARY; debugging, shows required input key, team name, and player name // 임시
            _uiManager.ChangeText(_uiManager.GoalText, _inputShower.ToString());
            return goal;                                                                                         // returns required input key // 목표 입력 키 반환
        }
        public void GetInput(ITeamTOW team, Vector2 vt)                                                          // Method that is subscribed to action, receives ITeamTOW and Vector2(each are player and input) // 구독되는 메서드, 입력을 받고 점수를 주는 용도
        {
            if (_isPenalty | !_isInGame)                                                                         // Ignore if player is in penalty state or game hasn't started yet // 게임 시작이 되지 않았거나 패널티가 적용된 경우 무시한다
                return;
            if (vt == _goalDict[team])                                                                           // If input equals to required key given to each player // 입력 키에 따라 true또는 false로 GiveScore메서드 실행 
            {
                GiveScore(team, true);                                                                           // Call GiveScore, IsCorrect = true
            }
            else                                                                                                 // Else then call GiveScore, IsCorrect = false
            {
                GiveScore(team, false);
            }
        }
        private Vector2 RollInput(ITeamTOW team)                                                                 // Method to shuffle required input key, then return it // 입력 키를 얻는 메서드
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    _inputShower = 'W';
                    _goalDict[team] = Vector2.up;
                    return Vector2.up;
                case 1:
                    _inputShower = 'A';
                    _goalDict[team] = Vector2.left;
                    return Vector2.left;
                case 2:
                    _inputShower = 'S';
                    _goalDict[team] = Vector2.down;
                    return Vector2.down;
                default:
                    _inputShower = 'D';
                    _goalDict[team] = Vector2.right;
                    return Vector2.right;
            }
        }
        public void EndMinigame() => _isInGame = false;
    }

}
