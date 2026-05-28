using _TeamFolder.PYH._02.Scripts.Data;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class TitleButtonControlHub : MonoBehaviour
    {
        public void OnClickPlayButton()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            AStarEventBus.Publish(new PlayModeUiEvent());
        }

        public void OnClickSettingButton()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            SettingControlHub.Instance.InteractSetting();
        }
        
        public void OnClickQuitButton()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            QuitControlHub.Instance.InteractQuit();
        }
    }
}