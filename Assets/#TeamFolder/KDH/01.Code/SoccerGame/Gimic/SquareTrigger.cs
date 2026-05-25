using System.Collections;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace KDH
{
    public class SquareTrigger : MonoBehaviour
    {
        private float _launchForce;
        private bool _ready = false;

        public void Initialize(float force)
        {
            _launchForce = force;
            StartCoroutine(ReadyDelay());
        }

        private IEnumerator ReadyDelay()
        {
            yield return new WaitForSeconds(0.3f);
            _ready = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 공은 완전 무시!
            if (collision.gameObject.CompareTag("Ball")) return;

            Launch(collision.gameObject);
        }

        private void Launch(GameObject target)
        {
            if (!_ready) return;

            if (target.CompareTag("Player1") || target.CompareTag("Player2") ||
                target.CompareTag("Player3") || target.CompareTag("Player4"))
            {
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb == null) return;
                
                rb.constraints = RigidbodyConstraints.None;
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * _launchForce, ForceMode.Impulse);

                SoundManager.Instance.PlaySound("PlayerSky");
                Debug.Log($"{target.name} 하늘로 날아감!");
            } 
        }
    }
}