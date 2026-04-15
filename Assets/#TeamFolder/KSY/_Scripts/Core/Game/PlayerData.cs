using KSY.ClientCore.User;
using KSY.Utility;
using UnityEngine;

namespace KSY.GameCore
{
    public abstract class PlayerData
    {
        //플레이어 정보
        public UserData User { get; private set; }
        //플레이어의 팀
        public TeamType CurrentTeam { get; private set; }
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
        //플레이어 점수
        private int _socre;

        //Init용 함수들
        protected void SetTeam(TeamType type) => this.CurrentTeam = type;
        protected void SetUserData(UserData user) => this.User = user;
        protected void SetScore(int score) => this.Score = score;

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