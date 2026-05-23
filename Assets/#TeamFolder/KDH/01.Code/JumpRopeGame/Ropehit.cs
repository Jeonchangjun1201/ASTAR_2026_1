using UnityEngine;
using JHJ.Scripts.Test.TestPlayer;

namespace KDH
{
    public class RopeHit : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player1") || other.CompareTag("Player2") ||
                other.CompareTag("Player3") || other.CompareTag("Player4"))
            {
                // 플레이어 컨트롤러 끄기
                JHJPlayerController controller = other.GetComponent<JHJPlayerController>();
                if (controller != null)
                    controller.enabled = false;

                // 탈락 이벤트 발생
                RopeGameManager.OnPlayerOut?.Invoke(other.gameObject.name);

                // 플레이어 비활성화
                other.gameObject.SetActive(false);

                Debug.Log($"{other.gameObject.name} out");
            }
        }
    }
}