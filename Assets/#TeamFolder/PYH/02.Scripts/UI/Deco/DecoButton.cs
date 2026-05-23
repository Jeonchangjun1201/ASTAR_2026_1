using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _TeamFolder.PYH._02.Scripts.UI.Deco
{
    public class DecoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _transform;
        [SerializeField] private Vector3 onPointerScale;
        [SerializeField] private float onPointerDuration;
        private Sequence moveSeq, onPointerSeq;

        private Vector2 _originPos;
        private Vector3 _originScale;

        private void Awake()
        {
            _transform = GetComponentInChildren<RectTransform>();
            _originPos = _transform.anchoredPosition;
            _originScale = _transform.localScale;
        }
        public void PlayMove(float differAmount, float delay)
        {
            ClearSequence(ref moveSeq);

            moveSeq = DOTween.Sequence();

            moveSeq.Append(
                _transform
                    .DOAnchorPosY(_originPos.y + differAmount, delay)
                    .SetEase(Ease.InOutSine)
            );

            moveSeq.SetDelay(delay / 2f);
            moveSeq.SetLoops(-1, LoopType.Yoyo);
        }
    
        public void OnPointerEnter(PointerEventData eventData)
        {
            ClearSequence(ref onPointerSeq);
            
            onPointerSeq = DOTween.Sequence();
            onPointerSeq.Append(_transform.DOScale(onPointerScale, onPointerDuration));
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            ClearSequence(ref onPointerSeq);
            
            onPointerSeq = DOTween.Sequence();
            onPointerSeq.Append(_transform.DOScale(_originScale, onPointerDuration));
        }

        private static void ClearSequence(ref Sequence targetSequence)
        {
            targetSequence?.Kill();
            targetSequence = null;
        }
    }
}
