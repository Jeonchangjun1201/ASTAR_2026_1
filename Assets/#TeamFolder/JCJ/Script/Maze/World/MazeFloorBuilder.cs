using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 크기에 맞춘 바닥 평면과 천장 콜라이더를 생성한다. 머티리얼이 없으면 기본 머티리얼을 만든다.
    /// </summary>
    public class MazeFloorBuilder : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Color _fallbackColor = new(0.25f, 0.22f, 0.20f);
        [SerializeField] private float _ceilingHeight = 6.5f;
        [SerializeField] private float _ceilingThickness = 0.5f;

        public GameObject Build(int width, int height, float cellSize, Transform parent)
        {
            float w = width * cellSize;
            float h = height * cellSize;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MazeFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3((width - 1) * 0.5f * cellSize, -0.05f, (height - 1) * 0.5f * cellSize);
            floor.transform.localScale = new Vector3(w / 10f + 0.5f, 1f, h / 10f + 0.5f);
            floor.isStatic = true;

            var mr = floor.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = ResolveMaterial();
                mr.receiveShadows = true;
            }
            BuildCeiling(width, height, cellSize, parent);
            return floor;
        }

        private void BuildCeiling(int width, int height, float cellSize, Transform parent)
        {
            var ceiling = new GameObject("MazeCeiling");
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.position = new Vector3((width - 1) * 0.5f * cellSize, _ceilingHeight, (height - 1) * 0.5f * cellSize);

            var col = ceiling.AddComponent<BoxCollider>();
            col.size = new Vector3(width * cellSize + cellSize, _ceilingThickness, height * cellSize + cellSize);
            ceiling.isStatic = true;
        }

        private Material ResolveMaterial()
        {
            if (_material != null) return _material;
#if UNITY_EDITOR
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/LowPolyDungeonsLite/Materials/LowPolyDungeonsLite_BG.mat");
            if (loaded != null) return loaded;
#endif
            return CreateFallbackMaterial();
        }

        private Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _fallbackColor);
            mat.color = _fallbackColor;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
            return mat;
        }
    }
}
