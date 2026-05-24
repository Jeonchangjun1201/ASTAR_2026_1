using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class QuitButtonControlHub : MonoBehaviour
    {
        public void OnClickQuit()
        {
            Application.Quit();
        }

        public void OnClickCancel()
        {
            AStarEventBus.Publish(new QuitCancelEvent());
        }
    }
}
