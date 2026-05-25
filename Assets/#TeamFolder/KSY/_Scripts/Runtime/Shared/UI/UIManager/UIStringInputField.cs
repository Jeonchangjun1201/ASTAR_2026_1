using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIStringInputField : MonoBehaviour
    {
        [SerializeField] private TMP_Text inputString;
        [SerializeField] private TMP_Text inputInfo;

        public bool Initialized { get; private set; }

        public void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
        }

        public string GetInput() => inputString.text;
        public void SetInputInfo(string info) => inputInfo.text = info;
    }
}
