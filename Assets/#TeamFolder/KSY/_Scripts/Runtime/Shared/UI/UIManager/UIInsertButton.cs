using System;
using UnityEngine;
using UnityEngine.UI;

namespace KSY.Shared.UI
{
    public class UIInsertButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        public event Action OnClicked;

        public void Initialize() => button.onClick.AddListener(()=>OnClicked.Invoke());
    }
}
