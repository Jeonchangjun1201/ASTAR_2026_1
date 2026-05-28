using _TeamFolder.PYH._02.Scripts.UI.Scene;
using UnityEngine;

public class PlayModeUiControlHub : PopupUi
{
    [SerializeField] private CanvasGroup popupCanvas;

    public override bool InteractPopup() // don't use on button => on click event
    {
        IsOpen = !IsOpen;

        popupCanvas.alpha = IsOpen ? 1 : 0;
        popupCanvas.interactable = IsOpen;
        popupCanvas.blocksRaycasts = IsOpen;

        return IsOpen;
    }
}
