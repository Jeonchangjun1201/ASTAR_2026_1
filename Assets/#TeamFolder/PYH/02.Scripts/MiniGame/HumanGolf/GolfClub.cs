using System.Collections;
using System.Collections.Generic;
using _TeamFolder.PYH._02.Scripts.Player;
using csiimnida.CSILib.SoundManager.RunTime;
using KSY.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.HumanGolf
{
    public class GolfClub : PlayerModuleBase
    {
        private HumanGolfModule _owner;

        [SerializeField] private LayerMask whatIsPlayer;
        [SerializeField] private float hitboxDistance;
        [SerializeField] private float hitboxSize;

        [SerializeField] private float _maxPower;
        [SerializeField] private float _powerMultpler;

        [SerializeField] private float _perPower = 0f;

        [SerializeField] private GameObject _visual;

        private Coroutine _swingCoroutine;
        private bool _isSwing;
        private bool _sounded;

        public override void Initialize(HumanGolfModule player)
        {
            _owner = player;
        }

        private void Update()
        {
            if (Mouse.current.leftButton.isPressed && !_isSwing)
            {
                _visual.transform.localRotation = Quaternion.Euler(-25, 0, 0);

                if (!_sounded)
                {
                    SoundManager.Instance.PlaySound("HumanGolf-Charging-S");
                    _sounded = true;
                }

                _perPower = Mathf.Clamp(
                    _perPower + _powerMultpler * Time.deltaTime,
                    0f,
                    100f
                );
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && !_isSwing)
            {
                if (_swingCoroutine == null)
                {
                    SoundManager.Instance.PlaySound("HumanGolf-Swing-S");
                    _sounded = false;
                    _swingCoroutine = StartCoroutine(SwingHitbox());
                }
            }
        }

        private IEnumerator SwingHitbox()
        {
            _isSwing = true;

            _visual.transform.localRotation = Quaternion.Euler(90, 0, 0);

            List<Collider> hitted = new();

            for (int i = 0; i < 5; i++)
            {
                Vector3 hitboxCenter = transform.position + transform.forward * hitboxDistance;

                Collider[] hits = Physics.OverlapSphere(
                    hitboxCenter,
                    hitboxSize,
                    whatIsPlayer
                );

                SwingPlayers(hitted, hits);

                yield return null;
            }

            _visual.transform.localRotation = Quaternion.Euler(0, 0, 0);

            _perPower = 0f;

            yield return new WaitForSeconds(0.25f);

            _isSwing = false;
            _swingCoroutine = null;
        }

        private void SwingPlayers(List<Collider> hitted, Collider[] hits)
        {
            foreach (Collider hit in hits)
            {
                if (hitted.Contains(hit)) continue;

                if (hit.gameObject.TryGetComponentInChildren(out HumanGolfModule player))
                {
                    if (player == _owner) continue;

                    float power = _maxPower * (_perPower / 100f);
                    Vector3 pushDir = transform.forward.normalized;

                    player.Push(pushDir, power);
                }

                hitted.Add(hit);
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 hitboxCenter = transform.position + transform.forward * hitboxDistance;

            Gizmos.DrawSphere(hitboxCenter, hitboxSize);
        }
    }
}