using _TeamFolder.PYH._02.Scripts.UI.Scene;
using System;
using UnityEngine;

namespace Assets._TeamFolder.PYH._02.Scripts.UI
{
    public class JoinUiControlHub : PopupUi
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
