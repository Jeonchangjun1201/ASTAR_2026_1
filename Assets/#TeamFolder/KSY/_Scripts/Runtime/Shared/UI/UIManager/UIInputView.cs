using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIInputView : MonoBehaviour, IView
    {
        [SerializeField] private UIStringInputField inputField;

        public string Name => gameObject.name;

        private void OnEnable()
        {
            inputField.Initialize();
        }

        public string GetInput() => inputField.GetInput();
        public void SetInputInfo(string info) => inputField.SetInputInfo(info);
    }
}
