using _TeamFolder.PYH._02.Scripts.Enum;

namespace PYH.Scripts.UI.Event
{
    public class RandomizerMiniGameEndEvent 
    {
        public MiniGameEnum SelectedMiniGameEnum { get; private set; }

        public RandomizerMiniGameEndEvent(MiniGameEnum selectedMiniGame, string selectedMiniGameSceneName)
        {
            this.SelectedMiniGameEnum = selectedMiniGame;
        }
    }
}
