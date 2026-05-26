using UnityEngine;

namespace BFS
{
    public class TOWGameOverManager
    {
        private TOWKeyQTEManager _qteManager;
        private TOWScoreManager _scoreManager;
        private TOWUIManager _uiManager;

        public TOWGameOverManager(TOWKeyQTEManager qteManager, TOWScoreManager scoreManager, TOWUIManager uiManager)
        {
            _qteManager = qteManager;
            _scoreManager = scoreManager;
            _uiManager = uiManager;
        }
        public void EndGame()                                                                                                               // End Game // 게임 종료
        {
            _qteManager.EndMinigame();                                                                                                      // QTE매니저 미니게임 종료
            string winnerTeam = _scoreManager.scoreBoard[(PlayerTeamTOW)1].CompareTo(_scoreManager.scoreBoard[(PlayerTeamTOW)2]) == 1
                ? $"{(PlayerTeamTOW)1} WINS!!!!!!" :
                _scoreManager.scoreBoard[(PlayerTeamTOW)1].CompareTo(_scoreManager.scoreBoard[(PlayerTeamTOW)2]) == 0
                ? "DRAW" : $"{(PlayerTeamTOW)2} WINS!!!!!!";
                                                                                                                                            // 점수를 비교해서 이긴 팀 출력
            EndGameText(winnerTeam);
        }

        public bool CheckForceEnd()
        {
            if (Mathf.Abs(_scoreManager.scoreBoard[(PlayerTeamTOW)1] - _scoreManager.scoreBoard[(PlayerTeamTOW)2]) >= 25)                   // 점수의 차이 25점 이상인지 확인
                return true;
            return false;
        }
        private void EndGameText(string overMessage)
        {
            _uiManager.ChangeText(_uiManager.GameOverText, overMessage, int.MaxValue);
        }
    }

}

