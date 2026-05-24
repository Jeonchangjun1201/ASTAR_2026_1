namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class DigitalClockUiTimeSetEvent
    {
        public int SEC { get; private set; }

        public DigitalClockUiTimeSetEvent(int sec)
        {
            SEC = sec;
        }
    }
}