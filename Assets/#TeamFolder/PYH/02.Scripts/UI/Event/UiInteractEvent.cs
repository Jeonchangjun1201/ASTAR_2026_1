using _TeamFolder.PYH._02.Scripts.UI.Scene;

namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class UiInteractEvent
    {
        public PopupUi Ui { get; private set; }
        
        public UiInteractEvent(PopupUi ui)
        {
            Ui = ui;
        }
    }
}