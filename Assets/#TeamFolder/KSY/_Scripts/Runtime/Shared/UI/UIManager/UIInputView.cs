using System;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIInputView : MonoBehaviour, IView
    {
        [SerializeField] private UIStringInputField inputField;
        [SerializeField] private UIInsertButton insertButton;

        public string Name => gameObject.name;

        private void OnEnable()
        {
            inputField.Initialize();
            insertButton.Initialize();
        }

        public void RegisterInsertEvent(Action OnInsertButtonClicked) => insertButton.OnClicked += OnInsertButtonClicked;

        public string GetInput() => inputField.GetInput();

        public void SetInputInfo(string info) => inputField.SetInputInfo(info);
        public void SetInputInfo(string info, Color textColor) => inputField.SetInputInfo(info, textColor);

        public void Show(string info)
        {
            gameObject.SetActive(true);
            SetInputInfo(info);
        }
        public void Show(string info, Color color)
        {
            gameObject.SetActive(true);
            SetInputInfo(info, color);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
