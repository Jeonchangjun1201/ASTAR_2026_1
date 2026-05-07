using Unity.Cinemachine;
using UnityEngine;
namespace BFS
{
    public class FSCameraManager : MonoBehaviour                                   // Manager script that helps GameManager to switch camera view // 게임 매니저가 카메라 시점을 변경하는 걸 돕는 스크립트
    {
        [SerializeField] private CinemachineCamera gameCamera;
        [SerializeField] private CinemachineCamera monitorCamera;

        public void FocusToScreen()                                                // Changes camera view to Monitor // 카메라 시점을 모니터로 변경
        {
            monitorCamera.Prioritize();
        }

        public void FocusToGame()                                                  // Changes camera view to game // 카메라 시점을 게임으로 변경 (발판)
        {
            gameCamera.Prioritize();
        }
    }
}
