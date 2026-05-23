using KSY.Utility;
using UnityEngine;

namespace KSY.Shared
{
    public class Player : MonoBehaviour
    {
        public PlayerMovementComponent MovementComponent { get; private set; }
        public PlayerRendererComponent RendererComponent { get; private set; }

        public string PlayerID => _playerID;
        private string _playerID = string.Empty;

        public void Initialize(string playerID)
        {
            this._playerID = playerID;
            if(!gameObject.TryGetComponentInChildren(out PlayerMovementComponent MovementComponent))
            {
                CustomLog.LogError("MovementComponent is null");
                return;
            }
            if (!gameObject.TryGetComponentInChildren(out PlayerRendererComponent RendererComponent))
            {
                CustomLog.LogError("RendererComponent is null");
                return;
            }

            GameConfigTable gameConfigTable = GameInstance.DataTableManager.gameConfigTable;

            float speed = gameConfigTable.GetPlayerSpeed();
            float rotationSpeed = gameConfigTable.GetPlayerRotationSpeed();

            this.MovementComponent.Initialize(speed, rotationSpeed);
            this.RendererComponent.Initialize(this);
        }
    }
}


