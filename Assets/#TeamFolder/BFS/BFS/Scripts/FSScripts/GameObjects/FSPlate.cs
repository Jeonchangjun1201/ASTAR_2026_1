using UnityEngine;
namespace BFS
{
    public class FSPlate : MonoBehaviour, IFSPlate                                            // Class for colored plates // 발판 클래스
    {
        [field: SerializeField] public PlateColor PlateColor { get; protected set; }          // PlateColor enum as property // 발판 색깔 이넘을 프로퍼티로 선언
        private MeshRenderer _meshRenderer;                                                   // Mesh Renderer // 메쉬 렌더러
        private ParticleSystem _destroyParticle;

        private void Awake()
        {
            Color color = new Color();                                                        // Declare local variable for Color // 색깔을 지역변수로 선언

            switch (PlateColor)                                                               // Changes color to match the value of PlateColor enum // 발판색 이넘에 따라 색을 변경
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
            Instantiate(_destroyParticle, transform.position, Quaternion.Euler(-90,0,0));
            gameObject.SetActive(false);
        }

        public void SetPartice(ParticleSystem particle)
        {
            _destroyParticle = particle;
        }
    }

}
