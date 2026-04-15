using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BFS
{
    public class PlateQue
    {
        Queue<PlateColor> plateQueue = new Queue<PlateColor>();

        public void AddPlateToQueue(PlateColor plateColor)
        {
            plateQueue.Enqueue(plateColor);
        }

        public PlateColor DeletePlateFromQueue()
        {
            PlateColor target = plateQueue.Dequeue();
            return target;
        }
        public int QueueSize => plateQueue.Count;
    }
    public class FSPlateManager : MonoBehaviour
    {
        PlateQue plateQue = new PlateQue();
        public event Action<PlateColor> OnPlateAdded;
        private Dictionary<PlateColor, IFSPlate> _plateDict = new Dictionary<PlateColor, IFSPlate>();
        private PlateColor _prevColor;

        private void Awake()
        {
            IFSPlate[] fsPlates = GetComponentsInChildren<IFSPlate>();
            foreach (IFSPlate f in fsPlates)
                _plateDict.Add(f.PlateColor, f);
        }
        public void EnqueuePlate()
        {
            PlateColor plate;
            plate = (PlateColor)UnityEngine.Random.Range(0, 4);
            _prevColor = plate;
            plateQue.AddPlateToQueue(plate);
            Debug.Log($"<color=green>{plate}</color>");
            OnPlateAdded?.Invoke(plate);
        }
        public PlateColor DequeuePlate(float duration)
        {
            PlateColor plate = plateQue.DeletePlateFromQueue();
            Debug.Log($"<color=red>{plate}</color>");
            StartCoroutine(PlateDisappearCoroutine(plate, duration));
            return plate;
        }

        private IEnumerator PlateDisappearCoroutine(PlateColor plate, float duration)
        {
            _plateDict[plate].Disappear();
            yield return new WaitForSeconds(duration);
            _plateDict[plate].Appear();
        }

        public int QueueSize => plateQue.QueueSize;
    }
}
