using _TeamFolder.PYH._02.Scripts.UI.Deco;
using csiimnida.CSILib.SoundManager.RunTime;
using DG.Tweening;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class TitleUiControlHub : MonoBehaviour
    {
        [Header("All Buttons")]
        [SerializeField] private DecoButton[] buttons;

        [SerializeField] private RectTransform logo;
        private Vector2 originPos;
        
        [SerializeField] private float differAmount;
        [SerializeField] private float[] durations;
        
        [Header("Star-Deco")]
        [SerializeField] private RectTransform starDeco;
        [SerializeField] private float rotDuration;

        private void Awake()
        {
            SoundManager.Instance.PlaySound("Title_OST_1");

            starDeco
                .DORotate(new Vector3(0, 0, -360f), rotDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
            
            originPos = logo.anchoredPosition;
        }
        private void Start()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].PlayMove(differAmount, durations[i]);
            }

            logo
                .DOAnchorPosY(originPos.y + differAmount, durations[4])
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(durations[4] / 2);
        }
    }
}
