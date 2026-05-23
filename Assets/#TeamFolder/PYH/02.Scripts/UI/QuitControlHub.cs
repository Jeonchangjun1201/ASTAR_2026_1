using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using _TeamFolder.PYH._02.Scripts.Util;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class QuitControlHub : MonoSingleton<QuitControlHub>
    {
        [SerializeField] private QuitUiControlHub quitUi;
        
        private new void Awake()
        {
            base.Awake();
            AStarEventBus.Subscribe<QuitCancelEvent>(InteractQuit);
            AStarEventBus.Subscribe<QuitUiEvent>(InteractQuit);
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<QuitCancelEvent>(InteractQuit);
            AStarEventBus.Unsubscribe<QuitUiEvent>(InteractQuit);
        }
        
        public void InteractQuit()
        {
            AStarEventBus.Publish(new UiInteractEvent(quitUi));
        }
        private void InteractQuit(QuitUiEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(quitUi));
        }
        private void InteractQuit(QuitCancelEvent @event)
        {
            AStarEventBus.Publish(new UiInteractEvent(quitUi));
        }
    }
}