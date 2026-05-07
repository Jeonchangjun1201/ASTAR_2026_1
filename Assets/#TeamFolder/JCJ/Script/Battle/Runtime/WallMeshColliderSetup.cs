using UnityEngine;

// 전투 맵 벽 메시 콜라이더를 자동 설정하는 보조 처리.

namespace _TeamFolder.JCJ.Battle
{
    public class WallMeshColliderSetup : MonoBehaviour
    {
        private void Awake()
        {
            SwapColliders(transform);
        }

        private static void SwapColliders(Transform root)
        {
            foreach (Transform child in root)
            {
                var mf = child.GetComponent<MeshFilter>();
                var box = child.GetComponent<BoxCollider>();
                if (mf != null && mf.sharedMesh != null && box != null && child.GetComponent<MeshCollider>() == null)
                {
                    bool isTrigger = box.isTrigger;
                    DestroyImmediate(box);
                    var mc = child.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.isTrigger = isTrigger;
                }
                if (child.childCount > 0) SwapColliders(child);
            }
        }
    }
}