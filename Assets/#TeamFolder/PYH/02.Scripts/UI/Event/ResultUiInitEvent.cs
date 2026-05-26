namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class ResultUiInitEvent
    {
        public PlayerInfo[] PlayerResultInfos { get; private set; }

        public ResultUiInitEvent(PlayerInfo[] playerResultInfos)
        {
            PlayerResultInfos = playerResultInfos;
        }
    }
}