using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.UI.Scene;
using _TeamFolder.PYH._02.Scripts.Util;
using _TeamFolder.PYH._04.Datas;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts
{
    public class UiInputManager : MonoSingleton<UiInputManager>
    {
        private PopupUi _curPop;
        [SerializeField] private UiInputSO uiInputData;
        private bool _isOccupied;
        private bool canInput = true;

        private new void Awake()
        {
            base.Awake();

            AStarEventBus.Subscribe<UiInteractEvent>(UiInteract);
            uiInputData.OnGuideEvent += GuideEventHandler;
            uiInputData.OnSettingEvent += SettingEventHandler;
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<UiInteractEvent>(UiInteract);
            uiInputData.OnGuideEvent -= GuideEventHandler;
            uiInputData.OnSettingEvent -= SettingEventHandler;
        }

        private void GuideEventHandler()
        {
            if (!_isOccupied || _curPop is TitleGuideUiControlHub) return;
            
            _isOccupied = true;
            AStarEventBus.Publish(new GuideUiEvent());
        }
        private void SettingEventHandler()
        {
            if (!canInput || _curPop is SettingUiControlHub) return;
            
            Debug.Log("진입함.");
            
            _isOccupied = true;
            AStarEventBus.Publish(new SettingUiEvent());
        }
        
        private void UiInteract(UiInteractEvent @event)
        {
            Debug.Log("진입점 333");
            Debug.Log($"현제 UI가 없고, 호출 UI가 동일한가? : {_curPop != null && _curPop.GetType() == @event.Ui.GetType()}");
            Debug.Log($"누군가가 점유 중인가? : {_isOccupied}");
            Debug.Log($"입력이 가능한가? : {canInput}");
            
            if ((_curPop != null && _curPop.GetType() != @event.Ui.GetType() && _isOccupied) || !canInput)
                return;

            
            Debug.Log("진입점 444");
            _isOccupied = !@event.Ui.IsOpen;
            @event.Ui.InteractPopup();

            if (!@event.Ui.IsOpen)
            {
                _curPop = null;
            }
            else
            {
                _curPop = @event.Ui;
            }
        }
    }
}
