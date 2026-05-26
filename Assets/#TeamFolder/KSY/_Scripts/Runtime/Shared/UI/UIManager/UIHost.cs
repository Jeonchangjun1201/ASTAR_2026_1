using TMPro;
using UnityEngine;

namespace KSY.Shared.UI
{
    public class UIHost : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomCode;
        [SerializeField] private TMP_Text personnelCount;

        private int _personalCount = 1;

        public void IncreaseCount()
        {
            _personalCount++;
            SetPersonnelCount(_personalCount);
        }
        public void Decrease()
        {
            _personalCount--;
            SetPersonnelCount(_personalCount);
        }

        private void SetPersonnelCount(int count) => personnelCount.text = $"( {count} / 4 )";
        public void SetRoomCode(string code) => roomCode.text = $"¹æ ÄÚµå : {code}";
    }
}
