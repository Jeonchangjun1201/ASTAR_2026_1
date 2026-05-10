using PYH.MiniGame;
using UnityEngine;

namespace PYH.Manager
{
    public class MiniGameManager : MonoBehaviour
    {
        [SerializeField] private AbstractMiniGame miniGame;
        private IMiniGame mini;
        [SerializeField] private TimeManager timeManager;
        [field: SerializeField] public MiniGameType CurrentMiniGameType { get; private set; }

        public void Awake()
        {
            mini = miniGame as IMiniGame;
            Debug.Assert(timeManager != null, "TimeManager is NULL!");

            timeManager.OnTickEndEvent.AddListener(mini!.GameEnd);

            mini.Initialize(); //BUG
            timeManager.Initialize();
        }

        public void OnMiniGameEndHandler()
        {
            Time.timeScale = 0;

            mini.OnMiniGameEndEvent.RemoveListener(OnMiniGameEndHandler);
            timeManager.OnTickEndEvent.RemoveListener(mini.GameEnd);

            Debug.Log("Game End.");
        }
    }
}