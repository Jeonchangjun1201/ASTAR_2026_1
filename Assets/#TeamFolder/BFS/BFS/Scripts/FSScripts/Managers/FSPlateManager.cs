using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BFS
{
    public class PlateQue                                                     // Main Class that you need to manage plate order
    {
        Queue<PlateColor> plateQueue = new Queue<PlateColor>();               // Queue that helps game to destroy plates in order

        public void AddPlateToQueue(PlateColor plateColor)                    // Method that adds plate to order(queue)
        {
            plateQueue.Enqueue(plateColor);
        }

        public PlateColor DeletePlateFromQueue()                              // Method that deletes plate from order(queue)
        {
            PlateColor target = plateQueue.Dequeue();
            return target;
        }
        public int QueueSize => plateQueue.Count;                             // Returns a size of the queue
    }
    public class FSPlateManager : MonoBehaviour
    {
        PlateQue plateQue = new PlateQue();                                   // PlateQue Instance; allows
        public event Action<PlateColor> OnPlateAdded;                         // Action that invokes whenever new plate is added to a queue
        private Dictionary<PlateColor, IFSPlate> _plateDict = new Dictionary<PlateColor, IFSPlate>();
                                                                              // Dictionary, can access to plate by color of plate (Key: PlateColor(enum), Value: IFSPlate(interface for plate objects))

        private void Awake()
        {
            IFSPlate[] fsPlates = GetComponentsInChildren<IFSPlate>();
            foreach (IFSPlate f in fsPlates)
                _plateDict.Add(f.PlateColor, f);
        }
        public void EnqueuePlate()                                            // Method that adds plate to queue using PlateQue instance
        {
            PlateColor plate;
            plate = (PlateColor)UnityEngine.Random.Range(0, 4);
            plateQue.AddPlateToQueue(plate);
            Debug.Log($"<color=green>{plate}</color>");
            OnPlateAdded?.Invoke(plate);
        }
        public PlateColor DequeuePlate(float duration)                        // Method that deletes plate from queue using PlateQue instance
        {
            PlateColor plate = plateQue.DeletePlateFromQueue();
            Debug.Log($"<color=red>{plate}</color>");
            StartCoroutine(PlateDisappearCoroutine(plate, duration));
            return plate;
        }

        private IEnumerator PlateDisappearCoroutine(PlateColor plate, float duration) // Method that calls plate to disappear itself(interface has method that sets its active false)
        {
            _plateDict[plate].Disappear();                                            // Plate goes byebye
            yield return new WaitForSeconds(duration);                                // Wait for short amount of time
            _plateDict[plate].Appear();                                               // Then plate comes back, hi
        }

        public int QueueSize => plateQue.QueueSize;                                   // Returns size of the queue from PlateQue instance
    }
}
