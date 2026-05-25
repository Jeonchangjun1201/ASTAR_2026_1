using TMPro;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class ResultBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text nickNameLabel;
        [SerializeField] private TMP_Text pointLabel;
        [SerializeField] private TMP_Text rankingLabel;

        public string NickName { get; private set; }
        public RectTransform Rect { get; private set; }

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }

        public void Initialize(string nickName, int point, int ranking)
        {
            NickName = nickName;
            nickNameLabel.text = nickName;
            pointLabel.text = point + "p";
            rankingLabel.text = ranking + "#";
        }
    }
}