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

        private int _personalCount = 1;

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
            CustomLog.Log($"Count : {_personalCount}");
            SetPersonnelCount(_personalCount);
        }
        public void Decrease()
        {
            ++_personalCount;
            CustomLog.Log($"Count : {_personalCount}");
            SetPersonnelCount(_personalCount);
        }

        private void SetPersonnelCount(int count)
        {
            CustomLog.Log("SetPersonnelCount Start");
            applyValue = count;
            canApplyValue = true;
            CustomLog.Log("SetPersonnelCount Stop");
        }
        public void SetRoomCode(string code) => roomCode.text = $"¹æ ÄÚµå : {code}";
    }
}
