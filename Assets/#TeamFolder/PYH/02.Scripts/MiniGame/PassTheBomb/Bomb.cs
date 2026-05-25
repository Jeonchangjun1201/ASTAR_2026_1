using System.Collections;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Player;
using _TeamFolder.PYH._02.Scripts.UI.Event;
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
        
        [SerializeField] private float distanceY;

        [SerializeField] private ParticleSystem prefab;
        private Effecter _curParticl;

        private void Awake()
        {
            _curParticl = Instantiate(prefab).GetComponent<Effecter>();
        }
        private void Start()
        {
            _curParticl.gameObject.SetActive(false);
        }

        public void StartBomb(PassTheBombModule startPlayer)
        {
            Debug.Log(startPlayer.gameObject.name + "에게 부착되어 시작.");
            _currentPlayer = startPlayer;
            _currentPlayer.OnTouchPlayerEvent += SetPlayer;
            transform.position = new Vector3(
                _currentPlayer.transform.position.x,
                _currentPlayer.transform.position.y + distanceY,
                _currentPlayer.transform.position.z);
            transform.SetParent(startPlayer.transform);
        
            _lastTime = Time.time;
            AStarEventBus.Publish(new DigitalClockUiTimeSetEvent((int)_lastTime));
            
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
            transform.position = new Vector3(
                _currentPlayer.transform.position.x,
                _currentPlayer.transform.position.y + distanceY,
                _currentPlayer.transform.position.z);
            transform.SetParent(targetPlayer.transform);
        
            _lastTime = Time.time;
        }
        private void ExplosionBomb()
        {
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            _curParticl.transform.position = new Vector3(
                _currentPlayer.transform.position.x,
                _currentPlayer.transform.position.y + distanceY,
                _currentPlayer.transform.position.z);
            _curParticl.gameObject.SetActive(true);
            _curParticl.ParticleTrigger();

            _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
            _currentPlayer.onExplosionEvent?.Invoke(_currentPlayer, _currentPlayer.Index);
            _timerCoroutine = null;
        }
        public void OnGameEnded() => StopAllCoroutines();

        private IEnumerator BombTimer()
        {
            int leftTime = maxTime;
        
            while (leftTime > 0)
            {
                leftTime -= 1;
                onTickEvent?.Invoke(leftTime);
                AStarEventBus.Publish(new DigitalClockUiTimeSetEvent(leftTime));
                yield return new WaitForSeconds(1);
            }

            ExplosionBomb();
        }
    
        private void OnDestroy() => _currentPlayer.OnTouchPlayerEvent -= SetPlayer;
    }
}
