using UnityEngine;

// 목표 지점 오브젝트를 생성하고 배치하는 스포너.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 프리팹과 골 관련 부가 컴포넌트를 생성하는 스포너 계약.
    /// </summary>
    public interface IMazeGoalSpawner
    {
        GameObject Spawn(Vector2Int cell, float cellSize, GameObject goalPrefab,
                        IRankService rankService, Color beaconColor, Transform parent);
    }

    /// <summary>
    /// 골 프리팹을 생성하고 GoalTrigger, 비콘 같은 필수 컴포넌트가 한 번씩만 붙도록 보장한다.
    /// </summary>
    public class MazeGoalSpawner : MonoBehaviour, IMazeGoalSpawner
    {
        public GameObject Spawn(Vector2Int cell, float cellSize, GameObject goalPrefab,
                               IRankService rankService, Color beaconColor, Transform parent)
        {
            if (goalPrefab == null)
            {
                Debug.LogWarning("[MazeGoalSpawner] goalPrefab is null.");
                return null;
            }

            Vector3 pos = new(cell.x * cellSize, 0f, cell.y * cellSize);
            var goal = Instantiate(goalPrefab, pos, Quaternion.identity, parent);

            var trigger = goal.GetComponent<GoalTrigger>() ?? goal.AddComponent<GoalTrigger>();
            trigger.Inject(rankService);

            var beacon = goal.GetComponent<GoalBeacon>() ?? goal.AddComponent<GoalBeacon>();
            beacon.Build(beaconColor);

            return goal;
        }
    }
}
