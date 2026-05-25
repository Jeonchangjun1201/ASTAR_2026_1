using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIStringInputField : MonoBehaviour
    {
        [SerializeField] private TMP_Text inputString;
        [SerializeField] private TMP_Text inputInfo;

        public void Initialize()
        {
            inputString.text = string.Empty;
            inputInfo.text = string.Empty;
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
