using PYH.MiniGame;
using UnityEngine;

namespace PYH.Manager
{
    public class MiniGameManager : MonoBehaviour
    {
        [SerializeField] private AbstractMiniGame miniGame;
        private IMiniGame iMiniGame;
        [SerializeField] private TimeManager timeManager;
        [field: SerializeField] public MiniGameType CurrentMiniGameType { get; private set; }

        public void Awake()
        {
            iMiniGame = miniGame as IMiniGame;
            
            Debug.Assert(iMiniGame != null, "IMiniGame is NULL!");
            Debug.Assert(timeManager != null, "TimeManager is NULL!");

            timeManager.OnTickEndEvent.AddListener(iMiniGame!.GameEnd);

            iMiniGame.Initialize();
            timeManager.Initialize();
        }

        public void OnMiniGameEndHandler()
        {
            Time.timeScale = 0;

            iMiniGame.OnMiniGameEndEvent.RemoveListener(OnMiniGameEndHandler);
            timeManager.OnTickEndEvent.RemoveListener(iMiniGame.GameEnd);

            Debug.Log("Game End.");
        }
    }
}