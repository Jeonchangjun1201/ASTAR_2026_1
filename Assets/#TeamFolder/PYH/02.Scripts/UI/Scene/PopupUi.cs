using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public abstract class PopupUi : MonoBehaviour
    {
        public bool IsOpen { get; protected set; }
        
        public abstract bool InteractPopup();
    }
}