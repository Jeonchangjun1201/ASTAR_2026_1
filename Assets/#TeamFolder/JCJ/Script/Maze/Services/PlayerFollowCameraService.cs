using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

// 플레이어를 따라가는 카메라 연결을 관리하는 서비스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 기존 Main Camera 위에 3인칭 팔로우 카메라 구성. Brain 없으면 추가하고,
    /// 마우스 입력에 반응하는 요/피치 피벗(<see cref="MazeCameraRig"/>)을 만든다.
    /// </summary>
    public class PlayerFollowCameraService : MonoBehaviour, ICameraService
    {
        public enum Preset { ThirdPersonHigh, OverShoulder, FirstPerson, Custom }

        [Header("프리셋")]
        [Tooltip("빠른 카메라 프리셋. Custom은 아래 값 사용.")]
        [SerializeField] private Preset _preset = Preset.OverShoulder;

        [Header("팔로우 리그(Custom일 때)")]
        [SerializeField] private Vector3 _followOffset = new(0f, 4.5f, -4.5f);
        [SerializeField] private Vector3 _lookOffset   = new(0f, 1.2f, 1.5f);

        [Header("댐핑(클수록 부드럽고 추적이 느림)")]
        [SerializeField] private Vector3 _positionDamping = new(1.2f, 1.2f, 1.2f);
        [Tooltip("0에 가깝게 — 요/피치는 마우스가 돌리므로 여기 댐핑이 있으면 빠른 시점이 밀리거나 떨림.")]
        [SerializeField] private Vector3 _rotationDamping = Vector3.zero;
        [Tooltip("컴포저 시선 댐핑. 0에 가까우면 빠른 회전도 부드럽게.")]
        [SerializeField] private Vector2 _composerDamping = Vector2.zero;

        [Header("느낌")]
        [SerializeField] private float _fov = 62f;

        [Header("마우스 룩")]
        [SerializeField] private bool  _enableMouseLook = true;
        [SerializeField] private float _minPitch        = -48f;
        [SerializeField] private float _maxPitch        =  48f;

        [Header("벽 가림")]
        [Tooltip("타깃과 사이에 벽이 있으면 카메라를 당긴다.")]
        [SerializeField] private bool  _avoidWallClip      = true;
        [SerializeField] private float _cameraRadius       = 0.25f;
        [SerializeField] private float _minDistanceFromTarget = 0.6f;
        [SerializeField] private float _occluderSmoothing  = 0f;

        private CinemachineCamera _vcam;
        private CinemachineFollow _follow;
        private Transform _currentTarget;
        private Camera    _brainCamera;
        private MazeCameraRig _rig;
        private Vector3 _baseFollowOffset;

        // 현재 로컬 플레이어를 팔로우 카메라에 연결하는 진입점이다.
        // 서버 연동 후에도 카메라 자체는 로컬 전용이라 소유 플레이어만 여기로 넘기면 된다.
        public void Follow(Transform target)
        {
            if (target == null) return;
            EnsureRig();
            _currentTarget = target;
            if (_rig != null) _rig.SetTarget(target);
        }

        public void Shake(float amplitude = 1f, float duration = 0.25f)
        {
            if (_brainCamera == null) return;
            // Brain이 매 LateUpdate 메인 카메라를 덮어쓰므로 직접 트윈하면 끊김 — vcam이 있으면 그쪽을 흔듦.
            Transform t = _vcam != null ? _vcam.transform : _brainCamera.transform;
            t.DOKill(true);
            t.DOShakePosition(duration, amplitude * 0.35f, vibrato: 14, randomness: 90f, fadeOut: true)
             .SetEase(Ease.OutQuad);
        }

        // 특정 영역 전체를 한 번에 잡는 연출용 프레이밍이다.
        // 결과 화면, 스폰 연출, 관전 전환에서 서버 상태와 무관하게 로컬 카메라만 움직일 때 쓰기 좋다.
        public void FrameAll(Vector3 center, Vector3 size)
        {
            if (_vcam == null) return;
            float distance = Mathf.Max(size.x, size.z) * 0.75f;
            _vcam.transform.position = center + new Vector3(0f, distance, -distance);
            _vcam.transform.LookAt(center);
        }

        private void EnsureRig()
        {
            EnsureBrainOnMainCamera();
            if (_vcam != null) return;

            ApplyPreset();
            _baseFollowOffset = _followOffset;

            // vcam이 실제로 따라가는 요/피치 피벗 — LateUpdate에 플레이어 위치를 맞추고 마우스로 회전.
            var pivotGo = new GameObject("CameraPivot");
            pivotGo.transform.SetParent(transform, false);
            _rig = pivotGo.AddComponent<MazeCameraRig>();
            var settings = SettingsService.EnsureInstance().Data;
            _rig.Configure(_enableMouseLook, _minPitch, _maxPitch, settings != null && !settings.lockPitch);

            var rigGo = new GameObject("CM_PlayerFollow");
            rigGo.transform.SetParent(transform, false);
            _vcam = rigGo.AddComponent<CinemachineCamera>();
            _vcam.Lens = LensSettings.Default;
            _vcam.Lens.FieldOfView = _fov;
            _vcam.Target = new CameraTarget
            {
                TrackingTarget = pivotGo.transform,
                LookAtTarget   = pivotGo.transform,
            };

            _follow = rigGo.AddComponent<CinemachineFollow>();
            _follow.FollowOffset = _followOffset;
            var tracker = _follow.TrackerSettings;
            tracker.BindingMode     = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            tracker.PositionDamping = _positionDamping;
            tracker.RotationDamping = _rotationDamping;
            _follow.TrackerSettings = tracker;

            var composer = rigGo.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = _lookOffset;
            composer.Damping = _composerDamping;
            composer.Lookahead = new LookaheadSettings { Time = 0f, Smoothing = 0f, IgnoreY = true };
            var comp = composer.Composition;
            // 데드존 끔 — 타깃이 항상 화면 중앙.
            comp.DeadZone.Enabled = false;
            comp.DeadZone.Size = Vector2.zero;
            composer.Composition = comp;

            if (_avoidWallClip) AddDeoccluder(rigGo);
        }

        // 마우스 룩 리그 각도를 실제 팔로우 오프셋에 반영하는 단계다.
        // 카메라 입력이 로컬 전용이라는 점을 확인하기 좋은 경계다.
        private void LateUpdate()
        {
            if (_follow == null || _rig == null) return;
            float pitch = _rig.IsPitchAllowed ? Mathf.Clamp(_rig.Pitch, _minPitch, _maxPitch) : 0f;
            float yaw = _rig.Yaw;
            Quaternion orbit = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            _follow.FollowOffset = orbit * _baseFollowOffset;
        }

        private void AddDeoccluder(GameObject rigGo)
        {
            var deo = rigGo.AddComponent<CinemachineDeoccluder>();
            deo.CollideAgainst           = Physics.DefaultRaycastLayers;
            deo.IgnoreTag                = "Player";
            deo.MinimumDistanceFromTarget = _minDistanceFromTarget;

            var avoid = deo.AvoidObstacles;
            avoid.Enabled             = true;
            avoid.CameraRadius        = _cameraRadius;
            avoid.SmoothingTime       = _occluderSmoothing;
            avoid.Strategy            = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            avoid.Damping             = 0.1f;
            avoid.DampingWhenOccluded = 0f;
            deo.AvoidObstacles        = avoid;
        }

        private void ApplyPreset()
        {
            switch (_preset)
            {
                case Preset.ThirdPersonHigh:
                    _followOffset = new Vector3(0f, 8.5f, -7f);
                    _lookOffset   = new Vector3(0f, 1.2f, 0f);
                    _fov = 55f;
                    break;
                case Preset.OverShoulder:
                    _followOffset = new Vector3(0f, 3.8f, -4.8f);
                    _lookOffset   = new Vector3(0f, 1.2f, 0.5f);
                    _fov = 65f;
                    break;
                case Preset.FirstPerson:
                    _followOffset = new Vector3(0f, 1.5f, 0.05f);
                    _lookOffset   = new Vector3(0f, 1.5f, 2.5f);
                    _fov = 75f;
                    break;
                case Preset.Custom:
                default:
                    break;
            }
        }

        private void EnsureBrainOnMainCamera()
        {
            if (_brainCamera == null) _brainCamera = Camera.main;
            if (_brainCamera == null)
            {
                var camGo = new GameObject("Main Camera (auto)");
                camGo.tag = "MainCamera";
                _brainCamera = camGo.AddComponent<Camera>();
            }
            var brain = _brainCamera.GetComponent<CinemachineBrain>();
            if (brain == null) brain = _brainCamera.gameObject.AddComponent<CinemachineBrain>();

            // 리그도 LateUpdate에서 회전 — Brain과 타이밍 맞춰 1프레임 밀림(끊김) 완화.
            brain.UpdateMethod   = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        private void OnDestroy()
        {
            _currentTarget = null;
        }
    }
}
