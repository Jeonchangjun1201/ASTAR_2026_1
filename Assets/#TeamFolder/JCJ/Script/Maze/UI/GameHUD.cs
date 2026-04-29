using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// HUD 문구는 영문(TMP 기본 폰트 대비). DOTween으로 다듬고, 비어 있으면 MazeHUDBuilder가 UI를 만든다.
    /// GameStateManager의 타이머·랭크·카운트다운·점수 서비스를 구독해 트윈으로 반응한다.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("핵심(선택 — 비어 있으면 자동 생성)")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _rankFeedText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private Image _staminaFill;

        [Header("패널")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private CanvasGroup _resultGroup;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Transform _restartButtonVisual;

        [Header("플레이어 참조(스태미나 바)")]
        [SerializeField] private PlayerController _localPlayer;

        private MazeHUDBuilder _builder;
        private bool _timerLowActive;

        private void Awake()
        {
            _builder = gameObject.AddComponent<MazeHUDBuilder>();
            _builder.Build();

            _timerText           ??= _builder.TimerText;
            _rankFeedText        ??= _builder.RankFeedText;
            _scoreText           ??= _builder.ScoreText;
            _countdownText       ??= _builder.CountdownText;
            _staminaSlider       ??= _builder.StaminaSlider;
            _staminaFill         ??= _builder.StaminaFill;
            _resultPanel         ??= _builder.ResultPanel;
            _resultText          ??= _builder.ResultText;
            _resultGroup         ??= _builder.ResultCanvasGroup;
            _restartButton       ??= _builder.RestartButton;
            _restartButtonVisual ??= _builder.RestartButtonVisual;
        }

        private void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                Debug.LogWarning("[GameHUD] GameStateManager not found.");
                return;
            }

            gsm.OnStateChanged += HandleStateChanged;
            if (gsm.Timer != null)     gsm.Timer.OnTimerUpdated += RefreshTimer;
            if (gsm.Rank != null)
            {
                gsm.Rank.OnPlayerFinished += AppendRankFeed;
                gsm.Rank.OnAllFinished    += ShowResult;
            }
            if (gsm.Countdown != null)
            {
                gsm.Countdown.OnTick += ShowCountdown;
                gsm.Countdown.OnGo   += ShowGo;
            }
            if (gsm.Score != null)
                gsm.Score.OnScoreChanged += RefreshScore;

            if (_rankFeedText != null)  _rankFeedText.text  = string.Empty;
            if (_countdownText != null) _countdownText.text = string.Empty;
            if (_resultGroup != null)
            {
                _resultGroup.gameObject.SetActive(false);
                _resultGroup.alpha = 0f;
            }
            else _resultPanel?.SetActive(false);

            if (_restartButton != null) _restartButton.onClick.AddListener(RequestRestart);
        }

        private void OnDestroy()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            gsm.OnStateChanged -= HandleStateChanged;
            if (gsm.Timer != null)     gsm.Timer.OnTimerUpdated  -= RefreshTimer;
            if (gsm.Rank != null)
            {
                gsm.Rank.OnPlayerFinished -= AppendRankFeed;
                gsm.Rank.OnAllFinished    -= ShowResult;
            }
            if (gsm.Countdown != null)
            {
                gsm.Countdown.OnTick -= ShowCountdown;
                gsm.Countdown.OnGo   -= ShowGo;
            }
            if (gsm.Score != null)
                gsm.Score.OnScoreChanged -= RefreshScore;
        }

        private void Update()
        {
            if (_staminaSlider == null) return;
            if (_localPlayer == null)
            {
                // 플레이어 스폰 중 매 프레임 FindFirstObjectByType를 호출하지 않도록 MazeManager 목록을 먼저 확인한다.
                var mm = MazeManager.Instance;
                if (mm != null && mm.Players != null && mm.Players.Count > 0)
                {
                    for (int i = 0; i < mm.Players.Count; i++)
                    {
                        var go = mm.Players[i];
                        if (go == null) continue;
                        _localPlayer = go.GetComponent<PlayerController>();
                        if (_localPlayer != null) break;
                    }
                }
                if (_localPlayer == null)
                    _localPlayer = Object.FindFirstObjectByType<PlayerController>();
                if (_localPlayer == null) return;
            }
            float target = _localPlayer.MaxStamina > 0f
                ? _localPlayer.Stamina / _localPlayer.MaxStamina
                : 0f;
            HudTweenHelpers.FillTween(_staminaSlider, target);
            if (_staminaFill != null)
                _staminaFill.color = Color.Lerp(JCJUiColors.HudDanger, JCJUiColors.HudPrimaryText, Mathf.Clamp01(target * 1.3f));
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Playing) ClearCountdownImmediate();
            // 종료 화면은 PodiumPresenter가 담당 — 여기서는 구식 결과 패널 대신 진행 중 HUD만 정리.
            if (state == GameState.Waiting)
            {
                if (_rankFeedText != null) _rankFeedText.text = string.Empty;
                HudTweenHelpers.HidePanel(_resultGroup);
                HudTweenHelpers.StopColorLoop(_timerText, JCJUiColors.HudPrimaryText);
                _timerLowActive = false;
            }
        }

        private void RefreshTimer(float remaining)
        {
            if (_timerText == null) return;
            // 서비스가 0 아래로 잠깐 가도 "-1:-3" 같은 표시가 나오지 않게 클램프.
            float safe = Mathf.Max(0f, remaining);
            int min = Mathf.FloorToInt(safe / 60f);
            int sec = Mathf.FloorToInt(safe % 60f);
            _timerText.text = $"{min:00}:{sec:00}";

            bool low = remaining <= 10f && remaining > 0f;
            if (low && !_timerLowActive)
            {
                HudTweenHelpers.PulseRed(_timerText);
                _timerLowActive = true;
            }
            else if (!low && _timerLowActive)
            {
                HudTweenHelpers.StopColorLoop(_timerText, JCJUiColors.HudPrimaryText);
                _timerLowActive = false;
            }
        }

        private void AppendRankFeed(string playerName, int rank)
        {
            if (_rankFeedText == null) return;
            string medal = rank switch { 1 => "#1", 2 => "#2", 3 => "#3", _ => $"#{rank}" };
            _rankFeedText.text += $"{medal}  {playerName}\n";
            HudTweenHelpers.PunchScale(_rankFeedText.transform);
        }

        private void ShowResult(List<PlayerRankData> rankings)
        {
            if (_resultText == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("──── RESULTS ────\n");
            foreach (var r in rankings)
                sb.AppendLine($"#{r.Rank}   {r.PlayerName}   —   {r.Score} pts");
            _resultText.text = sb.ToString();
        }

        private void RefreshScore(string playerName, int delta, int total)
        {
            if (_scoreText == null) return;
            HudTweenHelpers.TickUpNumber(_scoreText, total, "SCORE  ", duration: 0.4f);
            HudTweenHelpers.PunchScale(_scoreText.transform, 0.2f, 0.25f);

            // 짧은 전체 화면 틴트 — 스태미나 오브는 민트, 코인은 따뜻한 금색.
            Color tint = delta >= 10
                ? new Color(1.00f, 0.92f, 0.55f)  // 코인
                : new Color(0.55f, 0.95f, 0.75f); // 오브
            HudTweenHelpers.FlashFullscreen(tint, duration: 0.25f, maxAlpha: 0.20f);
        }

        private void ShowCountdown(int remaining)
        {
            if (_countdownText == null) return;
            _countdownText.text = remaining.ToString();
            HudTweenHelpers.BounceCountdown(_countdownText.transform);
        }

        private void ShowGo()
        {
            if (_countdownText == null) return;
            _countdownText.text = "GO!";
            HudTweenHelpers.GoBurst(_countdownText.transform, _countdownText);
        }

        private void ClearCountdownImmediate()
        {
            if (_countdownText == null) return;
            _countdownText.text = string.Empty;
            _countdownText.transform.localScale = Vector3.zero;
        }

        private void RequestRestart()
        {
            var mm = MazeManager.Instance;
            if (mm == null) return;

            HudTweenHelpers.HidePanel(_resultGroup);
            mm.GenerateMazeWithButton();
        }
    }
}
