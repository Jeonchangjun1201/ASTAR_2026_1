using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골인한 플레이어를 조작 불가 관전 상태로 전환하고 로컬 카메라를 다음 생존자에게 넘긴다.
    /// 관전 중 좌클릭으로 다음 생존자에게 카메라를 전환할 수 있다.
    /// </summary>
    [DefaultExecutionOrder(25)]
    public class MazeFinisherSpectator : MonoBehaviour
    {
        [SerializeField] private RankService _rankService;
        [SerializeField] private PlayerFollowCameraService _cameraService;

        [Tooltip("골인한 플레이어 본체 모델 숨김 여부.")]
        [SerializeField] private bool _hideFinisherRenderers = true;

        [Tooltip("골인한 플레이어 콜라이더 비활성 여부 (다른 플레이어와 충돌 X).")]
        [SerializeField] private bool _disableFinisherColliders = true;

        [Header("관전 입력")]
        [Tooltip("관전 중 마우스 감도.")]
        [SerializeField] private float _spectatorMouseSensitivity = 0.18f;

        private bool _subscribed;
        private readonly HashSet<string> _finishedPlayerIds = new();

        private bool _isSpectating;
        private PlayerController _currentSpectateTarget;

        public void ResetState()
        {
            _finishedPlayerIds.Clear();
            _isSpectating = false;
            _currentSpectateTarget = null;
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Update()
        {
            if (!_isSpectating) return;

            HandleSpectatorMouseLook();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CycleSpectateTarget();
        }

        private void HandleSpectatorMouseLook()
        {
            if (SettingsPanel.IsOpen) return;
            if (_currentSpectateTarget != null && _currentSpectateTarget.IsLocalControlled) return;

            var rig = MazeCameraRig.Instance;
            if (rig == null || Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            rig.AddLook(delta * _spectatorMouseSensitivity);
        }

        private void CycleSpectateTarget()
        {
            if (SettingsPanel.IsOpen) return;

            var alive = GetAlivePlayerList();
            if (alive.Count == 0) return;

            int cur = alive.IndexOf(_currentSpectateTarget);
            int next = (cur + 1) % alive.Count;
            var target = alive[next];

            if (target == _currentSpectateTarget) return;

            _currentSpectateTarget = target;
            ResolveReferences();
            if (_cameraService != null) _cameraService.Follow(target.transform);
            UpdateMinimapTarget(target);
            Debug.Log($"[FinisherSpectator] 관전 전환 (클릭) → {target.gameObject.name}");
        }

        private List<PlayerController> GetAlivePlayerList()
        {
            var result = new List<PlayerController>();
            var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.InstanceID);
            foreach (var pc in all)
            {
                if (pc == null || !pc.gameObject.activeInHierarchy) continue;
                var identity = RuntimePlayerIdentity.Find(pc);
                if (identity != null && _finishedPlayerIds.Contains(identity.PlayerId)) continue;
                result.Add(pc);
            }
            return result;
        }

        private void ResolveReferences()
        {
            if (_rankService == null && GameStateManager.Instance != null)
            {
                _rankService = GameStateManager.Instance.GetComponent<RankService>();
                if (_rankService == null)
                    _rankService = GameStateManager.Instance.GetComponentInChildren<RankService>(true);
            }
            if (_rankService == null) _rankService = FindFirstObjectByType<RankService>();

            if (_cameraService == null && MazeManager.Instance != null)
            {
                _cameraService = MazeManager.Instance.GetComponent<PlayerFollowCameraService>();
                if (_cameraService == null)
                    _cameraService = MazeManager.Instance.GetComponentInChildren<PlayerFollowCameraService>(true);
            }
            if (_cameraService == null) _cameraService = FindFirstObjectByType<PlayerFollowCameraService>();
        }

        private void Subscribe()
        {
            if (_subscribed || _rankService == null) return;
            _rankService.OnPlayerFinishedData += HandleFinished;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _rankService == null) return;
            _rankService.OnPlayerFinishedData -= HandleFinished;
            _subscribed = false;
        }

        private void HandleFinished(PlayerRankData entry)
        {
            if (string.IsNullOrWhiteSpace(entry.PlayerId)) return;
            if (!_finishedPlayerIds.Add(entry.PlayerId)) return;

            var finisher = FindPlayerById(entry.PlayerId);
            if (finisher == null) return;

            bool wasLocalControlled = finisher.IsLocalControlled;
            EnterSpectatorMode(finisher);

            if (wasLocalControlled)
            {
                _isSpectating = true;
                SwitchCameraToNextAlivePlayer(finisher);
            }
        }

        private static PlayerController FindPlayerById(string playerId)
        {
            var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in all)
            {
                if (pc == null) continue;
                var identity = RuntimePlayerIdentity.Find(pc);
                if (identity != null &&
                    string.Equals(identity.PlayerId, playerId, System.StringComparison.OrdinalIgnoreCase))
                    return pc;
            }
            return null;
        }

        private void EnterSpectatorMode(PlayerController pc)
        {
            pc.SetSpectating(true);
            pc.IsLocalControlled = false;
            pc.SetMovementEnabled(false);

            var rb = pc.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (_disableFinisherColliders)
            {
                foreach (var col in pc.GetComponentsInChildren<Collider>(true))
                    if (col != null) col.enabled = false;
            }

            if (_hideFinisherRenderers)
            {
                var visual = pc.GetComponent<PartyCharacterVisual>();
                if (visual != null) visual.SetVisualHidden(true);

                foreach (var r in pc.GetComponentsInChildren<Renderer>(true))
                    if (r != null) r.enabled = false;
                foreach (var t in pc.GetComponentsInChildren<TrailRenderer>(true))
                    if (t != null) t.enabled = false;
                foreach (var anim in pc.GetComponentsInChildren<Animator>(true))
                    if (anim != null) anim.enabled = false;
            }

            Debug.Log($"[FinisherSpectator] {pc.gameObject.name} → 관전 모드 진입");
        }

        private void SwitchCameraToNextAlivePlayer(PlayerController finisher)
        {
            var next = FindNextAlivePlayer(finisher);
            if (next == null)
            {
                Debug.Log("[FinisherSpectator] 남은 플레이어가 없음. 카메라 유지.");
                return;
            }

            _currentSpectateTarget = next;
            ResolveReferences();
            if (_cameraService != null) _cameraService.Follow(next.transform);
            UpdateMinimapTarget(next);
            Debug.Log($"[FinisherSpectator] 관전 카메라 전환 → {next.gameObject.name}");
        }

        private static void UpdateMinimapTarget(PlayerController target)
        {
            var minimap = Object.FindFirstObjectByType<MazeMinimap>();
            if (minimap == null || target == null) return;

            minimap.SetPlayer(target.transform);

            var peers = new List<Transform>();
            var mm = MazeManager.Instance;
            if (mm != null)
            {
                foreach (var go in mm.Players)
                {
                    if (go == null) continue;
                    if (go.transform == target.transform) continue;
                    peers.Add(go.transform);
                }
            }
            minimap.SetPeerPlayers(peers);
        }

        private PlayerController FindNextAlivePlayer(PlayerController exclude)
        {
            var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            PlayerController fallback = null;
            foreach (var pc in all)
            {
                if (pc == null) continue;
                if (pc == exclude) continue;
                var identity = RuntimePlayerIdentity.Find(pc);
                if (identity != null && _finishedPlayerIds.Contains(identity.PlayerId)) continue;
                if (!pc.gameObject.activeInHierarchy) continue;
                if (fallback == null) fallback = pc;
                if (pc.IsLocalControlled) return pc;
            }
            return fallback;
        }
    }
}
