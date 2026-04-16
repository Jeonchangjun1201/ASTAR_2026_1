
using UnityEngine;

namespace KDH
{
    public class Victory : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // 공이 골대에 닿았을 때
            if (other.CompareTag("Ball"))
            {
                Ball ballScript = other.GetComponent<Ball>();

                if (ballScript != null)
                {
                    // 공을 마지막으로 건드린 사람 가져오기
                    string scorer = ballScript.LastTouchPlayer;

                    // 디버그 콘솔에 출력
                    Debug.Log($"골 {scorer} ㅊㅊ");

                    // 공 다시 원점으로 리셋
                    ballScript.ResetBall();
                }
            }
        }
    
    }
}
