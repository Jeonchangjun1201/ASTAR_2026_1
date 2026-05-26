using System.Collections.Generic;
using UnityEngine;
using _TeamFolder.JCJ.Script;

//  플레이어 생성과 시작 위치 배치를 담당하는 스포너.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 최상 생존 층 테두리 타일에 플레이어를 분산 스폰하고 인덱스별 틴트.
    /// 좌표 계산은 TileBoard, 이 클래스는 조율·설정만(SRP).
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private GameObject playerPrefab;

        [Header("설정")]
        [Tooltip("타일 표면보다 위로 올려 스폰할 높이.")]
        [SerializeField] private float spawnHeightOffset = 1.5f;

        [Tooltip("플레이어 1..N에 순환 적용할 팔레트.")]
        [SerializeField] private Color[] playerPalette =
        {
            new(1.00f, 0.35f, 0.35f), // 빨강
            new(0.35f, 0.70f, 1.00f), // 파랑
            new(1.00f, 0.85f, 0.35f), // 노랑
            new(0.35f, 1.00f, 0.55f), // 초록
            new(0.85f, 0.55f, 1.00f), // 보라
            new(1.00f, 0.60f, 0.30f), // 주황
        };

        public List<PlayerController> SpawnPlayers(int count, TileBoard board, GameConfig config)
        {
            // 타일 모드 플레이어 생성과 초기 설정을 한곳에서 처리한다.
            // 멀티로 바꾸면 i == 0 같은 로컬 가정을 ownerId/connectionId 매핑으로 교체하면 된다.
            var players = new List<PlayerController>();

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnManager] playerPrefab is not assigned.");
                return players;
            }

            List<Vector3> positions = board.GetDispersedBorderSpawnPositions(count);
            int spawnCount = Mathf.Min(count, positions.Count);

            float scale = config != null ? config.playerScale : 1f;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3    spawnPos = positions[i] + Vector3.up * spawnHeightOffset;
                GameObject go       = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                go.name = $"Player_{i + 1}";
                if (scale > 0f) go.transform.localScale = Vector3.one * scale;
                RuntimePlayerIdentity.Ensure(go)?.Configure($"tile.player.{i + 1}", go.name, i, i == 0);

                if (!go.TryGetComponent<PlayerController>(out var pc))
                {
                    Debug.LogWarning($"[PlayerSpawnManager] {go.name} is missing PlayerController.");
                    Destroy(go);
                    continue;
                }

                pc.PlayerIndex       = i;
                pc.IsLocalControlled = (i == 0);      // 단일 플레이 테스트에서는 1번만 입력을 받는다.
                pc.ConfigureLives(config != null ? config.playerLives : 2);
                Color tint = playerPalette != null && playerPalette.Length > 0
                    ? playerPalette[i % playerPalette.Length]
                    : Color.white;
                pc.ApplyTint(tint);
                pc.InputLocked = true; // 카운트다운이 끝나면 TileGameManager가 입력 잠금을 해제한다.
                players.Add(pc);
            }

            Debug.Log($"[PlayerSpawnManager] Spawned {players.Count} PlayerList.");
            return players;
        }
    }
}
