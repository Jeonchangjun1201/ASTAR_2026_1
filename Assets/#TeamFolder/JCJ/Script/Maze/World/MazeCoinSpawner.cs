using System.Collections.Generic;
using UnityEngine;

// 점수용 코인을 미로 안에 배치하는 스포너.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 통로 셀에 코인을 배치하는 스포너 계약.
    /// </summary>
    public interface IMazeCoinSpawner
    {
        void Spawn(int[,] data, float cellSize, Vector2Int goal, HashSet<Vector2Int> occupied,
                  GameObject coinPrefab, Transform parent);
    }

    /// <summary>
    /// 골과 플레이어 시작 위치를 피하면서 미로 통로에 수집 코인을 랜덤 배치한다.
    /// </summary>
    public class MazeCoinSpawner : MonoBehaviour, IMazeCoinSpawner
    {
        [Range(0f, 0.2f)] [SerializeField] private float _spawnRatio = 0.03f;
        [SerializeField] private float _yOffset = 0.8f;

        public void Spawn(int[,] data, float cellSize, Vector2Int goal, HashSet<Vector2Int> occupied,
                         GameObject coinPrefab, Transform parent)
        {
            if (_spawnRatio <= 0f) return;

            // 열린 통로 수에 비례해 목표 코인 개수를 정하면 미로 크기가 바뀌어도 밀도가 비슷하게 유지된다.
            int w = data.GetLength(0);
            int h = data.GetLength(1);

            int openCount = 0;
            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                    if (data[x, y] == 0) openCount++;

            int target = Mathf.Max(1, Mathf.RoundToInt(openCount * _spawnRatio));

            int safety = target * 10;
            int placed = 0;
            while (placed < target && safety-- > 0)
            {
                // occupied 집합으로 플레이어, 골, 이미 배치된 코인이 같은 칸을 공유하지 않게 한다.
                int x = Random.Range(2, w - 2);
                int y = Random.Range(2, h - 2);
                var cell = new Vector2Int(x, y);
                if (data[x, y] != 0 || occupied.Contains(cell) || cell == goal) continue;

                occupied.Add(cell);
                var pos = new Vector3(x * cellSize, _yOffset, y * cellSize);
                var coin = coinPrefab != null
                    ? Instantiate(coinPrefab, pos, Quaternion.identity, parent)
                    : CreateDefaultCoin(pos, parent);

                var col = coin.GetComponent<Collider>();
                if (col == null)
                {
                    var sc = coin.AddComponent<SphereCollider>();
                    sc.isTrigger = true;
                    sc.radius = 0.6f;
                }
                else col.isTrigger = true;
                if (coin.GetComponent<Collectible>() == null) coin.AddComponent<Collectible>();
                placed++;
            }
        }

        private static GameObject CreateDefaultCoin(Vector3 pos, Transform parent)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coin.name = "Coin";
            coin.transform.SetParent(parent, false);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var mr = coin.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var c = new Color(1f, 0.85f, 0.2f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.6f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", c * 0.6f);
                }
                mat.color = c;
                mr.sharedMaterial = mat;
            }
            return coin;
        }
    }
}
