namespace _TeamFolder.PYH._02.Scripts.UI
{
    public struct PlayerInfo
    {
        public int Index { get; private set; }
        public string NickName { get; private set; }
        public int Ranking { get; private set; }
        public int Point { get; private set; }

        public PlayerInfo(string nickName, int ranking, int point)
        {
            Index = -1;
            NickName = nickName;
            Ranking = ranking;
            Point = point;
        }
        public PlayerInfo(int index, string nickName)
        {
            Index = index;
            NickName = nickName;
            Ranking = -1; // like null == -1
            Point = -1;
        }
    }
}