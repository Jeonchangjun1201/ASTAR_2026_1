using UnityEngine; // GameObject, Renderer, LayerMask 등을 사용한다.
using _TeamFolder.JCJ.Battle; // BattleFirstPersonCamera 정적 Instance를 참조한다.

namespace _TeamFolder.JCJ.Battle.Session // 카메라 모드에 맞춰 바디 레이어를 맞추는 헬퍼다.
{
    public static class BattlePlayerViewLayers // 메서드만 있는 정적 클래스로 특정 플레이어 루트를 처리한다.
    {
        public static void ApplyLocalThirdPersonBodyLayersToPlayer(GameObject playerRoot) // 3인칭에서는 모든 메시를 기본 레이어로 둔다.
        {
            if (playerRoot == null) return; // 처리할 대상이 없으면 즉시 종료한다.
            int defaultLayer = 0; // 유니티 기본 레이어 인덱스이다.
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true); // 비활성 자식까지 포함해 모든 렌더러를 가져온다.
            for (int i = 0; i < renderers.Length; i++) // 배열을 순회한다.
            {
                var r = renderers[i]; // 현재 렌더러 참조이다.
                if (r == null) continue; // 파괴된 항목은 건너뛴다.
                if (r is LineRenderer) continue; // 디버그 라인은 레이어 건드리지 않는다.
                r.gameObject.layer = defaultLayer; // 게임오브젝트 레이어 번호를 기본으로 설정한다.
            }
        }

        public static void ApplyLocalFirstPersonBodyLayersToPlayer(GameObject playerRoot) // 1인칭에서는 몸 메시를 BattleLocalBody로 숨긴다.
        {
            if (playerRoot == null) return; // 루트가 없으면 아무 것도 하지 않는다.
            int lb = LayerMask.NameToLayer("BattleLocalBody"); // 프로젝트에 정의된 커스텀 레이어 이름으로 번호를 조회한다.
            if (lb < 0) return; // 레이어가 없으면 조용히 빠져나간다.
            Transform camT = null; // 카메라 트랜스폼 캐시 변수이다.
            var fpc = BattleFirstPersonCamera.Instance; // 씬에 존재하는 싱글톤 FPS 카메라를 찾는다.
            if (fpc != null) camT = fpc.transform; // 카메라가 있으면 그 트랜스폼을 저장한다.
            Transform weaponMount = playerRoot.transform.Find("WeaponMount"); // 무기가 붙는 자식 트랜스폼을 이름으로 찾는다.
            int defaultLayer = 0; // 무기와 카메라에 해당하는 메시는 다시 기본 레이어를 쓴다.
            var renderers = playerRoot.GetComponentsInChildren<Renderer>(true); // 역시 비활성 포함 전체 렌더러이다.
            for (int i = 0; i < renderers.Length; i++) // 같은 순회 패턴이다.
            {
                var r = renderers[i]; // 현재 렌더러이다.
                if (r == null) continue; // null 방어이다.
                if (r is LineRenderer) continue; // 라인 렌더러 제외이다.
                if (camT != null && (r.transform == camT || r.transform.IsChildOf(camT))) continue; // 카메라 메시는 레이어를 바꾸지 않는다.
                if (weaponMount != null && (r.transform == weaponMount || r.transform.IsChildOf(weaponMount))) // 무기 하위는 보이게 기본 레이어로 둔다.
                {
                    r.gameObject.layer = defaultLayer; // 무기 계층 메시 레이어를 설정한다.
                    continue; // 아래 로컬 바디 레이어 설정을 건너뛴다.
                }

                r.gameObject.layer = lb; // 나머지 몸 메시는 로컬 전용 레이어로 보내 카메라 컬링과 맞춘다.
            }
        }
    }
}
