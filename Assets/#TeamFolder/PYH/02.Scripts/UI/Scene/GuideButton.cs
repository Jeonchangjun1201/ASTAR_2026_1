using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class GuideButton : MonoBehaviour
    {
        public event Action<GuideUiSO> OnButtonClickEvent;
        private GuideUiSO _so;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        
        public void Initialize(GuideUiSO so)
        {
            _so = so;
            label.text = so.MiniGameName;
            icon.sprite = so.MiniGameIcon;
        }

        public void Clicked()
        {
            OnButtonClickEvent?.Invoke(_so);
        }
    }
}