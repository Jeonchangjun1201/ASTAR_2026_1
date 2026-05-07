using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PYH.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class Player : MonoBehaviour
    {
        public Rigidbody Rigid { get; private set; }
        public CharacterController CharacterController { get; private set; }
        private Movement _movement;
        private Dictionary<Type, PlayerModuleBase> moduleDict;
        public event Action<Player, int> OnOutPlayerEvent;
        public int index;
        private bool _isOver;

        private bool isPush;

        private void Awake()
        {
            Rigid = GetComponent<Rigidbody>();
            CharacterController = GetComponent<CharacterController>();

            PlayerModuleBase[] array = GetComponentsInChildren<PlayerModuleBase>();
            moduleDict = array
                .ToDictionary(x => x.GetType(), x => x);

            foreach(PlayerModuleBase module in array)
            {
                module.Initialize(this);
            }
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
            if (isPush) return;
            
            Debug.Log("Push!");
            StartCoroutine(PushCoroutine(dir, force));
        }

        private IEnumerator PushCoroutine(Vector3 dir, float force)
        {
            float lastTime = Time.time;

            isPush = true;
            
            while (lastTime < Time.time + 5)
            {
                Rigid.AddForce(dir * force, ForceMode.Impulse);

                yield return null;
            }
            
            isPush = false;
        }
    }
}
