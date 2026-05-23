using System.Collections;
using UnityEngine;
using _TeamFolder.JCJ.Script;

// 모든 타일이 공통으로 쓰는 기본 동작과 상태를 담는 베이스 클래스.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 모든 타일 기반 추상 클래스.
    /// 낙하 로직(대기 → 경고 → 낙하 → 삭제)을 처리하고
    /// OnPlayerStep()을 자식에 위임한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class BaseTile : MonoBehaviour, ITile
    {
        // Initialize()로 주입
        private float _stepDelay    = 2.0f;
        private float _warnDuration = 0.5f;
        private float _fallDuration = 1.0f;
        private float _fallDistance = 15.0f;
        private bool _fadeOutEnabled = false;
        private float _fallShortDistance = 2.5f;

        public bool HasFallen    { get; private set; }
        public bool IsProcessing { get; private set; }

        /// <summary>이 타일의 색 태그(ColorCallDirector가 생존 필터에 사용).</summary>
        public TileColor TileTag  { get; private set; }
        public int LayerIndex { get; private set; } = -1;
        public int GridX { get; private set; }
        public int GridZ { get; private set; }

        public void SetGridIndex(int layer, int x, int z)
        {
            LayerIndex = layer;
            GridX = x;
            GridZ = z;
        }
        /// <summary>낙하 확정(경고 시작 또는 처리 중)이면 true.</summary>
        public bool IsCondemned   => HasFallen || IsProcessing;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        // ── 초기화 ─────────────────────────────────────
        public void Initialize(float stepDelay, float warnDuration,
                               float fallDuration, float fallDistance)
        {
            _stepDelay    = stepDelay;
            _warnDuration = warnDuration;
            _fallDuration = fallDuration;
            _fallDistance = fallDistance;
        }

        public void ConfigureFadeOut(bool enabled, float shortDistance)
        {
            _fadeOutEnabled = enabled;
            _fallShortDistance = Mathf.Max(0.1f, shortDistance);
        }

        public void SetColorTag(TileColor color) => TileTag = color;

        // ── 자식 구현 ──────────────────────────────────
        public abstract void OnPlayerStep(PlayerController player);

        protected virtual void OnCollisionEnter(Collision collision)
        {
            // 타일을 처음 밟는 순간을 감지한다.
            // 서버 연동 시에는 이 접촉 판정을 서버/호스트에서만 인정해야 클라이언트별 타일 낙하가 갈라지지 않는다.
            TryRegisterStep(collision);
        }

        // 스폰 직후 타일 위에 가만히 있는 경우: Enter는 카운트다운 중 한 번만 오고 상태 가드에 막힘.
        // Stay로 재시도해야 정지 플레이어에게도 타이머가 돈다. IsProcessing으로 중복 방지.
        protected virtual void OnCollisionStay(Collision collision)
        {
            TryRegisterStep(collision);
        }

        private void TryRegisterStep(Collision collision)
        {
            if (HasFallen || IsProcessing) return;

            // 실제 Playing에서만 낙하 타이머 시작 — 카운트다운 중 스폰 타일 전부 도는 것 방지.
            var gm = TileGameManager.Instance;
            if (gm != null && gm.State != GameState.Playing) return;

            // PlayerController가 닿았을 때만 타일 효과를 발동한다.
            // 플레이어 외 오브젝트나 파티클, 카메라 콜라이더가 타일을 떨어뜨리지 않게 하는 방어선이다.
            if (collision.gameObject.TryGetComponent<PlayerController>(out var player))
                OnPlayerStep(player);
        }

        // ── 낙하 트리거 ────────────────────────────────
        /// <param name="skipPreDelay">true = stepDelay 생략 (기믹이 이미 대기한 경우)</param>
        public void StartFalling(bool skipPreDelay = false)
        {
            // 낙하 예약의 단일 진입점이다.
            // IsProcessing으로 중복 호출을 막아서 여러 플레이어가 같은 타일을 동시에 밟아도 코루틴은 한 번만 돈다.
            if (IsProcessing || HasFallen) return;
            IsProcessing = true;
            StartCoroutine(FallRoutine(skipPreDelay));
        }

        private IEnumerator FallRoutine(bool skipPreDelay)
        {
            // 1) 밟은 뒤 대기, 2) 경고 깜빡임, 3) 실제 낙하/페이드, 4) 삭제 순서로 진행된다.
            // 네트워크에서는 코루틴 시간 대신 서버가 정한 dropTick/dropTime을 동기화하면 desync를 줄일 수 있다.
            // 1. 대기 단계
            if (!skipPreDelay)
                yield return new WaitForSeconds(_stepDelay);

            // 2. 경고 깜빡임
            yield return StartCoroutine(WarnRoutine());

            if (_fadeOutEnabled)
            {
                yield return StartCoroutine(FallWithFadeRoutine());
            }
            else
            {
                yield return StartCoroutine(FallLongRoutine());
            }

            HasFallen = true;
            SpawnDustPuff();
            Destroy(gameObject);
        }

        private IEnumerator FallLongRoutine()
        {
            Vector3 start = transform.position;
            Vector3 end   = start + Vector3.down * _fallDistance;
            float elapsed = 0f;

            while (elapsed < _fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _fallDuration);
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }

        private IEnumerator FallWithFadeRoutine()
        {
            var rend = GetComponent<Renderer>();
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Color startColor = ResolveBaseColor(rend);
            float duration = Mathf.Max(0.05f, _fallDuration);
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * _fallShortDistance;

            var mpb = new MaterialPropertyBlock();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float n = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, n);
                transform.position = Vector3.Lerp(start, end, ease);

                if (rend != null)
                {
                    rend.GetPropertyBlock(mpb);
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, ease);
                    mpb.SetColor(_baseColorId, c);
                    mpb.SetColor(_colorId, c);
                    rend.SetPropertyBlock(mpb);
                }

                yield return null;
            }
        }

        private static Color ResolveBaseColor(Renderer rend)
        {
            if (rend == null) return Color.white;
            var mat = rend.sharedMaterial;
            if (mat == null) return Color.white;
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color")) return mat.color;
            return Color.white;
        }

        private void SpawnDustPuff()
        {
            // 낙하하는 타일이 갑자기 사라져 보이지 않도록 작은 절차적 먼지 파티클을 만든다.
            var go = new GameObject("TileDust");
            go.transform.position = transform.position + Vector3.down * 0.05f;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed    = 1.2f;
            main.startSize     = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startColor    = new Color(1f, 1f, 1f, 0.9f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.9f, 0.05f, 0.9f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f)));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(UnityEngine.Color.white, 0f), new GradientColorKey(UnityEngine.Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(shader);

            ps.Play();
            Destroy(go, 1.6f);
        }

        private IEnumerator WarnRoutine()
        {
            if (!TryGetComponent<Renderer>(out var rend))
            {
                yield return new WaitForSeconds(_warnDuration);
                yield break;
            }

            var mat = rend.material;
            Color originalBase = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            Color origColor = mat.color;
            bool hasEmission = mat.HasProperty("_EmissionColor");
            Color originalEm = hasEmission ? mat.GetColor("_EmissionColor") : Color.black;

            int   flashes  = 4;
            float interval = _warnDuration / (flashes * 2f);

            for (int i = 0; i < flashes; i++)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.Lerp(originalBase, Color.white, 0.92f));
                mat.color = Color.Lerp(origColor, Color.white, 0.92f);
                if (hasEmission)
                    mat.SetColor("_EmissionColor", Color.white * 0.65f);
                yield return new WaitForSeconds(interval);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", originalBase);
                mat.color = origColor;
                if (hasEmission)
                    mat.SetColor("_EmissionColor", originalEm);
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>기믹/이펙트에서 타일 색상 변경 시 사용.</summary>
        public void SetColor(Color color)
        {
            if (!TryGetComponent<Renderer>(out var rend)) return;
            rend.material.color = color;
            rend.material.SetColor("_BaseColor", color); // URP 대응
        }
    }
}
