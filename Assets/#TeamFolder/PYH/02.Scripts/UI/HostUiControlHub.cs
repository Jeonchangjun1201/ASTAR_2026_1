using _TeamFolder.PYH._02.Scripts.UI.Scene;
using UnityEngine;

namespace Assets._TeamFolder.PYH._02.Scripts.UI
{
    internal class HostUiControlHub : PopupUi
    {
        [SerializeField] private CanvasGroup popupCanvas;

        public override bool InteractPopup()
        {
            IsOpen = !IsOpen;

            popupCanvas.alpha = IsOpen ? 1 : 0;
            popupCanvas.interactable = IsOpen;
            popupCanvas.blocksRaycasts = IsOpen;

            return IsOpen;
        }
    }
}
