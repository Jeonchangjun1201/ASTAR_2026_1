using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 절차적 평평한 상단(flat-top) 육각 프리즘 메시. 요청 시 공유 메시 하나(radius=1, height=1)를 만들고
    /// 스폰 시 트랜스폼으로 스케일한다.
    /// XZ 평면 꼭짓점 각도 0°~300°(60° 간격) 배치, 상단 법선 +Y, 하단 -Y, 옆면은 바깥 향함.
    /// 삼각형 와인딩은 Unity 전면(반시계) 규칙에 맞춤.
    /// </summary>
    public static class HexMeshBuilder
    {
        private static Mesh _shared;

        /// <summary>캐시된 유닛 메시(반지름 1, 높이 1) — 호출 측이 트랜스폼으로 스케일.</summary>
        public static Mesh GetShared()
        {
            if (_shared == null) _shared = BuildFlatTop(radius: 1f, height: 1f);
            return _shared;
        }

        public static Mesh BuildFlatTop(float radius, float height)
        {
            var mesh = new Mesh { name = "HexPrism" };

            // 정점 14: 상단 중심, 상단 링 6, 하단 중심, 하단 링 6.
            var verts = new Vector3[14];
            var uvs   = new Vector2[14];
            const int topCenter = 0;
            const int botCenter = 7;

            float halfH = height * 0.5f;
            verts[topCenter] = new Vector3(0f,  halfH, 0f);
            verts[botCenter] = new Vector3(0f, -halfH, 0f);
            uvs[topCenter]   = new Vector2(0.5f, 0.5f);
            uvs[botCenter]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 6; i++)
            {
                float ang = 60f * i * Mathf.Deg2Rad;
                float x = Mathf.Cos(ang) * radius;
                float z = Mathf.Sin(ang) * radius;

                verts[1 + i] = new Vector3(x,  halfH, z);
                verts[8 + i] = new Vector3(x, -halfH, z);

                // UV는 상단 평면 투영 — 단순 Lit 재질에 충분.
                uvs[1 + i] = new Vector2((Mathf.Cos(ang) + 1f) * 0.5f, (Mathf.Sin(ang) + 1f) * 0.5f);
                uvs[8 + i] = uvs[1 + i];
            }

            // 상6+하6+옆12 = 24삼각형 = 72 인덱스.
            var tris = new int[72];
            int t = 0;

            // 상단 뚜껑. 링 정점은 XZ 평면에서 반시계 순서라 그대로 쓰면 법선이 -Y가 된다.
            // 위에서 보이는 면이 되도록 정점 순서를 바꿔 +Y 법선이 나오게 한다.
            for (int i = 0; i < 6; i++)
            {
                int a = 1 + i;
                int b = 1 + ((i + 1) % 6);
                tris[t++] = topCenter;
                tris[t++] = b;
                tris[t++] = a;
            }

            // 하단 뚜껑 — 상단과 대칭, 법선 -Y.
            for (int i = 0; i < 6; i++)
            {
                int a = 8 + i;
                int b = 8 + ((i + 1) % 6);
                tris[t++] = botCenter;
                tris[t++] = a;
                tris[t++] = b;
            }

            // 옆면은 각 모서리마다 바깥을 향하는 삼각형 두 개로 만든다.
            for (int i = 0; i < 6; i++)
            {
                int ta = 1 + i;
                int tb = 1 + ((i + 1) % 6);
                int ba = 8 + i;
                int bb = 8 + ((i + 1) % 6);

                // 사각형(ta,tb,bb,ba)을 바깥에서 본 반시계 삼각형 둘로 분할.
                tris[t++] = ta; tris[t++] = tb; tris[t++] = bb;
                tris[t++] = ta; tris[t++] = bb; tris[t++] = ba;
            }

            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.uv        = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(markNoLongerReadable: false);
            return mesh;
        }

        /// <summary>flat-top 육각 그리드에서 인접 열 중심 간 가로 간격.</summary>
        public static float ColumnSpacing(float radius) => radius * 1.5f;

        /// <summary>flat-top 육각 그리드에서 인접 행 중심 간 세로 간격.</summary>
        public static float RowSpacing(float radius) => radius * Mathf.Sqrt(3f);
    }
}
