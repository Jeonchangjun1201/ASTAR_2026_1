using System.Collections.Generic;
using UnityEngine;

// 골인한 플레이어를 관전 상태로 전환하는 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골인한 플레이어를 조작 불가 관전 상태로 전환하고 로컬 카메라를 다음 생존자에게 넘긴다.
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

        private bool _subscribed;
        private readonly HashSet<string> _finishedPlayerIds = new();

        public void ResetState()
        {
            // Play Again 후 Player_1 같은 이름이 재사용되므로 이전 라운드 완주자 목록을 비운다.
            _finishedPlayerIds.Clear();
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

            // EnterSpectatorMode 안에서 IsLocalControlled가 false로 바뀐다.
            // 따라서 카메라 전환 여부는 변경 전 값을 따로 저장해서 판단한다.
            bool wasLocalControlled = finisher.IsLocalControlled;
            EnterSpectatorMode(finisher);

            if (wasLocalControlled)
                SwitchCameraToNextAlivePlayer(finisher);
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
            // 관전 모드 진입은 "조작권 제거 + 물리 정지 + 렌더/콜라이더 숨김" 세 단계로 처리한다.
            // 여기서 다음 플레이어에게 조작권을 넘기면 관전이 아니라 빙의가 되므로 금지한다.
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
            // 남아 있는 플레이어 중 아직 완주하지 않은 대상을 찾아 카메라만 따라간다.
            // 입력 권한은 넘기지 않는다. 관전 대상은 자동 조종/서버 동기화 대상으로 남아야 한다.
            var next = FindNextAlivePlayer(finisher);
            if (next == null)
            {
                Debug.Log("[FinisherSpectator] 남은 플레이어가 없음. 카메라 유지.");
                return;
            }

            ResolveReferences();
            if (_cameraService != null) _cameraService.Follow(next.transform);
            UpdateMinimapTarget(next);
            Debug.Log($"[FinisherSpectator] 관전 카메라 전환 → {next.gameObject.name}");
        }

        private static void UpdateMinimapTarget(PlayerController target)
        {
            // 관전 카메라가 다음 플레이어로 넘어가면 미니맵의 중심도 같이 바꾼다.
            // 그렇지 않으면 이미 탈출한 플레이어 위치가 계속 내 위치처럼 표시된다.
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
