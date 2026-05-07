using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 데이터에서 플레이어를 배치할 위치를 찾아 생성하는 스포너 계약.
    /// </summary>
    public interface IMazePlayerSpawner
    {
        List<GameObject> Spawn(int[,] data, float cellSize, int count, GameObject playerPrefab, Transform parent);
    }

    /// <summary>
    /// 시작 지점 근처의 통로 셀에 플레이어 프리팹을 생성하고 로컬 플레이어 설정을 적용한다.
    /// </summary>
    public class MazePlayerSpawner : MonoBehaviour, IMazePlayerSpawner
    {
        [SerializeField] private int _spawnAreaSize = 3;
        [SerializeField] private float _spawnHeight = 1f;
        [Tooltip("스폰된 플레이어 전체에 적용할 공통 스케일. 작을수록 맵이 더 넓게 느껴짐.")]
        [SerializeField] private float _playerScale = 0.4f;

        public List<GameObject> Spawn(int[,] data, float cellSize, int count, GameObject playerPrefab, Transform parent)
        {
            // 시작점 주변의 빈 칸을 먼저 모아 플레이어끼리 같은 위치에 겹치지 않게 배치한다.
            var result = new List<GameObject>(count);
            if (playerPrefab == null || count <= 0) return result;

            var settings = SettingsService.EnsureInstance().Data;
            int w = data.GetLength(0);
            int h = data.GetLength(1);
            var openCells = CollectOpenCells(data, 1, 1, _spawnAreaSize, w, h);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos;
                if (openCells.Count > 0)
                {
                    int idx = Random.Range(0, openCells.Count);
                    var cell = openCells[idx];
                    openCells.RemoveAt(idx);
                    pos = new Vector3(cell.x * cellSize, _spawnHeight, cell.y * cellSize);
                }
                else
                {
                    pos = new Vector3(1.5f * cellSize, _spawnHeight, 1.5f * cellSize);
                }

                var p = Instantiate(playerPrefab, pos, Quaternion.identity, parent);
                p.name = $"Player_{i + 1}";
                if (_playerScale > 0f && Mathf.Abs(_playerScale - 1f) > 0.001f)
                    p.transform.localScale = Vector3.one * _playerScale;

                bool isLocal = (i == 0);
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.IsLocalControlled = isLocal;
                    pc.SetMouseSensitivity(settings.cameraSensitivity);
                }

                result.Add(p);
            }
            return result;
        }

        private static List<Vector2Int> CollectOpenCells(int[,] data, int ox, int oy, int range, int w, int h)
        {
            var list = new List<Vector2Int>();
            for (int x = ox; x < ox + range; x++)
            {
                for (int y = oy; y < oy + range; y++)
                {
                    if (x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1) continue;
                    if (data[x, y] == 0) list.Add(new Vector2Int(x, y));
                }
            }
            return list;
        }
    }
}
