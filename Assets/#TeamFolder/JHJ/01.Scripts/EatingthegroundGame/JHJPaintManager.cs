using PYH.Util;
using System.Threading;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{

    [System.Serializable]
    public struct PaintSyncPacket
    {
        public Vector2 UV;         // 어디에 칠했는지 (x, y)
        public Color BrushColor;   // 무슨 색으로 칠했는지
        public float BrushSize;    // 붓 크기 (필요하다면)
    }

    public class JHJPaintManager : MonoSingleton<JHJPaintManager>
    {
        public RenderTexture paintCanvas;
        public Texture2D brushTexture;
        public float brushSize = 50f;

        private Material _paintMat;
        private bool _isCanvasInitialized;

        private void Awake()
        {
            _paintMat = new Material(Shader.Find("Sprites/Default"));
            EnsureCanvasInitialized();
        }

        private void OnEnable() => EnsureCanvasInitialized();

        private void OnDestroy()
        {
            if (_paintMat != null) Destroy(_paintMat);
        }

        public void EnsureCanvasInitialized()
        {
            if (paintCanvas == null) return;

            bool wasCreated = paintCanvas.IsCreated();
            if (!wasCreated) paintCanvas.Create();
            if (_isCanvasInitialized && wasCreated) return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = paintCanvas;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = previous;
            _isCanvasInitialized = true;
        }

        // ───────────────── [2. 내가 칠했을 때 서버로 정보 보내기] ─────────────────
        public void SendPaintDataToServer(Vector2 uv, Color brushColor)
        {
            PaintSyncPacket packet = new PaintSyncPacket
            {
                UV = uv,
                BrushColor = brushColor,
                BrushSize = this.brushSize
            };
        }

        // ───────────────── [3. 실제 그리기 (서버에서 명령받을 때)] ─────────────────
        public void DrawBrush(Vector2 uv, Color brushColor)
        {
            if (paintCanvas == null || brushTexture == null || _paintMat == null) return;

            EnsureCanvasInitialized();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = paintCanvas;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, paintCanvas.width, paintCanvas.height, 0);

            float xPos = uv.x * paintCanvas.width - (brushSize / 2f);
            float yPos = (1.0f - uv.y) * paintCanvas.height - (brushSize / 2f);

            Rect drawRect = new Rect(xPos, yPos, brushSize, brushSize);

            _paintMat.color = brushColor;
            Graphics.DrawTexture(drawRect, brushTexture, _paintMat);

            GL.PopMatrix();
            RenderTexture.active = previous;
        }
    }
}