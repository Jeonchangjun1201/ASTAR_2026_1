using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 _prepOffset = new Vector3(0f, 15f, -18f);
        [SerializeField] private Vector3 _combatOffset = new Vector3(0f, 7.5f, -8.5f);
        [SerializeField] private Vector3 _resultOffset = new Vector3(0f, 11f, -13f);
        [SerializeField] private float _positionSmoothTime = 0.18f;
        [SerializeField] private float _rotationLerpSpeed = 8f;
        [SerializeField] private float _prepFov = 58f;
        [SerializeField] private float _combatFov = 64f;
        [SerializeField] private float _resultFov = 54f;

        private Camera _camera;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            var manager = ArenaGameManager.Instance;
            if (manager == null || _camera == null)
            {
                return;
            }

            Vector3 focusPoint = ResolveFocusPoint(manager);
            Vector3 desiredPosition = focusPoint + ResolveOffset(manager, focusPoint);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);

            Quaternion targetRotation = Quaternion.LookRotation((focusPoint - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationLerpSpeed);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, ResolveFov(manager), Time.deltaTime * 5f);
        }

        private Vector3 ResolveFocusPoint(ArenaGameManager manager)
        {
            if (manager.CurrentPhase == ArenaPhase.Playing)
            {
                var local = manager.GetPreferredFollowTarget();
                if (local != null)
                {
                    return local.transform.position + Vector3.up * 1.45f;
                }
            }

            return manager.GetArenaCenter() + Vector3.up * 1.25f;
        }

        private Vector3 ResolveOffset(ArenaGameManager manager, Vector3 focusPoint)
        {
            if (manager.CurrentPhase == ArenaPhase.Playing)
            {
                var target = manager.GetPreferredFollowTarget();
                if (target != null)
                {
                    Vector3 backward = -Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
                    if (backward.sqrMagnitude < 0.01f)
                    {
                        backward = Vector3.back;
                    }

                    return Vector3.up * _combatOffset.y + backward * Mathf.Abs(_combatOffset.z);
                }
            }

            if (manager.CurrentPhase == ArenaPhase.Finished)
            {
                return _resultOffset;
            }

            return _prepOffset;
        }

        private float ResolveFov(ArenaGameManager manager)
        {
            return manager.CurrentPhase switch
            {
                ArenaPhase.Playing => _combatFov,
                ArenaPhase.Finished => _resultFov,
                _ => _prepFov
            };
        }
    }
}
