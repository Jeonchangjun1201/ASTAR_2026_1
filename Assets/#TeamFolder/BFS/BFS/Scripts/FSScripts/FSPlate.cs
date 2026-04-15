using UnityEngine;
namespace BFS
{
    public class FSPlate : MonoBehaviour, IFSPlate
    {
        [field: SerializeField] public PlateColor PlateColor { get; set; }
        [SerializeField] MeshRenderer meshRenderer;

        private void Awake()
        {
            Color color = new Color();

            switch (PlateColor)
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

            meshRenderer.material.color = color;
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
