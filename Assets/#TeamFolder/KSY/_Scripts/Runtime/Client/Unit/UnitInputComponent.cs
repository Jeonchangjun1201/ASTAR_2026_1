using KSY.Shared;
using System;
using UnityEngine;

namespace KSY.Clients
{
    public class UnitInputComponent : MonoBehaviour
    {
        private Unit unit = null;
        private PlayerInputReader playerInputReader = null;

        private Vector2 lastMoveInput = Vector2.zero;
        private bool isFire = false;

        private void Awake()
        {
            unit = GetComponent<Unit>();

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