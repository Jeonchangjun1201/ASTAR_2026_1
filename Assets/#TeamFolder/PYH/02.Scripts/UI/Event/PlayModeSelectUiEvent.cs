using Assets._TeamFolder.PYH._02.Scripts.Enum;

namespace Assets._TeamFolder.PYH._02.Scripts.UI.Event
{
    public class PlayModeSelectUiEvent
    {
        public PlayModeEnum SelectMode { get; private set; }

        public PlayModeSelectUiEvent(PlayModeEnum selectMode)
        {
            SelectMode = selectMode;
        }
    }
}
