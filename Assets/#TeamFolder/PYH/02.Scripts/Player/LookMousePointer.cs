using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._02.Scripts.Player
{
    public class LookMousePointer : MonoBehaviour
    {
        private Camera _main;
        [SerializeField] private GameObject visual;
        [SerializeField] private LayerMask groundLayer;

        private void Awake()
        {
            _main = Camera.main;
        }

        private void FixedUpdate()
        {
            Rotation(GetPointerPos());
        }
        private void Rotation(Vector3 dir)
        {
            if (dir.magnitude < 0.01f) return;
            
            Vector3 targetDir = dir - transform.position;

            targetDir.y = 0;
            visual.transform.forward = targetDir;
        }
        private Vector3 GetPointerPos()
        {
            Ray camRay = _main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(camRay, out RaycastHit hit, _main.farClipPlane, groundLayer))
            {
                return hit.point;
            }
            return Vector3.zero;
        }
    }
}