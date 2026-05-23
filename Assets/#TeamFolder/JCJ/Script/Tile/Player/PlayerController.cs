using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using _TeamFolder.JCJ.Script; // IPlayerVisual / PartyCharacterVisual (미로와 공유)

// 타일 모드 플레이어 이동과 입력을 처리하는 컨트롤러.

namespace _TeamFolder.JCJ.TileGame
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("플레이어 슬롯(싱글에서 0=로컬).")]
        public int PlayerIndex;
        [Tooltip("스폰 시 렌더러에 입히는 기본 틴트.")]
        public Color PlayerColor = Color.white;
        [Tooltip("true = 키보드 입력, false = AI/원격.")]
        public bool IsLocalControlled = true;

        [Header("이동")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 6f;
        [Tooltip("이동 방향으로 몸을 돌리는 속도(클수록 빠름).")]
        [SerializeField] private float rotationSpeed = 14f;

        [Header("지면 판정")]
        [Tooltip("캡슐 아래로 쏘는 레이 길이.")]
        [SerializeField] private float groundCheckDist = 0.3f;
        [Tooltip("밟을 수 있는 레이어. 비우면(Nothing) 런타임에 DefaultRaycastLayers로 폴백(타일이 Default에 있음).")]
        [SerializeField] private LayerMask groundMask;

        [Header("탈락")]
        [SerializeField] private float eliminationY = -6f;

        // 플레이 상태.
        public event System.Action<PlayerController> OnFell;          // 떨어짐, 목숨 남음
        public event System.Action<PlayerController> OnEliminated;    // 목숨 없음, 이 플레이어 게임오버
        public event System.Action<PlayerController> FallResolutionRequested;

        public bool IsEliminated  { get; private set; }
        public bool IsGrounded    { get; private set; }
        public bool InputLocked   { get; set; }
        public int  LivesRemaining { get; private set; } = 3;
        public bool IsInvulnerable => Time.time < _invulnUntil;

        /// <summary>
        /// 라운드 종료 또는 탈락 시 <see cref="TileGameManager"/>가 설정.
        /// 0 = 미순위. 숫자가 작을수록 좋음(1=승자, N=먼저 죽은 순).
        /// </summary>
        public int FinalRank { get; set; }
        /// <summary>라운드에서 빠져 관전 중일 때 true.</summary>
        public bool IsSpectating { get; private set; }

        public bool SurvivedLastColorCall { get; set; }

        private Rigidbody _rb;
        private Collider  _collider;
        private Renderer  _renderer;
        private Color     _originalColor;
        private float     _invulnUntil;
        // Camera.main 캐시 — 태그 검색이라 FixedUpdate마다 부르기 비쌈.
        private Transform _cachedCameraTf;

        private InputActionMap _inputMap;
        private InputAction    _moveAction;
        private InputAction    _jumpAction;

        // 기믹 효과 상태.
        private float _speedMultiplier = 1f;
        private bool  _isInputInverted;

        private Coroutine _slowRoutine;
        private Coroutine _confusionRoutine;
        private Coroutine _balloonRoutine;

        // 애니메이션 훅.
        private IPlayerVisual _visual;
        private bool          _wasGrounded;
        private bool          _fallAnimPlaying;
        // 점프 직후 idle/walk로 덮지 않게 잠깐 막아 점프 클립이 재생되게 함.
        private float         _jumpAnimLockUntil;

        // ── Unity ────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _collider = GetComponent<Collider>();
            _renderer = GetComponentInChildren<Renderer>();

            // 마찰 0 재질 — 공중에서 벽 옆면에 붙는 현상 완화(기본 재질은 마찰 있음).
            if (_collider != null && _collider.sharedMaterial == null)
                JcjPhysicsMaterials.ApplyPlayerLowFriction(_collider);

            // groundMask가 Nothing이면 전체 레이캐스트 레이어로 폴백 — Default 타일도 검출.
            if (groundMask.value == 0)
                groundMask = Physics.DefaultRaycastLayers;

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null) _visual = gameObject.AddComponent<PartyCharacterVisual>();

            BuildInputActions();
        }

        private void OnEnable()
        {
            _inputMap?.Enable();
        }

        private void OnDisable()
        {
            JcjFootstepAudio.Stop();
            _inputMap?.Disable();
        }

        private void OnDestroy()
        {
            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        private void BuildInputActions()
        {
            // Tile 플레이어도 Maze와 같은 JCJInputActions를 사용한다.
            // 서버 연동 시 IsLocalControlled가 true인 소유 플레이어만 입력 맵을 켜야 한다.
            _inputMap   = JCJInputActions.CreateMap();
            _moveAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionMove);
            _jumpAction = JCJInputActions.Find(_inputMap, JCJInputActions.ActionJump);
        }

        public InputActionMap GetInputMap() => _inputMap;

        // 플레이어별 목숨 수를 라운드 설정으로 맞춘다.
        // 서버에서 슬롯별 시작 목숨이 다를 수 있다면 이 메서드로 반영하면 된다.
        public void ConfigureLives(int lives) => LivesRemaining = Mathf.Max(1, lives);

        // 플레이어 색상을 슬롯/팀 기준으로 적용한다.
        // 네트워크에서는 서버가 정한 팀색이나 플레이어색을 여기로 내려주면 된다.
        public void ApplyTint(Color color)
        {
            PlayerColor = color;
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;
            var mat = _renderer.material;
            _originalColor = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.25f);
            }
        }

        // 탈락 체크, 점프 입력, 비주얼 갱신을 처리하는 프레임 루프다.
        // 로컬 소유 플레이어만 입력을 읽고 원격 플레이어는 상태 표시만 하게 만들 때 경계로 보기 좋다.
        private void Update()
        {
            if (IsEliminated) return;
            CheckElimination();
            CheckGround();
            if (!InputLocked && IsLocalControlled) HandleJumpInput();
            UpdateVisualState();
        }

        private void FixedUpdate()
        {
            if (IsEliminated) return;
            // 현재는 로컬 테스트용으로 IsLocalControlled가 true인 플레이어만 움직인다.
            // 원격 플레이어는 서버/네트워크 위치 동기화로 움직이고, 여기 입력 기반 이동은 타지 않아야 한다.
            if (InputLocked || !IsLocalControlled)
            {
                JcjFootstepAudio.Stop();
                KillHorizontal();
                return;
            }
            HandleMovement();
            UpdateFootstepSfx();
        }

        private void UpdateVisualState()
        {
            if (_visual == null) return;

            // 착지: 공중이었다가 지면 → OnLand.
            if (!_wasGrounded && IsGrounded && _fallAnimPlaying)
            {
                _visual.OnLand();
                _fallAnimPlaying = false;
                _jumpAnimLockUntil = 0f;
            }

            // 점프 클립 재생 중에는 애니메이터에 idle/walk를 건드리지 않음 — jump→fall 전환 유지.
            bool jumpLocked = Time.time < _jumpAnimLockUntil;

            if (!IsGrounded)
            {
                // 하강 중이면 낙하 애니, 상승은 점프가 준 상태 유지.
                if (_rb.linearVelocity.y < -1.5f && !_fallAnimPlaying)
                {
                    _visual.OnFall();
                    _fallAnimPlaying = true;
                }
            }
            else if (!jumpLocked)
            {
                _fallAnimPlaying = false;

                Vector3 v = _rb.linearVelocity;
                float horiz = new Vector2(v.x, v.z).magnitude;
                const float moveThreshold = 0.4f;
                if (horiz > moveThreshold)
                {
                    float norm = Mathf.Clamp01(horiz / Mathf.Max(0.01f, moveSpeed * _speedMultiplier));
                    _visual.OnWalk(norm);
                }
                else
                {
                    _visual.OnIdle();
                }
            }

            _wasGrounded = IsGrounded;
        }

        private void KillHorizontal()
        {
            var v = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }

        // ── Input ────────────────────────────────────
        private void HandleMovement()
        {
            // 카메라 기준 WASD 이동이다.
            // Confusion 기믹이 켜져 있으면 입력 축을 반대로 뒤집어 조작 혼란 효과를 만든다.
            Vector2 moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            float h = _isInputInverted ? -moveInput.x : moveInput.x;
            float v = _isInputInverted ? -moveInput.y : moveInput.y;

            Vector3 desired = PlayerControllerMovementModule.GetCameraRelativeDirection(
                new Vector2(h, v), ResolveCameraTransform()) * (moveSpeed * _speedMultiplier);
            Vector3 move = desired;
            move.y = _rb.linearVelocity.y;
            _rb.linearVelocity = move;

            // 이동 방향 바라보기. 거의 정지면 스킵 — 스틱 놓을 때 이전 방향으로 튀지 않게.
            if (desired.sqrMagnitude > 0.04f)
            {
                Quaternion target = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, target, Time.fixedDeltaTime * rotationSpeed);
            }
        }

        private Transform ResolveCameraTransform()
        {
            if (_cachedCameraTf == null)
            {
                var cam = Camera.main;
                _cachedCameraTf = cam != null ? cam.transform : null;
            }

            return _cachedCameraTf;
        }

        private void HandleJumpInput()
        {
            if (_speedMultiplier < 0.3f) return;
            if (!IsGrounded) return;
            if (_jumpAction == null || !_jumpAction.WasPressedThisFrame()) return;

            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            TileAudio.PlayStatic(TileSfx.Jump, 0.7f, Random.Range(0.95f, 1.05f));
            _visual?.OnJump();
            _fallAnimPlaying = false;
            _jumpAnimLockUntil = Time.time + 0.35f;
        }

        private void UpdateFootstepSfx()
        {
            bool walking = IsGrounded && _rb.linearVelocity.sqrMagnitude >= 1.5f;
            if (!walking)
            {
                JcjFootstepAudio.Stop();
                return;
            }

            float speedNorm = Mathf.Clamp01(
                new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z).magnitude
                / Mathf.Max(0.01f, moveSpeed * _speedMultiplier));
            JcjFootstepAudio.SetWalking(true, 0.35f, Mathf.Lerp(0.95f, 1.1f, speedNorm));
        }

        private void CheckGround()
        {
            IsGrounded = PlayerControllerMovementModule.CheckGround(
                _collider, transform, groundCheckDist, groundMask);
        }

        // ── 기믹 효과(기존 계약) ─────────────────────
        public void ApplySlow(float speedRatio, float duration)
        {
            if (_slowRoutine != null) StopCoroutine(_slowRoutine);
            _slowRoutine = StartCoroutine(SlowRoutine(speedRatio, duration));
        }

        public void ApplyBalloon(float force, float duration)
        {
            if (_balloonRoutine != null) StopCoroutine(_balloonRoutine);
            _balloonRoutine = StartCoroutine(BalloonRoutine(force, duration));
        }

        public void ApplyLaunch(float force)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * force, ForceMode.Impulse);
            TileAudio.PlayStatic(TileSfx.Trampoline, 0.9f, 1.15f);
        }

        public void ApplyConfusion(float duration)
        {
            if (_confusionRoutine != null) StopCoroutine(_confusionRoutine);
            _confusionRoutine = StartCoroutine(ConfusionRoutine(duration));
        }

        private IEnumerator SlowRoutine(float speedRatio, float duration)
        {
            _speedMultiplier = speedRatio;
            TintTemporary(new Color(0.55f, 0.1f, 0.7f), duration);
            yield return new WaitForSeconds(duration);
            _speedMultiplier = 1f;
            _slowRoutine = null;
        }

        private IEnumerator BalloonRoutine(float force, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                _rb.AddForce(Vector3.up * force, ForceMode.Force);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            _balloonRoutine = null;
        }

        private IEnumerator ConfusionRoutine(float duration)
        {
            _isInputInverted = true;
            TintTemporary(new Color(0.9f, 0.2f, 0.7f), duration);
            yield return new WaitForSeconds(duration);
            _isInputInverted = false;
            _confusionRoutine = null;
        }

        private void TintTemporary(Color tint, float duration)
        {
            if (_renderer == null) return;
            var mat = _renderer.material;
            Color original = mat.color;
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            StartCoroutine(RestoreTint(mat, original, duration));
        }

        private IEnumerator RestoreTint(Material mat, Color original, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (mat == null) yield break;
            mat.color = _originalColor != default ? _originalColor : original;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
        }

        // ── 목숨·낙하·리스폰 ───────────────────────
        private void CheckElimination()
        {
            if (transform.position.y < eliminationY) FallOffBoard();
        }

        private void FallOffBoard()
        {
            // eliminationY 아래로 내려가면 목숨을 하나 잃는다.
            // 목숨이 남아 있으면 매니저에게 리스폰을 요청하고, 0이면 완전 탈락 처리한다.
            if (IsEliminated) return;
            if (Time.time < _invulnUntil) return; // 리스폰 무적 대기 중

            if (!JcjRuntimeAuthority.UseLocalSimulation)
            {
                InputLocked = true;
                _rb.isKinematic = true;
                _rb.linearVelocity = Vector3.zero;
                gameObject.SetActive(false);
                FallResolutionRequested?.Invoke(this);
                return;
            }

            LivesRemaining = Mathf.Max(0, LivesRemaining - 1);
            TileAudio.PlayStatic(TileSfx.FallScream, 0.9f, 1f);

            if (LivesRemaining <= 0)
            {
                Eliminate();
                return;
            }

            OnFell?.Invoke(this);

            // 몸은 비활성, 리스폰 타이머는 매니저가 — 이 오브젝트에서 코루틴 돌리면 SetActive(false)에 취소됨.
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            gameObject.SetActive(false);

            if (TileGameManager.Instance != null)
                TileGameManager.Instance.RequestRespawn(this);
        }

        public void ApplyAuthoritativeLifeState(int livesRemaining, bool eliminated)
        {
            LivesRemaining = Mathf.Max(0, livesRemaining);
            if (eliminated)
            {
                Eliminate();
                return;
            }

            OnFell?.Invoke(this);
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.linearVelocity = Vector3.zero;
            }
            gameObject.SetActive(false);
        }

        /// <summary>리스폰 지연 후 TileGameManager가 호출.</summary>
        // 매니저가 정한 안전 위치로 플레이어를 다시 활성화하는 단계다.
        // 서버가 리스폰 좌표와 무적 시간을 확정하면 그 결과를 여기서 로컬 오브젝트에 반영하면 된다.
        public void CompleteRespawn(Vector3 worldPos, float invulnDuration)
        {
            if (IsEliminated) return;
            transform.position = worldPos + Vector3.up * 1.5f;
            gameObject.SetActive(true);
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
            }
            _invulnUntil = Time.time + invulnDuration;
            TileAudio.PlayStatic(TileSfx.Respawn, 0.85f, 1f);
            StartCoroutine(InvulnBlinkRoutine(invulnDuration));
        }

        private IEnumerator InvulnBlinkRoutine(float duration)
        {
            if (_renderer == null) yield break;
            var mat = _renderer.material;
            Color original = mat.color;
            Color ghost = new Color(1f, 1f, 1f, 0.5f);
            float t = 0f;
            while (t < duration)
            {
                mat.color = Color.Lerp(original, ghost, Mathf.PingPong(t * 6f, 1f));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
                t += Time.deltaTime;
                yield return null;
            }
            mat.color = original;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", original);
        }

        // 플레이어를 최종 탈락 상태로 전환하는 지점이다.
        // 서버 매치에서는 이 메서드 호출 여부 자체가 서버 확정 이벤트와 1:1로 맞는 편이 가장 안전하다.
        private void Eliminate()
        {
            // 최종 탈락 처리다.
            // 서버 연동 시 이 함수의 결과(IsEliminated, FinalRank, IsSpectating)를 모든 클라에 동기화해야 한다.
            if (IsEliminated) return;
            IsEliminated = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic = true;
            // OnEliminated 리스너가 일관된 상태를 보게 관전 모드 먼저(IsSpectating, 렌더 끔, 입력 잠금).
            EnterSpectatorMode();
            OnEliminated?.Invoke(this);
        }

        /// <summary>
        /// 탈락한 몸을 화면 밖에 두고 입력·물리 끄고 메시/애니메이터 숨김 — 나머지 라운드 관전.
        /// </summary>
        public void EnterSpectatorMode()
        {
            IsSpectating = true;
            InputLocked = true;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            if (_collider != null) _collider.enabled = false;

            // 이 플레이어 하위 렌더러 전부 끔(몸·의상·그림자 등).
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            // 정지한 몸에서 점프/낙하 클립 잔향 끄기.
            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
        }
    }
}
