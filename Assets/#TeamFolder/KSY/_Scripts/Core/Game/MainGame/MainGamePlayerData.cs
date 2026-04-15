using KSY.ClientCore.User;

namespace KSY.GameCore.MainGame
{
    public class MainGamePlayerData : PlayerData
    {
        public MainGamePlayerData(TeamType teamType, UserData user, int score = 0) : base()
        {
            SetTeam(teamType);
            SetUserData(user);
            SetScore(score);
        }
    }
}

