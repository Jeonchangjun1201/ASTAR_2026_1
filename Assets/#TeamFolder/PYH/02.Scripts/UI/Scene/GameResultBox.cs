using TMPro;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.UI
{
    public class GameResultBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private TMP_Text score;
        [SerializeField] private TMP_Text ranking;

        public void Initialize(string nickname, int score, int ranking)
        {
            this.nickName.text = nickname;
            this.score.text = score.ToString() + "p";
            this.ranking.text = ranking.ToString() + "#";
        }
    }
}