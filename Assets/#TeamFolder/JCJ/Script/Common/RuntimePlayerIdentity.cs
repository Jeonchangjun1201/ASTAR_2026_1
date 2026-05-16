using UnityEngine; // GameObject, MonoBehaviour, Component API를 사용한다.

namespace _TeamFolder.JCJ.Script // JcjRuntimeAuthority와 같은 JCJ 공용 네임스페이스다.
{
    public sealed class RuntimePlayerIdentity : MonoBehaviour // 플레이어 인스턴스마다 붙여 네트워크 세션에서 재사용할 고유 문자열 Id를 실어둔다.
    {
        [SerializeField] private string _playerId; // 서버 세션의 user id 또는 net id 문자열과 매핑하면 된다.
        [SerializeField] private string _displayName; // HUD 등에 보여 줄 표시 이름. 비우면 게임오브젝트 이름으로 대체된다.
        [SerializeField] private int _slotIndex = -1; // 매치 내 좌석 번호. Configure 호출 전까지는 미설정 의미로 -1이다.
        [SerializeField] private bool _isLocalOwned; // 이 본체가 로컬 입력 소유인지 표시한다.

        public string PlayerId => _playerId; // 외부가 서버 식별자를 읽을 때 사용한다.
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName; // 비어 있으면 안전하게 오브젝트 이름을 돌려준다.
        public int SlotIndex => _slotIndex; // 리더보드 정렬이나 스폰 슬롯과 짝을 맞출 때 쓴다.
        public bool IsLocalOwned => _isLocalOwned; // 카메라·입력 붙일 대상인지 판별한다.

        public void Configure(string playerId, string displayName, int slotIndex, bool isLocalOwned) // 스폰 직후 서버 값으로 한 번에 채운다.
        {
            _playerId = string.IsNullOrWhiteSpace(playerId) ? gameObject.name : playerId.Trim(); // 공백만 오면 오브젝트 이름으로 대체한다.
            _displayName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim(); // 표시 이름도 동일 규칙으로 정규화한다.
            _slotIndex = slotIndex; // 슬롯 인덱스를 저장한다.
            _isLocalOwned = isLocalOwned; // 로컬 소유 여부를 저장한다.
        }

        public void SetLocalOwned(bool isLocalOwned) // 소유권만 바꿀 때 호출한다.
        {
            _isLocalOwned = isLocalOwned; // 플래그만 덮어쓴다.
        }

        public static RuntimePlayerIdentity Ensure(GameObject target) // 컴포넌트가 없으면 추가하고 반환한다.
        {
            if (target == null) return null; // 대상이 없으면 아무 것도 하지 않는다.
            var identity = target.GetComponent<RuntimePlayerIdentity>(); // 이미 붙어 있는지 찾는다.
            if (identity == null) identity = target.AddComponent<RuntimePlayerIdentity>(); // 없으면 런타임에 붙인다.
            return identity; // 항상 유효한 참조를 돌려준다(null 아님이면).
        }

        public static RuntimePlayerIdentity Find(Component source) // 자식 콜라이더 등에서 부모 쪽 Identity를 찾을 때 쓴다.
        {
            if (source == null) return null; // 소스가 없으면 null이다.
            return source.GetComponentInParent<RuntimePlayerIdentity>(); // 부모 체인을 따라 올라가며 첫 컴포넌트를 반환한다.
        }

        public static bool TryResolve(Component source, out string playerId, out string displayName) // 서버 연동 없이 빠르게 id 문자열을 얻고 싶을 때 쓴다.
        {
            var identity = Find(source); // 동일한 탐색 규칙을 재사용한다.
            if (identity == null) // Identity가 없으면 실패 경로로 간다.
            {
                playerId = source != null ? source.gameObject.name : string.Empty; // 최소한 오브젝트 이름으로 채운다.
                displayName = source != null ? source.gameObject.name : string.Empty; // 표시 이름도 동일하게 둔다.
                return false; // 공식 Id가 없었다고 표시한다.
            }

            playerId = identity.PlayerId; // 확보한 컴포넌트에서 PlayerId를 꺼낸다.
            displayName = identity.DisplayName; // DisplayName도 함께 돌려준다.
            return true; // 정상적으로 Identity를 찾았다.
        }
    }
}
