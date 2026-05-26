using Codice.Client.BaseCommands;
using KSY.Utility;
using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIHost : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomCode;
        [SerializeField] private TMP_Text personnelCount;

        private int _personalCount = 0;

        private int applyValue = 0;
        private bool canApplyValue = false;

        private void Update()
        {
            if(canApplyValue)
            {
                canApplyValue = false;
                personnelCount.text = $"({applyValue} / 4)";
            }
        }

        public void IncreaseCount()
        {
            ++_personalCount;
            SetPersonnelCount(_personalCount);
        }
        public void Decrease()
        {
            ++_personalCount;
            SetPersonnelCount(_personalCount);
        }

        private void SetPersonnelCount(int count)
        {
            applyValue = count;
            canApplyValue = true;
        }
        public void SetRoomCode(string code) => roomCode.text = $"¹æ ÄÚµå : {code}";
    }
}
