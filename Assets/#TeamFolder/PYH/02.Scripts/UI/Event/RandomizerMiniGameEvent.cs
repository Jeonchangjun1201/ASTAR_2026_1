using _TeamFolder.PYH._02.Scripts.Enum;

namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class RandomizerMiniGameEvent
    {
        public MiniGameEnum TargetMiniGameEnum { get; private set; }

        public RandomizerMiniGameEvent(MiniGameEnum targetMiniGameEnum)
        {
            TargetMiniGameEnum = targetMiniGameEnum;
        }
    }
}