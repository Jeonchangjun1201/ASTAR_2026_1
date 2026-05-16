using _TeamFolder.KDH._01.Code.OutZone;
using UnityEngine;

namespace KDH
{
    public class OutZone : MonoBehaviour
    {
        private void OnTriggerExit(Collider other)
        {
            // 플레이어 아웃
            if (other.CompareTag("Player1") || other.CompareTag("Player2") ||
                other.CompareTag("Player3") || other.CompareTag("Player4"))
            {
                PlayerOut playerOut = other.GetComponent<PlayerOut>();
                if (playerOut != null)
                    playerOut.TriggerOut();
            }

            // 공 아웃 - Enter 대신 Exit으로 변경!
            if (other.CompareTag("Ball"))
            {
                BallSpawner spawner = FindObjectOfType<BallSpawner>();
                if (spawner != null)
                {
                    Destroy(other.gameObject);
                    spawner.OnGoalScored();
                    Debug.Log("공 아웃! 리스폰");
                }
            }
        }
    }
}