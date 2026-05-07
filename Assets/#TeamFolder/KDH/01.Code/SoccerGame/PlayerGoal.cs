using UnityEngine;

namespace KDH
{
    public class PlayerGoal : MonoBehaviour
    {
        // 인스펙터에서 자기 골대 오브젝트 드래그
        [SerializeField] private Transform myGoal;

        private void OnTriggerEnter(Collider other)
        {
            Ball ball = other.GetComponent<Ball>();
            if (ball == null) return;

            // 공이 내 골대 안에 들어왔는지 확인
            if (other.transform.position == myGoal.position) 
            {
                ball.NotifyGoal(gameObject.name);
                ball.ResetBall();
            }
        }
    }
}