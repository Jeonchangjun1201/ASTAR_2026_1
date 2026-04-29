using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 그리드 기반 미니맵. Fog of War 방식으로 플레이어 주변만 드러남.
    /// MazeManager가 데이터를 Bind로 전달. RawImage가 없으면 자동 생성.
    /// </summary>
    public class MazeMinimap : MonoBehaviour
    {
        [Header("색감(단색 톤)")]
        [SerializeField] private Color _unknownColor = new(0.035f, 0.040f, 0.050f, 0.95f);
        [SerializeField] private Color _wallColor    = new(0.22f, 0.24f, 0.28f, 1f);
        [SerializeField] private Color _pathColor    = new(0.72f, 0.74f, 0.78f, 1f);
        [Tooltip("골 픽셀 — 회색 맵 위에서 한눈에 들어오게 밝은 흰색 유지.")]
        [SerializeField] private Color _goalColor    = new(1.00f, 1.00f, 1.00f, 1f);
        [Tooltip("플레이어 픽셀 — 골과 구분되게 옅은 민트.")]
        [SerializeField] private Color _playerColor  = new(0.55f, 0.95f, 0.70f, 1f);
        [Tooltip("다른 플레이어(멀티) 점 색상 순환.")]
        [SerializeField] private Color[] _peerColors =
        {
            new(1.00f, 0.45f, 0.45f),  // 빨강
            new(0.45f, 0.75f, 1.00f),  // 파랑
            new(1.00f, 0.85f, 0.35f),  // 호박
            new(0.85f, 0.55f, 1.00f),  // 보라
        };

        [Header("배치")]
        [Tooltip("앵커(기본: 우하단).")]
        [SerializeField] private Vector2 _anchor = new(1f, 0f);
        [SerializeField] private Vector2 _anchoredPos = new(-24f, 24f);
        [SerializeField] private Vector2 _size = new(220f, 220f);
        [Range(1f, 20f)] [SerializeField] private float _visionRadiusCells = 6f;

        [Header("참조(선택)")]
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private Canvas _canvas;

        private int[,] _maze;
        private bool[,] _discovered;
        private int _w, _h;
        private float _cellSize;
        private Transform _player;
        private Vector2Int _goalCell;
        private Texture2D _tex;
        private Color[] _buffer;
        private readonly System.Collections.Generic.List<Transform> _peers = new();

        // 전체 텍스처 갱신(SetPixels+Apply)은 비용이 크므로, 시각 변화 없으면 매 프레임 생략.
        private int _lastPlayerCellX = int.MinValue;
        private int _lastPlayerCellY = int.MinValue;
        private float _nextForceRefreshTime;
        private const float _forceRefreshInterval = 0.2f; // 다른 플레이어 이동 반영용

        public void Bind(int[,] maze, float cellSize, Transform player, Vector2Int goal)
        {
            _maze = maze;
            _w = maze.GetLength(0);
            _h = maze.GetLength(1);
            _cellSize = cellSize;
            _player = player;
            _goalCell = goal;
            _discovered = new bool[_w, _h];

            EnsureUI();
            BuildTexture();
            Refresh();
        }

        /// <summary>미니맵에 점을 그릴 추가(원격) 플레이어를 등록한다.</summary>
        public void SetPeerPlayers(System.Collections.Generic.IEnumerable<Transform> peers)
        {
            // 현재 기준 플레이어(_player)를 제외한 나머지 플레이어를 미니맵 점으로 표시한다.
            // 실제 렌더링 시 IsSpectating인 완주자는 다시 한 번 걸러서 표시하지 않는다.
            _peers.Clear();
            if (peers == null) return;
            foreach (var p in peers)
            {
                if (p == null || p == _player) continue;
                _peers.Add(p);
            }
        }

        public void SetPlayer(Transform player)
        {
            // 관전 카메라가 다른 플레이어로 넘어갈 때 미니맵 중심도 함께 바꾸는 진입점이다.
            // 위치 캐시를 초기화해서 다음 Update에서 즉시 새 플레이어 주변을 다시 그리게 한다.
            if (player == null || player == _player) return;
            _player = player;
            _lastPlayerCellX = int.MinValue;
            _lastPlayerCellY = int.MinValue;
            _nextForceRefreshTime = 0f;
            Refresh();
        }

        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            _nextForceRefreshTime = 0f;
        }

        public void SetAnchor(Vector2 anchor, Vector2 anchoredPos)
        {
            _anchor = anchor;
            _anchoredPos = anchoredPos;
            if (_rawImage != null)
            {
                var rt = _rawImage.rectTransform;
                rt.anchorMin = rt.anchorMax = _anchor;
                rt.pivot = _anchor;
                rt.anchoredPosition = _anchoredPos;
            }
        }

        public void SetSize(Vector2 size)
        {
            _size = size;
            if (_rawImage != null)
            {
                _rawImage.rectTransform.sizeDelta = _size;
            }
        }

        public void RevealAll()
        {
            if (_discovered == null) return;
            for (int x = 0; x < _w; x++)
                for (int y = 0; y < _h; y++)
                    _discovered[x, y] = true;
            _nextForceRefreshTime = 0f;
            Refresh();
        }

        private void EnsureUI()
        {
            if (_rawImage != null) return;

            _canvas = FindOrCreateCanvas();
            var go = new GameObject("MinimapRaw");
            go.transform.SetParent(_canvas.transform, false);

            var frame = new GameObject("Frame");
            frame.transform.SetParent(go.transform, false);
            var frameImg = frame.AddComponent<Image>();
            frameImg.color = new Color(0f, 0f, 0f, 0.5f);
            frameImg.raycastTarget = false;
            var frt = frameImg.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(-3, -3);
            frt.offsetMax = new Vector2(3, 3);

            var img = go.AddComponent<RawImage>();
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = _anchor;
            rt.pivot = _anchor;
            rt.anchoredPosition = _anchoredPos;
            rt.sizeDelta = _size;
            _rawImage = img;
        }

        private Canvas FindOrCreateCanvas()
        {
            var c = Object.FindFirstObjectByType<Canvas>();
            if (c != null) return c;
            var go = new GameObject("Canvas (auto)");
            c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        private void BuildTexture()
        {
            if (_tex != null) Destroy(_tex);
            _tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _buffer = new Color[_w * _h];
            _rawImage.texture = _tex;
        }

        private void Update()
        {
            if (_maze == null || _player == null || _tex == null) return;

            Vector3 p = _player.position;
            int px = Mathf.RoundToInt(p.x / _cellSize);
            int py = Mathf.RoundToInt(p.z / _cellSize);

            // 칸이 바뀌었거나, 동료/안개 갱신을 위해 충분한 시간이 지났을 때만 전체 다시 그림(CPU·GC 절약).
            bool cellChanged = px != _lastPlayerCellX || py != _lastPlayerCellY;
            bool timeElapsed = Time.unscaledTime >= _nextForceRefreshTime;
            if (!cellChanged && !timeElapsed) return;

            _lastPlayerCellX = px;
            _lastPlayerCellY = py;
            _nextForceRefreshTime = Time.unscaledTime + _forceRefreshInterval;
            Refresh(px, py);
        }

        private void Refresh()
        {
            if (_player == null) return;
            Vector3 p = _player.position;
            int px = Mathf.RoundToInt(p.x / _cellSize);
            int py = Mathf.RoundToInt(p.z / _cellSize);
            Refresh(px, py);
        }

        private void Refresh(int px, int py)
        {
            RevealAround(px, py);

            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    int idx = y * _w + x;
                    _buffer[idx] = _discovered[x, y]
                        ? (_maze[x, y] == 1 ? _wallColor : _pathColor)
                        : _unknownColor;
                }
            }

            if (IsInside(_goalCell.x, _goalCell.y) && _discovered[_goalCell.x, _goalCell.y])
                _buffer[_goalCell.y * _w + _goalCell.x] = _goalColor;

            // 동료는 로컬이 이미 밝힌 칸에만 표시 — 안개 규칙 유지 + 같은 구역 힌트.
            for (int i = 0; i < _peers.Count; i++)
            {
                var peer = _peers[i];
                if (peer == null) continue;
                var pc = peer.GetComponent<PlayerController>();
                // 이미 탈출해서 관전 상태가 된 플레이어는 미니맵에 표시하지 않는다.
                // 설정 변경으로 미니맵 UI가 갱신될 때 골 옆에 완주자 점이 다시 나타나는 문제를 막는다.
                if (pc != null && pc.IsSpectating) continue;
                int qx = Mathf.RoundToInt(peer.position.x / _cellSize);
                int qy = Mathf.RoundToInt(peer.position.z / _cellSize);
                if (!IsInside(qx, qy)) continue;
                if (!_discovered[qx, qy]) continue;
                Color c = _peerColors != null && _peerColors.Length > 0
                    ? _peerColors[i % _peerColors.Length]
                    : Color.red;
                _buffer[qy * _w + qx] = c;
            }

            if (IsInside(px, py))
                _buffer[py * _w + px] = _playerColor;

            _tex.SetPixels(_buffer);
            _tex.Apply(false);
        }

        private void RevealAround(int cx, int cy)
        {
            int r = Mathf.CeilToInt(_visionRadiusCells);
            int rSq = Mathf.CeilToInt(_visionRadiusCells * _visionRadiusCells);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (!IsInside(x, y)) continue;
                    if (dx * dx + dy * dy > rSq) continue;
                    _discovered[x, y] = true;
                }
            }
        }

        private bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }
    }
}
