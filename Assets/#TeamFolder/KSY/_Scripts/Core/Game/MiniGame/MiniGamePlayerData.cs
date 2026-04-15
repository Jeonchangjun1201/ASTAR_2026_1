using KSY.ClientCore.User;

namespace KSY.GameCore.MiniGame
{
    public class MiniGamePlayerData : PlayerData
    {
        public MiniGamePlayerData(TeamType teamType, UserData user, int score = 0) : base()
        {
            SetTeam(teamType);
            SetUserData(user);
            SetScore(score);
        }
    }
}

