using UnityEngine;
using System.Collections.Generic;

namespace _TeamFolder.JCJ.Script
{
    public enum AlgorithmType { Prim, DFS, Sidewinder }

    public class MazeManager : MonoBehaviour
    {
        public static MazeManager Instance { get; private set; }

        [Header("Maze Settings")]
        [SerializeField] private int _width = 51; // 1. 1 과 2 두개의 값은 홀수로 하는거 추천 ( 왜냐하면 벽 | 경로 | 벽 형태라 2개면 벽|경로 )
        [SerializeField] private int _height = 51; // 2. ↑ (위에 보세요) ↑
        [SerializeField] private float _cellSize = 3.0f; // 벽 두께에 맞춘 셀 사이즈
        [SerializeField] private int _playerCount = 1;

        [Header("Assets")]
        [SerializeField] private GameObject _wallPrefab;//벽 프레팹
        [SerializeField] private GameObject _goalPrefab;//도착지점 프래팹
        [SerializeField] private GameObject _playerPrefab;//플레이어 프래팹
        
        [Header("Goal Audio")]
        [SerializeField] private GoalAudioSettings _goalAudioSettings;
        
        [Header("Services")]
        [SerializeField] private RankService _rankService; // Scene의 RankService 연결
        
        private int[,] _mazeData; // 맵 데이터 
        private List<GameObject> _spawnedObjects = new List<GameObject>(); // 소환된 벽이나 바닥들 모아둔 리스트
        
        [Header("Test")]
        [SerializeField] private AlgorithmType _algorithmType = AlgorithmType.Prim;
        void Awake() => Instance = this;

        public void GenerateMazeWithButton()//테스트하려고 만든 테스트용 버튼 ( 맵 생성)
        {
            CreateMaze(_algorithmType, _playerCount);
            GameStateManager.Instance.StartGame();
        }

        public void CreateMaze(AlgorithmType type, int playerCount)//매개변수는 미로생성 알고리즘 (Enum)이랑 플레이어 수 (int)
        {
            // 기존 오브젝트 정리 (메모리 누수 방지)
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedObjects.Clear();

            // 알고리즘 선택 및 데이터 생성
            _mazeData = GenerateMazeData(type);
            Vector2Int goalPos = new Vector2Int(_width - 2, _height - 2);

            // 물리적 배치 및 콜라이더 병합
            RenderMaze(goalPos);

            // 플레이어 소환
            SpawnPlayers(playerCount);
        }

        private void ClearAreaInternal(int[,] maze, int centerX, int centerY, int range)//시작점 주변 반경을 비워주는 메서드 매개변수는 (데이터 (2차원 배열) , 시작지점 X,Y좌표, 시작지점 크기 N (N*N즉 3이면 3*3만큼 비워줌))
        {
            for (int x = centerX; x < centerX + range; x++)
                for (int y = centerY; y < centerY + range; y++)
                    if (x < _width - 1 && y < _height - 1)//범위 내에 있으면
                        maze[x, y] = 0; // 벽이 없다고 처리해줌
        }

        private void SetGoalInternal(int[,] maze)//도착지점 생성 (도착지점은 언제나 오른쪽 아래 구석이니까 해당 구역을 땅으로 처리)
        {
            Vector2Int goal = new Vector2Int(_width - 2, _height - 2);//배열의 범위 -2(1칸은 벽)의 위치를 처리하기 위해서 Vector2Int 로 만듦
            maze[goal.x, goal.y] = 0;//위에서 만든 vector2int를 이용해서 x좌표,y좌표에 있는 벽을 땅으로 바꿈
        }

        private void RenderMaze(Vector2Int goalPos)//미로를 2차원 배열을 기반으로 만들기
        {
            // 미로 전체를 담을 컨테이너 생성
            GameObject container = new GameObject("MazeContainer");
            _spawnedObjects.Add(container);//소환된 오브젝트를 모아둔 리스트에 넣음

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 pos = new Vector3(x * _cellSize, 0, y * _cellSize);

                    // 벽 생성
                    if (_mazeData[x, y] == 1)
                    {
                        Instantiate(_wallPrefab, pos, Quaternion.identity, container.transform);
                    }
                    
                    // 도착점 생성
                    if (x == goalPos.x && y == goalPos.y)
                    {
                        GameObject goal = Instantiate(_goalPrefab, pos, Quaternion.identity, container.transform);
                        GoalTrigger gt = goal.AddComponent<GoalTrigger>();//테스트 기준으로 만든거지만 나중에 프래팹으로 만들면 거기에 컴포넌트 붙일거임
                        gt.Inject(_rankService);
                        IGoalAudioHint audioHint = goal.AddComponent<GoalAudioHint>();
                        if (_goalAudioSettings != null)
                        {
                            ((GoalAudioHint)audioHint).Inject(_goalAudioSettings);
                            audioHint.StartHint();
                        }
                        else Debug.LogWarning("[MazeManager] GoalAudioSettings가 없습니다.");

                        _spawnedObjects.Add(goal); // 별도로 정리 대상에 추가
                    }
                }
            }
            
            CombineMazeMesh(container);
        }

        private void CombineMazeMesh(GameObject container)// 모든 자식 벽들의 콜라이더와 메쉬를 하나로 합침(최적화 작업)
        {
            MeshFilter[] meshFilters = container.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            // 벽 프리팹의 머티리얼을 미리 가져옴
            Material mazeMaterial = _wallPrefab.GetComponent<MeshRenderer>().sharedMaterial;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                // 부모(container) 좌표계 기준으로 위치 계산
                combine[i].transform = container.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                
                // 개별 벽 오브젝트의 렌더러와 콜라이더를 꺼서 메모리 중복 방지
                meshFilters[i].gameObject.GetComponent<Renderer>().enabled = false;
                if (meshFilters[i].gameObject.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = false;
                }
            }

            // 새로운 통합 메쉬 생성
            Mesh finalMesh = new Mesh();
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 많은 수의 폴리곤 대응
            finalMesh.CombineMeshes(combine);

            // 컨테이너에 통합된 메쉬와 렌더러 추가
            MeshFilter mf = container.AddComponent<MeshFilter>();
            mf.mesh = finalMesh;

            MeshRenderer mr = container.AddComponent<MeshRenderer>();
            mr.material = mazeMaterial;

            // 단 하나의 통합 콜라이더 생성 (메모리 최적화의 핵심)
            container.AddComponent<MeshCollider>();
            
            // 물리 연산 최적화를 위해 정적 개체로 설정
            container.isStatic = true;
        }

        private void SpawnPlayers(int count)//플레이어 스폰
        {
            for (int i = 0; i < count; i++)
            {
                // 시작 광장(1,1) 근처에 플레이어들을 겹치지 않게 소환 (테스트용임 승영이가 알아서 플레이어 소환을 유지하고 서버에 연결하던지 아니면 새로 구현하던지는 맘대로 ㄱㄱ)
                Vector3 spawnPos = new Vector3(1.5f * _cellSize, 1f, 1.5f * _cellSize);
                spawnPos += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                GameObject p = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
                p.name = $"Player_{i + 1}";
                _spawnedObjects.Add(p);
            }
        }
        public int[,] GenerateMazeData(AlgorithmType type) //일단 서버에서 데이터 뽑아갈 수 있게 알고리즘을 매개변수로 받는 2차원 배열 데이터를 반환해주는 메서드로 만들어둠
        {
            IMazeGenerator gen = type switch
            {
                AlgorithmType.Prim => new PrimGenerator(),
                AlgorithmType.DFS => new DFSGenerator(),
                AlgorithmType.Sidewinder => new SidewinderGenerator(),
                _ => new PrimGenerator()
            };//매개변수로 들어온 enum을 기준으로 알고리즘 받기

            int[,] maze = gen.Generate(_width, _height);

            ClearAreaInternal(maze, 1, 1, 3);
            SetGoalInternal(maze);

            return maze;//데이터 반환
        }
    }
}