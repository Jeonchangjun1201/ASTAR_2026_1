using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using _TeamFolder.JCJ.Script;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// 조준과 반동을 반영하는 1인칭 전투 카메라.

namespace _TeamFolder.JCJ.Battle
{
    [DefaultExecutionOrder(100)]
    public class BattleFirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _cameraOffset = new(0.03f, 0.64f, 0.12f);
        [SerializeField] private Vector3 _cameraOffsetExtra;
        [SerializeField] private float _nearClipPlane = 0.02f;
        [SerializeField] private Color _crosshairColor = new(1f, 0.45f, 0.1f, 0.95f);
        [SerializeField] private float _crosshairGap = 7f;
        [SerializeField] private float _crosshairLength = 8f;
        [SerializeField] private float _crosshairThickness = 2f;
        [SerializeField] private float _defaultFov = 61f;
        [SerializeField] private float _adsFov = 34f;
        [SerializeField] private float _adsSpeed = 10f;
        [SerializeField] private float _minLookPitch = -34f;
        [SerializeField] private float _maxLookPitch = 52f;
        [SerializeField] private bool _suspendAutomaticCameraFollow;
        [SerializeField] private bool _thirdPersonMode;
        [SerializeField] private float _thirdPersonDistance = 5.35f;
        [SerializeField] private float _thirdPersonPivotHeight = 1.08f;
        [SerializeField] private float _thirdPersonShoulderOffset = 0.42f;
        [SerializeField] private float _thirdPersonHeightBias = 0.28f;
        [SerializeField] private float _thirdPersonNearClip = 0.07f;
        [SerializeField] private float _thirdPersonFieldOfView = 72f;
        [SerializeField] private LayerMask _thirdPersonObstructionMask = ~0;
        [SerializeField] private float _thirdPersonProbeRadius = 0.22f;
        [SerializeField] private float _thirdPersonProbeStartInset = 0.12f;
        [SerializeField] private float _thirdPersonWallPadding = 0.1f;
        [SerializeField] private float _thirdPersonMinArmLength = 0.52f;
        [SerializeField] private float _thirdPersonMinHeightAboveFloor = 0.26f;
        [SerializeField] private float _thirdPersonFloorRayUp = 0.35f;
        [SerializeField] private float _thirdPersonFloorRayDown = 2.5f;

        private MazeCameraRig _rig;
        private Camera _cameraComponent;

        private static BattleFirstPersonCamera _instance;
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeElapsed;
        private float _recoilPitch;
        private float _recoilRecoverySpeed = 12f;
        private bool _isAiming;
        private float _currentFov;
        private Image[] _crosshairBars;
        private float _hitMarkerTimer;
        private bool _hitMarkerHeadshot;
        private float _lastAppliedLookPitch;
        private float _lastAppliedLookYaw;
        private float _lastAppliedLookRoll;
        private int _battleLocalBodyLayer = -1;
        private Coroutine _applyLocalBodyLayersRoutine;
        private const int ThirdPersonHitBuffer = 24;
        private static readonly RaycastHit[] s_thirdPersonHits = new RaycastHit[ThirdPersonHitBuffer];

        public static BattleFirstPersonCamera Instance => _instance;
        public Transform FollowTarget => _target;
        public float LastAppliedLookPitch => _lastAppliedLookPitch;
        public float LastAppliedLookYaw => _lastAppliedLookYaw;
        public float LastAppliedLookRoll => _lastAppliedLookRoll;
        public bool IsAiming => _isAiming;
        public float AimSensitivityMultiplier => _isAiming ? 0.45f : 1f;
        public bool SuspendAutomaticCameraFollow => _suspendAutomaticCameraFollow;
        public bool IsThirdPersonMode => _thirdPersonMode;

        private void Awake()
        {
            _cameraComponent = GetComponent<Camera>();
            ApplyNearClipFromMode();
            _instance = this;
            _currentFov = _thirdPersonMode ? _thirdPersonFieldOfView : _defaultFov;
            _rig = GetComponent<MazeCameraRig>();
            if (_rig == null) _rig = gameObject.AddComponent<MazeCameraRig>();
            _rig.Configure(true, _minLookPitch, _maxLookPitch, true);
            _battleLocalBodyLayer = LayerMask.NameToLayer("BattleLocalBody");
            if (_battleLocalBodyLayer >= 0 && _cameraComponent != null && !_thirdPersonMode)
                _cameraComponent.cullingMask &= ~(1 << _battleLocalBodyLayer);

            EnsureCrosshair();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void SetThirdPersonMode(bool enabled)
        {
            _thirdPersonMode = enabled;
            if (_cameraComponent == null) _cameraComponent = GetComponent<Camera>();
            if (_battleLocalBodyLayer < 0)
                _battleLocalBodyLayer = LayerMask.NameToLayer("BattleLocalBody");
            if (_cameraComponent != null)
            {
                if (_thirdPersonMode && _battleLocalBodyLayer >= 0)
                    _cameraComponent.cullingMask |= 1 << _battleLocalBodyLayer;
                else if (!_thirdPersonMode && _battleLocalBodyLayer >= 0)
                    _cameraComponent.cullingMask &= ~(1 << _battleLocalBodyLayer);
            }

            ApplyNearClipFromMode();
            _currentFov = _thirdPersonMode ? _thirdPersonFieldOfView : _defaultFov;
        }

        private void ApplyNearClipFromMode()
        {
            if (_cameraComponent == null) return;
            if (_thirdPersonMode)
                _cameraComponent.nearClipPlane = Mathf.Clamp(_thirdPersonNearClip, 0.03f, 0.25f);
            else
                _cameraComponent.nearClipPlane = Mathf.Clamp(_nearClipPlane, 0.005f, 0.1f);
        }

        // 로컬 플레이어 헤드/루트를 카메라 추적 대상으로 연결한다.
        // 멀티에서는 각 클라이언트가 자기 플레이어만 이 메서드로 연결하면 된다.
        public void SetTarget(Transform target)
        {
            _target = target;
            if (target == null) return;
            if (_thirdPersonMode)
                BattlePrototypeManager.ApplyLocalThirdPersonBodyLayersToPlayer(target.gameObject);
            else
            {
                BattlePrototypeManager.ApplyLocalFirstPersonBodyLayersToPlayer(target.gameObject);
                if (_applyLocalBodyLayersRoutine != null) StopCoroutine(_applyLocalBodyLayersRoutine);
                _applyLocalBodyLayersRoutine = StartCoroutine(ApplyLocalBodyLayersNextFrame(target.gameObject));
            }
        }

        private IEnumerator ApplyLocalBodyLayersNextFrame(GameObject playerRoot)
        {
            yield return null;
            if (playerRoot != null && !_thirdPersonMode)
                BattlePrototypeManager.ApplyLocalFirstPersonBodyLayersToPlayer(playerRoot);
            _applyLocalBodyLayersRoutine = null;
        }

        private bool IsThirdPersonIgnoredCollider(Collider c)
        {
            if (c == null || _target == null) return true;
            Transform t = c.transform;
            return t == _target || t.IsChildOf(_target);
        }

        private Vector3 ResolveThirdPersonCameraWorldPosition(Vector3 pivot, Vector3 rawDesiredWorld)
        {
            Vector3 offset = rawDesiredWorld - pivot;
            float fullLen = offset.magnitude;
            if (fullLen < 0.02f) return ClampThirdPersonAboveFloor(rawDesiredWorld);
            Vector3 dir = offset / fullLen;
            float inset = Mathf.Clamp(_thirdPersonProbeStartInset, 0.01f, fullLen * 0.45f);
            float castMax = fullLen - inset;
            float allowedAlong = castMax;
            if (castMax > 0.001f)
            {
                int mask = _thirdPersonObstructionMask.value == 0 ? Physics.DefaultRaycastLayers : _thirdPersonObstructionMask;
                Vector3 castOrigin = pivot + dir * inset;
                int n = Physics.SphereCastNonAlloc(
                    castOrigin,
                    _thirdPersonProbeRadius,
                    dir,
                    s_thirdPersonHits,
                    castMax,
                    mask,
                    QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = s_thirdPersonHits[i];
                    if (h.collider == null) continue;
                    if (IsThirdPersonIgnoredCollider(h.collider)) continue;
                    float d = h.distance - _thirdPersonWallPadding;
                    if (d < allowedAlong) allowedAlong = d;
                }

                if (allowedAlong < 0f) allowedAlong = 0f;
            }

            float arm = inset + allowedAlong;
            arm = Mathf.Clamp(arm, _thirdPersonMinArmLength, fullLen);
            Vector3 pos = pivot + dir * arm;
            return ClampThirdPersonAboveFloor(pos);
        }

        private Vector3 ClampThirdPersonAboveFloor(Vector3 camWorld)
        {
            int mask = _thirdPersonObstructionMask.value == 0 ? Physics.DefaultRaycastLayers : _thirdPersonObstructionMask;
            Vector3 rayStart = camWorld + Vector3.up * _thirdPersonFloorRayUp;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit gh, _thirdPersonFloorRayUp + _thirdPersonFloorRayDown, mask, QueryTriggerInteraction.Ignore))
            {
                if (!IsThirdPersonIgnoredCollider(gh.collider))
                {
                    float minY = gh.point.y + _thirdPersonMinHeightAboveFloor;
                    if (camWorld.y < minY) camWorld.y = minY;
                }
            }

            return camWorld;
        }

        // 리그 각도, 반동, 흔들림, FOV를 최종 카메라 transform에 반영하는 단계다.
        // 순수 로컬 시점 연출이라 서버 동기화 대상이 아님을 파악하기 좋은 위치다.
        private void LateUpdate()
        {
            if (_target == null || _rig == null) return;
            float pitch = _rig.Pitch;
            float yaw = _rig.Yaw;
            float roll = 0f;

            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, Time.deltaTime * _recoilRecoverySpeed);
            pitch += _recoilPitch;

            float shakePitchDelta = 0f;
            float shakeYawDelta = 0f;
            if (_shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);
                shakePitchDelta = (Random.value - 0.5f) * 2f * _shakeIntensity * decay;
                shakeYawDelta = (Random.value - 0.5f) * 2f * _shakeIntensity * decay;
                pitch += shakePitchDelta;
                yaw += shakeYawDelta;
                roll = shakePitchDelta * 0.3f;
            }

            pitch = Mathf.Clamp(pitch, _minLookPitch, _maxLookPitch);
            _rig.SetPitch(pitch - _recoilPitch - shakePitchDelta);

            _lastAppliedLookPitch = pitch;
            _lastAppliedLookYaw = yaw;
            _lastAppliedLookRoll = roll;

            if (!_suspendAutomaticCameraFollow)
            {
                if (_thirdPersonMode)
                {
                    transform.rotation = Quaternion.Euler(pitch, yaw, roll);
                    Vector3 pivot = _target.position + Vector3.up * _thirdPersonPivotHeight;
                    Vector3 raw = pivot
                        - transform.forward * _thirdPersonDistance
                        + transform.right * _thirdPersonShoulderOffset
                        + Vector3.up * _thirdPersonHeightBias;
                    transform.position = ResolveThirdPersonCameraWorldPosition(pivot, raw);
                }
                else
                {
                    Vector3 combined = _cameraOffset + _cameraOffsetExtra;
                    transform.position = _target.position + Quaternion.Euler(0f, _rig.Yaw, 0f) * combined;
                    transform.rotation = Quaternion.Euler(pitch, yaw, roll);
                }
            }
            else
                transform.rotation = Quaternion.Euler(pitch, yaw, roll);

            float targetFov = _isAiming ? _adsFov : (_thirdPersonMode ? _thirdPersonFieldOfView : _defaultFov);
            _currentFov = Mathf.Lerp(_currentFov, targetFov, Time.deltaTime * _adsSpeed);
            if (_cameraComponent != null)
            {
                _cameraComponent.fieldOfView = _currentFov;
                if (_battleLocalBodyLayer >= 0)
                {
                    if (_thirdPersonMode)
                        _cameraComponent.cullingMask |= 1 << _battleLocalBodyLayer;
                    else
                        _cameraComponent.cullingMask &= ~(1 << _battleLocalBodyLayer);
                }
            }

            UpdateHitMarker();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying || !enabled) return;
            if (Keyboard.current == null || !Keyboard.current.hKey.wasPressedThisFrame) return;
            TryPersistCameraOffsetFromScene();
        }

        private void TryPersistCameraOffsetFromScene()
        {
            if (_target == null || _thirdPersonMode) return;
            if (_rig == null) _rig = GetComponent<MazeCameraRig>();
            if (_rig == null) return;
            Quaternion yawQ = Quaternion.Euler(0f, _rig.Yaw, 0f);
            _cameraOffset = Quaternion.Inverse(yawQ) * (transform.position - _target.position);
            _cameraOffsetExtra = Vector3.zero;
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.path))
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            _suspendAutomaticCameraFollow = false;
            Debug.Log($"[BattleFP] H: _cameraOffset 저장 {_cameraOffset}", this);
        }
#endif

        public void SetAiming(bool aiming) { _isAiming = aiming; }

        public void ApplyRecoil(float intensity)
        {
            _recoilPitch -= Mathf.Abs(intensity);
            _shakeIntensity = intensity * 0.4f;
            _shakeDuration = 0.08f;
            _shakeElapsed = 0f;
        }

        public static void Shake(float intensity)
        {
            if (_instance != null) _instance.ApplyRecoil(intensity);
        }

        public static void ShowHitMarker(bool headshot)
        {
            if (_instance == null) return;
            _instance._hitMarkerTimer = 0.15f;
            _instance._hitMarkerHeadshot = headshot;
        }

        private void UpdateHitMarker()
        {
            if (_crosshairBars == null || _crosshairBars.Length == 0) return;
            if (_hitMarkerTimer > 0f)
            {
                _hitMarkerTimer -= Time.deltaTime;
                Color c = _hitMarkerHeadshot ? new Color(1f, 0.15f, 0.15f) : Color.white;
                for (int i = 0; i < _crosshairBars.Length; i++)
                    if (_crosshairBars[i] != null) _crosshairBars[i].color = c;
            }
            else
            {
                for (int i = 0; i < _crosshairBars.Length; i++)
                    if (_crosshairBars[i] != null) _crosshairBars[i].color = _crosshairColor;
            }
        }

        private void EnsureCrosshair()
        {
            const string canvasName = "BattleCrosshairCanvas";
            var existingCanvas = GameObject.Find(canvasName);
            if (existingCanvas != null) return;

            var canvasObject = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("CrosshairRoot", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(32f, 32f);

            _crosshairBars = new Image[5];
            _crosshairBars[0] = CreateCrosshairBar(root.transform, "Top", new Vector2(0f, _crosshairGap + _crosshairLength * 0.5f), new Vector2(_crosshairThickness, _crosshairLength));
            _crosshairBars[1] = CreateCrosshairBar(root.transform, "Bottom", new Vector2(0f, -_crosshairGap - _crosshairLength * 0.5f), new Vector2(_crosshairThickness, _crosshairLength));
            _crosshairBars[2] = CreateCrosshairBar(root.transform, "Left", new Vector2(-_crosshairGap - _crosshairLength * 0.5f, 0f), new Vector2(_crosshairLength, _crosshairThickness));
            _crosshairBars[3] = CreateCrosshairBar(root.transform, "Right", new Vector2(_crosshairGap + _crosshairLength * 0.5f, 0f), new Vector2(_crosshairLength, _crosshairThickness));
            _crosshairBars[4] = CreateCrosshairBar(root.transform, "CenterDot", Vector2.zero, new Vector2(_crosshairThickness + 1f, _crosshairThickness + 1f));
        }

        private Image CreateCrosshairBar(Transform parent, string elementName, Vector2 anchoredPosition, Vector2 size)
        {
            var barObject = new GameObject(elementName, typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);
            var rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = barObject.GetComponent<Image>();
            image.color = _crosshairColor;
            image.raycastTarget = false;
            return image;
        }

        private void OnValidate()
        {
            if (_cameraComponent == null) _cameraComponent = GetComponent<Camera>();
            if (_cameraComponent != null)
            {
                if (Application.isPlaying)
                    ApplyNearClipFromMode();
                else
                    _cameraComponent.nearClipPlane = Mathf.Clamp(_thirdPersonMode ? _thirdPersonNearClip : _nearClipPlane, 0.005f, 0.25f);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying || !enabled) return;
            if (_suspendAutomaticCameraFollow || _thirdPersonMode) return;
            if (_rig == null) _rig = GetComponent<MazeCameraRig>();
            if (_target == null || _rig == null) return;
            transform.position = _target.position + Quaternion.Euler(0f, _rig.Yaw, 0f) * (_cameraOffset + _cameraOffsetExtra);
#endif
        }
    }
}
