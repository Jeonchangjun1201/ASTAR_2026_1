using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class QuitButtonControlHub : MonoBehaviour
    {
        public void OnClickQuit()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            Application.Quit();
        }

        public void OnClickCancel()
        {
            SoundManager.Instance.PlaySound("General-Ui_Click");
            AStarEventBus.Publish(new QuitCancelEvent());
        }
    }
}
