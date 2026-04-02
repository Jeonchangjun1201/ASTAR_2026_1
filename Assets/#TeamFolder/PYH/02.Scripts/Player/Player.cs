using System;
using UnityEngine;

namespace PYH.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class Player : MonoBehaviour
    {
        public Rigidbody _rigd;
        private Movement _movement;
        public event Action<Player, int> OnOutPlayerEvent;
        public int index;

        private bool _isOver;

        private void Awake()
        {
            _movement = GetComponentInChildren<Movement>();
            _rigd = GetComponent<Rigidbody>();

            Debug.Assert(_movement != null, "Movement Is NULL!");

            _movement.Initialize(_rigd);
        }

        public void DelPlayer()
        {
            Debug.Log($"Player {gameObject.name} Is Dead ");
            gameObject.SetActive(false);
        }
        public void OverPlayer()
        {
            if (_isOver) return;

            _isOver = true;
            OnOutPlayerEvent?.Invoke(this, index);
        }

        public void Push(Vector3 dir, float force)
        {
            Debug.Log("Push!");
            _rigd.AddForce(dir * force, ForceMode.Impulse);
        }
    }
}
