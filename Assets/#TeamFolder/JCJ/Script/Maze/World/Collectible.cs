using DG.Tweening;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 픽업 아이템. 플레이어와 접촉 시 점수·타이머 보너스 + DOTween 회전·상하 보빙 루프.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private int _scoreReward = 10;
        [SerializeField] private float _timeBonusSeconds = 2f;
        [SerializeField] private float _spinDegPerSec = 180f;
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobSpeed = 2f;

        private bool _collected;
        private Sequence _bobSeq;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Start()
        {
            transform.DORotate(new Vector3(0f, 360f, 0f), 360f / _spinDegPerSec, RotateMode.FastBeyond360)
                     .SetLoops(-1, LoopType.Incremental)
                     .SetEase(Ease.Linear);

            var baseLocal = transform.localPosition;
            _bobSeq = DOTween.Sequence();
            _bobSeq.Append(transform.DOLocalMoveY(baseLocal.y + _bobAmplitude, 1f / _bobSpeed).SetEase(Ease.InOutSine));
            _bobSeq.Append(transform.DOLocalMoveY(baseLocal.y - _bobAmplitude, 1f / _bobSpeed).SetEase(Ease.InOutSine));
            _bobSeq.SetLoops(-1);
        }

        private void OnDestroy()
        {
            _bobSeq?.Kill();
            transform.DOKill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag(_playerTag)) return;

            var gsm = GameStateManager.Instance;
            if (gsm == null) return;
            if (gsm.CurrentState != GameState.Playing) return;

            _collected = true;

            gsm.Score?.Add(other.name, _scoreReward);

            if (_timeBonusSeconds > 0 && gsm.Timer != null)
                gsm.Timer.AddTime(_timeBonusSeconds);

            var pc = other.GetComponent<PlayerController>();
            pc?.NotifyCollected();

            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
                     .OnComplete(() => Destroy(gameObject));
        }
    }
}
