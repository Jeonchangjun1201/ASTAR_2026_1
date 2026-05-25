using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public struct PlayerResultInfo
    {
        public string NickName { get; private set; }
        public int Ranking { get; private set; }
        public int Point { get; private set; }

        public PlayerResultInfo(string nickName, int ranking, int point)
        {
            NickName = nickName;
            Ranking = ranking;
            Point = point;
        }
    }
}