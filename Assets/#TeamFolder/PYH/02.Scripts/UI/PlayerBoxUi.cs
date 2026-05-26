using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class PlayerBoxUi : MonoBehaviour
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
