using System.Collections;
using _TeamFolder.PYH._02.Scripts.Util;
// 작성하신 싱글톤 유틸
using UnityEngine;
using static JHJItemPacket;

namespace JHJ.Scripts.EatingthegroundGame
{
    // ───────────────── [서버 전송용 스폰 패킷] ─────────────────
    [System.Serializable]
    public struct ItemSpawnPacket
    {
        public ItemType ItemTypeToSpawn; // 무슨 아이템인지
        public Vector3 SpawnPosition;    // 어디서 떨어질지
    }
    // ─────────────────────────────────────────────────────────

    public class JHJPaintGameManager : MonoSingleton<JHJPaintGameManager>
    {
        [Header("아이템 프리팹 (인스펙터 할당)")]
        [SerializeField] private GameObject _moveSpeedItemPrefab;
        [SerializeField] private GameObject _brushSizeItemPrefab;
        [SerializeField] private GameObject _knockbackItemPrefab;

        [Header("스폰 설정")]
        [SerializeField] private float _spawnInterval = 5f; // 몇 초마다 떨어질지
        [SerializeField] private float _spawnHeight = 15f;  // 하늘 높이 (Y축)

        // 맵의 크기에 맞춰 인스펙터에서 조절
        [SerializeField] private Vector2 _spawnAreaMin = new Vector2(-10, -10); // 최소 X, Z
        [SerializeField] private Vector2 _spawnAreaMax = new Vector2(10, 10);   // 최대 X, Z

        private bool _isGameRunning = false;

        private void Start()
        {
            // TODO: 실제 게임에서는 준비가 다 끝난 시점에 호출하세요.
            // 테스트를 위해 바로 시작하도록 둡니다.
            StartGame();
        }
        // !!!!이 스크립트 안 씀 쓰지 마셈 
        public void StartGame()
        {
            _isGameRunning = true;

            // 🚨 멀티플레이 주의: 이 코루틴은 '방장(Master Client)'만 실행해야 합니다!
            // 예: if (PhotonNetwork.IsMasterClient) { StartCoroutine(ItemSpawnRoutine()); }
            StartCoroutine(ItemSpawnRoutine());
        }

        // 1️⃣ [방장 전용] 랜덤한 시간, 위치에 아이템 스폰 결정
        private IEnumerator ItemSpawnRoutine()
        {
            while (_isGameRunning)
            {
                yield return new WaitForSeconds(_spawnInterval);

                // 랜덤 아이템 종류 선택 (enum 값이 0, 1, 2 중 하나)
                ItemType randomType = (ItemType)Random.Range(0, 3);

                // 하늘의 랜덤 위치 계산
                float randomX = Random.Range(_spawnAreaMin.x, _spawnAreaMax.x);
                float randomZ = Random.Range(_spawnAreaMin.y, _spawnAreaMax.y);
                Vector3 spawnPos = new Vector3(randomX, _spawnHeight, randomZ);

                // 결정된 정보를 서버로 전송
                SendItemSpawnDataToServer(randomType, spawnPos);
            }
        }

        // 2️⃣ [서버 통신] 아이템 스폰 데이터를 서버로 쏘기
        private void SendItemSpawnDataToServer(ItemType type, Vector3 pos)
        {
            ItemSpawnPacket packet = new ItemSpawnPacket
            {
                ItemTypeToSpawn = type,
                SpawnPosition = pos
            };

            // TODO: 서버/네트워크 매니저를 통해 전송 (예: Photon RPC)
            Debug.Log($"[서버 요청] 하늘에서 {type} 아이템이 {pos} 위치로 떨어집니다!");

            // 임시 테스트용 강제 실행 (실제 서버 연동 시 지우고, 서버가 호출하게)
            ExecuteItemSpawnFromServer(packet);
        }

        // 3️⃣ [서버 응답] 서버 명령을 받아 모든 플레이어 화면에 아이템 생성 -> 필요없는 거
        public void ExecuteItemSpawnFromServer(ItemSpawnPacket packet)
        {
            GameObject prefabToSpawn = null;

            switch (packet.ItemTypeToSpawn)
            {
                case ItemType.MoveSpeed: prefabToSpawn = _moveSpeedItemPrefab; break;
                case ItemType.BrushSize: prefabToSpawn = _brushSizeItemPrefab; break;
                case ItemType.Knockback: prefabToSpawn = _knockbackItemPrefab; break;
            }

            if (prefabToSpawn != null)
            {
                // 해당 좌표(하늘)에 프리팹 생성
                Instantiate(prefabToSpawn, packet.SpawnPosition, Quaternion.identity);
            }
        }
    }
}