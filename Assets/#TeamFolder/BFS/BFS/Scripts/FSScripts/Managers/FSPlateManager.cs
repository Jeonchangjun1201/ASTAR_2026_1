using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BFS
{
    public class PlateQue                                                     // Main Class that you need to manage plate order // 발판 순서를 관리하는 데에 필요한 주 클래스
    {
        public Queue<PlateColor> plateQueue { get; private set; } 
            = new Queue<PlateColor>();                                        // Queue that helps game to destroy plates in order // 게임이 발판들을 순서에 맞게 삭제하는 걸 도와주는 큐

        public void AddPlateToQueue(PlateColor plateColor)                    // Method that adds plate to order(queue) // 발판을 순서(큐)에 추가하는 메서드
        {
            plateQueue.Enqueue(plateColor);
        }

        public PlateColor DeletePlateFromQueue()                              // Method that deletes plate from order(queue) // 위에꺼 반대 (큐에서 발판 삭제)
        {
            PlateColor target = plateQueue.Dequeue();
            return target;
        }
        public int QueueSize => plateQueue.Count;                             // Returns a size of the queue // 큐의 사이즈 반환
    }
    public class FSPlateManager : MonoBehaviour
    {
        public PlateQue plateQue { get; private set; } = new PlateQue();      // PlateQue Instance // PlateQue 인스턴스
        public event Action<PlateColor> OnPlateAdded;                         // Action that invokes whenever new plate is added to a queue // 발판이 큐에 추가될 때마다 인보크되는 액션
        [SerializeField] private ParticleSystem plateDestroyParticle;
        [SerializeField] private ParticleSystem plateAppearParticle;
        private Dictionary<PlateColor, IFSPlate> _plateDict = new Dictionary<PlateColor, IFSPlate>();
                                                                              // Dictionary, can access to plate by color of plate (Key: PlateColor(enum), Value: IFSPlate(interface for plate objects)) // 색을 통해 발판에 접근할 수 있는 딕셔너리(PlateColor는 키, IFSPlate는 값)

        private void Awake()
        {
            IFSPlate[] fsPlates = GetComponentsInChildren<IFSPlate>();
            foreach (IFSPlate f in fsPlates)
            {
                _plateDict.Add(f.PlateColor, f);
                f.SetPartice(plateDestroyParticle, plateAppearParticle);
            }
        }
        public void EnqueuePlate()                                            // Method that adds plate to queue using PlateQue instance // PlateQue의 인스턴스를 통해 발판을 큐에 추가하는 메서드
        {
            PlateColor plate;
            plate = (PlateColor)UnityEngine.Random.Range(0, 4);
            plateQue.AddPlateToQueue(plate);
            OnPlateAdded?.Invoke(plate);
        }
        public PlateColor DequeuePlate(float duration)                        // Method that deletes plate from queue using PlateQue instance // 위에꺼 반대, 마찬가지로 PlateQue 인스턴스 사용
        {
            PlateColor plate = plateQue.DeletePlateFromQueue();
            StartCoroutine(PlateDisappearCoroutine(plate, duration));
            return plate;
        }

        private IEnumerator PlateDisappearCoroutine(PlateColor plate, float duration) // Method that calls plate to disappear itself(interface has method that sets its active false) // 자기 자신을 사라지게하는 메서드
        {
            _plateDict[plate].Disappear();                                            // Plate goes byebye // 발판 삭☆제
            yield return new WaitForSeconds(duration);                                // Wait for short amount of time // 조금 기다리면
            _plateDict[plate].Appear();                                               // Then plate comes back, hi // 발판이 도라와요!! ^^
        }

        public int QueueSize => plateQue.QueueSize;                                   // Returns size of the queue from PlateQue instance // PlateQue 인스턴스로부터 큐의 사이지를 받고 반환함
    }
}
