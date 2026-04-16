using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public class GoalTrigger : MonoBehaviour
    {
        private IRankService _rankService;
        public void Inject(IRankService rankService)
        {
            _rankService = rankService;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // 게임 진행 중일 때만 등록
            if (GameStateManager.Instance == null ||
                GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _rankService?.RegisterFinish(other.name);
        }
    }
}
