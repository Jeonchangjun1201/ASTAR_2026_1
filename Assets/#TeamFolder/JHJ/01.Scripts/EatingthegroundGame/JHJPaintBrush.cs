using JHJ.Scripts.EatingthegroundGame;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJPaintBrush : MonoBehaviour
    {
        [Header("페인트 설정")]
        [SerializeField] private RenderTexture _paintCanvas;
        [SerializeField] private float _brushSize = 50f;

        [SerializeField] private JHJPaintScoreManager _scoreManager;
        [SerializeField] private int _playerIndex = 0;

        [SerializeField] private JHJPaintManager _paintManager;

        private void Start()
        {
            if (_paintManager != null)
            {
                _paintManager.EnsureCanvasInitialized();
            }
        }

        private void Update()
        {
            PaintOnGround();
        }

        private void PaintOnGround()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
            {
                

                if (hit.collider is not MeshCollider) return;

                Color myColor = _scoreManager.GetPlayerColor(_playerIndex);
                _paintManager.DrawBrush(hit.textureCoord, myColor);
            }
            else
            {
                Debug.Log("Raycast 안 맞음");
            }
        }
    }
}