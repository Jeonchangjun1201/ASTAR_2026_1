using System;
using PYH.Player;
using UnityEngine;

namespace KDH.Gimic
{
    public class FireBallEffect : MonoBehaviour
    {
        [SerializeField] private float redBlindTime = 3f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player1") ||
                collision.gameObject.CompareTag("Player2") ||
                collision.gameObject.CompareTag("Player3") ||
                collision.gameObject.CompareTag("Player4"))
            {
                PlayerBlind blind = collision.gameObject.GetComponent<PlayerBlind>();
                if(blind != null)
                    blind.StartBlind(redBlindTime);
                
                Destroy(gameObject);
            }
        }
    }
}