using _TeamFolder.PYH._02.Scripts.Enum;

namespace PHY.Scripts
{
    public class RandomizerMiniGameEndEvent 
    {
        public MiniGameEnum SelectedMiniGame { get; private set; }
        public string SelectedMiniGameSceneName { get; private set; }

        public RandomizerMiniGameEndEvent(MiniGameEnum selectedMiniGame, string selectedMiniGameSceneName)
        {
            this.SelectedMiniGame = selectedMiniGame;
            this.SelectedMiniGameSceneName = selectedMiniGameSceneName;
        }
    }
}
