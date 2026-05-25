using System.Collections;
using _TeamFolder.PYH._02.Scripts.Player;
using UnityEngine;
using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame.PassTheBomb
{
    public class Bomb : MonoBehaviour
    {
        public UnityEvent<int> onTickEvent;
    
        [SerializeField] private int maxTime;
    
        private PassTheBombModule _currentPlayer;
        private Coroutine _timerCoroutine;

        [SerializeField] private float cooldown;
        private float _lastTime;

        public void StartBomb(PassTheBombModule startPlayer)
        {
            Debug.Log(startPlayer.gameObject.name + "에게 부착되어 시작.");
            _currentPlayer = startPlayer;
            _currentPlayer.OnTouchPlayerEvent += SetPlayer;
            transform.position = _currentPlayer.transform.position;
            transform.SetParent(startPlayer.transform);
        
            _lastTime = Time.time;
            
            _timerCoroutine ??= StartCoroutine(BombTimer());
        }
        public void StartTimer()
        {
            _timerCoroutine = StartCoroutine(BombTimer());
        }
        
        private void SetPlayer(PassTheBombModule targetPlayer)
        {
            if (targetPlayer == null) return;
            if (targetPlayer == _currentPlayer) return;
            if (Time.time - _lastTime < cooldown) return;
            
            Debug.Log(targetPlayer.gameObject.name + "부착됨.");
            _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
            _currentPlayer = targetPlayer;
            _currentPlayer.OnTouchPlayerEvent += SetPlayer;
            transform.position = _currentPlayer.transform.position;
            transform.SetParent(targetPlayer.transform);
        
            _lastTime = Time.time;
        }
        private void ExplosionBomb()
        {
            Debug.Log("펑");
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);
        
            _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
            _currentPlayer.onExplosionEvent?.Invoke(_currentPlayer, _currentPlayer.Index);
            _timerCoroutine = null;
            Debug.Log("초기화 완료.");
        }
        public void OnGameEnded() => StopAllCoroutines();

        private IEnumerator BombTimer()
        {
            int leftTime = maxTime;
        
            while (leftTime > 0)
            {
                leftTime -= 1;
                onTickEvent?.Invoke(leftTime);
                yield return new WaitForSeconds(1);
            }

            ExplosionBomb();
        }
    
        private void OnDestroy() => _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
    }
}
