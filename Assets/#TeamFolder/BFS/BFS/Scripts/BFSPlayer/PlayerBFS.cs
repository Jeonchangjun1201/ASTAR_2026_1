using UnityEngine;
namespace BFS
{
    public class PlayerBFS : MonoBehaviour
    {
        [field: SerializeField] public PlayerInputSOBFS PlayerInput { get; private set; }            // Player Input SO (made by me :D)(its bad) // 플레이어 인풋 SO(임시)
        private PlayerMovementBFS _movement;                                                         // Movement script // 무브먼트 스크립

        private void Awake()
        {
            _movement = GetComponentInChildren<PlayerMovementBFS>();

            _movement.Initialize(this, GetComponent<CharacterController>());                         // Initialize movement script // 무브먼트 스크립트
        }
    }   
}
