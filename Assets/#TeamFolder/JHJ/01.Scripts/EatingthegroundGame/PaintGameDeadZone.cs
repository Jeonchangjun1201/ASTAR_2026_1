using System;
using System.Collections;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class PaintGameDeadZone : MonoBehaviour
    {
        [SerializeField] private float _reSpwanTime = 3f;


        private void OnCollisionEnter(Collision collision)
        {
            GameObject player = collision.gameObject;
            if (collision.gameObject.CompareTag("Player"))
            {
                player.SetActive(false);
                player.transform.position = new Vector3(0, 5, 0);
                StartCoroutine(ReSpwanCT(player));
            }
        }

        private IEnumerator ReSpwanCT(GameObject player)
        {
            yield return new WaitForSeconds(_reSpwanTime);
            player.gameObject.SetActive(true);
        }
    }
}

