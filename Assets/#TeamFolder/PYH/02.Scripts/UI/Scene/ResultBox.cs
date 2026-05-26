using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class ResultBox : MonoBehaviour
    {
        [SerializeField] private Sprite[] icons;
        
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nickNameLabel;
        [SerializeField] private TMP_Text pointLabel;
        [SerializeField] private TMP_Text rankingLabel;

        public string NickName { get; private set; }
        public RectTransform Rect { get; private set; }

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }

        public void Initialize(int index, string nickName, int point, int ranking)
        {
            icon.sprite = icons[index];
            NickName = nickName;
            nickNameLabel.text = nickName;
            pointLabel.text = point + "p";
            rankingLabel.text = ranking + "#";
        }
    }
}