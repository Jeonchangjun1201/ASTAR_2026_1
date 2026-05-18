using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 타일 보드 생성, 저장, 조회를 담당하는 보드 관리자.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 다층 타일 보드.
    /// - layers[] 배열로 층 수, 위치, 크기, 기믹 분포를 인스펙터에서 설정.
    /// - 아래층일수록 fallDelayMultiplier 작고 기믹 비율 높음.
    /// - 타일 생성 자체는 ITileFactory에 위임 (DIP).
    ///
    /// 기본값(인스펙터 미설정 시):
    ///   Layer 0 (Top)    : y=8, 12×12, mult=1.2, gimmick≈15%
    ///   Layer 1 (Middle) : y=4, 10×10, mult=1.0, gimmick≈35%
    ///   Layer 2 (Bottom) : y=0,  8× 8, mult=0.6, gimmick≈60%
    /// </summary>
    public class TileBoard : MonoBehaviour
    {
        [Header("레이어 설정")]
        [SerializeField] private LayerConfig[] layers = new LayerConfig[]
        {
            new LayerConfig
            {
                layerName = "Top", yPosition = 8f, gridWidth = 12, gridDepth = 12,
                fallDelayMultiplier = 1.2f, maxGimmickCount = 18,
                bombRatio = 0.04f, webRatio = 0.04f, iceRatio = 0.04f,
                balloonRatio = 0.04f, trampolineRatio = 0.02f, confusionRatio = 0.01f,
            },
            new LayerConfig
            {
                layerName = "Middle", yPosition = 4f, gridWidth = 10, gridDepth = 10,
                fallDelayMultiplier = 1.0f, maxGimmickCount = 35,
                bombRatio = 0.07f, webRatio = 0.07f, iceRatio = 0.07f,
                balloonRatio = 0.07f, trampolineRatio = 0.05f, confusionRatio = 0.03f,
            },
            new LayerConfig
            {
                layerName = "Bottom", yPosition = 0f, gridWidth = 8, gridDepth = 8,
                fallDelayMultiplier = 0.6f, maxGimmickCount = int.MaxValue,
                bombRatio = 0.10f, webRatio = 0.10f, iceRatio = 0.12f,
                balloonRatio = 0.10f, trampolineRatio = 0.08f, confusionRatio = 0.06f,
            },
        };

        // [layerIndex][x, z]
        private List<BaseTile[,]> _layerTiles;
        private GameConfig        _config;

        public int LayerCount => layers.Length;

        // ── 초기화 ─────────────────────────────────────
        /// <summary>구 진입점(육각 크기 폴백).</summary>
        public void Initialize(ITileFactory factory) => Initialize(factory, null);

        public void Initialize(ITileFactory factory, GameConfig config)
        {
            // 라운드 시작 시 보드 전체를 새로 만든다.
            // 서버 연동 시에는 레이어/좌표/색상/기믹 정보를 tileId와 함께 공유해야 클라별 타일 상태가 맞는다.
            _config = config;
            _layerTiles = new List<BaseTile[,]>();
            for (int i = 0; i < layers.Length; i++)
                GenerateLayer(i, factory);
        }

        private float HexRadius  => _config != null ? _config.hexRadius : 0.6f;
        private float HexGap     => _config != null ? _config.hexGap    : 0.02f;
        private float HexHalfH   => (_config != null ? _config.hexHeight : 0.25f) * 0.5f;

        /// <summary>flat-top 육각: 열 중심 간 가로 간격(월드 단위).</summary>
        private float ColumnSpacing => HexMeshBuilder.ColumnSpacing(HexRadius) + HexGap * HexRadius;
        /// <summary>flat-top 육각: 행 중심 간 세로 간격(월드 단위).</summary>
        private float RowSpacing    => HexMeshBuilder.RowSpacing(HexRadius)    + HexGap * HexRadius;

        private Vector3 HexGridToWorld(int x, int z, Vector3 origin)
        {
            // flat-top 허니컴 — 짝수 열은 맞추고 홀수 열은 +Z로 반 행 밀림.
            float xPos = x * ColumnSpacing;
            float zPos = z * RowSpacing + ((x & 1) == 1 ? RowSpacing * 0.5f : 0f);
            return origin + new Vector3(xPos, 0f, zPos);
        }

        private void GenerateLayer(int layerIndex, ITileFactory factory)
        {
            // 한 층의 육각 타일들을 생성한다.
            // 현재는 로컬 UnityEngine.Random 기반 색상 셔플이므로, 멀티에서는 서버가 색상 배열을 결정하거나 seed를 공유해야 한다.
            LayerConfig cfg     = layers[layerIndex];
            int         w       = cfg.gridWidth;
            int         d       = cfg.gridDepth;
            int         total   = w * d;

            float yPos = cfg.yPosition;
            if (_config != null && _config.useLayerVerticalSpacing)
            {
                int topIndex = Mathf.Max(0, layers.Length - 1);
                yPos = (topIndex - layerIndex) * _config.layerVerticalSpacing;
            }

            // TileBoard 트랜스폼 기준으로 허니컴 중앙 정렬.
            float boardWidth  = (w - 1) * ColumnSpacing;
            float boardDepth  = (d - 1) * RowSpacing + (w > 1 ? RowSpacing * 0.5f : 0f);
            Vector3 origin = transform.position
                + new Vector3(-boardWidth * 0.5f, yPos, -boardDepth * 0.5f);

            TileColor[] pool = BuildColorPool(total, cfg);
            Shuffle(pool);

            BaseTile[,] grid = new BaseTile[w, d];

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < d; z++)
                {
                    TileColor color = pool[x * d + z];
                    Vector3   pos   = HexGridToWorld(x, z, origin);
                    BaseTile  tile  = factory.CreateTile(color, pos, transform, cfg.fallDelayMultiplier);
                    grid[x, z] = tile;
                }
            }

            _layerTiles.Add(grid);
        }

        /// <summary>층별 기믹 개수 제한을 지키면서 색상 풀을 빌드.</summary>
        private static TileColor[] BuildColorPool(int total, LayerConfig cfg)
        {
            var pool       = new List<TileColor>(total);
            int maxGimmick = cfg.maxGimmickCount < 0 ? total : cfg.maxGimmickCount;
            int placed     = 0;

            void AddGimmick(TileColor color, float ratio)
            {
                int count = Mathf.Min(
                    Mathf.RoundToInt(total * ratio),
                    maxGimmick - placed);
                for (int i = 0; i < count; i++) pool.Add(color);
                placed += count;
            }

            AddGimmick(TileColor.Red,     cfg.bombRatio);
            AddGimmick(TileColor.Purple,  cfg.webRatio);
            AddGimmick(TileColor.Cyan,    cfg.iceRatio);
            AddGimmick(TileColor.Orange,  cfg.balloonRatio);
            AddGimmick(TileColor.Lime,    cfg.trampolineRatio);
            AddGimmick(TileColor.Magenta, cfg.confusionRatio);

            // 나머지는 일반 타일
            TileColor[] normals = { TileColor.Green, TileColor.Blue, TileColor.Yellow };
            while (pool.Count < total)
                pool.Add(normals[Random.Range(0, normals.Length)]);

            return pool.ToArray();
        }

        private static void Shuffle<T>(T[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        // ── ColorCall 쿼리 / 액션 ────────────────────
        /// <summary>살아 있는 최상층 = 아직 안 떨어진 타일이 남은 가장 낮은 레이어 인덱스.</summary>
        // 현재 플레이 규칙이 적용되는 최상 생존 레이어를 찾는다.
        // 서버와 클라이언트가 같은 레이어를 보고 있는지 검증할 때도 기준이 되는 함수다.
        public int GetTopAliveLayerIndex()
        {
            if (_layerTiles == null) return -1;
            for (int i = 0; i < _layerTiles.Count; i++)
            {
                foreach (var t in _layerTiles[i])
                    if (t != null && !t.HasFallen) return i;
            }
            return -1;
        }

        /// <summary>최상 생존 층에서 색별 살아 있는 타일 개수.</summary>
        // 최상 레이어의 색 분포를 집계한다.
        // ColorCallDirector가 안전색을 정할 때 읽는 입력 데이터라 서버에서도 같은 계산 기준을 써야 한다.
        public Dictionary<TileColor, int> CountTopLayerColors()
        {
            var counts = new Dictionary<TileColor, int>();
            int top = GetTopAliveLayerIndex();
            if (top < 0) return counts;
            foreach (var t in _layerTiles[top])
            {
                if (t == null || t.HasFallen) continue;
                if (!counts.ContainsKey(t.TileTag)) counts[t.TileTag] = 0;
                counts[t.TileTag]++;
            }
            return counts;
        }

        /// <summary>
        /// 최상단 생존 층에서 safeColors에 없는 모든 타일을 떨어뜨린다.
        /// ColorCallDirector가 컬러콜 판정 때 사용한다.
        /// </summary>
        public int DropTopLayerExcept(System.Collections.Generic.HashSet<TileColor> safeColors,
                                       bool skipPreDelay = true)
        {
            // ColorCallDirector가 호출하는 핵심 함수다.
            // 최상 생존 층에서 안전색이 아닌 타일을 모두 낙하 예약한다.
            // 서버 연동 시 이 함수는 서버/호스트에서만 실행하고 결과 tileId 목록을 클라에 보내는 것이 안전하다.
            int dropped = 0;
            int top = GetTopAliveLayerIndex();
            if (top < 0) return 0;
            foreach (var t in _layerTiles[top])
            {
                if (t == null || t.HasFallen || t.IsCondemned) continue;
                if (safeColors != null && safeColors.Contains(t.TileTag)) continue;
                t.StartFalling(skipPreDelay);
                dropped++;
            }
            return dropped;
        }

        /// <summary>최상층에서 <paramref name="safeColor"/>와 일치하는 살아 있는 타일을 잠깐 강조.</summary>
        // 안전색 타일만 잠깐 강조해 경고 연출을 준다.
        // 판정은 서버가 하더라도 이런 시각 피드백은 클라이언트가 이 메서드로 재생하면 된다.
        public void HighlightTopLayerColor(TileColor safeColor, Color flashTint)
        {
            int top = GetTopAliveLayerIndex();
            if (top < 0) return;
            foreach (var t in _layerTiles[top])
            {
                if (t == null || t.HasFallen) continue;
                if (t.TileTag != safeColor) continue;
                t.SetColor(flashTint);
            }
        }

        /// <summary>살아 있는 모든 타일의 월드 중심(리스폰 후보).</summary>
        public List<Vector3> GetAliveTileCenters()
        {
            // 리스폰 위치 후보를 찾을 때 사용한다.
            // 이미 떨어졌거나 낙하 예정인 타일(IsCondemned)은 후보에서 제외한다.
            var result = new List<Vector3>();
            if (_layerTiles == null) return result;
            int top = GetTopAliveLayerIndex();
            if (top < 0) return result;
            float halfThick = HexHalfH;
            foreach (var t in _layerTiles[top])
            {
                if (t == null || t.HasFallen || t.IsCondemned) continue;
                Vector3 p = t.transform.position;
                result.Add(new Vector3(p.x, p.y + halfThick, p.z));
            }
            return result;
        }

        public Vector3 GetBoardCenter()
        {
            if (_layerTiles == null || _layerTiles.Count == 0) return transform.position;
            int top = GetTopAliveLayerIndex();
            if (top < 0) return transform.position;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var t in _layerTiles[top])
            {
                if (t == null || t.HasFallen) continue;
                sum += t.transform.position;
                count++;
            }
            return count > 0 ? sum / count : transform.position;
        }

        // ── 공간 쿼리 ──────────────────────────────────
        public BaseTile GetTile(int layerIndex, int x, int z)
        {
            if (_layerTiles == null || layerIndex < 0 || layerIndex >= _layerTiles.Count)
                return null;
            BaseTile[,] grid = _layerTiles[layerIndex];
            if (x < 0 || x >= grid.GetLength(0) || z < 0 || z >= grid.GetLength(1))
                return null;
            return grid[x, z];
        }

        /// <summary>모든 층에서 center 반경 내 살아있는 타일 반환 (BombGimmick 사용).</summary>
        public List<BaseTile> GetTilesInRadius(Vector3 center, float radius)
        {
            var result = new List<BaseTile>();
            if (_layerTiles == null) return result;

            foreach (var grid in _layerTiles)
            {
                foreach (var tile in grid)
                {
                    if (tile == null || tile.HasFallen) continue;
                    if (Vector3.Distance(tile.transform.position, center) <= radius)
                        result.Add(tile);
                }
            }
            return result;
        }

        // ── 스폰 위치 ──────────────────────────────────
        /// <summary>최상층 테두리에서 최대 분산 배치된 스폰 위치 반환.</summary>
        public List<Vector3> GetDispersedBorderSpawnPositions(int count)
        {
            // 시작 스폰 위치는 최상층 테두리에서 최대한 떨어진 지점을 고른다.
            // 서버가 스폰을 관리한다면 이 결과를 서버에서 확정해서 각 클라에 전달해야 한다.
            return SelectDispersed(GetTopLayerBorderPositions(), count);
        }

        private List<Vector3> GetTopLayerBorderPositions()
        {
            var positions = new List<Vector3>();
            if (_layerTiles == null || _layerTiles.Count == 0) return positions;

            BaseTile[,] topGrid = _layerTiles[0];
            int w = topGrid.GetLength(0);
            int d = topGrid.GetLength(1);
            float tileHalfHeight = HexHalfH;

            for (int x = 0; x < w; x++)
            for (int z = 0; z < d; z++)
            {
                if (x != 0 && x != w - 1 && z != 0 && z != d - 1) continue;
                BaseTile tile = topGrid[x, z];
                if (tile == null) continue;
                Vector3 p = tile.transform.position;
                positions.Add(new Vector3(p.x, p.y + tileHalfHeight, p.z));
            }

            return positions;
        }

        private static List<Vector3> SelectDispersed(List<Vector3> candidates, int count)
        {
            var result = new List<Vector3>();
            if (candidates.Count == 0 || count <= 0) return result;

            result.Add(candidates[Random.Range(0, candidates.Count)]);

            while (result.Count < count && result.Count < candidates.Count)
            {
                float   bestMinDist = float.MinValue;
                Vector3 bestPos     = Vector3.zero;

                foreach (var c in candidates)
                {
                    if (result.Contains(c)) continue;
                    float minDist = float.MaxValue;
                    foreach (var placed in result)
                    {
                        float d = Vector3.Distance(c, placed);
                        if (d < minDist) minDist = d;
                    }
                    if (minDist > bestMinDist) { bestMinDist = minDist; bestPos = c; }
                }

                result.Add(bestPos);
            }

            return result;
        }
    }
}
