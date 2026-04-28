using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 게임 전체 수치 설정. ScriptableObject로 인스펙터에서 조정 가능.
    /// 생성 경로: Assets → Create → TileGame → GameConfig
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "TileGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("─ 라운드 흐름 ─────────────────────")]
        [Tooltip("라운드 전 카운트다운 길이(초, 3-2-1-GO).")]
        [Range(0, 10)] public int countdownSeconds = 4;
        [Tooltip("GO! 이후 라운드 전체 시간(초).")]
        [Range(15f, 240f)] public float roundDuration = 90f;
        [Tooltip("플레이어당 목숨. 1이면 한 번 떨어지면 아웃(기본). 관대 모드만 올림.")]
        [Range(1, 9)] public int playerLives = 1;
        [Tooltip("플레이어 프리팹 전체에 곱하는 스케일(0.5 = 절반 크기).")]
        [Range(0.2f, 2f)] public float playerScale = 0.4f;
        [Tooltip("리스폰 후 무적 시간(초).")]
        [Range(0f, 3f)] public float respawnInvuln = 1.5f;
        [Tooltip("낙하 후 리스폰까지 지연(초).")]
        [Range(0.2f, 5f)] public float respawnDelay = 1.2f;

        [Header("─ 컬러콜 이벤트 ───────────────────")]
        [Tooltip("GO! 이후 첫 컬러콜까지 시간(초).")]
        [Range(5f, 60f)] public float colorCallFirstDelay = 18f;
        [Tooltip("컬러콜 사이 간격(초).")]
        [Range(8f, 60f)] public float colorCallInterval = 18f;
        [Tooltip("공지 배너가 유지되는 시간(초) — 이후 타일 낙하.")]
        [Range(1f, 6f)] public float colorCallWarnDuration = 2.0f;
        [Tooltip("최상 생존 층 타일이 이 개수 미만이면 컬러콜 생략.")]
        [Range(0, 80)] public int colorCallMinTiles = 6;

        [Header("─ 점수 ───────────────────────────")]
        [Tooltip("생존 초당 점수.")]
        public int scorePerSecondAlive = 1;
        [Tooltip("컬러콜 한 번 버틸 때 점수.")]
        public int scorePerColorCallSurvived = 50;
        [Tooltip("최후 생존자 보너스.")]
        public int scoreLastSurvivor = 200;
        [Tooltip("타이머 종료까지 살아 있을 때 보너스.")]
        public int scoreTimerSurvivor = 100;

        [Header("─ 육각 타일 기하 ─────────────────")]
        [Tooltip("육각 타일의 외접원 반지름(중심→꼭짓점). 가로폭 = 2*radius.")]
        [Range(0.3f, 3f)] public float hexRadius = 1.2f;
        [Tooltip("육각 타일 프리즘 두께 (Y).")]
        [Range(0.1f, 1f)] public float hexHeight = 0.55f;
        [Tooltip("허니컴 간 여백 비율. 0 = 딱 붙음, 0.02~0.05 = 타일 사이에 얇은 틈.")]
        [Range(0f, 0.3f)] public float hexGap = 0f;

        [Header("─ 레이어 간격 (선택, 인스펙터 yPosition 덮어쓰기) ─")]
        [Tooltip("켜면 TileBoard.layers[].yPosition 대신 layerVerticalSpacing 으로 자동 계산.")]
        public bool useLayerVerticalSpacing = true;
        [Tooltip("층 간 Y 간격(유닛). 가장 위층 = (layerCount-1) * spacing, 가장 아래층 = 0.")]
        [Range(1f, 12f)] public float layerVerticalSpacing = 6f;

        [Header("─ Base Tile ────────────────────────")]
        [Tooltip("밟은 후 낙하 경고 시작까지 대기 (초) — 호환성용 기본값. TileFactory가 색상별 분기로 덮어쓴다.")]
        public float stepDelay    = 3.5f;
        [Tooltip("Web/Confusion 등 '어쩔수없음' 기믹용 stepDelay (초) — 길게 유지해 회피 시간 보장.")]
        public float stepDelayWeb = 3.5f;
        [Tooltip("일반/일반기믹 타일용 stepDelay (초) — 빠르게 단축해 긴장감 부여.")]
        public float stepDelayDefault = 1.2f;
        [Tooltip("경고 깜빡임 지속 시간 (초)")]
        public float warnDuration = 0.7f;
        [Tooltip("낙하 애니메이션 소요 시간 (초)")]
        public float fallDuration = 1.0f;
        [Tooltip("낙하 거리 (유닛) — 호환성용. tileFadeOutEnabled가 켜져 있으면 tileFallShortDistance가 우선.")]
        public float fallDistance = 15.0f;
        [Tooltip("페이드 아웃 + 짧은 낙하 사용 여부.")]
        public bool tileFadeOutEnabled = true;
        [Tooltip("페이드 아웃 시 짧은 낙하 거리 (유닛). 추천 2~3.")]
        [Range(0.5f, 6f)] public float tileFallShortDistance = 2.5f;

        [Header("─ 폭탄 기믹 ───────────────────────")]
        [Tooltip("밟은 후 폭발까지 대기 (초)")]
        public float bombDelay  = 2.0f;
        [Tooltip("폭발 반경 (유닛 / 1~3블럭)")]
        [Range(1f, 4f)] public float bombRadius = 2.5f;

        [Header("─ 거미줄 기믹 ─────────────────────")]
        [Tooltip("이동 제한 지속 시간 (초)")]
        public float webDuration   = 3.0f;
        [Tooltip("이동 속도 비율 (0.1 = 10%)")]
        [Range(0.05f, 0.5f)] public float webSpeedRatio = 0.15f;

        [Header("─ 풍선 기믹 ───────────────────────")]
        [Tooltip("부유 힘 (ForceMode.Force per FixedUpdate)")]
        public float balloonForce    = 12.0f;
        [Tooltip("부유 지속 시간 (초)")]
        public float balloonDuration = 1.2f;

        [Header("─ 트램폴린 기믹 ───────────────────")]
        [Tooltip("점프 충격 힘 (Impulse)")]
        public float trampolineForce      = 18.0f;
        [Tooltip("타일이 버티는 최대 밟기 횟수")]
        public int   trampolineMaxBounces = 3;

        [Header("─ 혼란 기믹 ───────────────────────")]
        [Tooltip("조작 반전 지속 시간 (초)")]
        public float confusionDuration = 4.0f;
    }
}
