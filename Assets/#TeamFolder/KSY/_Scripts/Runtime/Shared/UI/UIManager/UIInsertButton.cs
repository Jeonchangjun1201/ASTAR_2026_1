using System;
using UnityEngine;
using UnityEngine.UI;

namespace KSY.Shared.UI
{
    public class UIInsertButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        public Action OnClicked;

        public void Initialize() => button.onClick.AddListener(()=>OnClicked.Invoke());

        private void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
