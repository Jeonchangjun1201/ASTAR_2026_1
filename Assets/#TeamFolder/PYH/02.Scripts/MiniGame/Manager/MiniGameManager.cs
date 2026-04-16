using PYH.MiniGame;
using UnityEngine;

namespace PYH.Manager
{
    public class MiniGameManager : MonoBehaviour
    {
        // ALL SCRIPTS IS JUST FOR TEST, OK?
        [SerializeField] private HumanGolf _humanGolf;
        [SerializeField] private TimeManager _timeManager;
        public MiniGameType CurrentMiniGameType { get; private set; }

        public void Awake()
        {
            Debug.Assert(_humanGolf != null, "HumanGolf is NULL!");
            Debug.Assert(_timeManager != null, "TimeManager is NULL!");

            CurrentMiniGameType = MiniGameType.HumanGolf;

            _timeManager.OnTickEndEvent.AddListener(_humanGolf.GameEnd);
            _humanGolf.OnMiniGameEndEvent += OnMiniGameEndHandler;

            _humanGolf.Initialize();
            _timeManager.Initialize();
        }

        private void OnMiniGameEndHandler() // For Test, Not Yet.
        {
            Time.timeScale = 0;

            _humanGolf.OnMiniGameEndEvent -= OnMiniGameEndHandler;
            _timeManager.OnTickEndEvent.RemoveListener(_humanGolf.GameEnd);

            Debug.Log("Game End.");
        }
    }
}