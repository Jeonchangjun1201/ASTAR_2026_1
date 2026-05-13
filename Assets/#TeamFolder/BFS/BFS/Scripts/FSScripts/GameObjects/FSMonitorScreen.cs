using UnityEngine;
namespace BFS
{
    public class FSMonitorScreen : MonoBehaviour, IFSScreen
    {
        private MeshRenderer _meshRenderer;
        [field: SerializeField] public PlateColor GivenColor { get; protected set; }

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            ChangeScreenColor(Color.black);
        }
        public void ChangeScreenColor(Color color)               // Gets color as parameter, then changes its material(color) equal to that // 매개변수로 색을 받고, 메터리얼의 색을 변경
        {
            _meshRenderer.material.color = color;
        }
        public void ResetScreenColor()                           // Sets its color to black // 색을 검정색으로 지정
        {
            _meshRenderer.material.color = Color.black;
        }
    }
}
