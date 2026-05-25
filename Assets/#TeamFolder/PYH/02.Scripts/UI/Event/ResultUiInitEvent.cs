namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class ResultUiInitEvent
    {
        public PlayerResultInfo[] PlayerResultInfos { get; private set; }

        public ResultUiInitEvent(PlayerResultInfo[] playerResultInfos)
        {
            PlayerResultInfos = playerResultInfos;
        }
    }
}