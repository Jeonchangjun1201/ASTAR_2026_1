using KSY.Shared;
using KSY.Shared.Packets;
using UnityEngine;

namespace KSY.Clients
{
    public class UnitInputComponent : MonoBehaviour
    {
        private Player player = null;
        private PlayerInputReader playerInputReader = null;

        private Vector2 lastMoveInput = Vector2.zero;

        private void Awake()
        {
            player = GetComponent<Player>();
            playerInputReader = InputManager.GetInput<PlayerInputReader>();
        }

        private void Update()
        {
            if (lastMoveInput != playerInputReader.MovementInput)
            {
                lastMoveInput = playerInputReader.MovementInput;
                ClientInstance.GameClient.Send(new C2S_MoveInputPacket() { MoveInput = lastMoveInput });
            }
        }
    }
}