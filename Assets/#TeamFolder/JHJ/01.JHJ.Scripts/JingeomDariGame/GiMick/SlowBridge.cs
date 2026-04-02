using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;

namespace JHJ.JHJ.Scripts.GiMick
{
    public class SlowBridge : MonoBehaviour
    {
        [Header("Setting")]
        [SerializeField] private float slowSpeed = 2f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out TestPlayerController player))
            {
                player.SetMoveSpeed(slowSpeed);
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out TestPlayerController player))
            {
                player.ResetMoveSpeed();
            }
        }
    }
}