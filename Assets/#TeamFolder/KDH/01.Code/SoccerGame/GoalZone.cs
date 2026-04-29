using UnityEngine;

namespace KDH
{
    // 골대 오브젝트에 붙이는 스크립트
    // Tag: "Goal" 설정 필요
    public class GoalZone : MonoBehaviour
    {
        // 인스펙터에서 어느 팀/플레이어의 골대인지 지정
        [SerializeField] private string goalOwnerName = "Player 1";

        private void OnTriggerEnter(Collider other)
        {
            Ball ball = other.GetComponent<Ball>();
            if (ball == null) return;

            ball.NotifyGoal(goalOwnerName);
            ball.ResetBall();
        }
    }
}