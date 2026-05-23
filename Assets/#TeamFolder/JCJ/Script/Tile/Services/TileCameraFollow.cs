using System.Collections;
using System.Collections.Generic;
using _TeamFolder.JCJ.Script;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// 타일 모드에서 플레이어를 따라가는 카메라 처리.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 타일 미니게임용 단일 타깃 Cinemachine 리그. 첫 등록 플레이어를 고정 대각 오프셋으로 추적하고
    /// 회전 컴포저로 화면 중앙에 두는 구성.
    /// RegisterTargets / RegisterTarget / ClearTargets / Shake API 유지.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class TileCameraFollow : MonoBehaviour
    {
        [Header("구도")]
        [Tooltip("플레이어에서 가상 카메라까지 월드 오프셋.")]
        [SerializeField] private Vector3 _cameraOffset = new(0f, 8f, -8f);
        [Tooltip("플레이어 위치 위에 더하는 시선 오프셋(머리 높이 등).")]
        [SerializeField] private Vector3 _lookOffset = new(0f, 1.8f, 0f);
        [Tooltip("고정 시야각(도). 단일 타깃 모드에서 동적 줌 없음.")]
        [Range(35f, 90f)] [SerializeField] private float _fov = 62f;

        [Header("댐핑")]
        [Tooltip("낮게 유지 — 프레임마다 요 변경이 밀리면 마우스 궤도가 거칠어짐.")]
        [SerializeField] private Vector3 _positionDamping = new(0.08f, 0.12f, 0.08f);
        [SerializeField] private Vector2 _composerDamping = new(0.04f, 0.04f);

        [Header("마우스 룩")]
        [Tooltip("마우스 가로 픽셀당 카메라 요 회전(도).")]
        [Range(0.01f, 1f)] [SerializeField] private float _mouseSensitivity = 0.18f;
        [Tooltip("끄면 카메라 방향 고정.")]
        [SerializeField] private bool _mouseLookEnabled = true;
        [SerializeField] private float _minPitch = -30f;
        [SerializeField] private float _maxPitch = 55f;
        [SerializeField] private float _minElevation = -20f;
        [SerializeField] private float _maxElevation = 75f;

        [Header("타일 가림")]
        [SerializeField] private bool _avoidTileOcclusion = true;
        [SerializeField] private float _cameraRadius = 0.35f;
        [SerializeField] private float _minDistanceFromTarget = 1.2f;
        [SerializeField] private float _occluderDamping = 0.05f;

        private CinemachineBrain       _brain;
        private Camera                 _brainCamera;
        private CinemachineCamera      _vcam;
        private CinemachineFollow      _follow;
        private CinemachineRotationComposer _composer;

        private Transform _target;

        // 마우스 궤도 상태 — _baseOffset을 매 LateUpdate 월드 Y축으로 회전.
        private float    _yaw;
        private float    _pitch;
        private bool     _allowPitch;
        private Vector3  _baseOffset;

        private void Awake()
        {
            EnsureBrain();
            EnsureRig();
            ApplySettings(SettingsService.EnsureInstance().Data);
        }

        // ── 공개 API ───────────────────────────────
        public void RegisterTargets(IEnumerable<Transform> targets)
        {
            Transform first = null;
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t == null) continue;
                    first = t;
                    break;
                }
            }
            RegisterTarget(first);
        }

        // 현재 로컬 플레이어를 카메라 추적 대상으로 지정한다.
        // 멀티에서는 소유권이 바뀌거나 관전 대상을 전환할 때 이 메서드가 직접 쓰일 가능성이 크다.
        public void RegisterTarget(Transform target)
        {
            _target = target;
            if (_vcam == null) EnsureRig();
            if (_vcam == null) return;

            // GoToOverview 상태 해제 — 새 라운드에서 새 플레이어 추적.
            if (!_vcam.enabled) _vcam.enabled = true;
            _mouseLookEnabled = true;

            _vcam.Target = new CameraTarget
            {
                TrackingTarget = target,
                LookAtTarget   = target,
            };
        }

        public void ClearTargets()
        {
            _target = null;
            if (_vcam == null) return;
            _vcam.Target = default;
        }

        /// <summary>
        /// 팔로우 해제 후 월드 지점을 내려다보는 고정 뷰 — 로컬 탈락 후 관전할 생존자 없을 때.
        /// 마우스 룩 끔(결과 오버레이에서 화면 안정).
        /// </summary>
        // 더 이상 추적 대상이 없을 때 판 전체를 보여주는 오버뷰 모드로 바꾼다.
        // 전멸, 관전, 결과 화면에서 공통으로 재사용하기 좋은 전환 지점이다.
        public void GoToOverview(Vector3 worldPoint)
        {
            _target = null;
            _mouseLookEnabled = false;

            if (_brainCamera == null) return;
            // vcam 끔 — Brain이 카메라를 덮어쓰지 않게 하고 메인 카메라에 오버뷰 직접 기록.
            if (_vcam != null)
            {
                _vcam.Target = default;
                _vcam.enabled = false;
            }

            Vector3 camPos = worldPoint + new Vector3(0f, 18f, -14f);
            _brainCamera.transform.position = camPos;
            _brainCamera.transform.rotation = Quaternion.LookRotation((worldPoint - camPos).normalized, Vector3.up);
        }

        public void Shake(float duration = 0.3f, float magnitude = 0.35f)
        {
            if (_brainCamera == null) return;
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        public void SetMouseSensitivity(float value)
        {
            _mouseSensitivity = Mathf.Clamp(value, 0.01f, 2f);
        }

        public void SetMouseLookEnabled(bool enabled)
        {
            _mouseLookEnabled = enabled;
        }

        public void SetAllowPitch(bool allow)
        {
            _allowPitch = allow;
            if (!allow) _pitch = 0f;
            ApplyOrbitOffset();
        }

        // ── 리그 생성 ───────────────────────────────
        private void EnsureBrain()
        {
            _brainCamera = Camera.main;
            if (_brainCamera == null)
            {
                var camGO = new GameObject("Main Camera (auto)");
                camGO.tag = "MainCamera";
                _brainCamera = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            _brain = _brainCamera.GetComponent<CinemachineBrain>();
            if (_brain == null) _brain = _brainCamera.gameObject.AddComponent<CinemachineBrain>();
            _brain.UpdateMethod      = CinemachineBrain.UpdateMethods.LateUpdate;
            _brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        private void EnsureRig()
        {
            // Tile 카메라는 로컬 시각 전용 리그다.
            // 네트워크 동기화 대상이 아니며, 각 클라이언트가 자기 로컬 플레이어를 target으로 등록하면 된다.
            if (_vcam != null) return;

            var rigGO = new GameObject("CM_TileFollow");
            rigGO.transform.SetParent(transform, false);
            _vcam = rigGO.AddComponent<CinemachineCamera>();
            _vcam.Lens = LensSettings.Default;
            _vcam.Lens.FieldOfView = _fov;

            _follow = rigGO.AddComponent<CinemachineFollow>();
            _baseOffset = _cameraOffset;
            ApplyOrbitOffset();
            var tracker = _follow.TrackerSettings;
            tracker.BindingMode     = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            tracker.PositionDamping = _positionDamping;
            tracker.RotationDamping = Vector3.zero;
            _follow.TrackerSettings = tracker;

            _composer = rigGO.AddComponent<CinemachineRotationComposer>();
            _composer.TargetOffset = _lookOffset;
            _composer.Damping      = _composerDamping;
            _composer.Lookahead    = new LookaheadSettings { Time = 0f, Smoothing = 0f, IgnoreY = true };
            var comp = _composer.Composition;
            comp.DeadZone.Enabled = false;
            comp.DeadZone.Size    = Vector2.zero;
            _composer.Composition = comp;

            if (_avoidTileOcclusion) AddDeoccluder(rigGO);
        }

        private void AddDeoccluder(GameObject rigGO)
        {
            // 위층 타일이 카메라와 플레이어 사이에 있을 때 카메라가 타일을 관통해서 보지 않도록 한다.
            // 타일은 MeshCollider를 가지고 있으므로 DefaultRaycastLayers에 포함되어 있으면 여기서 막힌다.
            var deo = rigGO.AddComponent<CinemachineDeoccluder>();
            deo.CollideAgainst = Physics.DefaultRaycastLayers;
            deo.IgnoreTag = "Player";
            deo.MinimumDistanceFromTarget = _minDistanceFromTarget;

            var avoid = deo.AvoidObstacles;
            avoid.Enabled = true;
            avoid.CameraRadius = _cameraRadius;
            avoid.SmoothingTime = _occluderDamping;
            avoid.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            avoid.Damping = _occluderDamping;
            avoid.DampingWhenOccluded = 0f;
            deo.AvoidObstacles = avoid;
        }

        // 마우스는 Update에서 읽어 LateUpdate 소비자(Brain·탈락 가드)보다 먼저 FollowOffset 반영 —
        // LateUpdate에서만 읽으면 궤도가 한 프레임 늦어 빠른 스와이프 시 끊김.
        private void Update()
        {
            if (_vcam == null) return;
            if (!_mouseLookEnabled) return;
            if (_target == null || !_target.gameObject.activeInHierarchy) return;
            ApplyMouseLook();
        }

        // 추적 대상이 사라지면(라운드 사이 파괴·탈락) 타깃 해제 — 원점으로 튀는 것 방지.
        private void LateUpdate()
        {
            if (_vcam == null) return;
            if (_target == null) return;
            if (!_target.gameObject.activeInHierarchy)
            {
                _vcam.Target = default;
            }
            else if (_vcam.Target.TrackingTarget != _target)
            {
                _vcam.Target = new CameraTarget
                {
                    TrackingTarget = _target,
                    LookAtTarget   = _target,
                };
            }
        }

        private void ApplyMouseLook()
        {
            if (Mouse.current == null || _follow == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (SettingsPanel.IsOpen) return;

            float dx = Mouse.current.delta.x.ReadValue();
            float dy = Mouse.current.delta.y.ReadValue();
            if (Mathf.Abs(dx) < 0.001f && (!_allowPitch || Mathf.Abs(dy) < 0.001f)) return;

            _yaw += dx * _mouseSensitivity;
            if (_allowPitch)
            {
                _pitch -= dy * _mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            }

            if (_yaw > 360f) _yaw -= 360f; else if (_yaw < -360f) _yaw += 360f;

            ApplyOrbitOffset();
        }

        /// <summary>런타임에 인스펙터 오프셋이 바뀌면 궤도 기준을 다시 맞춤.</summary>
        public void ResetOrbit()
        {
            _yaw = 0f;
            _pitch = 0f;
            _baseOffset = _cameraOffset;
            ApplyOrbitOffset();
        }

        private void ApplySettings(SettingsData settings)
        {
            if (settings == null) return;
            SetMouseSensitivity(settings.cameraSensitivity);
            SetAllowPitch(!settings.lockPitch);
        }

        private void ApplyOrbitOffset()
        {
            if (_follow == null) return;
            float pitch = _allowPitch ? _pitch : 0f;
            float baseElevation = Mathf.Atan2(_baseOffset.y, new Vector2(_baseOffset.x, _baseOffset.z).magnitude) * Mathf.Rad2Deg;
            float targetElevation = Mathf.Clamp(baseElevation + pitch, _minElevation, _maxElevation);
            float orbitPitch = targetElevation - baseElevation;
            _follow.FollowOffset = Quaternion.Euler(orbitPitch, _yaw, 0f) * _baseOffset;
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            if (_brainCamera == null) yield break;

            // Brain이 다음 틱에 덮어쓰므로 메인 카메라 위치에 지터만 가산 — 순수 시각적 흔들기.
            var tr = _brainCamera.transform;
            float t = 0f;
            while (t < duration)
            {
                float falloff = 1f - (t / duration);
                Vector3 jitter = Random.insideUnitSphere * (magnitude * falloff);
                jitter.y *= 0.35f;
                tr.position += jitter;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
