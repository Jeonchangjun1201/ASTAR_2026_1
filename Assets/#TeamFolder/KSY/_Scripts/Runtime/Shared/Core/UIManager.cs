using KSY.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KSY.Shared
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public Dictionary<string, GameObject> uiItems = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
                Destroy(gameObject);
        }


        private void Initialize()
        {
            uiItems.Clear();

            // 1단계: UIManager 바로 아래에 있는 1차 자식들을 순회
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out Canvas canvas)) return;
                foreach (Transform grandChild in canvas.transform)
                {
                    // Image 컴포넌트가 붙어있는지(Panel인지) 확인
                    if (grandChild.TryGetComponent(out Image _) && !uiItems.ContainsKey(grandChild.name))
                    {
                        // 딕셔너리에 이름 중복 검사
                        uiItems.Add(grandChild.name, grandChild.gameObject);
                    }
                }

            }

            CustomLog.Log($"총 {uiItems.Count}개의 패널 UI 등록 완료", Color.green);
        }
    }
}
