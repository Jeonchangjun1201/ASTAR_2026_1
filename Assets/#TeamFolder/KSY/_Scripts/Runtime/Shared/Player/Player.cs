using KSY.Utility;
using UnityEngine;

namespace KSY.Shared
{
    public class Player : MonoBehaviour
    {
        public PlayerMovementComponent MovementComponent => _movementComponent;
        public PlayerRendererComponent RendererComponent => _rendererComponent;
        public string PlayerID => _playerID;

        private PlayerMovementComponent _movementComponent;
        private PlayerRendererComponent _rendererComponent;
        private string _playerID = string.Empty;

        public void Initialize(string playerID)
        {
            this._playerID = playerID;
            if(!gameObject.TryGetComponentInChildren(out _movementComponent))
            {
                CustomLog.LogError("MovementComponent is null");
                return;
            }
            if (!gameObject.TryGetComponentInChildren(out _rendererComponent))
            {
                CustomLog.LogError("RendererComponent is null");
                return;
            }

            GameConfigTable gameConfigTable = GameInstance.DataTableManager.gameConfigTable;

            float speed = gameConfigTable.GetPlayerSpeed();
            float rotationSpeed = gameConfigTable.GetPlayerRotationSpeed();

            this._movementComponent.Initialize(this, speed, rotationSpeed);
            this._rendererComponent.Initialize(this);
        }
    }
}


