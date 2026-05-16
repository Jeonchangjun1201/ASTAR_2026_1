using TMPro;
using UnityEngine;

namespace PYH.UI
{
    public class ForDebugTimerUi : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        
        public void ViewTimer(int sec)
        {
            int minutes = sec / 60;
            int seconds = sec % 60;

            text.text = $"{minutes:00} : {seconds:00}";
        }
    }
}
