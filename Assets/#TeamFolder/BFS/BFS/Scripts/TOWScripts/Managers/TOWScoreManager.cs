using System.Collections.Generic;

namespace BFS
{
    public class TOWScoreManager                                                                                         // Score Manager script for Tug of war // 줄다리기 스코어 매니저
    {
        public Dictionary<PlayerTeamTOW, int> scoreBoard { get; private set; } = new Dictionary<PlayerTeamTOW, int>();   // Dictionary, for easier score managing for each team(Key: PlayerTeamTOW enum(Team), Value: int(score)) // 점수 관리를 돕는 딕셔너리
        private List<ITeamTOW> _playerList;                                                                              // List, to sub/unsub AddScore method to OnScoreGain action for each player // 구독 및 해제를 위한 리스트
        private TOWUIManager _uiManager;
        private PlayerTeamTOW _benefitTeam;
        public TOWScoreManager(ITeamTOW[] players, TOWUIManager uiManager)                                                                       // Constructor, receives array of ITeamTOW //생성자
        {
            _playerList = new List<ITeamTOW>();
            for (int i = 0; i < players.Length; i++)
            {
                _playerList.Add(players[i]);                                                                             // Add player to list // 플레이어를 리스트에 추가
            }
            foreach (RopePull player in _playerList)
            {
                player.OnScoreGain += AddScore;                                                                          // Sub to action from player for each of them in list //그리고 구독
            }
            for (int i = 0; i < sizeof(PlayerTeamTOW); i++)
            {
                scoreBoard.Add((PlayerTeamTOW)i + 1, 0);                                                                 // Typecast i into PlayerTeamTOW(enum), then set its value(score) to 0(default) for문에서 i를 타입캐스트하고 스코어보드에 값 추가
            }

            _uiManager = uiManager;
        }
        public void OnDestroyThen()                                                                                      // OnDestroy(to unsub) //구독 해제
        {
            foreach (RopePull player in _playerList)                                                                     // Unsub foreach
            {
                player.OnScoreGain -= AddScore;
            }
        }
        private void AddScore(ITeamTOW team, int score)                                                                  // Method to add score, receives ITeamTOW and int. Locates the team through ITeamTOW and adds/subtracts a score with dictionary //점수를 추가하는 메서드, ITeamTOW와 int받음.
        {
            if (score < 0 & scoreBoard[team.Team] <= 0)                                                                   // Checks if score of the team after calculation is negative. If it is, then do not change // 계산 후 점수가 음수가 되는지 검사, 음수가 되면 아무것도 하지 않음
                return;
            scoreBoard[team.Team] += score;
            UpdateTeamScore();
        }

        private void UpdateTeamScore()
        {
            _uiManager.ChangeText(_uiManager.TeamOneText, $"{scoreBoard[PlayerTeamTOW.TEAMONE]}");
            _uiManager.ChangeText(_uiManager.TeamTwoText, $"{scoreBoard[PlayerTeamTOW.TEAMTWO]}");
        }
    }
}
