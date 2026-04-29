using System;
using UnityEngine;
namespace BFS
{
    public class FSPlayer : MonoBehaviour
    {
        [field: SerializeField] public bool IsOut { get; private set; } = false;

        public event Action OnOut;
        private bool _outDetected = false;
        private void FixedUpdate()
        {
            if (transform.position.y <= -2 & !_outDetected)
            {
                IsOut = true;
                OnOut?.Invoke();
                _outDetected = true;
            }
        }
    }
}
