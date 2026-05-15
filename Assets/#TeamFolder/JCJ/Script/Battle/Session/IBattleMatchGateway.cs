using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Battle.Session
{
    // BattlePrototypeManager가 구현한다. 네트워크 매니저는 BattleMatchRegistry.Match로 캐스팅해 구독·호출하면 BattlePrototypeScene과 결합도를 낮출 수 있다.
    public interface IBattleMatchGateway
    {
        // JcjRuntimeAuthority가 ServerAuthoritative일 때 BattlePrototypeManager.Start가 씬 셸만 준비한 뒤 발생시킨다. 여기서 서버에 매치 참가·스폰 요청을 내면 된다.
        event Action MatchSetupRequested;

        // 서버 권한 데스 시 플레이어Id 전달. 클라는 서버가 내려준 리스폰 패킷에 맞춰 ApplyAuthoritativeRespawn을 호출하는 식으로 연결하면 된다.
        event Action<string> RespawnRequested;

        // 서버가 확정한 랭크·팀 배열. 슬롯이 없으면 SpawnPlayers까지 수행한다. 팀만 바꿀 때는 배열 길이를 플레이어 수와 맞출 것.
        void ApplyAuthoritativeMatchSetup(int[] playerRanks, int[] playerTeamIndices);

        // 서버가 확정한 월드 좌표로 즉시 리스폰. 로컬 코루틴 리스폰과 중복되지 않게 모드별로 한 경로만 사용하는 것이 좋다.
        void ApplyAuthoritativeRespawn(string playerId, Vector3 respawnPosition);

        // 인트로 코루틴(BeginMatchRoutine) 시작. ApplyAuthoritativeMatchSetup으로 슬롯이 채워진 뒤 호출하는 흐름이 자연스럽다.
        void StartMatchPresentation();
    }
}
