using UnityEngine;
namespace BFS
{
    public class RopeTOW : MonoBehaviour, IRopeTOW                             // Rope class //줄 클래스
    {
        [SerializeField] private float moveSpeed = 1f;
        private Vector3 _destinationPoint;
        private void Awake()
        {
            _destinationPoint = transform.position;
        }
        public void MoveRope(Vector3 v)                                        // Method that moves itself to given direction from parameter // 움직이는 메서드
        {
            _destinationPoint += v;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _destinationPoint, Time.deltaTime * moveSpeed);
        }
    }

}
