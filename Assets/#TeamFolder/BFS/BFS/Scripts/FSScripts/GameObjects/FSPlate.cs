using UnityEngine;
namespace BFS
{
    public class FSPlate : MonoBehaviour, IFSPlate                                            // Class for colored plates
    {
        [field: SerializeField] public PlateColor PlateColor { get; protected set; }          // PlateColor enum as property
        private MeshRenderer _meshRenderer;                                                   // Mesh Renderer

        private void Awake()
        {
            Color color = new Color();                                                        // Declare local variable for Color

            switch (PlateColor)                                                               // Changes color to match the value of PlateColor enum
            {
                case PlateColor.RED:
                    color = Color.red;
                    break;
                case PlateColor.GREEN:
                    color = Color.green;
                    break;
                case PlateColor.BLUE:
                    color = Color.blue;
                    break;
                case PlateColor.YELLOW:
                    color = Color.yellow;
                    break;
            }
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.material.color = color;
        }
        public void Appear()
        {
            gameObject.SetActive(true);
        }

        public void Disappear()
        {
            gameObject.SetActive(false);
        }
    }

}
