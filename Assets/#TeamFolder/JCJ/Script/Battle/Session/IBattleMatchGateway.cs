using System; // Action 대리자 타입을 쓰기 위해 포함한다.
using UnityEngine; // Vector3를 메서드 인자로 쓴다.

namespace _TeamFolder.JCJ.Battle.Session // 배틀 매치 게이트웨이 계약만 분리해 두는 네임스페이스다.
{
    public interface IBattleMatchGateway // BattlePrototypeManager가 구현하며 서버 붙는 코드는 BattleMatchRegistry.Match로만 접근하면 된다.
    {
        event Action MatchSetupRequested; // 서버 권한 모드에서 씬 셸 준비가 끝난 뒤 발생한다. 여기서 서버 RPC 호출을 걸면 된다.

        event Action<string> RespawnRequested; // 피해자 PlayerId만 알려 준다. 좌표는 서버가 ApplyAuthoritativeRespawn으로 밀어 넣는 흐름과 짝이다.

        void ApplyAuthoritativeMatchSetup(int[] playerRanks, int[] playerTeamIndices); // 서버가 확정한 랭크와 팀을 배열로 넘긴다. 길이는 플레이어 수와 같아야 한다.

        void ApplyAuthoritativeRespawn(string playerId, Vector3 respawnPosition); // 서버가 고른 월드 좌표로 해당 PlayerId 오브젝트를 즉시 옮긴다.

        void StartMatchPresentation(); // 무기 연출과 카운트다운 코루틴을 시작한다. 슬롯이 채워진 뒤 호출하는 것이 안전하다.
    }
}
