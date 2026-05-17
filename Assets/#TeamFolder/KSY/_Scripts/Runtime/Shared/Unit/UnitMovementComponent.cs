using UnityEngine;

namespace KSY.Shared
{
    public abstract class UnitMovementComponent : MonoBehaviour
    {
        public void Initialize()
        {
            OnInitialize();
        }

        public abstract void OnInitialize();
    }
}

