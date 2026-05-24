using _TeamFolder.PYH._02.Scripts.Enum;

namespace _TeamFolder.PYH._02.Scripts.UI.Event
{
    public class SettingPopupUiEvent
    {
        public SettingPopupEnum PopupType { get; private set; }

        public SettingPopupUiEvent(SettingPopupEnum popupType)
        {
            PopupType = popupType;
        }
    }
}