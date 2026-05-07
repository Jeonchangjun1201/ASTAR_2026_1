using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class PaintManager : MonoBehaviour
    {
        public RenderTexture paintCanvas;
        public Texture2D brushTexture;
        public float brushSize = 50f;

        private Material _paintMat;

        private void Awake()
        {
            _paintMat = new Material(Shader.Find("Sprites/Default"));

            if (paintCanvas != null)
            {
                Debug.Log("여기 실행됨");
                RenderTexture.active = paintCanvas;
                GL.Clear(true, true, Color.white);
                RenderTexture.active = null;
                Debug.Log("Awake 완료");
            }
            
        }
        private void Start()
        {
            /*  if (paintCanvas != null)
              {
                  Debug.Log("여기 실행됨");
                  RenderTexture.active = paintCanvas;
                  GL.Clear(true, true, Color.white);
                  RenderTexture.active = null;
              }*/


        }

  
        public void DrawBrush(Vector2 uv, Color brushColor)
        {
            if (paintCanvas == null || brushTexture == null) return;

            RenderTexture.active = paintCanvas;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, paintCanvas.width, paintCanvas.height, 0);

            float xPos = uv.x * paintCanvas.width - (brushSize / 2f);
            float yPos = (1.0f - uv.y) * paintCanvas.height - (brushSize / 2f);

            Rect drawRect = new Rect(xPos, yPos, brushSize, brushSize);

            _paintMat.color = brushColor;

            Graphics.DrawTexture(drawRect, brushTexture, _paintMat);

            GL.PopMatrix();
            RenderTexture.active = null;
        }
    }
}