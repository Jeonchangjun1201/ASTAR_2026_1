using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KSY.Shared.UI
{
    public class KSY_PlayerBoxUI : MonoBehaviour
    {
        [SerializeField] private Sprite[] icons;

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nickName;

        public void Initialize(int iconIndex, string nickName)
        {
            icon.sprite = icons[iconIndex];
            this.nickName.text = nickName;
        }
    }
}
