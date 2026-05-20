using UnityEngine;
namespace BFS
{
    public class FSMonitorMovement : MonoBehaviour
    {
        private Vector3 _destinationPoint;

        private void Awake()
        {
            _destinationPoint = transform.position;
        }
        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _destinationPoint, Time.deltaTime);
            //transform.position = _destinationPoint;
            _destinationPoint.y = Mathf.Sin(Time.time) * 2;
        }
    }
}
