using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public interface IMazeWallRenderer
    {
        void Render(int[,] data, float cellSize, GameObject wallPrefab, Transform parent);
    }

    /// <summary>
    /// 그리드 칸에 벽 프리팹을 배치하고, 선택적으로 시각 메시만 합쳐 드로우 호출을 줄인다.
    /// 벽마다 BoxCollider는 유지해 물리는 그대로 둔다.
    /// </summary>
    public class MazeWallRenderer : MonoBehaviour, IMazeWallRenderer
    {
        [SerializeField] private bool _combineMesh = true;

        [Tooltip("벽 프리팹이 기준으로 잡은 셀 크기. 런타임에 X/Z 스케일을 (cellSize / 이 값)으로 맞춘다.")]
        [SerializeField] private float _prefabReferenceCell = 3f;

        private static PhysicsMaterial _lowFrictionWallMaterial;

        public void Render(int[,] data, float cellSize, GameObject wallPrefab, Transform parent)
        {
            if (wallPrefab == null)
            {
                Debug.LogWarning("[MazeWallRenderer] wallPrefab is null, skipping walls.");
                return;
            }

            float scaleXZ = _prefabReferenceCell > 0.01f ? cellSize / _prefabReferenceCell : 1f;
            Vector3 prefabScale = wallPrefab.transform.localScale;
            Vector3 targetScale = new(prefabScale.x * scaleXZ, prefabScale.y, prefabScale.z * scaleXZ);
            float prefabLocalY = wallPrefab.transform.localPosition.y;

            // 벽만 별도 루트에 두어 메시 합칠 때 바닥·골·코인·데코와 섞이지 않게 한다.
            var wallsRoot = new GameObject("Walls");
            wallsRoot.transform.SetParent(parent, false);

            int w = data.GetLength(0);
            int h = data.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (data[x, y] != 1) continue;
                    var wall = Instantiate(wallPrefab, wallsRoot.transform);
                    wall.transform.localPosition = new Vector3(x * cellSize, prefabLocalY, y * cellSize);
                    wall.transform.localRotation = Quaternion.identity;
                    wall.transform.localScale    = targetScale;
                    ApplyLowFriction(wall);
                }
            }

            if (_combineMesh) CombineVisuals(wallsRoot);
            else              MarkStatic(wallsRoot);
        }

        private static void ApplyLowFriction(GameObject wall)
        {
            if (wall == null) return;
            _lowFrictionWallMaterial ??= new PhysicsMaterial("MazeWallLowFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            foreach (var col in wall.GetComponentsInChildren<Collider>(true))
            {
                if (col == null) continue;
                col.sharedMaterial = _lowFrictionWallMaterial;
            }
        }

        private void CombineVisuals(GameObject container)
        {
            var allFilters = container.GetComponentsInChildren<MeshFilter>();
            if (allFilters.Length == 0) return;

            // 자식만 합친다(컨테이너 본인에 MeshFilter가 붙은 예외는 건너뜀). null 메시는 방어적으로 제외.
            var valid = new System.Collections.Generic.List<MeshFilter>(allFilters.Length);
            foreach (var mf in allFilters)
            {
                if (mf == null) continue;
                if (mf.gameObject == container) continue;
                if (mf.sharedMesh == null) continue;
                valid.Add(mf);
            }
            if (valid.Count == 0) return;

            // 소스 메시가 CPU 읽기 불가(FBX에서 Read/Write 꺼짐)면 합치기 중단.
            foreach (var mf in valid)
            {
                if (!mf.sharedMesh.isReadable)
                {
                    Debug.Log($"[MazeWallRenderer] Mesh '{mf.sharedMesh.name}' not readable → combine skipped, keeping wall GOs intact.");
                    MarkStatic(container);
                    return;
                }
            }

            var firstRenderer = valid[0].GetComponent<MeshRenderer>();
            if (firstRenderer == null) firstRenderer = container.GetComponentInChildren<MeshRenderer>();
            Material mat = firstRenderer != null ? firstRenderer.sharedMaterial : null;

            var combine = new CombineInstance[valid.Count];
            var worldToContainer = container.transform.worldToLocalMatrix;
            for (int i = 0; i < valid.Count; i++)
            {
                combine[i].mesh = valid[i].sharedMesh;
                combine[i].transform = worldToContainer * valid[i].transform.localToWorldMatrix;
            }

            var finalMesh = new Mesh
            {
                name = "MazeWallsCombined",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            finalMesh.CombineMeshes(combine, true, true, false);

            foreach (var mfChild in valid)
            {
                if (mfChild == null) continue;
                if (mfChild.gameObject == container) continue;
                // 시각용 자식만 제거. 콜라이더는 벽 루트에 있고 이 메시 자식에는 없다.
                Destroy(mfChild.gameObject);
            }

            var mf2 = container.AddComponent<MeshFilter>();
            mf2.sharedMesh = finalMesh;

            var mr = container.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            // 런타임에 만든 Mesh를 컨테이너 파괴 시 같이 해제하기 위한 소유자 컴포넌트.
            // sharedMesh는 GO와 함께 자동 Destroy되지 않아 재생성마다 메시가 누수된다.
            var owner = container.AddComponent<RuntimeMeshOwner>();
            owner.OwnedMesh = finalMesh;

            container.isStatic = true;
        }

        private static void MarkStatic(GameObject container)
        {
            if (container == null) return;
            container.isStatic = true;
            foreach (var t in container.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.gameObject != null) t.gameObject.isStatic = true;
            }
        }
    }

    /// <summary>
    /// 런타임 할당 Mesh를 소유 GameObject 파괴 시 같이 제거한다.
    /// sharedMesh에 넣은 Mesh는 자동으로 지워지지 않아, 이게 없으면 미로 재생성마다 합쳐진 Mesh가 누수된다.
    /// </summary>
    internal sealed class RuntimeMeshOwner : MonoBehaviour
    {
        public Mesh OwnedMesh;

        private void OnDestroy()
        {
            if (OwnedMesh != null)
            {
                if (Application.isPlaying) Destroy(OwnedMesh);
                else                       DestroyImmediate(OwnedMesh);
                OwnedMesh = null;
            }
        }
    }
}
