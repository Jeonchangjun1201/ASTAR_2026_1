using System.Collections;
using PYH.Player;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame.PassTheBomb
{
    public class Bomb : PlayerModuleBase
    {
        private Player _owner;
        public UnityEvent<int> onTickEvent;
    
        [SerializeField] private int maxTime;
        private int _leftTime;
    
        private Player _currentPlayer;
        private Coroutine _timerCoroutine;
    
        public override void Initialize(Player player)
        {
            _owner = player;
        
            _owner.OnTouchPlayerEvent += SetPlayer;
        }

        public void StartBomb() => _timerCoroutine ??= StartCoroutine(BombTimer());
        public void SetPlayer(Player targetPlayer)
        {
            _currentPlayer = targetPlayer;
            transform.position = _currentPlayer.transform.position;
            transform.SetParent(targetPlayer.transform);
        
            _timerCoroutine ??= StartCoroutine(BombTimer());
        }
        private void ExplosionBomb()
        {
            StopCoroutine(_timerCoroutine);
        
            _currentPlayer.OnExplosionEvent?.Invoke(_currentPlayer, _currentPlayer.index);
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
    
        private void OnDestroy() => _owner.OnTouchPlayerEvent -= SetPlayer;
    }
}
