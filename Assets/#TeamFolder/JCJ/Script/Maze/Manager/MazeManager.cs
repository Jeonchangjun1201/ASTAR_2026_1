using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로를 만들 때 사용할 절차적 생성 알고리즘 종류.
    /// </summary>
    public enum AlgorithmType { Prim, DFS, Sidewinder }

    /// <summary>
    /// 세션 설정용 미리 맞춘 미로 크기(라운드 타이머와 밸런스).
    /// Easy — 25×25, 셀 3m / Medium — 35×35 / Hard — 45×45 / Custom — 인스펙터 값 그대로.
    /// </summary>
    public enum DifficultyPreset { Easy, Medium, Hard, Custom }

    /// <summary>
    /// 미로 데이터 소유·전문 서비스 조율·게임 상태 전환을 담당하는 오케스트레이터.
    /// 벽/골/플레이어/코인/데코/카메라/미니맵 등은 각 인터페이스에 위임(SRP).
    /// </summary>
    public class MazeManager : MonoBehaviour
    {
        public static MazeManager Instance { get; private set; }

        [Header("미로 데이터")]
        [Tooltip("Easy/Medium/Hard는 Awake에서 가로·세로 덮어씀. Custom만 아래 값 그대로.")]
        [SerializeField] private DifficultyPreset _difficulty = DifficultyPreset.Medium;
        [Tooltip("배열 칸 가로(벽+길 포함). 미니맵 1픽셀=1칸. 월드 가로 ≈ Width×CellSize(m).")]
        [SerializeField] private int _width = 41;
        [Tooltip("배열 칸 세로(벽+길). 월드 깊이 ≈ Height×CellSize(m).")]
        [SerializeField] private int _height = 41;
        [Tooltip("한 칸당 미터. 값이 작으면 같은 Width도 월드에서 더 작아 보임.")]
        [SerializeField] private float _cellSize = 3.0f;
        [SerializeField] private int _playerCount = 1;
        // 서버 연동 전 관전 흐름을 로컬에서 테스트하기 위한 옵션이다.
        // true면 인스펙터의 플레이어 수가 1이어도 최소 2명을 스폰해서 골인 후 카메라 관전을 확인할 수 있다.
        [SerializeField] private bool _spectatorTestMode = true;
        [SerializeField] private int _seed = 0;     // 0 = 랜덤
        [SerializeField] private AlgorithmType _algorithmType = AlgorithmType.Prim;

        [Header("Asset References")]
        [SerializeField] private GameObject _wallPrefab;
        [SerializeField] private GameObject _goalPrefab;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _coinPrefab;

        [Header("오디오 / 점수")]
        [SerializeField] private GoalAudioSettings _goalAudioSettings;
        [SerializeField] private RankService _rankService;

        [Header("비주얼")]
        [Tooltip("골 비콘 색. 차가운 단색 씬에서 랜드마크로 읽히게 따뜻한 오프화이트.")]
        [SerializeField] private Color _beaconColor = new(1.00f, 0.92f, 0.72f, 1f);
        [SerializeField] private bool _buildMinimap = true;
        [SerializeField] private bool _buildFollowCamera = true;
        [Tooltip("시네마틱 앰비언스(URP Volume·조명·림라이트) 켜기.")]
        [SerializeField] private bool _buildAmbience = true;
        [Tooltip("미로에 상자·통·횃불 등 데코 랜덤 배치.")]
        [SerializeField] private bool _spawnDecor = false;

        [Header("서비스(선택 — 비어 있으면 자동 생성)")]
        [SerializeField] private MazeWallRenderer _wallRenderer;
        [SerializeField] private MazeGoalSpawner _goalSpawner;
        [SerializeField] private MazePlayerSpawner _playerSpawner;
        [SerializeField] private MazeCoinSpawner _coinSpawner;
        [SerializeField] private MazeDecorSpawner _decorSpawner;
        [SerializeField] private MazeFloorBuilder _floorBuilder;
        [SerializeField] private PlayerFollowCameraService _cameraService;
        [SerializeField] private MazeAmbience _ambience;
        [Tooltip("줍으면 스프린트 게이지를 채우는 발광 스태미나 오브 배치.")]
        [SerializeField] private bool _spawnStaminaOrbs = true;
        [SerializeField] private MazeStaminaOrbSpawner _staminaOrbSpawner;
        [Tooltip("절차적 SFX + 앰비언트 뮤직 베드 구동.")]
        [SerializeField] private bool _buildAudio = true;
        [SerializeField] private MazeAudio _audio;
        [Tooltip("골 통과 시 플래시·컨페티·슬로모 등 피니시 FX.")]
        [SerializeField] private bool _buildFinishFX = true;
        [SerializeField] private GoalFinishFX _finishFX;

        private int[,] _mazeData;
        private readonly List<GameObject> _spawnedObjects = new();
        private readonly List<GameObject> _playerInstances = new();
        private Vector2Int _goalCell;
        private Transform _primaryPlayer;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public int[,] MazeData => _mazeData;
        public Vector2Int GoalCell => _goalCell;
        public IReadOnlyList<GameObject> Players => _playerInstances;

        // ───────── Lifecycle ─────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 씬에 MazeManager 중복(리로드 잔여 등) — 첫 번째만 남기고 나머지 파괴(스폰·서비스 충돌 방지).
                Debug.LogWarning("[MazeManager] Duplicate instance detected — destroying extra.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ApplyDifficultyPreset();
            ResolveServices();
            EnsureSceneHud();
        }

        private void ApplyDifficultyPreset()
        {
            switch (_difficulty)
            {
                case DifficultyPreset.Easy:
                    _width = 25; _height = 25; _cellSize = 3.0f; break;
                case DifficultyPreset.Medium:
                    _width = 35; _height = 35; _cellSize = 3.0f; break;
                case DifficultyPreset.Hard:
                    _width = 45; _height = 45; _cellSize = 3.0f; break;
                case DifficultyPreset.Custom:
                default: break;
            }
            // 미로 알고리즘은 벽/통로 격자 패턴을 위해 홀수 크기가 필요하다.
            if ((_width  & 1) == 0) _width  += 1;
            if ((_height & 1) == 0) _height += 1;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ResolveServices()
        {
            _wallRenderer   ??= SceneComponentResolver.GetOrAdd<MazeWallRenderer>(this);
            _goalSpawner    ??= SceneComponentResolver.GetOrAdd<MazeGoalSpawner>(this);
            _playerSpawner  ??= SceneComponentResolver.GetOrAdd<MazePlayerSpawner>(this);
            _coinSpawner    ??= SceneComponentResolver.GetOrAdd<MazeCoinSpawner>(this);
            if (_spawnDecor) _decorSpawner ??= SceneComponentResolver.GetOrAdd<MazeDecorSpawner>(this);
            _floorBuilder   ??= SceneComponentResolver.GetOrAdd<MazeFloorBuilder>(this);
            if (_buildFollowCamera)  _cameraService     ??= SceneComponentResolver.GetOrAdd<PlayerFollowCameraService>(this);
            if (_buildAmbience)      _ambience          ??= SceneComponentResolver.GetOrAdd<MazeAmbience>(this);
            if (_spawnStaminaOrbs)   _staminaOrbSpawner ??= SceneComponentResolver.GetOrAdd<MazeStaminaOrbSpawner>(this);
            if (_buildAudio)         _audio             ??= SceneComponentResolver.GetOrAdd<MazeAudio>(this);
            if (_buildFinishFX)      _finishFX          ??= SceneComponentResolver.GetOrAdd<GoalFinishFX>(this);
        }

        private static void EnsureSceneHud()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("HUD (auto)");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<UnityEngine.UI.CanvasScaler>();
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            if (Object.FindFirstObjectByType<GameHUD>() == null)
                canvas.gameObject.AddComponent<GameHUD>();

            if (Object.FindFirstObjectByType<PodiumPresenter>() == null)
                canvas.gameObject.AddComponent<PodiumPresenter>();
        }

        // ───────── Public API ─────────
        public void GenerateMazeWithButton()
        {
            var gsm = GameStateManager.Instance;
            // 새 라운드 생성 전에 반드시 게임 상태와 랭킹을 먼저 초기화한다.
            // 순서가 반대면 이전 라운드의 골인자/포디움 상태가 새 플레이어 이름과 충돌할 수 있다.
            if (gsm != null) gsm.ResetToWaiting();
            ResetRoundHelpers();
            CreateMaze(_algorithmType, ResolvePlayerCount());
            if (gsm != null) gsm.StartGame();
        }

        public void CreateMaze(AlgorithmType type, int playerCount)
        {
            // 미로, 벽, 골, 플레이어, 아이템은 모두 _spawnedObjects에 등록된다.
            // Play Again 또는 난이도 변경 시 ClearSpawned가 한 번에 정리하기 위한 소유권 목록이다.
            ClearSpawned();

            // 현재는 로컬 랜덤 시드다.
            // 서버를 붙이면 이 seed는 서버/호스트가 정하고 모든 클라이언트가 같은 seed로 GenerateMazeData를 실행해야 한다.
            int seed = _seed == 0 ? Random.Range(1, int.MaxValue) : _seed;
            _mazeData = GenerateMazeData(type, seed);
            _goalCell = new Vector2Int(_width - 2, _height - 2);

            var container = new GameObject("MazeContainer");
            _spawnedObjects.Add(container);

            _floorBuilder?.Build(_width, _height, _cellSize, container.transform);
            _wallRenderer?.Render(_mazeData, _cellSize, _wallPrefab, container.transform);

            var goal = _goalSpawner?.Spawn(_goalCell, _cellSize, _goalPrefab, _goalAudioSettings,
                                           ResolveRankService(), _beaconColor, container.transform);
            if (goal != null) _spawnedObjects.Add(goal);

            var players = _playerSpawner?.Spawn(_mazeData, _cellSize, playerCount, _playerPrefab, null);
            if (players != null)
            {
                foreach (var p in players) { _spawnedObjects.Add(p); _playerInstances.Add(p); }
                if (_playerInstances.Count > 0) _primaryPlayer = _playerInstances[0].transform;
            }

            var occupied = BuildOccupiedSet();

            var coinRoot = new GameObject("MazeCoins");
            _spawnedObjects.Add(coinRoot);
            _coinSpawner?.Spawn(_mazeData, _cellSize, _goalCell, occupied, _coinPrefab, coinRoot.transform);

            if (_spawnStaminaOrbs && _staminaOrbSpawner != null)
            {
                var orbRoot = new GameObject("StaminaOrbs");
                _spawnedObjects.Add(orbRoot);
                _staminaOrbSpawner.Spawn(_mazeData, _cellSize, _goalCell, occupied, orbRoot.transform);
            }

            if (_spawnDecor && _decorSpawner != null)
            {
                var decorRoot = new GameObject("MazeDecor");
                _spawnedObjects.Add(decorRoot);
                _decorSpawner.Spawn(_mazeData, _cellSize, occupied, decorRoot.transform);
            }

            BuildMinimap();
            HookCamera();
            // RankService는 전체 플레이어 수를 알아야 포디움 종료 조건을 계산한다.
            // 서버 연동 시에도 스폰 확정 후 SetTotalPlayers를 호출해야 한다.
            SyncRankTotal(playerCount);

            float worldW = _width * _cellSize;
            float worldD = _height * _cellSize;
            Debug.Log(
                $"[MazeManager] 미로 그리드 {_width}×{_height} (벽·길 포함 한 칸 = 미니맵 1픽셀). " +
                $"CellSize={_cellSize}m → 월드 가로·세로 약 {worldW:0}m × {worldD:0}m. 난이도 프리셋={_difficulty}. " +
                $"13×13만 쓰려면 Difficulty를 Custom으로 두세요.",
                this);
        }

        // ───────── Data ─────────
        public int[,] GenerateMazeData(AlgorithmType type, int seed)
        {
            IMazeGenerator gen = type switch
            {
                AlgorithmType.Prim       => new PrimGenerator(),
                AlgorithmType.DFS        => new DFSGenerator(),
                AlgorithmType.Sidewinder => new SidewinderGenerator(),
                _                        => new PrimGenerator()
            };

            int[,] maze = gen.Generate(_width, _height, seed);
            ClearArea(maze, 1, 1, 3);                      // 플레이어 시작 공간
            ClearArea(maze, _width - 3, _height - 3, 2);   // 골 주변 공간
            maze[_width - 2, _height - 2] = 0;             // 골 셀 개방
            return maze;
        }

        private void ClearArea(int[,] maze, int ox, int oy, int range)
        {
            for (int x = ox; x < ox + range; x++)
                for (int y = oy; y < oy + range; y++)
                    if (x > 0 && y > 0 && x < _width - 1 && y < _height - 1)
                        maze[x, y] = 0;
        }

        // ───────── Helpers ─────────
        private HashSet<Vector2Int> BuildOccupiedSet()
        {
            var occupied = new HashSet<Vector2Int> { _goalCell };
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                    occupied.Add(new Vector2Int(1 + dx, 1 + dy));

            // 모든 플레이어 시작 셀
            foreach (var p in _playerInstances)
            {
                int x = Mathf.RoundToInt(p.transform.position.x / _cellSize);
                int y = Mathf.RoundToInt(p.transform.position.z / _cellSize);
                occupied.Add(new Vector2Int(x, y));
            }
            return occupied;
        }

        private IRankService ResolveRankService()
        {
            if (_rankService != null) return _rankService;
            return GameStateManager.Instance?.Rank;
        }

        private void SyncRankTotal(int playerCount)
        {
            if (_rankService != null) { _rankService.SetTotalPlayers(playerCount); return; }
            if (GameStateManager.Instance?.Rank is RankService rs) rs.SetTotalPlayers(playerCount);
        }

        private int ResolvePlayerCount()
        {
            return _spectatorTestMode ? Mathf.Max(2, _playerCount) : Mathf.Max(1, _playerCount);
        }

        private static void ResetRoundHelpers()
        {
            // 관전/첫 골인 보너스는 라운드 간 내부 상태를 가진다.
            // 새 라운드에서 Player_1 같은 이름이 재사용되므로 반드시 초기화해야 한다.
            var spectator = Object.FindFirstObjectByType<MazeFinisherSpectator>();
            if (spectator != null) spectator.ResetState();

            var firstBonus = Object.FindFirstObjectByType<MazeFirstFinisherBonus>();
            if (firstBonus != null) firstBonus.ResetState();
        }

        private void BuildMinimap()
        {
            if (!_buildMinimap || _primaryPlayer == null) return;
            var mm = GetComponent<MazeMinimap>() ?? gameObject.AddComponent<MazeMinimap>();
            mm.Bind(_mazeData, _cellSize, _primaryPlayer, _goalCell);

            if (_playerInstances.Count > 1)
            {
                var peers = new List<Transform>(_playerInstances.Count - 1);
                foreach (var go in _playerInstances)
                    if (go != null && go.transform != _primaryPlayer) peers.Add(go.transform);
                mm.SetPeerPlayers(peers);
            }
        }

        private void HookCamera()
        {
            if (_primaryPlayer == null) return;
            // 카메라는 로컬 클라이언트 전용 시각 요소다.
            // 서버 연동 시 NetworkObject로 동기화하지 말고 각 클라이언트가 자기 로컬 플레이어를 Follow하면 된다.
            if (_buildFollowCamera && _cameraService != null) _cameraService.Follow(_primaryPlayer);
            if (_buildAmbience     && _ambience      != null) _ambience.AttachHeroLight(_primaryPlayer);
            if (_buildFinishFX     && _finishFX      != null)
                _finishFX.SetLocalPlayerName(_primaryPlayer.name);
        }

        private void ClearSpawned()
        {
            foreach (var obj in _spawnedObjects)
                if (obj != null) Destroy(obj);
            _spawnedObjects.Clear();
            _playerInstances.Clear();
            _primaryPlayer = null;
        }

        public Vector3 CellToWorld(int x, int y) => new(x * _cellSize, 0f, y * _cellSize);
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;
    }
}
