using System.Collections.Generic;
using System.Diagnostics;

namespace BFS
{
    public class TOWScoreManager
    {
        public Dictionary<PlayerTeamTOW, int> scoreBoard { get; private set; } = new Dictionary<PlayerTeamTOW, int>();
        private List<ITeamTOW> _playerList;
        public TOWScoreManager(ITeamTOW[] players)
        {
            _playerList = new List<ITeamTOW>();
            for (int i = 0; i < players.Length; i++)
            {
                _playerList.Add(players[i]);
            }
            foreach (RopePull player in _playerList)
            {
                player.OnScoreGain += AddScore;
            }
            for (int i = 0; i < sizeof(PlayerTeamTOW); i++)
            {
                scoreBoard.Add((PlayerTeamTOW)i + 1, 0);
            }
        }
        public void OnDestroyThen()
        {
            foreach (RopePull player in _playerList)
            {
                player.OnScoreGain -= AddScore;
            }
        }
        public int CheckTeamScore(int teamNum)                               // TEMPORARY; FOR DEBUGGING
        {
            return scoreBoard[(PlayerTeamTOW)teamNum];
        }
        private void AddScore(ITeamTOW team, int score)
        {
            scoreBoard[team.Team] += score;
            if (scoreBoard[team.Team] < 0)
                scoreBoard[team.Team] = 0;
        }
    }
}
