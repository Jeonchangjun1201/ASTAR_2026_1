using System.Collections.Generic;
using UnityEngine;

// 플레이어를 슬롯 기준으로 생성하고 배치하는 스포너.

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
            // 서버 연결 후에는 이 함수가 "실제 생성"이 아니라 서버 스폰 결과를 씬에 반영하는 단계가 될 수 있다.
            // 중요한 것은 i번째 스폰 슬롯과 네트워크 플레이어 식별자를 일관되게 매핑하는 것이다.
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
                RuntimePlayerIdentity.Ensure(p)?.Configure($"maze.player.{i + 1}", p.name, i, isLocal);
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // 현재는 첫 번째 플레이어를 로컬로 가정한다.
                    // 서버를 붙이면 여기서 "내 connectionId/ownerId와 일치하는가"로 교체하면 된다.
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
