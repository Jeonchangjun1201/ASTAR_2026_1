using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class GolfClub : PlayerModuleBase
    {
        private Player _owner;
        [SerializeField] private LayerMask _whatIsPlayer;
        [SerializeField] private float hitboxDistance;
        [SerializeField] private float hitboxSize;
        [SerializeField] private float _maxPower;
        [SerializeField] private float _powerMultpler;

        [SerializeField] private float _perPower = 0;

        [SerializeField] private GameObject _visual;
        private Coroutine _swingCoroutine;
        private bool _isSwing;

        public override void Initialize(Player player)
        {
            _owner = player;
        }

        private void Update()
        {
            if (Mouse.current.leftButton.isPressed && !_isSwing)
            {
                _visual.transform.localRotation = Quaternion.Euler(-25, 0, 0);

                _perPower = Mathf.Clamp((_perPower + (1 * _powerMultpler) * Time.deltaTime), 0, 100);
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && !_isSwing)
            {
                if (_swingCoroutine != null)
                {
                    _swingCoroutine = null;
                }

                _swingCoroutine ??= StartCoroutine(SwingHitbox());
            }
        }

        private IEnumerator SwingHitbox()
        {
            _isSwing = true;
            Debug.Log("Swing!");

            _visual.transform.localRotation = Quaternion.Euler(90, 0, 0);

            List<Collider> hitted = new List<Collider>();

            for (int i = 0; i < 5; i++)
            {
                Collider[] hits = Physics.OverlapSphere(
                    (transform.position + transform.forward) * hitboxDistance,
                    hitboxSize,
                    _whatIsPlayer);

                SwingPlayers(hitted, hits);
            }

            _visual.transform.localRotation = Quaternion.Euler(0, 0, 0); 
           _perPower = 0;

            yield return new WaitForSeconds(0.25f);

            _isSwing = false;
        }

        private void SwingPlayers(List<Collider> hitted, Collider[] hits)
        {
            if (hits.Length != 0)
            {
                foreach (Collider a in hits)
                {
                    if (hitted.Contains(a)) continue;

                    if (a.gameObject.TryGetComponent(out Player player))
                    {
                        player.Push(transform.position + transform.forward, (_maxPower / 100) * _perPower);
                    }
                    hitted.Add(a);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere((transform.position + transform.forward) * hitboxDistance, hitboxSize);
        }
    }

}
