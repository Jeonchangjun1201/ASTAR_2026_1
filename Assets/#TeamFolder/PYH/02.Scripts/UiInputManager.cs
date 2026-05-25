using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.UI.Scene;
using _TeamFolder.PYH._02.Scripts.Util;
using _TeamFolder.PYH._04.Datas;
using Assets._TeamFolder.PYH._02.Scripts.UI;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
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
            //uiInputData.OnPlayEvent += 
            uiInputData.OnSettingEvent += SettingEventHandler;
            uiInputData.OnGuideEvent += GuideEventHandler;
            uiInputData.OnQuitEvent += QuitEventHandler;
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<UiInteractEvent>(UiInteract);
            //uiInputData.OnPlayEvent -=
            uiInputData.OnSettingEvent -= SettingEventHandler;
            uiInputData.OnGuideEvent -= GuideEventHandler;
            uiInputData.OnQuitEvent -= QuitEventHandler;
        }

        private void QuitEventHandler()
        {
            if (!canInput) return;
            
            if (_isOccupied)
                return;
            
            AStarEventBus.Publish(new QuitUiEvent());
        }
        private void GuideEventHandler()
        {
            if (!canInput) return;
        
            if (_isOccupied)
                return;
        
            AStarEventBus.Publish(new GuideUiEvent());
        }
        private void SettingEventHandler()
        {
            if (!canInput) return;
        
            if (_isOccupied && _curPop is not SettingUiControlHub)
            {
                switch (_curPop)
                {
                    case TitleGuideUiControlHub:
                        AStarEventBus.Publish(new GuideUiEvent());
                        break;
                    case QuitUiControlHub:
                        AStarEventBus.Publish(new QuitUiEvent());
                        break;
                    case PlayModeUiControlHub:
                        AStarEventBus.Publish(new PlayModeUiEvent());
                        break;
                    case HostUiControlHub:
                        AStarEventBus.Publish(new HostUiEvent());
                        break;
                    case JoinUiControlHub:
                        AStarEventBus.Publish(new JoinUiEvent());
                        break;
                }

                return;
            }
            AStarEventBus.Publish(new SettingUiEvent());
        }
        
        private void UiInteract(UiInteractEvent @event)
        {
            if ((_curPop != null && _curPop.GetType() != @event.Ui.GetType() && _isOccupied) || !canInput)
                return;
            
            bool isOpen = @event.Ui.InteractPopup();
            _isOccupied = @event.Ui.IsOpen;

            if (!isOpen)
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
