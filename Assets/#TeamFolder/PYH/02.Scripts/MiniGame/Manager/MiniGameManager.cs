using PYH.MiniGame;
using UnityEngine;

namespace PYH.Manager
{
    public class MiniGameManager : MonoBehaviour
    {
        [SerializeField] private AbstractMiniGame miniGame;
        private IMiniGame mini;
        [field: SerializeField] public MiniGameType CurrentMiniGameType { get; private set; }

        public void Awake()
        {
            mini = miniGame as IMiniGame;
            mini.Initialize(); //BUG
        }

        public void OnMiniGameEndHandler()
        {
            Time.timeScale = 0;
            mini.OnMiniGameEndEvent.RemoveListener(OnMiniGameEndHandler);

            Debug.Log("Game End.");
        }
    }
}