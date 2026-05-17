using UnityEngine.InputSystem;

namespace KSY.Clients
{
    public abstract class InputReaderBase
    {
        public virtual void Initialize(KSY_InputActions inputAction) { }
        public virtual void Release() { }
        public abstract InputActionMap GetInputActionMap();
    }
}

