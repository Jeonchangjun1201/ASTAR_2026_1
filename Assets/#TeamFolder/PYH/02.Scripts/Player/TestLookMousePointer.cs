using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class TestLookMousePointer : MonoBehaviour
    {
        [SerializeField] private GameObject visual;
        [SerializeField] private LayerMask groundLayer;

        private void Update()
        {
            Rotation(GetPointerPos());
        }
        private void Rotation(Vector3 dir)
        {
            if (dir.magnitude < 0.01f) return;

            dir.y = 0;
            visual.transform.forward = dir;
        }
        private Vector3 GetPointerPos()
        {
            Ray camRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(camRay, out RaycastHit hit, Camera.main.farClipPlane, groundLayer))
            {
                return hit.point;
            }
            return Vector3.zero;
        }
    }
}