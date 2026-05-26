namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class RandomizerMiniGameInitEvent
    {
        public PlayerInfo[] Infos { get; private set; }

        public RandomizerMiniGameInitEvent(PlayerInfo[] infos)
        {
            Infos = infos;
        }
    }
}