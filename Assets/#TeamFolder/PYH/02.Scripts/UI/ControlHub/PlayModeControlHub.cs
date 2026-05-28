using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using Assets._TeamFolder.PYH._02.Scripts.Enum;
using Assets._TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;

namespace Assets._TeamFolder.PYH._02.Scripts.UI
{
    public class PlayModeControlHub : MonoBehaviour
    {
        [SerializeField] private PlayModeUiControlHub playModeUi;
        [SerializeField] private JoinUiControlHub joinUi;
        [SerializeField] private HostUiControlHub hostUi;

        private void Awake()
        {
            AStarEventBus.Subscribe<PlayModeUiEvent>(InteractPlayMode);
            AStarEventBus.Subscribe<PlayModeSelectUiEvent>(SelectPlayMode);
            AStarEventBus.Subscribe<HostUiEvent>(HostUiEvent);
            AStarEventBus.Subscribe<JoinUiEvent>(JoinUiEvent);
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<PlayModeUiEvent>(InteractPlayMode);
            AStarEventBus.Unsubscribe<PlayModeSelectUiEvent>(SelectPlayMode);
            AStarEventBus.Unsubscribe<HostUiEvent>(HostUiEvent);
            AStarEventBus.Unsubscribe<JoinUiEvent>(JoinUiEvent);
        }

        private void HostUiEvent(HostUiEvent @event)
        {
            AStarEventBus.Publish(new PlayModeSelectUiEvent(PlayModeEnum.HOST));
        }
        private void JoinUiEvent(JoinUiEvent @event)
        {
            AStarEventBus.Publish(new PlayModeSelectUiEvent(PlayModeEnum.JOIN));
        }
        public void InteractPlayMode(PlayModeUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(playModeUi));
        }
        public void SelectPlayMode(PlayModeSelectUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(playModeUi));

            switch (@event.SelectMode)
            {
                case PlayModeEnum.HOST:
                    {
                        AStarEventBus.Publish(new UiInteractEvent(hostUi));
                        break;
                    }

                case PlayModeEnum.JOIN:
                    {
                        AStarEventBus.Publish(new UiInteractEvent(joinUi));
                        break;
                    }
            }
        }
    }
}
