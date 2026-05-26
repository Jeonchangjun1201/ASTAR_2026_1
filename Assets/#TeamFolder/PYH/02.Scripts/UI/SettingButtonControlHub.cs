using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class SettingButtonControlHub : MonoBehaviour
    {
        public void OnClickSound()
        {
            AStarEventBus.Publish(new SettingPopupUiEvent(SettingPopupEnum.SOUND));
        }
        public void OnClickClose()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
        }
        public void OnClickDisplay()
        {
            AStarEventBus.Publish(new SettingPopupUiEvent(SettingPopupEnum.DISPLAY));
        }

        public void OnClickQuitInSetting()
        {
            AStarEventBus.Publish(new SettingUiEvent());
            AStarEventBus.Publish(new QuitUiEvent());
        }
    }
}
