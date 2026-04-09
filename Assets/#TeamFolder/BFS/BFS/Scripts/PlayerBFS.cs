using UnityEngine;
namespace GDH
{
    public class PlayerBFS : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSOBFS PlayerInput { get; private set; }
        private PlayerMovementBFS _movement;

        private void Awake()
        {
            _movement = GetComponentInChildren<PlayerMovementBFS>();

            _movement.Initialize(this, GetComponent<CharacterController>());
        }
    }
}
