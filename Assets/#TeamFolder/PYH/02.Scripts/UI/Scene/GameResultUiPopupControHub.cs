using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.MiniGame.UI;
using DG.Tweening;
using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI
{
    public class GameResultUiPopupControlHub : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private RectTransform point;

        [Header("Prefab")]
        [SerializeField] private GameResultBox prefab;

        [Header("Animation")]
        [SerializeField] private float popScale = 1.25f;
        [SerializeField] private float popDuration = 0.25f;
        [SerializeField] private float returnDuration = 0.15f;
        [SerializeField] private float delayBetween = 0.1f;

        private RectTransform[] points;
        private GameResultBox[] boxes;
        private Sequence _seq;

        private void Awake()
        {
            points = new RectTransform[4];
            boxes = new GameResultBox[4];

            for (int i = 0; i < points.Length; i++)
            {
                GameObject pointObj = new GameObject($"Result Point {i + 1}", typeof(RectTransform));
                RectTransform pointTrans = pointObj.GetComponent<RectTransform>();

                pointTrans.SetParent(point, false);
                pointTrans.localScale = Vector3.one;

                points[i] = pointTrans;

                GameResultBox box = Instantiate(prefab, pointTrans);
                box.transform.localScale = Vector3.zero;

                boxes[i] = box;
            }

            AStarEventBus.Subscribe<GameResultUiEvent>(Result);
        }

        public void Result(GameResultUiEvent @event)
        {
            _seq?.Kill();

            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].transform.localScale = Vector3.zero;

                boxes[i].Initialize(
                    @event.Results[i].nickname,
                    @event.Results[i].score,
                    i + 1
                );
            }

            _seq = DOTween.Sequence();

            for (int i = 0; i < boxes.Length; i++)
            {
                Transform target = boxes[i].transform;

                _seq.Append(target
                    .DOScale(popScale, popDuration)
                    .SetEase(Ease.OutBack));

                _seq.Append(target
                    .DOScale(1f, returnDuration)
                    .SetEase(Ease.OutSine));

                _seq.AppendInterval(delayBetween);
            }
        }

        private void OnDestroy()
        {
            _seq?.Kill();
            AStarEventBus.Unsubscribe<GameResultUiEvent>(Result);
        }
    }
}