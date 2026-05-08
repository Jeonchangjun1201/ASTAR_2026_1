using UnityEngine;
using UnityEngine.UI;
using _TeamFolder.JCJ.Script;

// 조준과 반동을 반영하는 1인칭 전투 카메라.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleFirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _cameraOffset = new(0f, 1.55f, 0.05f);
        [SerializeField] private float _nearClipPlane = 0.01f;
        [SerializeField] private Color _crosshairColor = new(1f, 0.45f, 0.1f, 0.95f);
        [SerializeField] private float _crosshairGap = 7f;
        [SerializeField] private float _crosshairLength = 8f;
        [SerializeField] private float _crosshairThickness = 2f;
        [SerializeField] private float _defaultFov = 60f;
        [SerializeField] private float _adsFov = 35f;
        [SerializeField] private float _adsSpeed = 10f;

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

        public static BattleFirstPersonCamera Instance => _instance;
        public bool IsAiming => _isAiming;
        public float AimSensitivityMultiplier => _isAiming ? 0.45f : 1f;

        private void Awake()
        {
            _cameraComponent = GetComponent<Camera>();
            if (_cameraComponent != null) _cameraComponent.nearClipPlane = Mathf.Clamp(_nearClipPlane, 0.005f, 0.1f);
            _instance = this;
            _currentFov = _defaultFov;
            _rig = GetComponent<MazeCameraRig>();
            if (_rig == null) _rig = gameObject.AddComponent<MazeCameraRig>();
            _rig.Configure(true, -35f, 60f, true);
            EnsureCrosshair();
        }

        // 로컬 플레이어 헤드/루트를 카메라 추적 대상으로 연결한다.
        // 멀티에서는 각 클라이언트가 자기 플레이어만 이 메서드로 연결하면 된다.
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        // 리그 각도, 반동, 흔들림, FOV를 최종 카메라 transform에 반영하는 단계다.
        // 순수 로컬 시점 연출이라 서버 동기화 대상이 아님을 파악하기 좋은 위치다.
        private void LateUpdate()
        {
            if (_target == null || _rig == null) return;
            transform.position = _target.position + _cameraOffset;

            float pitch = _rig.Pitch;
            float yaw = _rig.Yaw;
            float roll = 0f;

            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, Time.deltaTime * _recoilRecoverySpeed);
            pitch += _recoilPitch;

            if (_shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);
                float shakeX = (Random.value - 0.5f) * 2f * _shakeIntensity * decay;
                float shakeY = (Random.value - 0.5f) * 2f * _shakeIntensity * decay;
                pitch += shakeX;
                yaw += shakeY;
                roll = shakeX * 0.3f;
            }

            transform.rotation = Quaternion.Euler(pitch, yaw, roll);

            float targetFov = _isAiming ? _adsFov : _defaultFov;
            _currentFov = Mathf.Lerp(_currentFov, targetFov, Time.deltaTime * _adsSpeed);
            if (_cameraComponent != null) _cameraComponent.fieldOfView = _currentFov;

            UpdateHitMarker();
        }

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
    }
}
