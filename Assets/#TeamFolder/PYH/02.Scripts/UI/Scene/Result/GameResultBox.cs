using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class GameResultBox : MonoBehaviour
    {
        [SerializeField] private Sprite[] icons;

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private TMP_Text score;
        [SerializeField] private TMP_Text ranking;

        public void Initialize(int index, string nickname, int score, int ranking)
        {
            icon.sprite = icons[index];
            this.nickName.text = nickname;
            this.score.text = score.ToString() + "p";
            this.ranking.text = ranking.ToString() + "#";
        }
    }
}