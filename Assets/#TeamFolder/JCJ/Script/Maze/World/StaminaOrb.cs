using DG.Tweening;
using UnityEngine;

// 획득 시 스태미나를 회복시키는 오브 아이템 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 플레이어 스태미나를 채워주는 픽업 아이템. 프리팹 없이 떠오르며 회전하는 발광 오브 비주얼을 직접 만든다.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class StaminaOrb : MonoBehaviour
    {
        [SerializeField] private string _playerTag     = "Player";
        [SerializeField] private float  _staminaRefill = 40f;
        [SerializeField] private int    _scoreReward   = 5;
        [SerializeField] private Color  _orbColor      = new(0.80f, 0.92f, 1.00f);

        private bool _collected;
        private Transform _visual;
        private Light _glow;

        private void Awake()
        {
            BuildVisual();
            var col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            if (col is SphereCollider sc) { sc.radius = 0.6f; sc.center = new Vector3(0f, 0.6f, 0f); }
        }

        private void Start()
        {
            if (_visual != null)
            {
                _visual.DORotate(new Vector3(0f, 360f, 0f), 3f, RotateMode.FastBeyond360)
                       .SetLoops(-1, LoopType.Incremental)
                       .SetEase(Ease.Linear);

                var baseLocal = _visual.localPosition;
                var seq = DOTween.Sequence();
                seq.Append(_visual.DOLocalMoveY(baseLocal.y + 0.18f, 1f).SetEase(Ease.InOutSine));
                seq.Append(_visual.DOLocalMoveY(baseLocal.y - 0.18f, 1f).SetEase(Ease.InOutSine));
                seq.SetLoops(-1);
            }
            if (_glow != null)
            {
                DOTween.To(() => _glow.intensity, v => _glow.intensity = v, 2.8f, 0.9f)
                       .SetLoops(-1, LoopType.Yoyo)
                       .SetEase(Ease.InOutSine);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag(_playerTag)) return;

            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.CurrentState != GameState.Playing) return;

            var pc = other.GetComponent<PlayerController>();
            if (pc == null) return;

            _collected = true;
            pc.RefillStamina(_staminaRefill);
            if (_scoreReward > 0)
            {
                RuntimePlayerIdentity.TryResolve(other, out var playerId, out var displayName);
                gsm.Score?.Add(playerId, displayName, _scoreReward);
            }
            pc.NotifyCollected();

            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
                     .OnComplete(() => Destroy(gameObject));
        }

        private void BuildVisual()
        {
            _visual = new GameObject("OrbVisual").transform;
            _visual.SetParent(transform, false);
            _visual.localPosition = new Vector3(0f, 0.7f, 0f);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Core";
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(_visual, false);
            sphere.transform.localScale = Vector3.one * 0.45f;
            PaintMaterial(sphere, _orbColor, emission: 1.8f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring.name = "Halo";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(_visual, false);
            ring.transform.localScale = new Vector3(0.7f, 0.04f, 0.7f);
            PaintMaterial(ring, _orbColor, emission: 1.4f);

            var lgo = new GameObject("OrbLight");
            lgo.transform.SetParent(_visual, false);
            _glow = lgo.AddComponent<Light>();
            _glow.type      = LightType.Point;
            _glow.color     = _orbColor;
            _glow.intensity = 2.2f;
            _glow.range     = 6f;
        }

        private static void PaintMaterial(GameObject go, Color color, float emission = 0f)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0.2f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            mr.sharedMaterial = mat;
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_visual != null) _visual.DOKill();
            DOTween.Kill(this);
        }
    }
}
