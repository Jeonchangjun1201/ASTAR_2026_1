using System.Collections;
using UnityEngine;
using JHJ.Scripts.Test.TestPlayer;
using static JHJItemPacket;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class JHJItemBase : MonoBehaviour
    {
        [Header("아이템 설정")]
        [SerializeField] private ItemType _itemType;
        [SerializeField] private float _duration = 5f;

        private JHJIItemEffect _effect;
        private bool _isRequested = false;

        // 1️⃣ [로컬] 플레이어가 닿았을 때
        private void OnTriggerEnter(Collider other)
        {
            if (_isRequested) return;

            if (other.TryGetComponent(out JHJPlayerController player))
            {
                _isRequested = true;
                SendItemConsumeRequestToServer(player.PlayerIndex);
            }
        }

        // 2️⃣ [서버 통신] "저 이거 먹을래요!" 요청
        private void SendItemConsumeRequestToServer(PlayerIndex pIndex)
        {
            ItemConsumePacket packet = new ItemConsumePacket
            {
                TargetPlayerIndex = pIndex,
                ConsumedItemType = _itemType,
                ItemPosition = transform.position
            };

            // TODO: Photon 같은 서버로 packet 전송
            Debug.Log($"서버로 전송: {pIndex}가 {_itemType} 획득 요청!");
        }

        // 3️⃣ [서버 응답] "그래 먹어라!" 허락 떨어지면 실행 (모든 클라이언트 공통)
        public void ExecuteItemFromServer(JHJPlayerController targetPlayer)
        {
            if (targetPlayer == null) return;

            switch (_itemType)
            {
                case ItemType.MoveSpeed: _effect = new JHJMoveSpeedEffect(); break;
                case ItemType.BrushSize: _effect = new JHJBrushSizeEffect(); break;
                case ItemType.Knockback: _effect = new JHJKnockbackEffect(transform.position); break;
            }

            if (_effect != null)
            {
                StartCoroutine(ItemLifecycleRoutine(targetPlayer));
            }
        }

        // 4️⃣ [실행 연출]
        private IEnumerator ItemLifecycleRoutine(JHJPlayerController player)
        {
            _effect.Apply(player);

            // 먹은 것처럼 보이게 외형 감추기
            if (TryGetComponent(out Collider col)) col.enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }

            if (_itemType != ItemType.Knockback)
            {
                yield return new WaitForSeconds(_duration);
                _effect.Remove(player);
            }

            Destroy(gameObject);
        }
    }
}