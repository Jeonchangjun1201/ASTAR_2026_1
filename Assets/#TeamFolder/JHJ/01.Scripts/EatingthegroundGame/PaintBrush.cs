using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class PaintBrush : MonoBehaviour
    {
        [Header("페인트 설정")]
        [SerializeField] private RenderTexture _paintCanvas;
        [SerializeField] private float _brushSize = 50f;

        [SerializeField] private PaintScoreManager _scoreManager;
        [SerializeField] private int _playerIndex = 0; 

        [SerializeField] private PaintManager _paintManager;

        private void Start()
        {
            RenderTexture.active = _paintCanvas;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = null;
        }

        private void Update()
        {
            PaintOnGround();
        }

        private void PaintOnGround()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
            {
                if (hit.collider is not MeshCollider)
                {
                    return;
                }

                Color myColor = _scoreManager.GetPlayerColor(_playerIndex);
                _paintManager.DrawBrush(hit.textureCoord, myColor);
            }
        }
    }
}