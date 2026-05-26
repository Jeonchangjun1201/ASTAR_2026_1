using System.Collections;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.Manager
{
    public class MiniGameManager : MonoBehaviour
    {
        [SerializeField] private AbstractMiniGame miniGame;
        private IMiniGame mini;
        [field: SerializeField] public MiniGameType CurrentMiniGameType { get; private set; }
        public UnityEvent OnMiniGameInitEvent;
        public UnityEvent OnMiniGameEndEvent;

        public void Awake()
        {
            mini = miniGame as IMiniGame;

            StartCoroutine(Count());
        }

        private IEnumerator Count()
        {
            int count = 4;
            Time.timeScale = 0;
            AStarEventBus.Publish(new PopupClearEvent());
            AStarEventBus.Publish(new SetUiInputEvent(false));
            AStarEventBus.Publish(new CountdownUiEvent());
            
            while (count > 0)
            {
                count--;
                yield return new WaitForSecondsRealtime(1);
            }
            
            AStarEventBus.Publish(new SetUiInputEvent(true));
            Time.timeScale = 1;
            mini.Initialize();
            OnMiniGameInitEvent?.Invoke();
        }
        
        public void OnMiniGameEndHandler() // non use
        {
            Time.timeScale = 0;
            mini.OnMiniGameEndEvent.RemoveListener(OnMiniGameEndHandler);
            OnMiniGameEndEvent?.Invoke();

            Debug.Log("Game End.");
        }
    }
}