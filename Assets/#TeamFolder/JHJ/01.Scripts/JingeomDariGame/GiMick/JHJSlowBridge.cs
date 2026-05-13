using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.GiMick
{
    public class JHJSlowBridge : MonoBehaviour
    {
        [Header("Setting")]
        [SerializeField] private float slowSpeed = 2f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out JHJPlayerController player))
            {
               // player.SetMoveSpeed(slowSpeed);
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out JHJPlayerController player))
            {
               // player.ResetMoveSpeed();
            }
        }
    }
}