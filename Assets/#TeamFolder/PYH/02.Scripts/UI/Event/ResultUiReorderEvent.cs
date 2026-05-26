namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class ResultUiReorderEvent
    {
        public PlayerInfo[] CurInfos { get; private set; }

        public ResultUiReorderEvent(PlayerInfo[] curInfos)
        {
            CurInfos = curInfos;
        }
    }
}