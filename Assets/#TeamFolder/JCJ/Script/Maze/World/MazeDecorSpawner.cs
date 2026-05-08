using System.Collections.Generic;
using UnityEngine;

// 미로 장식 오브젝트를 배치하는 스포너.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 막다른 길·벽 인접 칸 등에 상자·화톳불·바위 등 데코 프리팹을 배치한다.
    /// 프리팹은 인스펙터에서 주입(보통 LowPolyDungeonsLite/Prefabs/*).
    /// </summary>
    public class MazeDecorSpawner : MonoBehaviour, IDecorSpawner
    {
        [System.Serializable]
        public struct DecorEntry
        {
            public GameObject prefab;
            [Range(0f, 1f)] public float weight;
            public bool deadEndOnly;    // true면 막다른 칸(주변 벽 3면)만
            public bool wallAdjacent;   // true면 벽에 닿은 칸만
            public float yOffset;
            public Vector3 randomScaleRange;  // 균일 스케일을 1±range 범위에서 선택
        }

        [Header("데코 팔레트")]
        [SerializeField] private List<DecorEntry> _entries = new();

        [Header("Density")]
        [Range(0f, 0.5f)] [SerializeField] private float _density = 0.12f;
        [SerializeField] private bool _alignRandomYaw = true;

        public void Spawn(int[,] data, float cellSize, HashSet<Vector2Int> excluded, Transform parent)
        {
            if (_entries.Count == 0) TryAutoPopulateDefaults();
            if (_entries.Count == 0 || _density <= 0f) return;

            int w = data.GetLength(0);
            int h = data.GetLength(1);
            var rng = new System.Random();
            float totalWeight = 0f;
            foreach (var e in _entries) totalWeight += Mathf.Max(0f, e.weight);
            if (totalWeight <= 0f) return;

            for (int x = 1; x < w - 1; x++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    if (data[x, y] != 0) continue;
                    var cell = new Vector2Int(x, y);
                    if (excluded.Contains(cell)) continue;
                    if (rng.NextDouble() > _density) continue;

                    int wallsAround = CountWalls(data, x, y);
                    bool isDeadEnd = wallsAround >= 3;
                    bool touchesWall = wallsAround >= 1;

                    var entry = PickEntry(rng, totalWeight, isDeadEnd, touchesWall);
                    if (entry.prefab == null) continue;

                    Vector3 pos = new Vector3(x * cellSize, entry.yOffset, y * cellSize);
                    Quaternion rot = _alignRandomYaw
                        ? Quaternion.Euler(0f, rng.Next(0, 4) * 90f, 0f)
                        : Quaternion.identity;

                    var instance = Instantiate(entry.prefab, pos, rot, parent);
                    ApplyRandomScale(instance.transform, entry.randomScaleRange, rng);
                    excluded.Add(cell);
                }
            }
        }

        private static int CountWalls(int[,] data, int x, int y)
        {
            int c = 0;
            if (data[x + 1, y] == 1) c++;
            if (data[x - 1, y] == 1) c++;
            if (data[x, y + 1] == 1) c++;
            if (data[x, y - 1] == 1) c++;
            return c;
        }

        private DecorEntry PickEntry(System.Random rng, float total, bool isDeadEnd, bool touchesWall)
        {
            // 필터를 통과한 항목만 모아 가중 룰렛. 없으면 null — 열린 홀에 막다른길 전용 횃불을 두는 실수 방지.
            float eligibleTotal = 0f;
            foreach (var e in _entries)
            {
                if (e.deadEndOnly && !isDeadEnd) continue;
                if (e.wallAdjacent && !touchesWall) continue;
                eligibleTotal += Mathf.Max(0f, e.weight);
            }
            if (eligibleTotal <= 0f) return default;

            float r = (float)rng.NextDouble() * eligibleTotal;
            float acc = 0f;
            foreach (var e in _entries)
            {
                if (e.deadEndOnly && !isDeadEnd) continue;
                if (e.wallAdjacent && !touchesWall) continue;
                acc += Mathf.Max(0f, e.weight);
                if (r <= acc) return e;
            }
            return default;
        }

        private static void ApplyRandomScale(Transform t, Vector3 range, System.Random rng)
        {
            if (range == Vector3.zero) return;
            float s = 1f + ((float)rng.NextDouble() * 2f - 1f) * Mathf.Max(range.x, range.y, range.z);
            t.localScale *= s;
        }

        [ContextMenu("Load LowPolyDungeonsLite Defaults")]
        private void LoadLowPolyDefaultsMenu() => TryAutoPopulateDefaults();

        /// <summary>
        /// 에디터 전용 폴백: LowPolyDungeonsLite에서 무난한 데코 프리팹을 채워 넣어 플레이 모드 반복 시 수동 연결 없이 동작하게 한다.
        /// </summary>
        private void TryAutoPopulateDefaults()
        {
#if UNITY_EDITOR
            string root = "Assets/LowPolyDungeonsLite/Prefabs/";
            var defs = new (string path, float weight, bool deadEnd, bool wall)[]
            {
                (root + "Box_02.prefab",       1.0f, false, true),
                (root + "Rock_01.prefab",      0.8f, false, true),
                (root + "Pot_01.prefab",       0.7f, true,  false),
                (root + "Candle_01.prefab",    0.6f, false, true),
                (root + "Book_09.prefab",      0.4f, true,  false),
                (root + "Wood_02.prefab",      0.6f, false, true),
                (root + "Jug_02.prefab",       0.4f, true,  false),
                (root + "Bottle_05.prefab",    0.3f, true,  false),
                (root + "Chair_05.prefab",     0.3f, true,  false),
                (root + "Column_01.prefab",    0.2f, false, true),
                (root + "WallDecor_05.prefab", 0.5f, false, true),
                (root + "Light_08.prefab",     0.6f, false, true),
            };
            foreach (var d in defs)
            {
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(d.path);
                if (p == null) continue;
                _entries.Add(new DecorEntry
                {
                    prefab = p,
                    weight = d.weight,
                    deadEndOnly  = d.deadEnd,
                    wallAdjacent = d.wall,
                    yOffset = 0f,
                    randomScaleRange = new Vector3(0.1f, 0.1f, 0.1f),
                });
            }
            if (_entries.Count > 0)
                Debug.Log($"[MazeDecorSpawner] Auto-loaded {_entries.Count} LowPolyDungeonsLite decor prefabs.");
#endif
        }
    }
}
