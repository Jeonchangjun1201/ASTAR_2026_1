using System.Threading;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintManager : MonoBehaviour
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

        private void OnEnable()
        {
            EnsureCanvasInitialized();
        }

        private void OnDestroy()
        {
            if (_paintMat != null)
            {
                Destroy(_paintMat);
            }
        }

        public void EnsureCanvasInitialized()
        {
            if (paintCanvas == null)
            {
                return;
            }

            bool wasCreated = paintCanvas.IsCreated();

            if (!wasCreated)
            {
                paintCanvas.Create();
            }

            if (_isCanvasInitialized && wasCreated)
            {
          
                return;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = paintCanvas;
            Debug.Log("Clear before");
            GL.Clear(true, true, Color.white);
            Debug.Log($"Clear after : Time -> {Time.time}");
            RenderTexture.active = previous;
            _isCanvasInitialized = true;
        }

        public void DrawBrush(Vector2 uv, Color brushColor)
        {
            if (paintCanvas == null || brushTexture == null || _paintMat == null)
            {
                return;
            }

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