using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 안전 색상 호출과 라운드 이벤트 타이밍을 지휘하는 디렉터.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 일정 주기로 “안전한” 타일 색을 공지한다. 최상층에서 해당 색이 아닌 살아 있는 타일은
    /// 짧은 경고 후 낙하 — 게임의 핵심 “강제 이동” 박자.
    /// </summary>
    public class ColorCallDirector : MonoBehaviour
    {
        public event System.Action<TileColor, float, int> OnAnnounced;   // 색 + 경고 시간 + 레이어
        public event System.Action<TileColor, int, int>   OnDropped;     // 색 + 낙하 수 + 레이어
        public event System.Action                         OnEventEnded;

        [SerializeField] private GameConfig    _config;
        [SerializeField] private TileBoard     _board;

        [Tooltip("후보 색. 일반 타일 색 위주로 유지.")]
        [SerializeField] private TileColor[]   _callableColors =
            { TileColor.Green, TileColor.Blue, TileColor.Yellow };

        private Coroutine _loop;
        private bool      _running;

        public void Inject(GameConfig config, TileBoard board)
        {
            _config = config;
            _board  = board;
        }

        public void BeginLoop()
        {
            if (_running) return;
            _running = true;
            _loop = StartCoroutine(Loop());
        }

        public void EndLoop()
        {
            _running = false;
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        private IEnumerator Loop()
        {
            if (_config == null || _board == null) yield break;

            yield return new WaitForSeconds(_config.colorCallFirstDelay);

            while (_running)
            {
                int targetLayer = PickTargetLayer();
                if (targetLayer < 0)
                {
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                var counts = _board.CountLayerColors(targetLayer);
                int totalAlive = 0;
                foreach (var c in counts.Values) totalAlive += c;
                if (totalAlive <= _config.colorCallMinTiles)
                {
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                TileColor safe = PickSafeColor(counts);
                yield return StartCoroutine(RunCall(safe, targetLayer));
                yield return new WaitForSeconds(_config.colorCallInterval);
            }
        }

        private int PickTargetLayer()
        {
            var alive = _board.GetAliveLayerIndices();
            if (alive.Count == 0) return -1;
            return alive[Random.Range(0, alive.Count)];
        }

        private TileColor PickSafeColor(Dictionary<TileColor, int> counts)
        {
            if (_callableColors == null || _callableColors.Length == 0)
            {
                foreach (var kvp in counts)
                    if (kvp.Value > 0) return kvp.Key;
                return TileColor.Purple;
            }

            TileColor best = _callableColors[0];
            int bestCount = int.MaxValue;
            bool any = false;

            foreach (var c in _callableColors)
            {
                if (!counts.TryGetValue(c, out int count) || count <= 0) continue;
                any = true;
                if (count < bestCount) { bestCount = count; best = c; }
            }

            if (!any)
            {
                foreach (var kvp in counts)
                    if (kvp.Value > 0) return kvp.Key;
            }
            return best;
        }

        private IEnumerator RunCall(TileColor safe, int layerIndex)
        {
            float warn = Mathf.Max(0.5f, _config.colorCallWarnDuration);
            OnAnnounced?.Invoke(safe, warn, layerIndex);
            TileAudio.PlayStatic(TileSfx.ColorCallAnnounce, 1f, 1.1f);

            float t = 0f;
            int lastBeepedAt = -1;
            while (t < warn)
            {
                t += Time.deltaTime;
                int tickSecond = Mathf.FloorToInt(t);
                if (tickSecond > lastBeepedAt && tickSecond < warn)
                {
                    lastBeepedAt = tickSecond;
                    TileAudio.PlayStatic(TileSfx.CountdownTick, 0.6f, 1.3f);
                }
                yield return null;
            }

            var safeSet = new HashSet<TileColor> { safe };
            int dropped = _board.DropLayerExcept(layerIndex, safeSet, skipPreDelay: true);
            OnDropped?.Invoke(safe, dropped, layerIndex);
            TileAudio.PlayStatic(TileSfx.ColorCallDrop, 1f, 0.95f);

            yield return new WaitForSeconds(1.2f);
            OnEventEnded?.Invoke();
        }
    }
}
