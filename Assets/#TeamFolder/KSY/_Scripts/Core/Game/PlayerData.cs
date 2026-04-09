using KSY.GameCore;
using KSY.Utility;
using UnityEngine;

namespace KSY.ClientCore.User
{
    public class PlayerData
    {
        public PlayerData(UserData user, MainGameTeamType currentTeam = MainGameTeamType.None, int score = 0)
        {
            this._user = user;
            this.CurrentTeam = currentTeam;
            this.Score = score;
        }

        //플레이어의 팀
        public MainGameTeamType CurrentTeam { get; private set; } = MainGameTeamType.None; 
        //플레이어 점수에 대한 접근을 제어하는 프로퍼티
        private int Score
        {
            get
            {
                return _socre;
            }
            set
            {
                int sum = Mathf.Clamp(_socre + value, 0, int.MaxValue);
                _socre = sum;
            }
        }
        //플레이어 정보
        private UserData _user;
        //플레이어 점수
        private int _socre;
        //팀을 정하는 메서드
        public void SetTeam(MainGameTeamType type = MainGameTeamType.None) => CurrentTeam = type;
        //플레이어의 점수를 증가시키는 메서드
        public int IncreaseScore(int score = 1)
        {
            if (score > 0)
                this.Score += score;
            else
                CustomLog.LogError("The score value you want to increase is 0 or less.");

            return Score;
        }
        //플레이어의 점수를 감소시키는 메서드
        public int DecreaseScore(int score = 1)
        {
            if (score > 0)
                this.Score -= score;
            else
                CustomLog.LogError("The score value you want to decrease is 0 or less.");

            return Score;
        }
    }
}