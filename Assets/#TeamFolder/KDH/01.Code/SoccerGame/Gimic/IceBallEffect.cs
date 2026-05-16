using System;
using UnityEngine;

namespace KDH.Gimic
{
    public class IceBallEffect : MonoBehaviour
    {
        [SerializeField] private float freezeDuration = 3f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player1") ||
                collision.gameObject.CompareTag("Player2") ||
                collision.gameObject.CompareTag("Player3") ||
                collision.gameObject.CompareTag("Player4"))
            {
                PlayerFreeze freeze = collision.gameObject.GetComponent<PlayerFreeze>();
                if (freeze != null)
                    freeze.StartFreeze(freezeDuration);

                Destroy(gameObject);
            }
        }
    }
}