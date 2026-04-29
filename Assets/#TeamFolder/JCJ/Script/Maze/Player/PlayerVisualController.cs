using DG.Tweening;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 리깅 메시 없이 절차적으로 만드는 스타일라이즈 캐릭터 비주얼.
    /// 실제 이동 Transform을 따라 회전하고 이동 상태에 맞춰 DOTween 애니메이션을 재생한다.
    /// </summary>
    public class PlayerVisualController : MonoBehaviour, IPlayerVisual
    {
        [Header("Palette")]
        [SerializeField] private Color _bodyColor    = new(0.94f, 0.88f, 0.74f);
        [SerializeField] private Color _accentColor  = new(0.95f, 0.45f, 0.20f);
        [SerializeField] private Color _eyeColor     = new(0.08f, 0.08f, 0.10f);

        [Header("Body")]
        [SerializeField] private float _bodyHeight = 1.4f;
        [SerializeField] private float _bodyRadius = 0.38f;
        [SerializeField] private float _visualYOffset = -0.45f;

        [Header("Anim")]
        [SerializeField] private float _idleScaleAmp   = 0.03f;
        [SerializeField] private float _walkBobAmp     = 0.12f;
        [SerializeField] private float _sprintBobAmp   = 0.22f;
        [SerializeField] private float _walkBobSpeed   = 7.5f;
        [SerializeField] private float _sprintBobSpeed = 11f;
        [SerializeField] private float _leanAngle      = 12f;

        private Transform _root;
        private Transform _body;
        private Transform _head;
        private Tween _idleTween;
        private float _bobPhase;
        private MovementKind _lastKind = MovementKind.Idle;
        private float _currentLeanZ;

        private enum MovementKind { Idle, Walk, Sprint }

        private void Awake()
        {
            BuildVisualHierarchy();
            StartIdleLoop();
        }

        private void OnDestroy()
        {
            _idleTween?.Kill();
        }

        // ───────── Build ─────────
        private void BuildVisualHierarchy()
        {
            var rootGo = new GameObject("Visual");
            _root = rootGo.transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, _visualYOffset, 0f);

            _body = MakePrimitive(PrimitiveType.Capsule, "Body", _root, _bodyColor,
                                  localPos: new Vector3(0f, 0f, 0f),
                                  localScale: new Vector3(_bodyRadius * 2f, _bodyHeight * 0.5f, _bodyRadius * 2f));

            var accent = MakePrimitive(PrimitiveType.Sphere, "Accent", _body, _accentColor,
                                       localPos: new Vector3(0f, -0.55f, 0f),
                                       localScale: new Vector3(1.02f, 0.35f, 1.02f));
            SetEmission(accent, _accentColor, 0.4f);

            _head = MakePrimitive(PrimitiveType.Sphere, "Head", _root, _bodyColor,
                                  localPos: new Vector3(0f, _bodyHeight * 0.65f, 0f),
                                  localScale: Vector3.one * (_bodyRadius * 1.7f));

            float eyeZ = _bodyRadius * 1.25f;
            float eyeY = _bodyHeight * 0.68f;
            MakePrimitive(PrimitiveType.Sphere, "EyeL", _root, _eyeColor,
                          localPos: new Vector3(-0.12f, eyeY, eyeZ),
                          localScale: Vector3.one * 0.12f);
            MakePrimitive(PrimitiveType.Sphere, "EyeR", _root, _eyeColor,
                          localPos: new Vector3( 0.12f, eyeY, eyeZ),
                          localScale: Vector3.one * 0.12f);
        }

        private Transform MakePrimitive(PrimitiveType type, string name, Transform parent, Color color,
                                        Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = BuildMaterial(color);
            return go.transform;
        }

        private static Material BuildMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            return mat;
        }

        private static void SetEmission(Transform t, Color color, float intensity)
        {
            var mr = t.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var mat = mr.sharedMaterial;
            if (mat == null) return;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * intensity);
            }
        }

        // ───────── State-driven anim ─────────
        public void OnIdle()
        {
            if (_lastKind == MovementKind.Idle) return;
            _lastKind = MovementKind.Idle;
            StartIdleLoop();
            ResetLean();
        }

        public void OnWalk(float speedNormalized)
        {
            ApplyLocomotion(MovementKind.Walk, _walkBobSpeed, _walkBobAmp, _leanAngle * 0.6f);
        }

        public void OnSprint(float speedNormalized)
        {
            ApplyLocomotion(MovementKind.Sprint, _sprintBobSpeed, _sprintBobAmp, _leanAngle);
        }

        public void OnPickup()
        {
            if (_root == null) return;
            _root.DOPunchPosition(transform.forward * 0.12f + Vector3.up * 0.08f, 0.18f, 1, 0.2f);
            _root.DOPunchRotation(new Vector3(-12f, 0f, 0f), 0.18f, 1, 0.2f);
        }

        public void OnJump()
        {
            if (_root == null) return;
            _root.DOKill(complete: false);
            _root.localScale = Vector3.one;
            var seq = DOTween.Sequence();
            seq.Append(_root.DOScaleY(1.25f, 0.12f).SetEase(Ease.OutQuad));
            seq.Join(_root.DOScaleX(0.85f, 0.12f).SetEase(Ease.OutQuad));
            seq.Join(_root.DOScaleZ(0.85f, 0.12f).SetEase(Ease.OutQuad));
            seq.Append(_root.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack));
        }

        public void OnFall()
        {
            if (_root == null) return;
            _root.DOKill(complete: false);
            _root.DOScaleY(0.9f, 0.2f).SetEase(Ease.OutQuad);
        }

        public void OnLand()
        {
            if (_root == null) return;
            _root.DOPunchScale(new Vector3(0.25f, -0.3f, 0.25f), 0.22f, vibrato: 1, elasticity: 0.4f);
        }

        public void OnCollect()
        {
            if (_root == null) return;
            _root.DOPunchRotation(new Vector3(0f, 0f, 20f), 0.3f, vibrato: 3, elasticity: 0.8f);
        }

        public void OnPush()
        {
            if (_root == null) return;
            _root.DOPunchPosition(transform.forward * 0.22f, 0.18f, 1, 0.2f);
            _root.DOPunchRotation(new Vector3(-16f, 0f, 0f), 0.18f, 1, 0.2f);
        }

        public void OnThrow()
        {
            if (_root == null) return;
            _root.DOPunchPosition(transform.forward * 0.18f, 0.22f, 1, 0.2f);
            _root.DOPunchRotation(new Vector3(-10f, 0f, 0f), 0.2f, 1, 0.2f);
        }

        public void SetCarryState(bool carrying, bool moving)
        {
            if (_root == null) return;
            StopIdleLoop();
            if (!carrying)
            {
                if (_lastKind == MovementKind.Idle) StartIdleLoop();
                return;
            }

            _root.localScale = Vector3.one;
            _root.localPosition = new Vector3(0f, _visualYOffset + (moving ? 0.05f : 0f), 0f);
            _root.localRotation = Quaternion.Euler(moving ? 8f : 4f, 0f, 0f);
        }

        private void ApplyLocomotion(MovementKind kind, float bobSpeed, float bobAmp, float leanDeg)
        {
            _lastKind = kind;
            StopIdleLoop();

            _bobPhase += Time.deltaTime * bobSpeed;
            float y = Mathf.Sin(_bobPhase) * bobAmp;
            _root.localPosition = new Vector3(0f, _visualYOffset + y, 0f);

            // 현재 전진 기준 기울기 (rigidbody가 있다면 로컬 Z+ 방향)
            float targetLean = leanDeg;
            _currentLeanZ = Mathf.Lerp(_currentLeanZ, targetLean, Time.deltaTime * 8f);
            _root.localRotation = Quaternion.Euler(_currentLeanZ, 0f, 0f);
        }

        private void StartIdleLoop()
        {
            if (_root == null) return;
            StopIdleLoop();
            _root.localPosition = new Vector3(0f, _visualYOffset, 0f);
            _root.localRotation = Quaternion.identity;
            _idleTween = _root
                .DOScale(Vector3.one * (1f + _idleScaleAmp), 1.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopIdleLoop()
        {
            if (_idleTween != null && _idleTween.IsActive())
            {
                _idleTween.Kill();
                _idleTween = null;
            }
            if (_root != null) _root.localScale = Vector3.one;
        }

        private void ResetLean()
        {
            _currentLeanZ = 0f;
            if (_root != null) _root.localRotation = Quaternion.identity;
        }
    }
}
