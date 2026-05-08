using UnityEngine;

// 첫 완주자 보너스를 계산하고 적용하는 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 첫 번째 골인자가 나오면 남은 시간을 줄이고 필요하면 미니맵을 공개하는 보너스 규칙.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class MazeFirstFinisherBonus : MonoBehaviour
    {
        [SerializeField] private RankService _rankService;
        [SerializeField] private TimerService _timerService;
        [SerializeField] private MazeMinimap _minimap;
        [SerializeField] private ScoreConfig _scoreConfig;

        [Tooltip("ScoreConfig가 비어 있을 때 사용할 기본 단축 비율 (0~1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _fallbackShrinkRatio = 0.6f;

        [Tooltip("ScoreConfig가 비어 있을 때 미니맵 자동 공개 여부.")]
        [SerializeField] private bool _fallbackRevealMap = true;

        private bool _subscribed;
        private bool _firstFinisherProcessed;

        private void OnEnable()
        {
            ResolveReferencesIfNeeded();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            ResolveReferencesIfNeeded();
            Subscribe();
        }

        private void ResolveReferencesIfNeeded()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                if (_rankService == null) _rankService = gsm.GetComponent<RankService>();
                if (_timerService == null) _timerService = gsm.GetComponent<TimerService>();
            }
            if (_minimap == null) _minimap = FindFirstObjectByType<MazeMinimap>();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (_rankService == null) return;
            _rankService.OnPlayerFinished += HandlePlayerFinished;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_rankService != null) _rankService.OnPlayerFinished -= HandlePlayerFinished;
            _subscribed = false;
        }

        private void HandlePlayerFinished(string playerName, int rank)
        {
            if (rank != 1) return;
            if (_firstFinisherProcessed) return;
            _firstFinisherProcessed = true;
            ApplyBonus();
        }

        private void ApplyBonus()
        {
            // ScoreConfig가 연결되어 있으면 그 값을 우선하고, 없을 때만 인스펙터 기본값을 사용한다.
            float ratio = _scoreConfig != null
                ? Mathf.Clamp01(_scoreConfig.FirstFinisherTimeShrinkRatio)
                : Mathf.Clamp01(_fallbackShrinkRatio);

            if (_timerService != null && ratio > 0f)
            {
                float remaining = _timerService.Remaining;
                if (remaining > 0f) _timerService.AddTime(-remaining * ratio);
            }

            bool reveal = _scoreConfig != null
                ? _scoreConfig.FirstFinisherRevealMap
                : _fallbackRevealMap;

            if (reveal && _minimap != null) _minimap.RevealAll();

            Debug.Log($"[FirstFinisherBonus] timeShrink={ratio:0.00}, revealMap={reveal}");
        }

        public void ResetState()
        {
            _firstFinisherProcessed = false;
        }
    }
}
