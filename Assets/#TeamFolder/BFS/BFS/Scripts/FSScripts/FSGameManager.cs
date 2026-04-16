using System.Collections;
using UnityEngine;
namespace BFS
{
    public class FSGameManager : MonoBehaviour
    {
        [SerializeField] private FSPlateManager plateManager;
        [SerializeField] private FSCameraManager cameraManager;
        [SerializeField] private FSStageManager stageManager;
        private IFSScreen monitorScreen;
        private void Awake()
        {
            cameraManager.FocusToGame();
            monitorScreen = GetComponentInChildren<IFSScreen>();
            plateManager.OnPlateAdded += ManageScreenColor;

            stageManager.OnCameraViewChange += ChangeCameraView;
            stageManager.OnPlateQueue += QueuePlate;
            stageManager.OnScreenReset += ResetScreen;
            stageManager.OnPlateDequeue += DeQueuePlate;
        }

        private void Start()
        {

        }

        private void OnDestroy()
        {
            plateManager.OnPlateAdded -= ManageScreenColor;
        }
        private void ChangeCameraView(FSCameraView cameraView)
        {
            switch (cameraView)
            {
                case FSCameraView.GAME:
                    cameraManager.FocusToGame();
                    break;
                case FSCameraView.SCREEN:
                    cameraManager.FocusToScreen();
                    break;
                default:
                    throw new System.ArgumentException("INVALID TYPE");
            }
        }
        private void QueuePlate() => plateManager.EnqueuePlate();
        private void ResetScreen() => monitorScreen.ResetScreenColor();
        private void DeQueuePlate(float duration) => plateManager.DequeuePlate(duration);
        private void ManageScreenColor(PlateColor plate)
        {
            Color color = new Color();
            switch (plate)
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
                default:
                    throw new System.ArgumentException("INVALID TYPE");
            }

            monitorScreen.ChangeScreenColor(color);
        }
    }
}
