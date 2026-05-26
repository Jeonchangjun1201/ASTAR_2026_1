using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.UI.Scene;
using _TeamFolder.PYH._02.Scripts.Util;
using _TeamFolder.PYH._04.Datas;
using Assets._TeamFolder.PYH._02.Scripts.UI;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _TeamFolder.PYH._02.Scripts
{
    public class UiInputManager : MonoSingleton<UiInputManager>
    {
        private string _curSceneName;
        
        private PopupUi _curPop;
        [SerializeField] private UiInputSO uiInputData;
        private bool _isOccupied;
        private bool _canInput = true;

        private new void Awake()
        {
            base.Awake();

            AStarEventBus.Subscribe<SetUiInputEvent>(SetInputControl);
            AStarEventBus.Subscribe<PopupClearEvent>(PopupClear);
            AStarEventBus.Subscribe<UiInteractEvent>(UiInteract);
            uiInputData.OnPlayEvent += PlayModeEventHandler;
            uiInputData.OnSettingEvent += SettingEventHandler;
            uiInputData.OnGuideEvent += GuideEventHandler;
            uiInputData.OnQuitEvent += QuitEventHandler;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<SetUiInputEvent>(SetInputControl);
            AStarEventBus.Unsubscribe<PopupClearEvent>(PopupClear);
            AStarEventBus.Unsubscribe<UiInteractEvent>(UiInteract);
            uiInputData.OnPlayEvent -= PlayModeEventHandler;
            uiInputData.OnSettingEvent -= SettingEventHandler;
            uiInputData.OnGuideEvent -= GuideEventHandler;
            uiInputData.OnQuitEvent -= QuitEventHandler;
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_curPop != null)
            {
                if (_curPop is SettingUiControlHub)
                {
                    SettingEventHandler();
                }
                else
                {
                    _isOccupied = false;
                }
                
                _curPop = null;
            }
            
            _curSceneName = scene.name;
        }
        
        private void PlayModeEventHandler()
        {
            if (!_canInput) return;
            if (_curSceneName != "Title Scene") return;
            
            AStarEventBus.Publish(new PlayModeUiEvent());
        }
        private void QuitEventHandler()
        {
            if (!_canInput) return;
            if (_curSceneName != "Title Scene") return;
            
            if (_isOccupied)
                return;
            
            AStarEventBus.Publish(new QuitUiEvent());
        }
        private void GuideEventHandler()
        {
            if (!_canInput) return;
            if (_curSceneName != "Title Scene") return;
        
            if (_isOccupied)
                return;
        
            AStarEventBus.Publish(new GuideUiEvent());
        }
        private void SettingEventHandler()
        {
            if (!_canInput) return;
        
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
            if ((_curPop != null && _curPop.GetType() != @event.Ui.GetType() && _isOccupied) || !_canInput)
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

        private void SetInputControl(SetUiInputEvent @event)
        {
            _canInput = @event.CanInput;   
        }

        private void PopupClear(PopupClearEvent @event)
        {
            if (_curPop != null)
            {
                SettingEventHandler();
                _curPop = null;
            }
        }
    }
}
