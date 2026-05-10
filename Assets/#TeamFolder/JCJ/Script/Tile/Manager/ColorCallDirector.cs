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
        public event System.Action<TileColor, float> OnAnnounced;   // 색 + 경고 시간
        public event System.Action<TileColor, int>   OnDropped;     // 색 + 낙하한 타일 수
        public event System.Action                    OnEventEnded;

        [SerializeField] private GameConfig    _config;
        [SerializeField] private TileBoard     _board;

        [Tooltip("후보 색. 일반 타일 색 위주로 유지.")]
        [SerializeField] private TileColor[]   _callableColors =
            { TileColor.Green, TileColor.Blue, TileColor.Yellow };

        private Coroutine _loop;
        private bool      _running;

        // 컬러콜이 참조할 보드와 설정을 외부에서 주입받는다.
        // 서버가 안전색을 결정하는 구조에서도 보드 읽기와 설정 참조는 이 연결 지점에서 정리하면 된다.
        public void Inject(GameConfig config, TileBoard board)
        {
            _config = config;
            _board  = board;
        }

        public void BeginLoop()
        {
            // Playing 상태가 된 뒤 컬러콜 루프를 시작한다.
            // 멀티에서는 여러 클라이언트가 각자 BeginLoop를 돌리면 안전색이 갈라질 수 있으므로 한 권위자만 실행해야 한다.
            if (_running) return;
            _running = true;
            _loop = StartCoroutine(Loop());
        }

        // 진행 중인 컬러콜 루프를 종료한다.
        // 매치 종료나 일시정지처럼 더 이상 안전색 이벤트를 만들면 안 되는 순간에 끊는 지점이다.
        public void EndLoop()
        {
            _running = false;
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        private IEnumerator Loop()
        {
            // 일정 간격으로 "살아남을 색"을 고르고, 경고 시간이 끝나면 나머지 타일을 떨어뜨린다.
            // 현재는 코루틴 기반 로컬 시간이다. 서버 연동 시에는 서버가 callTime/dropTime을 확정하는 형태가 좋다.
            if (_config == null || _board == null) yield break;

            yield return new WaitForSeconds(_config.colorCallFirstDelay);

            while (_running)
            {
                // 최상층이 거의 비었으면 스킵.
                var counts = _board.CountTopLayerColors();
                int totalAlive = 0;
                foreach (var c in counts.Values) totalAlive += c;
                if (totalAlive <= _config.colorCallMinTiles)
                {
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                TileColor safe = PickSafeColor(counts);
                yield return StartCoroutine(RunCall(safe));
                yield return new WaitForSeconds(_config.colorCallInterval);
            }
        }

        private TileColor PickSafeColor(Dictionary<TileColor, int> counts)
        {
            // 현재 정책은 남아 있는 호출 후보 중 타일 수가 가장 적은 색을 안전색으로 골라 압박을 만든다.
            // 경쟁감은 강하지만 운빨 느낌도 생길 수 있으므로, 서버/밸런스 담당자는 이 함수만 보면 컬러콜 난이도를 조정할 수 있다.
            // 남은 타일이 가장 적은 호출 색을 우선(압박·밀집). 폴백: 살아 있는 아무 색.
            // _callableColors 비어 있으면 [0] 접근 방지.
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
                // 호출 후보 색이 하나도 안 남음 — 살아 있는 아무 색.
                foreach (var kvp in counts)
                    if (kvp.Value > 0) return kvp.Key;
            }
            return best;
        }

        private IEnumerator RunCall(TileColor safe)
        {
            // 한 번의 컬러콜 이벤트 전체 흐름이다.
            // 1) UI/사운드 알림, 2) 안전색 강조, 3) 경고 시간 대기, 4) 안전색 외 타일 낙하, 5) 점수 지급 이벤트.
            float warn = Mathf.Max(0.5f, _config.colorCallWarnDuration);
            OnAnnounced?.Invoke(safe, warn);
            TileAudio.PlayStatic(TileSfx.ColorCallAnnounce, 1f, 1.1f);

            // 안전 색을 짧게 플래시로 강조.
            _board.HighlightTopLayerColor(safe, Color.white);

            // 틱 비프 카운트다운.
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
            int dropped = _board.DropTopLayerExcept(safeSet, skipPreDelay: true);
            OnDropped?.Invoke(safe, dropped);
            TileAudio.PlayStatic(TileSfx.ColorCallDrop, 1f, 0.95f);

            // 낙하 연출이 조금 진행된 뒤 이벤트 종료 신호.
            yield return new WaitForSeconds(1.2f);
            OnEventEnded?.Invoke();
        }
    }
}
