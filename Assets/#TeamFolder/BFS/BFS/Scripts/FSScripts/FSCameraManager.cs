using Unity.Cinemachine;
using UnityEngine;
namespace GDH
{
    public class FSCameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera gameCamera;
        [SerializeField] private CinemachineCamera monitorCamera;

        public void FocusToScreen()
        {
            monitorCamera.Prioritize();
        }

        public void FocusToGame()
        {
            gameCamera.Prioritize();
        }
    }
}
