namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class ResultUiReorderEvent
    {
        public PlayerResultInfo[] CurInfos { get; private set; }

        public ResultUiReorderEvent(PlayerResultInfo[] curInfos)
        {
            CurInfos = curInfos;
        }
    }
}