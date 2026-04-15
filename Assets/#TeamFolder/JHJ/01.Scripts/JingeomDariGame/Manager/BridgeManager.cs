using UnityEngine;
using System.Collections.Generic;

namespace JHJ.Scripts.JingeomDariGame.Manager
{
    public class BridgeManager : MonoBehaviour
    {

        [Header("Prefabs")]
        [SerializeField] private GameObject[] bridgePrefab;
        [SerializeField] private GameObject normalBridgePrefab;  // 그냥 다리
                                                                 // [SerializeField] private GameObject fallingBridgePrefab; // 떨어지는 다리
                                                                 //  [SerializeField] private GameObject slowBridgePrefab;    // 느려지는 다리
        [SerializeField] private GameObject safePlatformPrefab;  // 중간 지점
        [SerializeField] private GameObject finalGoalPrefab;     // 도착 지점


        [Header("Spawn Settings")]
        [SerializeField] private Transform startPoint;    // 생성 할 기준점
        [SerializeField] private int totalStages = 3;     //다리 + 지점 세트 만들 개수
        [SerializeField] private int bridgesPerRow = 4;   // 사지선다

        [Header("Distance Settings")]
        [SerializeField] private float xOffset = 3f; // 좌우 간격
        [SerializeField] private float zOffset = 5f; // 앞뒤 간격

        //프리팹들 모아둔 리스트
        private List<GameObject> spawnedObjects = new List<GameObject>();

        private void Start()
        {
            GenerateBridge();
        }

        public void GenerateBridge()
        {
            //중앙 위치 구하기
            //중앙에 xOffset 곱하기

            float centerX = (bridgesPerRow - 1) * xOffset / 2f;


            //스테이지 만들기 
            //다리 4개 + 중간지점 한개 해가지고 1세트
            for (int stage = 0; stage < totalStages; stage++)
            {
                // z값 계산
                float bridgeZPos = (stage * 2 + 1) * zOffset;

                // 진짜 다리 뽑기
                int safeBridgeIndex = Random.Range(0, bridgesPerRow);
                // 다리 깔기
                for (int i = 0; i < bridgesPerRow; i++)
                {
                    // xOffset만큼 이동한 위치를 spawnPos에 저장
                    Vector3 spawnPos = startPoint.position + new Vector3(i * xOffset, 0, bridgeZPos);

                    GameObject bridgeToSpawn;

                    if (i == safeBridgeIndex)
                        bridgeToSpawn = normalBridgePrefab;
                    else
                    {
                        int randomIndex = Random.Range(0, bridgePrefab.Length);
                        bridgeToSpawn = bridgePrefab[randomIndex];
                    }
                }

                // 중간지점 + 도착지점 까는 부분
                // 다리 묶음 바로 다음 칸(짝수 칸)에 놓기 위한 위치 계산
                float platformZPos = (stage * 2 + 2) * zOffset;

                // 휴식처 위치 설정
                Vector3 platformPos = startPoint.position + new Vector3(centerX, 0, platformZPos);

                // 마지막 스테이지면 골인지점 생성
                GameObject platformToSpawn = (stage == totalStages - 1) ? finalGoalPrefab : safePlatformPrefab;

                // 이제 생성
                GameObject spawnedPlatform = Instantiate(platformToSpawn, platformPos, Quaternion.identity);
                spawnedPlatform.transform.SetParent(this.transform);
                spawnedObjects.Add(spawnedPlatform);
            }
        }
    }
}