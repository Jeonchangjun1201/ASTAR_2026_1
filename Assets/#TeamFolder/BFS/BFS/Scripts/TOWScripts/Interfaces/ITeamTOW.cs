namespace BFS
{
    public interface ITeamTOW
    {
        string ObjectName { get; }                                //TEMPORARY; FOR DEBUGGING // 임시, 디버그용
        PlayerTeamTOW Team { get; }
        IRopeTOW Rope { get; }
        void Initialize(PlayerTeamTOW team, PlayerTOW player);
        void ReceiveScore(ITeamTOW team, int score);
    }

}
