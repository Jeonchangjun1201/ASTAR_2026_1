using System.Collections;
using PYH.Player;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame.PassTheBomb
{
    public class Bomb : MonoBehaviour
    {
        public UnityEvent<int> onTickEvent;
    
        [SerializeField] private int maxTime;
        private int _leftTime;
    
        private Player _currentPlayer;
        private Coroutine _timerCoroutine;

        [SerializeField] private float cooldown;
        private float _lastTime;

        public void StartBomb()
        {
            _timerCoroutine ??= StartCoroutine(BombTimer());
        }
        public void SetPlayer(Player targetPlayer)
        {
            Debug.Log("SetPlayer " + targetPlayer.gameObject.name);
            
            if (Time.time - _lastTime < cooldown)
            {
                Debug.Log("returned");
                return;
            }
            
            _currentPlayer = targetPlayer;
            _currentPlayer.OnTouchPlayerEvent += SetPlayer;
            transform.position = _currentPlayer.transform.position;
            transform.SetParent(targetPlayer.transform);
        
            _lastTime = Time.time;
            _timerCoroutine ??= StartCoroutine(BombTimer());
        }
        private void ExplosionBomb()
        {
            StopCoroutine(_timerCoroutine);
        
            _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
            _currentPlayer.onExplosionEvent?.Invoke(_currentPlayer, _currentPlayer.index);
            _currentPlayer = null;
        }
        public void OnGameEnded() => StopAllCoroutines();

        private IEnumerator BombTimer()
        {
            _leftTime = maxTime;
        
            while (_leftTime != 0)
            {
                _leftTime -= 1;
                onTickEvent?.Invoke(_leftTime);
                yield return new WaitForSeconds(1);
            }

            ExplosionBomb();
        }
    
        private void OnDestroy() => _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
    }
}
