using UnityEngine;

namespace KDH
{
    public class BlackHolePull : MonoBehaviour
    {
        private float _pullForce;
        private float _pullRadius;

        public void Initialize(float force, float radius)
        {
            _pullForce = force;
            _pullRadius = radius;
        }

        private void FixedUpdate()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _pullRadius);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Player1") || col.CompareTag("Player2") ||
                    col.CompareTag("Player3") || col.CompareTag("Player4"))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb == null) continue;

                    Vector3 direction = (transform.position - col.transform.position);
                    direction.y = 0f; // Y축 힘 제거!
                    direction.Normalize();

                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    float force = _pullForce * (1f - distance / _pullRadius);
                    rb.AddForce(direction * force, ForceMode.Force);
                }
            }
        }
    }
}