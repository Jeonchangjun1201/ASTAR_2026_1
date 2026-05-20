using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//  골인 시 재생되는 시각 효과 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 통과 시 “FINISH!” 연출.
    ///  - 골(또는 오버라이드 트랜스폼)에 컨페티 파티클.
    ///  - 전체 화면 흰 플래시.
    ///  - 큰 배너 + 바운스.
    ///  - 잠깐 Time.timeScale을 내려 무게감.
    ///  - 있으면 <see cref="MazeCameraRig"/>로 카메라 흔들기.
    /// </summary>
    public class GoalFinishFX : MonoBehaviour
    {
        [Header("타이밍")]
        [Tooltip("Time.timeScale이 _slowScale까지 내려간 뒤 1로 돌아오기까지 걸리는 시간.")]
        [SerializeField] private float _slowDuration = 0.45f;
        [SerializeField] [Range(0.1f, 1f)] private float _slowScale = 0.45f;
        [SerializeField] private float _bannerLife = 1.8f;

        [Header("컨페티")]
        [SerializeField] private int _confettiPieces = 160;
        [SerializeField] private float _confettiBurstSpeed = 12f;

        [Header("오버레이")]
        [SerializeField] private Color _flashColor = new(1f, 1f, 1f, 0.85f);
        [Tooltip("로컬 플레이어 이름. 이 이름일 때만 큰 배너(봇/상대가 골 넣어도 로컬 화면이 안 뺏김).")]
        [SerializeField] private string _localPlayerName = "Player";
        [SerializeField] private string _localPlayerId = "maze.player.1";

        private Canvas _canvas;
        private CanvasGroup _flashGroup;
        private Image _flashImage;
        private TextMeshProUGUI _bannerText;
        private CanvasGroup _bannerGroup;
        private IRankService _rank;
        private Transform _goalOverride;
        private bool _hooked;

        // 슬로모 중 씬 언로드돼도 OnDestroy에서 timeScale·Invoke를 확실히 복구하기 위한 기록.
        private Coroutine _flashCo;
        private Coroutine _slowmoCo;
        private bool  _slowmoActive;
        private float _savedTimeScale = 1f;
        private float _savedFixedDelta;
        private int   _hookRetries;
        private const int _maxHookRetries = 30; // 0.2초 폴링 시 약 6초

        public void SetLocalPlayerName(string name)
        {
            if (!string.IsNullOrEmpty(name)) _localPlayerName = name;
        }

        public void SetLocalPlayerIdentity(string playerId, string displayName)
        {
            if (!string.IsNullOrWhiteSpace(playerId)) _localPlayerId = playerId;
            if (!string.IsNullOrWhiteSpace(displayName)) _localPlayerName = displayName;
        }

        public void SetGoalTransform(Transform goal) => _goalOverride = goal;

        private void Start()
        {
            EnsureOverlay();
            HookRankService();
        }

        private void OnDestroy()
        {
            UnhookRankService();
            CancelInvoke(nameof(HookRankService));
            if (_flashCo != null) StopCoroutine(_flashCo);
            if (_slowmoCo != null) StopCoroutine(_slowmoCo);
            // 슬로모 중이면 timeScale 복구 — 다음 씬이 느리게 고정되는 것 방지.
            if (_slowmoActive)
            {
                Time.timeScale = 1f;
                if (_savedFixedDelta > 0f) Time.fixedDeltaTime = _savedFixedDelta;
                _slowmoActive = false;
            }
        }

        private void HookRankService()
        {
            if (_hooked) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                if (_hookRetries++ < _maxHookRetries)
                    Invoke(nameof(HookRankService), 0.2f);
                return;
            }
            _rank = gsm.Rank;
            if (_rank != null)
            {
                _rank.OnPlayerFinishedData += HandlePlayerFinished;
                _hooked = true;
            }
        }

        private void UnhookRankService()
        {
            if (!_hooked || _rank == null) return;
            _rank.OnPlayerFinishedData -= HandlePlayerFinished;
            _hooked = false;
        }

        private void HandlePlayerFinished(PlayerRankData entry)
        {
            // 컨페티·사운드는 전원. 큰 배너·슬로모는 로컬 도착자만.
            SpawnConfetti();
            if (IsLocal(entry))
            {
                if (_flashCo != null) StopCoroutine(_flashCo);
                _flashCo = StartCoroutine(FlashOverlayRoutine());
                if (_slowmoCo != null) StopCoroutine(_slowmoCo);
                _slowmoCo = StartCoroutine(SlowmoRoutine());
                ShowBanner(entry.PlayerName, entry.Rank);
                ShakeCamera();
            }
        }

        private bool IsLocal(PlayerRankData entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.PlayerId) &&
                _localPlayerId.Equals(entry.PlayerId, System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.IsNullOrEmpty(entry.PlayerName)) return false;
            if (_localPlayerName.Equals(entry.PlayerName, System.StringComparison.OrdinalIgnoreCase)) return true;
            return entry.PlayerName.StartsWith(_localPlayerName, System.StringComparison.OrdinalIgnoreCase);
        }

        // ── 오버레이·배너 ─────────────────────────────

        private void EnsureOverlay()
        {
            _canvas = FindOrCreateCanvas();

            var flashGo = new GameObject("FinishFlash");
            flashGo.transform.SetParent(_canvas.transform, false);
            _flashImage = flashGo.AddComponent<Image>();
            _flashImage.color = _flashColor;
            _flashImage.raycastTarget = false;
            var rt = _flashImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _flashGroup = flashGo.AddComponent<CanvasGroup>();
            _flashGroup.alpha = 0f;
            _flashGroup.blocksRaycasts = false;
            _flashGroup.interactable   = false;

            var bannerGo = new GameObject("FinishBanner");
            bannerGo.transform.SetParent(_canvas.transform, false);
            _bannerText = bannerGo.AddComponent<TextMeshProUGUI>();
            _bannerText.alignment = TextAlignmentOptions.Center;
            _bannerText.enableAutoSizing = false;
            _bannerText.fontSize = 110f;
            _bannerText.fontStyle = FontStyles.Bold;
            _bannerText.color = new Color(0.98f, 0.98f, 1f, 1f);
            var brt = _bannerText.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(1400f, 260f);
            brt.anchoredPosition = new Vector2(0f, 120f);
            _bannerGroup = bannerGo.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable   = false;
        }

        private Canvas FindOrCreateCanvas()
        {
            var c = Object.FindFirstObjectByType<Canvas>();
            if (c != null) return c;
            var go = new GameObject("Canvas (auto)");
            c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 5;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        private IEnumerator FlashOverlayRoutine()
        {
            if (_flashGroup == null) yield break;
            _flashGroup.DOKill(true);
            _flashGroup.alpha = 1f;
            _flashGroup.DOFade(0f, 0.55f).SetEase(Ease.OutQuad).SetUpdate(true);
            yield break;
        }

        private void ShowBanner(string playerName, int rank)
        {
            if (_bannerText == null || _bannerGroup == null) return;

            string medal = rank switch { 1 => "#1", 2 => "#2", 3 => "#3", _ => $"#{rank}" };
            string prefix = rank == 1 ? "VICTORY!" : "FINISH!";
            _bannerText.text = $"{prefix}\n<size=60><color=#CFD3DA>{medal}  {playerName}</color></size>";
            _bannerText.color = rank == 1 ? new Color(1f, 0.95f, 0.55f) : new Color(0.95f, 0.96f, 1f);

            _bannerGroup.DOKill(true);
            _bannerText.transform.DOKill(true);
            _bannerText.transform.localScale = Vector3.one * 0.3f;
            _bannerGroup.alpha = 0f;

            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(_bannerGroup.DOFade(1f, 0.18f));
            seq.Join(_bannerText.transform.DOScale(1.15f, 0.28f).SetEase(Ease.OutBack));
            seq.Append(_bannerText.transform.DOScale(1.0f, 0.12f).SetEase(Ease.InOutSine));
            seq.AppendInterval(_bannerLife);
            seq.Append(_bannerGroup.DOFade(0f, 0.35f));
        }

        private IEnumerator SlowmoRoutine()
        {
            _savedTimeScale = Time.timeScale;
            _savedFixedDelta = Time.fixedDeltaTime;
            _slowmoActive = true;
            try
            {
                Time.timeScale = _slowScale;
                Time.fixedDeltaTime = _savedFixedDelta * _slowScale;

                float t = 0f;
                while (t < _slowDuration)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                // 약 0.25초에 1로 부드럽게 — 툭 튀는 느낌 완화.
                float ease = 0f;
                while (ease < 0.25f)
                {
                    ease += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(ease / 0.25f);
                    Time.timeScale = Mathf.Lerp(_slowScale, 1f, u);
                    Time.fixedDeltaTime = _savedFixedDelta * Time.timeScale;
                    yield return null;
                }
            }
            finally
            {
                // 코루틴 취소(슬로모 중 씬 리로드 등)에도 timeScale 복구 보장.
                Time.timeScale = 1f;
                Time.fixedDeltaTime = _savedFixedDelta;
                _slowmoActive = false;
                _slowmoCo = null;
            }
        }

        private static void ShakeCamera()
        {
            // 있으면 살짝 카메라 흔들기, 없으면 무시.
            var rig = MazeCameraRig.Instance;
            if (rig == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            // 브레인이 붙은 카메라 트랜스폼은 트윈 금지 — Cinemachine이 매 LateUpdate 덮어써서 떨림이 끊김.
            // CinemachineBrain이 있으면 리그(팔로우 타깃)를 흔든다.
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            Transform shakeTarget = brain != null ? rig.transform : cam.transform;
            shakeTarget.DOKill(true);
            shakeTarget.DOShakePosition(0.35f, 0.25f, vibrato: 12, randomness: 70f, snapping: false, fadeOut: true)
                       .SetUpdate(true);
        }

        // ── 컨페티 파티클 ─────────────────────────────

        private void SpawnConfetti()
        {
            Vector3 worldPos;
            if (_goalOverride != null) worldPos = _goalOverride.position;
            else
            {
                var mm = MazeManager.Instance;
                if (mm == null) return;
                worldPos = mm.CellToWorld(mm.GoalCell.x, mm.GoalCell.y) + Vector3.up * 1.5f;
            }

            var go = new GameObject("ConfettiBurst");
            go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.startLifetime = 2.6f;
            main.startSpeed = _confettiBurstSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = MakeConfettiGradient();
            main.gravityModifier = 1.15f;
            main.maxParticles = 512;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, _confettiPieces)
            });

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            color.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(shader);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Destroy(go, 4f);
        }

        private static ParticleSystem.MinMaxGradient MakeConfettiGradient()
        {
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.35f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.85f, 1f), 0.33f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.65f), 0.66f),
                    new GradientColorKey(new Color(0.55f, 1f, 0.75f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(grad)
                { mode = ParticleSystemGradientMode.RandomColor };
        }
    }
}
