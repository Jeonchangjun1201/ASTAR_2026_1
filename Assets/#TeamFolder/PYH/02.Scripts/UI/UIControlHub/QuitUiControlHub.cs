using _TeamFolder.PYH._02.Scripts.UI.Scene;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class QuitUiControlHub : PopupUi
    {
        [SerializeField] private CanvasGroup popupCanvas;
        [SerializeField] private CanvasGroup settingCanvas;
        
        public override bool InteractPopup()
        {
            IsOpen = !IsOpen;
                        
            popupCanvas.alpha = IsOpen ? 1 : 0;
            popupCanvas.interactable = IsOpen;
            popupCanvas.blocksRaycasts = IsOpen;
            
            settingCanvas.alpha = IsOpen ? 1 : 0;
            settingCanvas.interactable = IsOpen;
            settingCanvas.blocksRaycasts = IsOpen;
            
            return IsOpen;
        }
    }
}