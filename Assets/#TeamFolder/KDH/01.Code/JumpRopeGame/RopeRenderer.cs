using UnityEngine;

namespace KDH
{
    public class RopeRenderer : MonoBehaviour
    {
        [Header("줄 설정")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private int segments = 15;
        [SerializeField] private float ropeHeight = 2f;

        [Header("콜라이더 설정")]
        [SerializeField] private float colliderRadius = 0.05f;

        private LineRenderer _lineRenderer;
        private GameObject[] _colliderSegments;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = segments;

            CreateColliders();
        }

        private void CreateColliders()
        {
            _colliderSegments = new GameObject[segments - 1];
            for (int i = 0; i < segments - 1; i++)
            {
                GameObject seg = new GameObject($"RopeSeg_{i}");
                seg.transform.SetParent(transform);

                CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.radius = colliderRadius;
                col.direction = 2; // Z축

                seg.AddComponent<RopeHit>();
                _colliderSegments[i] = seg;
            }
        }

        private void Update()
        {
            DrawRope();
            UpdateColliders();
        }

        private void DrawRope()
        {
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);

                // Vector3.down 대신 회전 방향 기준으로 휘기
                pos -= transform.parent.up * Mathf.Sin(t * Mathf.PI) * ropeHeight;

                _lineRenderer.SetPosition(i, pos);
            }
        }

        private void UpdateColliders()
        {
            for (int i = 0; i < segments - 1; i++)
            {
                Vector3 a = _lineRenderer.GetPosition(i);
                Vector3 b = _lineRenderer.GetPosition(i + 1);

                _colliderSegments[i].transform.position = (a + b) / 2f;
                _colliderSegments[i].transform.rotation =
                    Quaternion.LookRotation(b - a);

                CapsuleCollider col =
                    _colliderSegments[i].GetComponent<CapsuleCollider>();
                col.height = Vector3.Distance(a, b);
                col.radius = colliderRadius;
            }
        }
    }
}