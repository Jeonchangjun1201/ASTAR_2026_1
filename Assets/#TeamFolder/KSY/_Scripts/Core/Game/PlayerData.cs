using UnityEngine;

namespace KSY.ClientCore.User
{
    public class PlayerData
    {
        private UserData _user;
        private byte Score
        {
            get
            {
                return _socre;
            }
            set
            {
                byte sum = (byte)Mathf.Clamp(_socre + value, 0, byte.MaxValue);
                _socre = sum;
            }
        }
        private byte _socre;

        public int IncreaseScore(byte score)
        {
            if(score > 0)
                this.Score += score;
            else
                DebugX.Log("")
        }
        public int DecreaseScore(byte score)
        {
            if (score > 0)
                this.Score -= score;
            else
                DebugX.Log("");
        }
    }
}