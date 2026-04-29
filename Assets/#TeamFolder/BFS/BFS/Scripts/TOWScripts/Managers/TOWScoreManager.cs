using System.Collections.Generic;
using System.Diagnostics;

namespace BFS
{
    public class TOWScoreManager                                                                                         // Score Manager script for Tug of war // 줄다리기 스코어 매니저
    {
        public Dictionary<PlayerTeamTOW, int> scoreBoard { get; private set; } = new Dictionary<PlayerTeamTOW, int>();   // Dictionary, for easier score managing for each team(Key: PlayerTeamTOW enum(Team), Value: int(score))
        private List<ITeamTOW> _playerList;                                                                              // List, to sub/unsub AddScore method to OnScoreGain action for each player
        public TOWScoreManager(ITeamTOW[] players)                                                                       // Constructor, receives array of ITeamTOW
        {
            _playerList = new List<ITeamTOW>();
            for (int i = 0; i < players.Length; i++)
            {
                _playerList.Add(players[i]);                                                                             // Add player to list
            }
            foreach (RopePull player in _playerList)
            {
                player.OnScoreGain += AddScore;                                                                          // Sub to action from player for each of them in list
            }
            for (int i = 0; i < sizeof(PlayerTeamTOW); i++)
            {
                scoreBoard.Add((PlayerTeamTOW)i + 1, 0);                                                                 // Typecast i into PlayerTeamTOW(enum), then set its value(score) to 0(default)
            }
        }
        public void OnDestroyThen()                                                                                      // OnDestroy(to unsub)
        {
            foreach (RopePull player in _playerList)                                                                     // Unsub foreach
            {
                player.OnScoreGain -= AddScore;
            }
        }
        public int CheckTeamScore(int teamNum)                                                                           // TEMPORARY; FOR DEBUGGING
        {
            return scoreBoard[(PlayerTeamTOW)teamNum];
        }
        private void AddScore(ITeamTOW team, int score)                                                                  // Method to add score, receives ITeamTOW and int. Locates the team through ITeamTOW and adds/subtracts a score with dictionary
        {
            if (score < 0 & scoreBoard[team.Team] < 0)                                                                   // Checks if score of the team after calculation is negative. If it is, then do not change
                return;
            scoreBoard[team.Team] += score;
        }
    }
}
