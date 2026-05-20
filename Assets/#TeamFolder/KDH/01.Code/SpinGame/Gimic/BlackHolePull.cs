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
            // 범위 안에 있는 팽이 전부 당기기
            Collider[] colliders = Physics.OverlapSphere(transform.position, _pullRadius);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Player1") || col.CompareTag("Player2") ||
                    col.CompareTag("Player3") || col.CompareTag("Player4"))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb == null) continue;

                    // 블랙홀 방향으로 당기기
                    Vector3 direction = (transform.position - col.transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, col.transform.position);

                    // 가까울수록 더 강하게 당김
                    float force = _pullForce * (1f - distance / _pullRadius);
                    rb.AddForce(direction * force, ForceMode.Force);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _pullRadius);
        }
    }
}