using UnityEngine;

// 플레이어 골인 판정을 발생시키는 트리거.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어가 골 지점 트리거에 들어왔을 때 랭킹 서비스에 완주를 등록한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";

        private IRankService _rankService;

        public void Inject(IRankService rankService)
        {
            _rankService = rankService;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
            // 충돌한 Collider가 플레이어 루트가 아닐 수 있으므로 부모에서 PlayerController를 찾는다.
            // 랭크 등록에는 반드시 루트 플레이어 이름을 사용해야 관전/미니맵/포디움에서 같은 대상을 찾을 수 있다.
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) player = other.GetComponent<PlayerController>();
            if (player == null) return;

            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.CurrentState != GameState.Playing) return;

            var identity = RuntimePlayerIdentity.Find(player);
            _rankService ??= gsm.Rank;
            if (identity != null) _rankService?.RegisterFinish(identity.PlayerId, identity.DisplayName);
            else _rankService?.RegisterFinish(player.gameObject.name);
        }
    }
}
