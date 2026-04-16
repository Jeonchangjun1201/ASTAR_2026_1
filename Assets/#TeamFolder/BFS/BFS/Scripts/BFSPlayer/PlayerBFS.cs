using UnityEngine;
namespace BFS
{
    public class PlayerBFS : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSOBFS PlayerInput { get; private set; }            // Player Input SO (made by me :D)(its bad)
        private PlayerMovementBFS _movement;                                                         // Movement script

        private void Awake()
        {
            _movement = GetComponentInChildren<PlayerMovementBFS>();

            _movement.Initialize(this, GetComponent<CharacterController>());                         // Initialize movement script
        }
    }
}
