using UnityEngine;

// 플레이어를 전투 구역 안으로 유지하고 이탈을 보정하는 경계 처리.

namespace _TeamFolder.JCJ.Battle
{
    public class BattleArenaBoundary : MonoBehaviour
    {
        private Transform _arenaRoot;
        private Rigidbody _rigidbody;
        private Bounds _bounds;
        private Vector3 _safePosition;
        private float _padding = 0.9f;
        private float _fallThresholdY = -6f;
        private float _recoverHeight = 0.8f;
        private bool _initialized;

        // 아레나 경계 계산에 필요한 기준값을 주입한다.
        // 서버가 스폰/세이프 위치를 정하면 그 값을 이 메서드로 넘겨 로컬 보정만 맡기면 된다.
        public void Configure(Transform arenaRoot, Vector3 safePosition, float padding, float fallThresholdY, float recoverHeight)
        {
            _arenaRoot = arenaRoot;
            _safePosition = safePosition;
            _padding = Mathf.Max(0.1f, padding);
            _fallThresholdY = fallThresholdY;
            _recoverHeight = Mathf.Max(0.2f, recoverHeight);
            _rigidbody = GetComponent<Rigidbody>();
            _initialized = TryBuildBounds();
        }

        public void SetSafePosition(Vector3 safePosition)
        {
            _safePosition = safePosition;
        }

        // 전투 구역 밖으로 밀려난 플레이어를 경계 안으로 되돌리는 물리 루프다.
        // 서버 authoritative 이동이면 이 로컬 강제 보정은 최소화하거나 결과 표시용으로만 써야 한다.
        private void FixedUpdate()
        {
            if (!_initialized)
            {
                _initialized = TryBuildBounds();
                if (!_initialized) return;
            }

            var position = transform.position;
            if (position.y <= _fallThresholdY)
            {
                RecoverToSafePosition();
                return;
            }

            float minX = _bounds.min.x + _padding;
            float maxX = _bounds.max.x - _padding;
            float minZ = _bounds.min.z + _padding;
            float maxZ = _bounds.max.z - _padding;

            float clampedX = Mathf.Clamp(position.x, minX, maxX);
            float clampedZ = Mathf.Clamp(position.z, minZ, maxZ);

            if (!Mathf.Approximately(clampedX, position.x) || !Mathf.Approximately(clampedZ, position.z))
            {
                transform.position = new Vector3(clampedX, position.y, clampedZ);
                if (_rigidbody != null)
                {
                    var velocity = _rigidbody.linearVelocity;
                    velocity.x = 0f;
                    velocity.z = 0f;
                    _rigidbody.linearVelocity = velocity;
                }
            }
            else
            {
                _safePosition = position;
            }
        }

        private void RecoverToSafePosition()
        {
            Vector3 recoverPosition = _safePosition;
            recoverPosition.y = Mathf.Max(recoverPosition.y, _bounds.min.y + _recoverHeight);
            transform.position = recoverPosition;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private bool TryBuildBounds()
        {
            if (_arenaRoot == null) return false;

            var colliders = _arenaRoot.GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            Bounds result = default;
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                if (!hasBounds)
                {
                    result = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
            {
                var renderers = _arenaRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null || !renderer.enabled) continue;
                    if (!hasBounds)
                    {
                        result = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        result.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!hasBounds) return false;
            _bounds = result;
            return true;
        }
    }
}
