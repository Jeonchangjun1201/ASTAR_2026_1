using Unity.Cinemachine;
using UnityEngine;
namespace BFS
{
    public class FSCameraManager : MonoBehaviour                                   // Manager script that helps GameManager to switch camera view
    {
        [SerializeField] private CinemachineCamera gameCamera;
        [SerializeField] private CinemachineCamera monitorCamera;

        public void FocusToScreen()                                                // Changes camera view to Monitor
        {
            monitorCamera.Prioritize();
        }

        public void FocusToGame()                                                  // Changes camera view to game
        {
            gameCamera.Prioritize();
        }
    }
}
