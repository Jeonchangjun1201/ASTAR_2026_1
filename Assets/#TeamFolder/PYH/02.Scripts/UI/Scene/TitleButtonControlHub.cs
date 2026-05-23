using _TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class TitleButtonControlHub : MonoBehaviour
    {
        public void OnClickPlayButton()
        {
            
        }

        public void OnClickSettingButton()
        {
            SettingControlHub.Instance.InteractSetting(new SettingUiEvent());
        }
        
        public void OnClickQuitButton()
        {
            Application.Quit();
        }
    }
}