using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KDH.Gimic
{
    public class SquareOut : MonoBehaviour
    {
        [SerializeField] private float spawnInterval = 8f;
        [SerializeField] private float launchForce = 50f;
        [SerializeField] private float squareLifetime = 1f;
        [SerializeField] private float planeSize = 10f;
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float squareHeight = 5f;

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                StartCoroutine(SpawnSquare());
            }
        }

        private IEnumerator SpawnSquare()
        {
            float x = Random.Range(-planeSize / 2f, planeSize / 2f);
            float z = Random.Range(-planeSize / 2f, planeSize / 2f);

            Vector3 hidePos = new Vector3(x, groundY - 3f, z);
            Vector3 warnPos = new Vector3(x, groundY + 0.3f, z);
            Vector3 showPos = new Vector3(x, groundY + squareHeight, z);

            // 큐브 생성
            GameObject square = GameObject.CreatePrimitive(PrimitiveType.Cube);
            square.transform.position = hidePos;
            square.transform.localScale = new Vector3(10f, 0.5f, 10f);
            square.GetComponent<Renderer>().material.color = Color.yellow;

            square.GetComponent<Collider>().isTrigger = false;
            

            // Rigidbody 설정
            Rigidbody rb = square.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // LaunchOnHit 추가
            square.AddComponent<SquareTrigger>().Initialize(launchForce);

            // 1단계: 경고 (살짝 올라옴)
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                square.transform.position = Vector3.Lerp(hidePos, warnPos, elapsed / 0.5f);
                yield return null;
            }

            // 2단계: 훅 솟구치기
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                square.transform.position = Vector3.Lerp(warnPos, showPos, elapsed / 0.1f);
                yield return null;
            }

            // 3단계: 유지
            yield return new WaitForSeconds(squareLifetime);

            // 4단계: 내려가기 전에 Collider 끄기
            square.GetComponent<Collider>().enabled = false;

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                square.transform.position = Vector3.Lerp(showPos, hidePos, elapsed / 0.2f);
                yield return null;
            }

            Destroy(square);
        }
    }
}