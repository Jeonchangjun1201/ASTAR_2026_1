using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIStringInputField : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputString;
        [SerializeField] private TMP_Text inputInfo;

        public void Initialize(TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int inputCount = 10)
        {
            inputString.text = string.Empty;
            inputInfo.text = string.Empty;

            inputString.contentType = contentType;
            inputString.characterLimit = inputCount;
        }

        public string GetInput() => inputString.text;
        public void SetInputInfo(string info) => inputInfo.text = info;
        public void SetInputInfo(string info, Color textColor)
        {
            inputInfo.text = info;
            inputInfo.color = textColor;
        }
    }
}
