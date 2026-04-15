using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class Player : MonoBehaviour
    {
        public LayerMask groundLayer;
        public Rigidbody Rigid { get; private set; }
        private Movement _movement;
        private Dictionary<Type, PlayerModuleBase> moduleDict;
        public event Action<Player, int> OnOutPlayerEvent;
        public int index;
        private bool _isOver;

        private void Awake()
        {
            Rigid = GetComponent<Rigidbody>();

            PlayerModuleBase[] array = GetComponentsInChildren<PlayerModuleBase>();
            moduleDict = array
                .ToDictionary(x => x.GetType(), x => x);

            foreach(PlayerModuleBase module in array)
            {
                module.Initialize(this);
            }
        }
        private void Update()
        {
            Rotation(GetPointerPos());
        }

        private void Rotation(Vector3 dir)
        {
            if (dir.magnitude < 0.01f) return;

            dir.y = 0;
            transform.forward = dir.normalized;
        }
        private Vector3 GetPointerPos()
        {
            Ray camRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(camRay, out RaycastHit hit, Camera.main.farClipPlane, groundLayer))
            {
                return hit.point;
            }
            return Vector3.zero;
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
        public PlayerModuleBase GetPlayerModule(Type type)
        {
            if (moduleDict.TryGetValue(type, out PlayerModuleBase value))
            {
                return value;
            }

            Debug.LogError("Module of invalid type!");
            return null;
        }

        public void Push(Vector3 dir, float force)
        {
            Debug.Log("Push!");
            Rigid.AddForce(dir * force, ForceMode.Impulse);
        }
    }
}
