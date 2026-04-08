using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PYH.Player
{
    public class GolfClub : PlayerModuleBase
    {
        private Player _owner;
        [SerializeField] private float _maxPower;
        [SerializeField] private float _powerMultpler;

        [SerializeField] private float _perPower;

        private Coroutine _swingCoroutine;
        private bool _isSwing;

        public override void Initialize(Player player)
        {
            _owner = player;
        }

        private void Update()
        {
            if (Mouse.current.leftButton.isPressed && !_isSwing)
            {
                _perPower = Mathf.Clamp((_perPower + (1 * _powerMultpler) * Time.deltaTime), 0, 100);
            }
            else if (!Mouse.current.leftButton.isPressed && _isSwing)
            {
                _swingCoroutine = StartCoroutine(SwingHitbox());
            }
        }

        private IEnumerator SwingHitbox()
        {
            _isSwing = true;
            Debug.Log("Swing!");

            for (int i = 0; i < 5; i++)
            {
                // 플레이어가 바라보는 방향에서 N좌표 앞 만큼 오버랩 박스 감지

                // 만약 감지 성공 시 코루틴을 멈추고 해당 감지된 플레이어들의 리스트를
                // 가져와 함수 호출
                // SwingPlayers
                yield return new WaitForSeconds(0.75f);
            }

            _perPower = 0;
            _isSwing = false;
        }

        private void SwingPlayers(Player[] players)
        {
            _swingCoroutine = null;

            foreach (Player player in players)
            {
                player.Push(Vector3.zero, (_maxPower / 100) * _perPower);
            }

            _perPower = 0;
            _isSwing = false;
        }

        private void OnDrawGizmos()
        {
            
        }
    }

}
