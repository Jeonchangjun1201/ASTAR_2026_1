using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class TitleButtonControlHub : MonoBehaviour
    {
        public void OnClickPlayButton()
        {
            // 시작씬을 여기로 연결
        }

        public void OnClickSettingButton()
        {
            SettingControlHub.Instance.InteractSetting();
        }
        
        public void OnClickQuitButton()
        {
            QuitControlHub.Instance.InteractQuit();
        }
    }
}