using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingButtonControlHub : MonoBehaviour
    {
        public void OnClickSound()
        {
            AStarEventBus.Publish(new SettingPopupUiEvent(SettingPopupEnum.SOUND));
        }
    
        public void OnClickDisplay()
        {
            AStarEventBus.Publish(new SettingPopupUiEvent(SettingPopupEnum.DISPLAY));
        }
    }
}
