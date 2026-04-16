using System.Collections;
using UnityEngine;
namespace BFS
{
    public class FSGameManager : MonoBehaviour                                // Game manager script for Four Side game
    {
        [SerializeField] private FSPlateManager plateManager;
        [SerializeField] private FSCameraManager cameraManager;
        [SerializeField] private FSStageManager stageManager;
        private IFSScreen monitorScreen;
        private void Awake()
        {
            cameraManager.FocusToGame();                                  
            monitorScreen = GetComponentInChildren<IFSScreen>();
            plateManager.OnPlateAdded += ManageScreenColor;                   // Subscribes, monitor screen color is changed everytime plate is added to queue

            stageManager.OnCameraViewChange += ChangeCameraView;              // Subscribes, now camera view will change depending on parameter sent from stage manager
            stageManager.OnPlateQueue += QueuePlate;                          // Subscribes, stage manager can alert plate manager to que plates now
            stageManager.OnScreenReset += ResetScreen;                        // Subscribes, stage manager can reset monitor screen to default
            stageManager.OnPlateDequeue += DeQueuePlate;                      // Subscribes, now stage manager can remove plates
        }

        private void Start()
        {

        }

        private void OnDestroy()                                              // Unsub
        {
            plateManager.OnPlateAdded -= ManageScreenColor;

            stageManager.OnCameraViewChange += ChangeCameraView; 
            stageManager.OnPlateQueue += QueuePlate;   
            stageManager.OnScreenReset += ResetScreen;
            stageManager.OnPlateDequeue += DeQueuePlate;
        }
        private void ChangeCameraView(FSCameraView cameraView)                // Method to change camera view, receiving FSCameraView enum that decides which camera to view    
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
                    throw new System.ArgumentException("INVALID TYPE");       // Exception
            }
        }
        private void ManageScreenColor(PlateColor plate)                      // Method to change monitor screen color
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
        private void QueuePlate() => plateManager.EnqueuePlate();
        private void ResetScreen() => monitorScreen.ResetScreenColor();
        private void DeQueuePlate(float duration) => plateManager.DequeuePlate(duration);
    }
}
